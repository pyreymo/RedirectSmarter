using System;
using System.Collections.Generic;
using System.Globalization;

namespace RedirectSmarter.Targeting.Parameters
{
    internal sealed class TargetSelectionContext(IReadOnlyDictionary<string, string> parameters)
    {
        public static TargetSelectionContext Empty { get; } = new(new Dictionary<string, string>());

        public int GetInt(string name, int defaultValue)
        {
            return
                parameters.TryGetValue(name, out var value)
                && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue)
                ? parsedValue
                : defaultValue;
        }

        public bool GetBool(string name, bool defaultValue)
        {
            return parameters.TryGetValue(name, out var value) && bool.TryParse(value, out var parsedValue) ? parsedValue : defaultValue;
        }

        public static TargetSelectionContext From(
            IReadOnlyList<TargetParameterDefinition> definitions,
            IReadOnlyDictionary<string, string>? parameters
        )
        {
            if (definitions.Count == 0)
                return Empty;

            var normalizedParameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var definition in definitions)
            {
                var value =
                    parameters is not null && parameters.TryGetValue(definition.Name, out var configuredValue)
                        ? configuredValue
                        : definition.DefaultValue;

                normalizedParameters[definition.Name] = definition.TryNormalize(value, out var normalizedValue)
                    ? normalizedValue
                    : definition.DefaultValue;
            }

            return new TargetSelectionContext(normalizedParameters);
        }
    }
}
