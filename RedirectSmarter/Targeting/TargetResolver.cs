using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Types;

namespace RedirectSmarter.Targeting
{
    internal class TargetResolver
    {
        private readonly Dictionary<string, IRedirectTargetSelector> selectors;
        private readonly Dictionary<string, IRedirectTargetSelector> macroPlaceholderSelectors;

        public TargetResolver()
            : this(RedirectTargets.Definitions) { }

        private TargetResolver(IEnumerable<RedirectTargetDefinition> definitions)
        {
            selectors = definitions.ToDictionary(
                definition => definition.Id,
                definition => definition.Selector
            );
            macroPlaceholderSelectors = definitions
                .Where(definition => definition.MacroPlaceholder is not null)
                .ToDictionary(
                    definition => definition.MacroPlaceholder!,
                    definition => definition.Selector
                );
        }

        public IGameObject? Resolve(string target)
        {
            if (!selectors.TryGetValue(target, out var selector))
            {
                Services.PluginLog.Debug("Target resolver missed selector: target={Target}", target);
                return null;
            }

            Services.PluginLog.Debug("Target resolver resolving: target={Target}", target);
            var resolvedTarget = selector.Resolve();
            Services.PluginLog.Debug(
                "Target resolver resolved: target={Target}, result={Result}, gameObjectId={GameObjectId}",
                target,
                resolvedTarget?.Name.ToString() ?? "null",
                resolvedTarget?.GameObjectId ?? 0
            );
            return resolvedTarget;
        }

        public IGameObject? ResolveMacroPlaceholder(string placeholder)
        {
            if (!macroPlaceholderSelectors.TryGetValue(placeholder, out var selector))
            {
                Services.PluginLog.Debug(
                    "Macro placeholder resolver missed selector: placeholder={Placeholder}",
                    placeholder
                );
                return null;
            }

            Services.PluginLog.Debug(
                "Macro placeholder resolver resolving: placeholder={Placeholder}",
                placeholder
            );
            var resolvedTarget = selector.Resolve();
            Services.PluginLog.Debug(
                "Macro placeholder resolver resolved: placeholder={Placeholder}, result={Result}, gameObjectId={GameObjectId}",
                placeholder,
                resolvedTarget?.Name.ToString() ?? "null",
                resolvedTarget?.GameObjectId ?? 0
            );
            return resolvedTarget;
        }
    }
}
