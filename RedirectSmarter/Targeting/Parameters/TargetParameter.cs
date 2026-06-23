using System.Collections.Generic;
using System.Globalization;

namespace RedirectSmarter.Targeting.Parameters
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
            bool allowPositional = false,
            IReadOnlyList<string>? aliases = null
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
                allowPositional,
                aliases
            );
        }

        public static TargetParameterDefinition Bool(
            string name,
            string displayNameKey,
            bool defaultValue,
            IReadOnlyList<string>? aliases = null
        )
        {
            return new TargetParameterDefinition(
                name,
                displayNameKey,
                TargetParameterKind.Bool,
                defaultValue.ToString().ToLowerInvariant(),
                Aliases: aliases
            );
        }
    }
}
