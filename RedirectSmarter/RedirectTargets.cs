using System.Collections.Generic;

namespace RedirectSmarter
{
    internal static class RedirectTargets
    {
        public const string Target = "Target";
        public const string Focus = "Focus";
        public const string TargetOfTarget = "Target of Target";
        public const string Self = "Self";
        public const string SoftTarget = "Soft Target";
        public const string Chocobo = "Chocobo";
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
            Chocobo,
            Party2,
            Party3,
            Party4,
            Party5,
            Party6,
            Party7,
            Party8,
        ];

        public static readonly HashSet<string> Valid = new(All);
    }
}
