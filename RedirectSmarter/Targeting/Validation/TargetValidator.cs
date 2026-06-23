using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Config;
using FFXIVClientStructs.FFXIV.Client.Game;
using ClientGameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
using LuminaAction = Lumina.Excel.Sheets.Action;

namespace RedirectSmarter.Targeting.Validation
{
    /// <summary>
    /// Validates whether an action can be redirected to a resolved target using the client's range, line-of-sight, and target-kind rules.
    /// </summary>
    internal static class TargetValidator
    {
        private const uint ActionStatusSuccess = 0;
        private const uint ActionStatusRangeError = 562;
        private const uint ActionStatusNotFacing = 565;
        private const uint ActionStatusLineOfSightError = 566;

        public static TargetValidationResult Validate(LuminaAction action, IGameObject target)
        {
            if (!CanReachTarget(action, target, out var reachError))
            {
                return TargetValidationResult.Invalid(reachError);
            }

            if (!CanTargetObjectKind(action, target))
            {
                return TargetValidationResult.Invalid(TargetValidationError.InvalidTarget);
            }

            return TargetValidationResult.Valid;
        }

        private static bool CanReachTarget(LuminaAction action, IGameObject target, out TargetValidationError error)
        {
            error = TargetValidationError.InvalidTarget;

            if (Services.ObjectTable.LocalPlayer is not { } player)
            {
                return false;
            }

            var actionStatus = GetActionRangeOrLineOfSightStatus(action, player, target);
            if (actionStatus == ActionStatusSuccess)
            {
                return true;
            }

            if (actionStatus == ActionStatusNotFacing)
            {
                return CanIgnoreNotFacing(action, target);
            }

            error = ErrorFromActionStatus(actionStatus);
            return false;
        }

        private static unsafe uint GetActionRangeOrLineOfSightStatus(LuminaAction action, IGameObject player, IGameObject target)
        {
            var playerPtr = (ClientGameObject*)player.Address;
            var targetPtr = (ClientGameObject*)target.Address;

            return ActionManager.MemberFunctionPointers.GetActionInRangeOrLoS(action.RowId, playerPtr, targetPtr);
        }

        private static bool CanIgnoreNotFacing(LuminaAction action, IGameObject target)
        {
            var autoFaceEnabled = AutoFaceTargetOnActionEnabled();
            Services.PluginLog.Information(
                "Target validation returned NotFacing for action {ActionId} target {TargetId}; AutoFaceTargetOnAction={AutoFaceTargetOnAction}",
                action.RowId,
                target.GameObjectId,
                autoFaceEnabled
            );

            return autoFaceEnabled;
        }

        private static bool CanTargetObjectKind(LuminaAction action, IGameObject target)
        {
            return target switch
            {
                IBattleNpc { BattleNpcKind: BattleNpcSubKind.Combatant } => action.CanTargetHostile,
                IBattleNpc => CanTargetFriendly(action),
                { ObjectKind: ObjectKind.EventNpc or ObjectKind.Pc or ObjectKind.Companion } => CanTargetFriendly(action),
                _ => false,
            };
        }

        private static bool AutoFaceTargetOnActionEnabled()
        {
            return Services.GameConfig.TryGet(UiControlOption.AutoFaceTargetOnAction, out uint enabled) && enabled != 0;
        }

        private static bool CanTargetFriendly(LuminaAction action) => action.CanTargetAlly || action.CanTargetParty;

        private static TargetValidationError ErrorFromActionStatus(uint status)
        {
            return status switch
            {
                ActionStatusLineOfSightError => TargetValidationError.NotInLineOfSight,
                ActionStatusRangeError => TargetValidationError.NotInRange,
                _ => TargetValidationError.InvalidTarget,
            };
        }
    }
}
