using Dalamud.Game.ClientState.Objects.Types;

namespace RedirectSmarter.Targeting
{
    internal sealed class TargetResolver(RedirectTargetCatalog catalog)
    {
        public IGameObject? Resolve(string target)
        {
            if (!catalog.TryGetSelector(target, out var selector))
            {
                return null;
            }

            return selector.Resolve();
        }

        public IGameObject? ResolveMacroPlaceholder(string placeholder)
        {
            if (!catalog.TryGetMacroPlaceholderSelector(placeholder, out var selector))
            {
                return null;
            }

            return selector.Resolve();
        }
    }
}
