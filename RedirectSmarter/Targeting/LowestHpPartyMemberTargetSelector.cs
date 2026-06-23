using Dalamud.Game.ClientState.Objects.Types;

namespace RedirectSmarter.Targeting
{
    internal sealed class LowestHpPartyMemberTargetSelector : IRedirectTargetSelector
    {
        public IGameObject? Resolve()
        {
            IGameObject? lowestHpMember = null;
            var lowestHpPercent = double.MaxValue;

            foreach (var partyMember in Services.PartyList)
            {
                var gameObject = partyMember.GameObject;
                var currentHp = partyMember.CurrentHP;
                var maxHp = partyMember.MaxHP;

                if (gameObject is null || gameObject.IsDead || currentHp == 0 || maxHp == 0 || currentHp >= maxHp)
                {
                    continue;
                }

                var hpPercent = (double)currentHp / maxHp;
                if (hpPercent >= lowestHpPercent)
                {
                    continue;
                }

                lowestHpPercent = hpPercent;
                lowestHpMember = gameObject;
            }

            if (lowestHpMember is null && Services.ObjectTable.LocalPlayer is { } localPlayer)
            {
                var currentHp = localPlayer.CurrentHp;
                var maxHp = localPlayer.MaxHp;

                if (!localPlayer.IsDead && currentHp > 0 && maxHp > 0 && currentHp < maxHp)
                {
                    return localPlayer;
                }
            }

            return lowestHpMember;
        }
    }
}
