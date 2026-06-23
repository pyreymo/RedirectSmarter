using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Types;
using RedirectSmarter.Targeting.MacroPlaceholders;
using RedirectSmarter.Targeting.Parameters;

namespace RedirectSmarter.Targeting
{
    /// <summary>
    /// Maps persisted target ids and custom macro placeholders to their current resolved game objects.
    /// </summary>
    internal sealed class TargetResolver(RedirectTargetCatalog catalog)
    {
        public IGameObject? Resolve(string target) => Resolve(target, null);

        public IGameObject? Resolve(string target, IReadOnlyDictionary<string, string>? parameters)
        {
            if (!catalog.TryGetDefinition(target, out var definition))
            {
                return null;
            }

            return definition.Selector.Resolve(TargetSelectionContext.From(definition.Parameters, parameters));
        }

        public IGameObject? ResolveMacroPlaceholder(string placeholder)
        {
            var parseResult = MacroPlaceholderTargetParser.Parse(placeholder, catalog, out var targetId, out var parameters);
            if (parseResult == MacroPlaceholderParseResult.Parsed)
            {
                return Resolve(targetId, parameters);
            }

            if (parseResult == MacroPlaceholderParseResult.Invalid)
            {
                return null;
            }

            if (!catalog.TryGetMacroPlaceholderSelector(placeholder, out var selector))
            {
                return null;
            }

            return selector.Resolve(TargetSelectionContext.Empty);
        }
    }
}
