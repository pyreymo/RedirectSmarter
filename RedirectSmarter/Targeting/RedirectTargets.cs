using System.Collections.Generic;
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

        public static readonly string[] All =
        [
            Target,
            Focus,
            TargetOfTarget,
            Self,
            SoftTarget,
            Party2,
            Party3,
            Party4,
            Party5,
            Party6,
            Party7,
            Party8,
        ];

        public static readonly HashSet<string> Valid = [.. All];

        public static string DisplayName(string target)
        {
            return target switch
            {
                Target => Loc.Text("RedirectTarget.Target"),
                Focus => Loc.Text("RedirectTarget.Focus"),
                TargetOfTarget => Loc.Text("RedirectTarget.TargetOfTarget"),
                Self => Loc.Text("RedirectTarget.Self"),
                SoftTarget => Loc.Text("RedirectTarget.SoftTarget"),
                Party2 => Loc.Text("RedirectTarget.Party2"),
                Party3 => Loc.Text("RedirectTarget.Party3"),
                Party4 => Loc.Text("RedirectTarget.Party4"),
                Party5 => Loc.Text("RedirectTarget.Party5"),
                Party6 => Loc.Text("RedirectTarget.Party6"),
                Party7 => Loc.Text("RedirectTarget.Party7"),
                Party8 => Loc.Text("RedirectTarget.Party8"),
                _ => target,
            };
        }
    }
}
