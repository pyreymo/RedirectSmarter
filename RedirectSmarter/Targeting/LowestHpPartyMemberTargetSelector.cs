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
                if (partyMember.MaxHP == 0 || partyMember.CurrentHP == 0)
                {
                    continue;
                }

                var gameObject = partyMember.GameObject;
                if (gameObject is null || gameObject.IsDead)
                {
                    continue;
                }

                if (partyMember.CurrentHP >= partyMember.MaxHP)
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
