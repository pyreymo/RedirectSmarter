using System;
using System.Collections.Generic;
using System.Globalization;

namespace RedirectSmarter.Targeting.Parameters
{
    internal sealed record TargetParameterDefinition(
        string Name,
        string DisplayNameKey,
        TargetParameterKind Kind,
        string DefaultValue,
        int Min = int.MinValue,
        int Max = int.MaxValue,
        string? Suffix = null,
        bool AllowPositional = false,
        IReadOnlyList<string>? Aliases = null
    )
    {
        public IReadOnlyList<string> Aliases { get; init; } = Aliases ?? [];

        public bool MatchesName(string name)
        {
            if (Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return true;

            foreach (var alias in Aliases)
            {
                if (alias.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public bool TryNormalize(string value, out string normalizedValue)
        {
            normalizedValue = DefaultValue;

            return Kind switch
            {
                TargetParameterKind.Int => TryNormalizeInt(value, out normalizedValue),
                TargetParameterKind.Bool => TryNormalizeBool(value, out normalizedValue),
                _ => false,
            };
        }

        private bool TryNormalizeInt(string value, out string normalizedValue)
        {
            normalizedValue = DefaultValue;

            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
                return false;

            if (intValue < Min || intValue > Max)
                return false;

            normalizedValue = intValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        private static bool TryNormalizeBool(string value, out string normalizedValue)
        {
            normalizedValue = string.Empty;

            if (!bool.TryParse(value, out var boolValue))
                return false;

            normalizedValue = boolValue.ToString().ToLowerInvariant();
            return true;
        }
    }
}
