using Dalamud.Game.ClientState.Objects.Types;

namespace RedirectSmarter.Targeting
{
    /// <summary>
    /// Resolves one configured redirect target option to the current game object it represents.
    /// </summary>
    internal interface IRedirectTargetSelector
    {
        IGameObject? Resolve(TargetSelectionContext context);
    }
}
