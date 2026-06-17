using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Types;

namespace RedirectSmarter.Targeting
{
    internal class TargetResolver
    {
        private readonly Dictionary<string, IRedirectTargetSelector> selectors;

        public TargetResolver()
            : this(RedirectTargets.Definitions) { }

        private TargetResolver(IEnumerable<RedirectTargetDefinition> definitions)
        {
            selectors = definitions.ToDictionary(
                definition => definition.Id,
                definition => definition.Selector
            );
        }

        public IGameObject? Resolve(string target)
        {
            return selectors.TryGetValue(target, out var selector) ? selector.Resolve() : null;
        }
    }
}
