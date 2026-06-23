using System.Collections.Generic;
using System.Linq;
using RedirectSmarter.Localization;
using RedirectSmarter.Targeting.Selectors;

namespace RedirectSmarter.Targeting
{
    /// <summary>
    /// Owns the ordered redirect target definitions and lookup tables used by config, UI, and runtime resolution.
    /// </summary>
    internal sealed class RedirectTargetCatalog
    {
        private readonly Dictionary<string, RedirectTargetDefinition> definitionsById;
        private readonly Dictionary<string, RedirectTargetDefinition> definitionsByMacroPlaceholder;
        private readonly Dictionary<string, string> displayNameKeys;

        public RedirectTargetCatalog()
            : this(CreateDefinitions()) { }

        private RedirectTargetCatalog(IReadOnlyList<RedirectTargetDefinition> definitions)
        {
            Definitions = definitions;
            definitionsById = definitions.ToDictionary(definition => definition.Id);
            definitionsByMacroPlaceholder = definitions
                .Where(definition => definition.MacroPlaceholder is not null)
                .ToDictionary(definition => NormalizeMacroPlaceholder(definition.MacroPlaceholder!), definition => definition);
            displayNameKeys = definitions.ToDictionary(definition => definition.Id, definition => definition.DisplayNameKey);
            ValidTargets = new HashSet<string>(definitionsById.Keys);
        }

        public IReadOnlyList<RedirectTargetDefinition> Definitions { get; }
        public IReadOnlySet<string> ValidTargets { get; }

        public bool TryGetSelector(string target, out IRedirectTargetSelector selector)
        {
            if (definitionsById.TryGetValue(target, out var definition))
            {
                selector = definition.Selector;
                return true;
            }

            selector = null!;
            return false;
        }

        public bool TryGetDefinition(string target, out RedirectTargetDefinition definition)
        {
            return definitionsById.TryGetValue(target, out definition!);
        }

        public bool TryGetMacroPlaceholderSelector(string placeholder, out IRedirectTargetSelector selector)
        {
            if (TryGetMacroPlaceholderDefinition(placeholder, out var definition))
            {
                selector = definition.Selector;
                return true;
            }

            selector = null!;
            return false;
        }

        public bool TryGetMacroPlaceholderDefinition(string placeholder, out RedirectTargetDefinition definition)
        {
            return definitionsByMacroPlaceholder.TryGetValue(NormalizeMacroPlaceholder(placeholder), out definition!);
        }

        public string DisplayName(string target)
        {
            return displayNameKeys.TryGetValue(target, out var key) ? Loc.Text(key) : target;
        }

        private static IReadOnlyList<RedirectTargetDefinition> CreateDefinitions() =>
            [
                new(RedirectTargets.Target, "RedirectTarget.Target", new DelegateTargetSelector(() => Services.TargetManager.Target)),
                new(RedirectTargets.Focus, "RedirectTarget.Focus", new DelegateTargetSelector(() => Services.TargetManager.FocusTarget)),
                new(
                    RedirectTargets.TargetOfTarget,
                    "RedirectTarget.TargetOfTarget",
                    new DelegateTargetSelector(() => Services.TargetManager.Target?.TargetObject)
                ),
                new(RedirectTargets.Self, "RedirectTarget.Self", new DelegateTargetSelector(() => Services.ObjectTable.LocalPlayer)),
                new(
                    RedirectTargets.SoftTarget,
                    "RedirectTarget.SoftTarget",
                    new DelegateTargetSelector(() => Services.TargetManager.SoftTarget)
                ),
                new(RedirectTargets.Party2, "RedirectTarget.Party2", new PlaceholderTargetSelector(RedirectTargets.Party2)),
                new(RedirectTargets.Party3, "RedirectTarget.Party3", new PlaceholderTargetSelector(RedirectTargets.Party3)),
                new(RedirectTargets.Party4, "RedirectTarget.Party4", new PlaceholderTargetSelector(RedirectTargets.Party4)),
                new(RedirectTargets.Party5, "RedirectTarget.Party5", new PlaceholderTargetSelector(RedirectTargets.Party5)),
                new(RedirectTargets.Party6, "RedirectTarget.Party6", new PlaceholderTargetSelector(RedirectTargets.Party6)),
                new(RedirectTargets.Party7, "RedirectTarget.Party7", new PlaceholderTargetSelector(RedirectTargets.Party7)),
                new(RedirectTargets.Party8, "RedirectTarget.Party8", new PlaceholderTargetSelector(RedirectTargets.Party8)),
                new(
                    RedirectTargets.LowestHpPartyMember,
                    "RedirectTarget.LowestHpPartyMember",
                    new LowestHpPartyMemberTargetSelector(),
                    RedirectTargets.LowestHpPartyMemberPlaceholder,
                    LowestHpPartyMemberTargetSelector.Parameters
                ),
                new(
                    RedirectTargets.AoeEnemy,
                    "RedirectTarget.AoeEnemy",
                    new AoeEnemyTargetSelector(),
                    RedirectTargets.AoeEnemyPlaceholder,
                    AoeEnemyTargetSelector.Parameters
                ),
            ];

        private static string NormalizeMacroPlaceholder(string placeholder)
        {
            var normalized = placeholder.Trim();
            if (normalized.Length >= 2 && normalized[0] == '<' && normalized[^1] == '>')
            {
                normalized = normalized[1..^1];
            }

            return normalized;
        }
    }
}
