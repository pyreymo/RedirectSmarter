using Dalamud.Game.ClientState.Objects.Types;

namespace RedirectSmarter.Targeting
{
    internal interface IRedirectTargetSelector
    {
        IGameObject? Resolve();
    }
}
