using System;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using RedirectSmarter.Actions;
using RedirectSmarter.Configuration;
using RedirectSmarter.Localization;
using RedirectSmarter.Targeting;
using LuminaAction = Lumina.Excel.Sheets.Action;
using UseActionMode = FFXIVClientStructs.FFXIV.Client.Game.ActionManager.UseActionMode;

namespace RedirectSmarter.Hooks
{
    internal class GameHooks : IDisposable
    {
        private readonly PluginConfiguration configuration;
        private readonly ActionCatalog actionCatalog;
        private readonly TargetResolver targetResolver = new();
        private static IToastGui ToastGui => Services.ToastGui;

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
                new(
                    ActionManager,
                    ActionType,
                    ActionId,
                    TargetId,
                    ExtraParam,
                    mode,
                    ComboRouteId,
                    OutOptAreaTargeted
                );
        }

        public GameHooks(PluginConfiguration configuration, ActionCatalog actions)
        {
            this.configuration = configuration;
            actionCatalog = actions;

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

            var adjustedActionId = ActionManager.MemberFunctionPointers.GetAdjustedActionId(
                (ActionManager*)actionManager,
                actionId
            );
            var adjustedAction = actionCatalog.GetRow(adjustedActionId);

            if (!ShouldRedirect(adjustedAction, context.Mode))
            {
                return ContinueOriginal(context);
            }

            var configurationId = GetConfigurationId(requestedAction, adjustedAction);
            if (
                configuration.Redirections.TryGetValue(configurationId, out var redirection)
                && TryUseConfiguredTarget(context, adjustedAction, redirection, out var result)
            )
            {
                return result;
            }

            return ContinueOriginal(context);
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

        private static bool ShouldRedirect(LuminaAction adjustedAction, UseActionMode mode)
        {
            return mode != UseActionMode.Queue && adjustedAction.HasConfigurableTarget();
        }

        private static uint GetConfigurationId(
            LuminaAction requestedAction,
            LuminaAction adjustedAction
        )
        {
            return adjustedAction.IsPlayerAction ? adjustedAction.RowId : requestedAction.RowId;
        }

        private bool TryUseConfiguredTarget(
            UseActionContext context,
            LuminaAction adjustedAction,
            Redirection redirection,
            out bool result
        )
        {
            foreach (var targetName in redirection.Priority)
            {
                var resolvedTarget = targetResolver.Resolve(targetName);
                if (resolvedTarget is null)
                {
                    continue;
                }

                if (IsUsableTarget(adjustedAction, resolvedTarget, out var error))
                {
                    result = ContinueOriginal(context, resolvedTarget.GameObjectId);
                    return true;
                }

                if (!configuration.IgnoreErrors)
                {
                    ShowTargetError(error);
                    result = false;
                    return true;
                }
            }

            if (redirection.PreventDefault)
            {
                if (!configuration.IgnoreErrors)
                {
                    ToastGui.ShowError(Loc.Text("Error.NoRedirectTarget"));
                }

                result = false;
                return true;
            }

            result = ContinueOriginal(context);
            return true;
        }

        private static bool IsUsableTarget(
            LuminaAction action,
            IGameObject target,
            out TargetValidationError error
        )
        {
            var rangeOk = action.TargetInRangeAndLOS(target, out var rangeError);
            var typeOk = action.TargetTypeValid(target);

            error =
                rangeOk && !typeOk
                    ? TargetValidationError.InvalidTarget
                    : TargetValidationErrors.FromActionStatus(rangeError);

            return rangeOk && typeOk;
        }

        private static void ShowTargetError(TargetValidationError error)
        {
            ToastGui.ShowError(
                error switch
                {
                    TargetValidationError.NotInLineOfSight => Loc.Text(
                        "Error.TargetNotInLineOfSight"
                    ),
                    TargetValidationError.NotInRange => Loc.Text("Error.TargetNotInRange"),
                    _ => Loc.Text("Error.InvalidTarget"),
                }
            );
        }

        private bool ContinueOriginal(UseActionContext context) =>
            ContinueOriginal(context, context.TargetId);

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
