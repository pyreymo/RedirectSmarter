using System;
using Dalamud.Game.ClientState.Objects.Types;

namespace RedirectSmarter.Targeting
{
    internal sealed class DelegateTargetSelector(Func<IGameObject?> resolve) : IRedirectTargetSelector
    {
        public IGameObject? Resolve() => resolve();
    }
}
