using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Types;
using RedirectSmarter.Targeting.Parameters;

namespace RedirectSmarter.Targeting.Selectors
{
    /// <summary>
    /// Selects the living party member with the lowest current HP percentage.
    /// </summary>
    internal sealed class LowestHpPartyMemberTargetSelector : IRedirectTargetSelector
    {
        private const string BelowParameterName = "below";
        private const string SelfParameterName = "self";
        private const string BelowParameterDisplayNameKey = "RedirectTarget.LowestHpPartyMember.Parameter.Below";
        private const string SelfParameterDisplayNameKey = "RedirectTarget.LowestHpPartyMember.Parameter.Self";
        private const int DefaultBelowPercent = 100;
        private const bool DefaultIncludeSelf = true;
        private const string LogPrefix = "[LowestHpSelector]";

        public static IReadOnlyList<TargetParameterDefinition> Parameters { get; } =
        [
            TargetParameter.Int(
                BelowParameterName,
                BelowParameterDisplayNameKey,
                defaultValue: DefaultBelowPercent,
                min: 1,
                max: 100,
                suffix: "%",
                allowPositional: true
            ),
            TargetParameter.Bool(SelfParameterName, SelfParameterDisplayNameKey, defaultValue: DefaultIncludeSelf),
        ];

        public IGameObject? Resolve(TargetSelectionContext context)
        {
            var belowHpRatio = context.GetInt(BelowParameterName, DefaultBelowPercent) / 100.0;
            var includeSelf = context.GetBool(SelfParameterName, DefaultIncludeSelf);
            var localPlayer = Services.ObjectTable.LocalPlayer;
            IGameObject? lowestHpMember = null;
            var lowestHpPercent = double.MaxValue;
            var inspectedPartyMembers = 0;
            var eligiblePartyMembers = 0;

            foreach (var partyMember in Services.PartyList)
            {
                inspectedPartyMembers++;

                var gameObject = partyMember.GameObject;
                if (!includeSelf && IsSameObject(gameObject, localPlayer))
                {
                    continue;
                }

                if (!TryGetDamagedAliveTarget(gameObject, partyMember.CurrentHP, partyMember.MaxHP, belowHpRatio, out var hpPercent, out _))
                {
                    continue;
                }

                eligiblePartyMembers++;

                if (hpPercent >= lowestHpPercent)
                    continue;

                lowestHpPercent = hpPercent;
                lowestHpMember = gameObject;
            }

            if (lowestHpMember is not null)
            {
                Services.PluginLog.Debug(
                    "{Prefix} selected party member: {Object}, hpPercent={HpPercent:P2}, below={Below:P2}, self={Self}, inspected={Inspected}, eligible={Eligible}",
                    LogPrefix,
                    DescribeObject(lowestHpMember),
                    lowestHpPercent,
                    belowHpRatio,
                    includeSelf,
                    inspectedPartyMembers,
                    eligiblePartyMembers
                );

                return lowestHpMember;
            }

            if (!includeSelf)
            {
                Services.PluginLog.Debug(
                    "{Prefix} no eligible party member found: inspected={Inspected}, eligible={Eligible}, below={Below:P2}; self disabled",
                    LogPrefix,
                    inspectedPartyMembers,
                    eligiblePartyMembers,
                    belowHpRatio
                );

                return null;
            }

            Services.PluginLog.Debug(
                "{Prefix} no eligible party member found: inspected={Inspected}, eligible={Eligible}, below={Below:P2}; trying local player fallback",
                LogPrefix,
                inspectedPartyMembers,
                eligiblePartyMembers,
                belowHpRatio
            );

            if (
                !TryGetDamagedAliveTarget(
                    localPlayer,
                    localPlayer?.CurrentHp ?? 0,
                    localPlayer?.MaxHp ?? 0,
                    belowHpRatio,
                    out var localHpPercent,
                    out var fallbackFailReason
                )
            )
            {
                Services.PluginLog.Debug(
                    "{Prefix} local player fallback failed: {Reason}; object={Object}",
                    LogPrefix,
                    fallbackFailReason,
                    DescribeObject(localPlayer)
                );

                return null;
            }

            Services.PluginLog.Debug(
                "{Prefix} selected local player: {Object}, hpPercent={HpPercent:P2}, below={Below:P2}",
                LogPrefix,
                DescribeObject(localPlayer),
                localHpPercent,
                belowHpRatio
            );

            return localPlayer;
        }

        private static bool TryGetDamagedAliveTarget(
            IGameObject? gameObject,
            ulong currentHp,
            ulong maxHp,
            double belowHpRatio,
            out double hpPercent,
            out string failReason
        )
        {
            hpPercent = 0;
            failReason = string.Empty;

            if (gameObject is null)
            {
                failReason = "object is null";
                return false;
            }

            if (gameObject.IsDead)
            {
                failReason = "object is dead";
                return false;
            }

            if (currentHp == 0)
            {
                failReason = "currentHp is 0";
                return false;
            }

            if (maxHp == 0)
            {
                failReason = "maxHp is 0";
                return false;
            }

            if (currentHp >= maxHp)
            {
                failReason = $"full HP, hp={currentHp}/{maxHp}";
                return false;
            }

            hpPercent = (double)currentHp / maxHp;
            if (hpPercent >= belowHpRatio)
            {
                failReason = $"hpPercent {hpPercent:P2} is not below {belowHpRatio:P2}";
                return false;
            }

            return true;
        }

        private static bool IsSameObject(IGameObject? first, IGameObject? second)
        {
            if (first is null || second is null)
                return false;

            return first.GameObjectId == second.GameObjectId || first.Address == second.Address;
        }

        private static string DescribeObject(IGameObject? gameObject)
        {
            if (gameObject is null)
                return "null";

            return $"{gameObject.Name} / address=0x{gameObject.Address:X} / kind={gameObject.ObjectKind} / baseId={gameObject.BaseId}";
        }
    }
}
