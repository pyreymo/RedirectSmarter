using System;
using Dalamud.Game.ClientState.Objects.Types;
using RedirectSmarter.Targeting.Parameters;

namespace RedirectSmarter.Targeting.Selectors
{
    /// <summary>
    /// Adapts a simple target lookup delegate into the shared redirect target selector contract.
    /// </summary>
    internal sealed class DelegateTargetSelector(Func<IGameObject?> resolve) : IRedirectTargetSelector
    {
        public IGameObject? Resolve(TargetSelectionContext context) => resolve();
    }
}
