using System;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using LuminaAction = Lumina.Excel.Sheets.Action;

namespace RedirectSmarter
{
    internal class GameHooks : IDisposable
    {
        private const uint BarOrigin = 0;
        private const uint QueueOrigin = 1;
        private const uint MacroOrigin = 2;

        private readonly Configuration configuration;
        private readonly ActionCatalog actionCatalog;
        private readonly TargetResolver targetResolver = new();
        private static IToastGui ToastGui => Services.ToastGui;

        private unsafe delegate bool TryActionDelegate(
            IntPtr actionManager,
            ActionType actionType,
            uint actionId,
            ulong targetId,
            uint param,
            uint origin,
            uint unk,
            Vector3* location
        );

        private readonly Hook<TryActionDelegate> useActionHook;

        public GameHooks(Configuration configuration, ActionCatalog actions)
        {
            this.configuration = configuration;
            actionCatalog = actions;

            unsafe
            {
                useActionHook = Services.InteropProvider.HookFromAddress<TryActionDelegate>(
                    (IntPtr)ActionManager.MemberFunctionPointers.UseAction,
                    UseActionCallback
                );
            }

            useActionHook.Enable();
        }

        private unsafe bool UseActionCallback(
            IntPtr actionManager,
            ActionType actionType,
            uint actionId,
            ulong targetId,
            uint param,
            uint origin,
            uint unk,
            Vector3* location
        )
        {
            if (actionType != ActionType.Action)
            {
                return ContinueOriginal(
                    actionManager,
                    actionType,
                    actionId,
                    targetId,
                    param,
                    origin,
                    unk,
                    location
                );
            }

            if (!actionCatalog.IsReady)
            {
                return ContinueOriginal(
                    actionManager,
                    actionType,
                    actionId,
                    targetId,
                    param,
                    origin,
                    unk,
                    location
                );
            }

            var requestedAction = actionCatalog.GetRow(actionId);
            if (requestedAction.IsPvP)
            {
                return ContinueOriginal(
                    actionManager,
                    actionType,
                    actionId,
                    targetId,
                    param,
                    origin,
                    unk,
                    location
                );
            }

            origin = NormalizeOrigin(origin);

            var adjustedActionId = ActionManager.MemberFunctionPointers.GetAdjustedActionId(
                (ActionManager*)actionManager,
                actionId
            );
            var adjustedAction = actionCatalog.GetRow(adjustedActionId);

            if (!ShouldRedirect(adjustedAction, origin))
            {
                return ContinueOriginal(
                    actionManager,
                    actionType,
                    actionId,
                    targetId,
                    param,
                    origin,
                    unk,
                    location
                );
            }

            var configurationId = GetConfigurationId(requestedAction, adjustedAction);
            if (
                configuration.Redirections.TryGetValue(configurationId, out var redirection)
                && TryUseConfiguredTarget(
                    actionManager,
                    actionType,
                    actionId,
                    targetId,
                    param,
                    origin,
                    unk,
                    location,
                    adjustedAction,
                    redirection,
                    out var result
                )
            )
            {
                return result;
            }

            return ContinueOriginal(
                actionManager,
                actionType,
                actionId,
                targetId,
                param,
                origin,
                unk,
                location
            );
        }

        private uint NormalizeOrigin(uint origin)
        {
            return origin == MacroOrigin && configuration.EnableMacroQueueing ? BarOrigin : origin;
        }

        private static bool ShouldRedirect(LuminaAction adjustedAction, uint origin)
        {
            return origin != QueueOrigin && adjustedAction.HasConfigurableTarget();
        }

        private static uint GetConfigurationId(
            LuminaAction requestedAction,
            LuminaAction adjustedAction
        )
        {
            return adjustedAction.IsPlayerAction ? adjustedAction.RowId : requestedAction.RowId;
        }

        private unsafe bool TryUseConfiguredTarget(
            IntPtr actionManager,
            ActionType actionType,
            uint actionId,
            ulong originalTargetId,
            uint param,
            uint origin,
            uint unk,
            Vector3* location,
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
                    result = ContinueOriginal(
                        actionManager,
                        actionType,
                        actionId,
                        resolvedTarget.GameObjectId,
                        param,
                        origin,
                        unk,
                        location
                    );
                    return true;
                }

                if (!configuration.IgnoreErrors)
                {
                    ShowTargetError(error);
                    result = false;
                    return true;
                }
            }

            result = ContinueOriginal(
                actionManager,
                actionType,
                actionId,
                originalTargetId,
                param,
                origin,
                unk,
                location
            );
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
                    TargetValidationError.NotInLineOfSight => "Target not in line of sight.",
                    TargetValidationError.NotInRange => "Target is not in range.",
                    _ => "Invalid target.",
                }
            );
        }

        private unsafe bool ContinueOriginal(
            IntPtr actionManager,
            ActionType actionType,
            uint actionId,
            ulong targetId,
            uint param,
            uint origin,
            uint unk,
            Vector3* location
        )
        {
            return useActionHook.Original(
                actionManager,
                actionType,
                actionId,
                targetId,
                param,
                origin,
                unk,
                location
            );
        }

        public void Dispose()
        {
            useActionHook.Dispose();
        }
    }
}
