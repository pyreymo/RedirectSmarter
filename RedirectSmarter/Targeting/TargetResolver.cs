using Dalamud.Game.ClientState.Objects.Types;

namespace RedirectSmarter.Targeting
{
    /// <summary>
    /// Maps persisted target ids and custom macro placeholders to their current resolved game objects.
    /// </summary>
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
