using System.Collections.Generic;
using System.Linq;
using RedirectSmarter.Localization;

namespace RedirectSmarter.Targeting
{
    internal static class RedirectTargets
    {
        public const string Target = "Target";
        public const string Focus = "Focus";
        public const string TargetOfTarget = "Target of Target";
        public const string Self = "Self";
        public const string SoftTarget = "Soft Target";
        public const string Party2 = "<2>";
        public const string Party3 = "<3>";
        public const string Party4 = "<4>";
        public const string Party5 = "<5>";
        public const string Party6 = "<6>";
        public const string Party7 = "<7>";
        public const string Party8 = "<8>";
        public const string LowestHpPartyMember = "Lowest HP Party Member";
        public const string LowestHpPartyMemberPlaceholder = "<lowhp>";

        public static readonly IReadOnlyList<RedirectTargetDefinition> Definitions =
        [
            new(
                Target,
                "RedirectTarget.Target",
                new DelegateTargetSelector(() => Services.TargetManager.Target)
            ),
            new(
                Focus,
                "RedirectTarget.Focus",
                new DelegateTargetSelector(() => Services.TargetManager.FocusTarget)
            ),
            new(
                TargetOfTarget,
                "RedirectTarget.TargetOfTarget",
                new DelegateTargetSelector(() => Services.TargetManager.Target?.TargetObject)
            ),
            new(
                Self,
                "RedirectTarget.Self",
                new DelegateTargetSelector(() => Services.ObjectTable.LocalPlayer)
            ),
            new(
                SoftTarget,
                "RedirectTarget.SoftTarget",
                new DelegateTargetSelector(() => Services.TargetManager.SoftTarget)
            ),
            new(Party2, "RedirectTarget.Party2", new PlaceholderTargetSelector(Party2)),
            new(Party3, "RedirectTarget.Party3", new PlaceholderTargetSelector(Party3)),
            new(Party4, "RedirectTarget.Party4", new PlaceholderTargetSelector(Party4)),
            new(Party5, "RedirectTarget.Party5", new PlaceholderTargetSelector(Party5)),
            new(Party6, "RedirectTarget.Party6", new PlaceholderTargetSelector(Party6)),
            new(Party7, "RedirectTarget.Party7", new PlaceholderTargetSelector(Party7)),
            new(Party8, "RedirectTarget.Party8", new PlaceholderTargetSelector(Party8)),
            new(
                LowestHpPartyMember,
                "RedirectTarget.LowestHpPartyMember",
                new LowestHpPartyMemberTargetSelector(),
                LowestHpPartyMemberPlaceholder
            ),
        ];

        public static readonly HashSet<string> Valid =
        [
            .. Definitions.Select(definition => definition.Id),
        ];

        private static readonly Dictionary<string, string> DisplayNameKeys =
            Definitions.ToDictionary(
                definition => definition.Id,
                definition => definition.DisplayNameKey
            );

        public static string DisplayName(string target)
        {
            return DisplayNameKeys.TryGetValue(target, out var key) ? Loc.Text(key) : target;
        }
    }
}
