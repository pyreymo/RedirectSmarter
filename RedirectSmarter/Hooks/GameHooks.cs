using System;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;
using RedirectSmarter.Actions;
using RedirectSmarter.Configuration;
using RedirectSmarter.Redirecting;
using UseActionMode = FFXIVClientStructs.FFXIV.Client.Game.ActionManager.UseActionMode;

namespace RedirectSmarter.Hooks
{
    internal class GameHooks : IDisposable
    {
        private readonly PluginConfiguration configuration;
        private readonly ActionCatalog actionCatalog;
        private readonly ActionRedirector actionRedirector;

        private unsafe delegate bool UseActionDelegate(
            IntPtr actionManager,
            ActionType actionType,
            uint actionId,
            ulong targetId,
            uint extraParam,
            UseActionMode mode,
            uint comboRouteId,
            bool* outOptAreaTargeted
        );

        private readonly Hook<UseActionDelegate> useActionHook;

        private readonly unsafe ref struct UseActionContext(
            IntPtr actionManager,
            ActionType actionType,
            uint actionId,
            ulong targetId,
            uint extraParam,
            UseActionMode mode,
            uint comboRouteId,
            bool* outOptAreaTargeted
        )
        {
            public IntPtr ActionManager { get; } = actionManager;
            public ActionType ActionType { get; } = actionType;
            public uint ActionId { get; } = actionId;
            public ulong TargetId { get; } = targetId;
            public uint ExtraParam { get; } = extraParam;
            public UseActionMode Mode { get; } = mode;
            public uint ComboRouteId { get; } = comboRouteId;
            public bool* OutOptAreaTargeted { get; } = outOptAreaTargeted;

            public UseActionContext WithMode(UseActionMode mode) =>
                new(ActionManager, ActionType, ActionId, TargetId, ExtraParam, mode, ComboRouteId, OutOptAreaTargeted);
        }

        public GameHooks(PluginConfiguration configuration, ActionCatalog actions, ActionRedirector actionRedirector)
        {
            this.configuration = configuration;
            actionCatalog = actions;
            this.actionRedirector = actionRedirector;

            unsafe
            {
                useActionHook = CreateUseActionHook();
            }

            useActionHook.Enable();
        }

        private unsafe bool UseActionCallback(
            IntPtr actionManager,
            ActionType actionType,
            uint actionId,
            ulong targetId,
            uint extraParam,
            UseActionMode mode,
            uint comboRouteId,
            bool* outOptAreaTargeted
        )
        {
            var context = new UseActionContext(
                actionManager,
                actionType,
                actionId,
                targetId,
                extraParam,
                mode,
                comboRouteId,
                outOptAreaTargeted
            );

            if (!configuration.EnableRedirects)
            {
                return ContinueOriginal(context);
            }

            if (actionType != ActionType.Action)
            {
                return ContinueOriginal(context);
            }

            if (!actionCatalog.IsReady)
            {
                return ContinueOriginal(context);
            }

            var requestedAction = actionCatalog.GetRow(actionId);
            if (requestedAction.IsPvP)
            {
                return ContinueOriginal(context);
            }

            context = context.WithMode(GetModeWithMacroQueueing(mode));

            var adjustedActionId = ActionManager.MemberFunctionPointers.GetAdjustedActionId((ActionManager*)actionManager, actionId);
            var adjustedAction = actionCatalog.GetRow(adjustedActionId);

            var redirectResult = actionRedirector.Resolve(requestedAction, adjustedAction, context.Mode);
            if (redirectResult.Kind == RedirectResultKind.UseTarget)
            {
                return ContinueOriginal(context, redirectResult.TargetId);
            }

            return redirectResult.Kind != RedirectResultKind.Block && ContinueOriginal(context);
        }

        private unsafe Hook<UseActionDelegate> CreateUseActionHook()
        {
            return Services.InteropProvider.HookFromAddress<UseActionDelegate>(
                (IntPtr)ActionManager.MemberFunctionPointers.UseAction,
                UseActionCallback
            );
        }

        private UseActionMode GetModeWithMacroQueueing(UseActionMode mode)
        {
            if (!configuration.EnableMacroQueueing)
            {
                return mode;
            }

            if (mode == UseActionMode.Macro)
            {
                return UseActionMode.None;
            }

            return mode;
        }

        private bool ContinueOriginal(UseActionContext context) => ContinueOriginal(context, context.TargetId);

        private unsafe bool ContinueOriginal(UseActionContext context, ulong targetId)
        {
            return useActionHook.Original(
                context.ActionManager,
                context.ActionType,
                context.ActionId,
                targetId,
                context.ExtraParam,
                context.Mode,
                context.ComboRouteId,
                context.OutOptAreaTargeted
            );
        }

        public void Dispose()
        {
            useActionHook.Dispose();
        }
    }
}
