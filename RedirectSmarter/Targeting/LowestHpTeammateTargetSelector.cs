using Dalamud.Game.ClientState.Objects.Types;

namespace RedirectSmarter.Targeting
{
    internal sealed class LowestHpTeammateTargetSelector : IRedirectTargetSelector
    {
        public IGameObject? Resolve()
        {
            var localPlayer = Services.ObjectTable.LocalPlayer;
            IGameObject? lowestHpMember = null;
            var lowestHpPercent = double.MaxValue;

            foreach (var partyMember in Services.PartyList)
            {
                if (partyMember.MaxHP == 0 || partyMember.CurrentHP == 0)
                {
                    continue;
                }

                var gameObject = partyMember.GameObject;
                if (gameObject is null || gameObject.IsDead)
                {
                    continue;
                }

                if (localPlayer is not null && gameObject.GameObjectId == localPlayer.GameObjectId)
                {
                    continue;
                }

                var hpPercent = (double)partyMember.CurrentHP / partyMember.MaxHP;
                if (hpPercent >= lowestHpPercent)
                {
                    continue;
                }

                lowestHpPercent = hpPercent;
                lowestHpMember = gameObject;
            }

            return lowestHpMember;
        }
    }
}
