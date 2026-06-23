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
            Services.PluginLog.Debug("UseAction hook enabled.");
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

            Services.PluginLog.Debug(
                "UseAction intercepted: actionType={ActionType}, actionId={ActionId}, targetId={TargetId}, mode={Mode}, extraParam={ExtraParam}, comboRouteId={ComboRouteId}",
                actionType,
                actionId,
                targetId,
                mode,
                extraParam,
                comboRouteId
            );

            if (!configuration.EnableRedirects)
            {
                Services.PluginLog.Debug(
                    "UseAction bypassed: redirects disabled, actionId={ActionId}",
                    actionId
                );
                return ContinueOriginal(context);
            }

            if (actionType != ActionType.Action)
            {
                Services.PluginLog.Debug(
                    "UseAction bypassed: unsupported action type, actionType={ActionType}, actionId={ActionId}",
                    actionType,
                    actionId
                );
                return ContinueOriginal(context);
            }

            if (!actionCatalog.IsReady)
            {
                Services.PluginLog.Debug(
                    "UseAction bypassed: action catalog not ready, actionId={ActionId}",
                    actionId
                );
                return ContinueOriginal(context);
            }

            var requestedAction = actionCatalog.GetRow(actionId);
            if (requestedAction.IsPvP)
            {
                Services.PluginLog.Debug(
                    "UseAction bypassed: PvP action, actionId={ActionId}, actionName={ActionName}",
                    actionId,
                    requestedAction.Name.ToString()
                );
                return ContinueOriginal(context);
            }

            context = context.WithMode(GetModeWithMacroQueueing(mode));
            if (context.Mode != mode)
            {
                Services.PluginLog.Debug(
                    "UseAction mode adjusted: actionId={ActionId}, originalMode={OriginalMode}, adjustedMode={AdjustedMode}",
                    actionId,
                    mode,
                    context.Mode
                );
            }

            var adjustedActionId = ActionManager.MemberFunctionPointers.GetAdjustedActionId(
                (ActionManager*)actionManager,
                actionId
            );
            var adjustedAction = actionCatalog.GetRow(adjustedActionId);
            Services.PluginLog.Debug(
                "UseAction action resolved: requestedId={RequestedId}, requestedName={RequestedName}, adjustedId={AdjustedId}, adjustedName={AdjustedName}",
                actionId,
                requestedAction.Name.ToString(),
                adjustedActionId,
                adjustedAction.Name.ToString()
            );

            if (!ShouldRedirect(adjustedAction, context.Mode))
            {
                Services.PluginLog.Debug(
                    "UseAction bypassed: ShouldRedirect false, adjustedId={AdjustedId}, mode={Mode}, canTargetAlly={CanTargetAlly}, canTargetHostile={CanTargetHostile}, canTargetParty={CanTargetParty}",
                    adjustedActionId,
                    context.Mode,
                    adjustedAction.CanTargetAlly,
                    adjustedAction.CanTargetHostile,
                    adjustedAction.CanTargetParty
                );
                return ContinueOriginal(context);
            }

            var configurationId = GetConfigurationId(requestedAction, adjustedAction);
            if (!configuration.Redirections.TryGetValue(configurationId, out var redirection))
            {
                Services.PluginLog.Debug(
                    "UseAction bypassed: no configured redirection, requestedId={RequestedId}, adjustedId={AdjustedId}, configurationId={ConfigurationId}",
                    actionId,
                    adjustedActionId,
                    configurationId
                );
                return ContinueOriginal(context);
            }

            Services.PluginLog.Debug(
                "UseAction trying configured redirection: requestedId={RequestedId}, adjustedId={AdjustedId}, configurationId={ConfigurationId}, priorityCount={PriorityCount}, preventDefault={PreventDefault}",
                actionId,
                adjustedActionId,
                configurationId,
                redirection.Priority.Count,
                redirection.PreventDefault
            );

            if (TryUseConfiguredTarget(context, adjustedAction, redirection, out var result))
            {
                return result;
            }

            Services.PluginLog.Debug(
                "UseAction fell through configured redirection unexpectedly, configurationId={ConfigurationId}",
                configurationId
            );
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
                Services.PluginLog.Debug(
                    "Trying redirect target: actionId={ActionId}, actionName={ActionName}, target={Target}",
                    adjustedAction.RowId,
                    adjustedAction.Name.ToString(),
                    targetName
                );
                var resolvedTarget = targetResolver.Resolve(targetName);
                if (resolvedTarget is null)
                {
                    Services.PluginLog.Debug(
                        "Redirect target unresolved: actionId={ActionId}, target={Target}",
                        adjustedAction.RowId,
                        targetName
                    );
                    continue;
                }

                if (IsUsableTarget(adjustedAction, resolvedTarget, out var error))
                {
                    Services.PluginLog.Debug(
                        "Redirect target accepted: actionId={ActionId}, target={Target}, resultName={ResultName}, gameObjectId={GameObjectId}",
                        adjustedAction.RowId,
                        targetName,
                        resolvedTarget.Name.ToString(),
                        resolvedTarget.GameObjectId
                    );
                    result = ContinueOriginal(context, resolvedTarget.GameObjectId);
                    return true;
                }

                Services.PluginLog.Debug(
                    "Redirect target rejected: actionId={ActionId}, target={Target}, resultName={ResultName}, gameObjectId={GameObjectId}, error={Error}, ignoreErrors={IgnoreErrors}",
                    adjustedAction.RowId,
                    targetName,
                    resolvedTarget.Name.ToString(),
                    resolvedTarget.GameObjectId,
                    error,
                    configuration.IgnoreErrors
                );

                if (!configuration.IgnoreErrors)
                {
                    ShowTargetError(error);
                    result = false;
                    return true;
                }
            }

            if (redirection.PreventDefault)
            {
                Services.PluginLog.Debug(
                    "Redirect prevented default: actionId={ActionId}, priorityCount={PriorityCount}",
                    adjustedAction.RowId,
                    redirection.Priority.Count
                );
                if (!configuration.IgnoreErrors)
                {
                    ToastGui.ShowError(Loc.Text("Error.NoRedirectTarget"));
                }

                result = false;
                return true;
            }

            Services.PluginLog.Debug(
                "Redirect falling back to original target: actionId={ActionId}",
                adjustedAction.RowId
            );
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

            Services.PluginLog.Debug(
                "Redirect target validation: actionId={ActionId}, targetName={TargetName}, gameObjectId={GameObjectId}, rangeOk={RangeOk}, rangeError={RangeError}, typeOk={TypeOk}, error={Error}",
                action.RowId,
                target.Name.ToString(),
                target.GameObjectId,
                rangeOk,
                rangeError,
                typeOk,
                error
            );

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
