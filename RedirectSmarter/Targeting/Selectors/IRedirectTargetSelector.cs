using Dalamud.Game.ClientState.Objects.Types;
using RedirectSmarter.Targeting.Parameters;

namespace RedirectSmarter.Targeting.Selectors
{
    /// <summary>
    /// Resolves one configured redirect target option to the current game object it represents.
    /// </summary>
    internal interface IRedirectTargetSelector
    {
        IGameObject? Resolve(TargetSelectionContext context);
    }
}
