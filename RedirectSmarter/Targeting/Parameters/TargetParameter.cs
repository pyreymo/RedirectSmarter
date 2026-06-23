using System.Globalization;

namespace RedirectSmarter.Targeting
{
    internal static class TargetParameter
    {
        public static TargetParameterDefinition Int(
            string name,
            string displayNameKey,
            int defaultValue,
            int min = int.MinValue,
            int max = int.MaxValue,
            string? suffix = null,
            bool allowPositional = false
        )
        {
            return new TargetParameterDefinition(
                name,
                displayNameKey,
                TargetParameterKind.Int,
                defaultValue.ToString(CultureInfo.InvariantCulture),
                min,
                max,
                suffix,
                allowPositional
            );
        }

        public static TargetParameterDefinition Bool(string name, string displayNameKey, bool defaultValue)
        {
            return new TargetParameterDefinition(
                name,
                displayNameKey,
                TargetParameterKind.Bool,
                defaultValue.ToString().ToLowerInvariant()
            );
        }
    }
}
