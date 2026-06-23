using Dalamud.Game.ClientState.Objects.Types;

namespace RedirectSmarter.Targeting
{
    internal sealed class LowestHpPartyMemberTargetSelector : IRedirectTargetSelector
    {
        public IGameObject? Resolve()
        {
            IGameObject? lowestHpMember = null;
            var lowestHpPercent = double.MaxValue;
            var inspectedMembers = 0;

            foreach (var partyMember in Services.PartyList)
            {
                inspectedMembers++;
                var gameObject = partyMember.GameObject;
                var currentHp = partyMember.CurrentHP;
                var maxHp = partyMember.MaxHP;

                if (
                    gameObject is null
                    || gameObject.IsDead
                    || currentHp == 0
                    || maxHp == 0
                    || currentHp >= maxHp
                )
                {
                    Services.PluginLog.Debug(
                        "Lowest HP selector skipped: name={Name}, currentHp={CurrentHp}, maxHp={MaxHp}, object={Object}, dead={Dead}",
                        partyMember.Name.ToString(),
                        currentHp,
                        maxHp,
                        gameObject?.Name.ToString() ?? "null",
                        gameObject?.IsDead.ToString() ?? "unknown"
                    );
                    continue;
                }

                var hpPercent = (double)currentHp / maxHp;
                if (hpPercent >= lowestHpPercent)
                {
                    Services.PluginLog.Debug(
                        "Lowest HP selector kept current: name={Name}, currentHp={CurrentHp}, maxHp={MaxHp}, hpPercent={HpPercent}, lowestHpPercent={LowestHpPercent}",
                        partyMember.Name.ToString(),
                        currentHp,
                        maxHp,
                        hpPercent,
                        lowestHpPercent
                    );
                    continue;
                }

                lowestHpPercent = hpPercent;
                lowestHpMember = gameObject;
                Services.PluginLog.Debug(
                    "Lowest HP selector selected: name={Name}, currentHp={CurrentHp}, maxHp={MaxHp}, hpPercent={HpPercent}",
                    partyMember.Name.ToString(),
                    currentHp,
                    maxHp,
                    hpPercent
                );
            }

            if (lowestHpMember is null && Services.ObjectTable.LocalPlayer is { } localPlayer)
            {
                var currentHp = localPlayer.CurrentHp;
                var maxHp = localPlayer.MaxHp;

                if (!localPlayer.IsDead && currentHp > 0 && maxHp > 0 && currentHp < maxHp)
                {
                    Services.PluginLog.Debug(
                        "Lowest HP selector using local player fallback: inspectedMembers={InspectedMembers}, name={Name}, gameObjectId={GameObjectId}, currentHp={CurrentHp}, maxHp={MaxHp}",
                        inspectedMembers,
                        localPlayer.Name.ToString(),
                        localPlayer.GameObjectId,
                        currentHp,
                        maxHp
                    );
                    return localPlayer;
                }

                Services.PluginLog.Debug(
                    "Lowest HP selector skipped local player fallback: inspectedMembers={InspectedMembers}, name={Name}, dead={Dead}, currentHp={CurrentHp}, maxHp={MaxHp}",
                    inspectedMembers,
                    localPlayer.Name.ToString(),
                    localPlayer.IsDead,
                    currentHp,
                    maxHp
                );
            }

            Services.PluginLog.Debug(
                "Lowest HP selector result: inspectedMembers={InspectedMembers}, result={Result}, gameObjectId={GameObjectId}, hpPercent={HpPercent}",
                inspectedMembers,
                lowestHpMember?.Name.ToString() ?? "null",
                lowestHpMember?.GameObjectId ?? 0,
                lowestHpPercent == double.MaxValue ? -1 : lowestHpPercent
            );
            return lowestHpMember;
        }
    }
}
