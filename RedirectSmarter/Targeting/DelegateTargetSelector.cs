using System;
using Dalamud.Game.ClientState.Objects.Types;

namespace RedirectSmarter.Targeting
{
    /// <summary>
    /// Adapts a simple target lookup delegate into the shared redirect target selector contract.
    /// </summary>
    internal sealed class DelegateTargetSelector(Func<IGameObject?> resolve) : IRedirectTargetSelector
    {
        public IGameObject? Resolve() => resolve();
    }
}
