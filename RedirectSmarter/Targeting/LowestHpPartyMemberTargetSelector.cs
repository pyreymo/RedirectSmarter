using Dalamud.Game.ClientState.Objects.Types;

namespace RedirectSmarter.Targeting
{
    /// <summary>
    /// Selects the living damaged party member with the lowest current HP percentage,
    /// falling back to a damaged local player.
    /// </summary>
    internal sealed class LowestHpPartyMemberTargetSelector : IRedirectTargetSelector
    {
        private const string LogPrefix = "[LowestHpSelector]";

        public IGameObject? Resolve()
        {
            IGameObject? lowestHpMember = null;
            var lowestHpPercent = double.MaxValue;
            var inspectedPartyMembers = 0;
            var validDamagedPartyMembers = 0;

            foreach (var partyMember in Services.PartyList)
            {
                inspectedPartyMembers++;

                var gameObject = partyMember.GameObject;
                if (!TryGetDamagedAliveTarget(gameObject, partyMember.CurrentHP, partyMember.MaxHP, out var hpPercent, out _))
                {
                    continue;
                }

                validDamagedPartyMembers++;

                if (hpPercent >= lowestHpPercent)
                    continue;

                lowestHpPercent = hpPercent;
                lowestHpMember = gameObject;
            }

            if (lowestHpMember is not null)
            {
                Services.PluginLog.Debug(
                    "{Prefix} selected party member: {Object}, hpPercent={HpPercent:P2}, inspected={Inspected}, damaged={Damaged}",
                    LogPrefix,
                    DescribeObject(lowestHpMember),
                    lowestHpPercent,
                    inspectedPartyMembers,
                    validDamagedPartyMembers
                );

                return lowestHpMember;
            }

            Services.PluginLog.Debug(
                "{Prefix} no damaged party member found: inspected={Inspected}, damaged={Damaged}; trying local player fallback",
                LogPrefix,
                inspectedPartyMembers,
                validDamagedPartyMembers
            );

            var localPlayer = Services.ObjectTable.LocalPlayer;
            if (
                !TryGetDamagedAliveTarget(
                    localPlayer,
                    localPlayer?.CurrentHp ?? 0,
                    localPlayer?.MaxHp ?? 0,
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
                "{Prefix} selected local player: {Object}, hpPercent={HpPercent:P2}",
                LogPrefix,
                DescribeObject(localPlayer),
                localHpPercent
            );

            return localPlayer;
        }

        private static bool TryGetDamagedAliveTarget(
            IGameObject? gameObject,
            ulong currentHp,
            ulong maxHp,
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
            return true;
        }

        private static string DescribeObject(IGameObject? gameObject)
        {
            if (gameObject is null)
                return "null";

            return $"{gameObject.Name} / address=0x{gameObject.Address:X} / kind={gameObject.ObjectKind} / baseId={gameObject.BaseId}";
        }
    }
}
