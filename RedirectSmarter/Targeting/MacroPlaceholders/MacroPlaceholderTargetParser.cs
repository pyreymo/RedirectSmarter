using System;
using System.Collections.Generic;
using RedirectSmarter.Targeting.Parameters;

namespace RedirectSmarter.Targeting.MacroPlaceholders
{
    internal static class MacroPlaceholderTargetParser
    {
        public static MacroPlaceholderParseResult Parse(
            string placeholder,
            RedirectTargetCatalog catalog,
            out string targetId,
            out Dictionary<string, string> parameters
        )
        {
            targetId = string.Empty;
            parameters = [];

            if (!TryGetParts(placeholder, out var parts))
                return MacroPlaceholderParseResult.NotCustom;

            if (!catalog.TryGetMacroPlaceholderDefinition(parts[0], out var definition))
                return MacroPlaceholderParseResult.NotCustom;

            targetId = definition.Id;
            if (parts.Length == 1)
                return MacroPlaceholderParseResult.Parsed;

            return TryParseArguments(parts, definition.Parameters, parameters)
                ? MacroPlaceholderParseResult.Parsed
                : MacroPlaceholderParseResult.Invalid;
        }

        private static bool TryGetParts(string placeholder, out string[] parts)
        {
            parts = [];
            var trimmedPlaceholder = placeholder.Trim();

            if (trimmedPlaceholder.Length < 2 || trimmedPlaceholder[0] != '<' || trimmedPlaceholder[^1] != '>')
                return false;

            var body = trimmedPlaceholder[1..^1];
            parts = body.Split(':');

            return parts.Length > 0 && parts[0].Length > 0;
        }

        private static bool TryParseArguments(
            IReadOnlyList<string> parts,
            IReadOnlyList<TargetParameterDefinition> definitions,
            Dictionary<string, string> parameters
        )
        {
            var positionalDefinition = FindPositionalDefinition(definitions);

            for (var i = 1; i < parts.Count; i++)
            {
                var argument = parts[i].Trim();
                if (argument.Length == 0)
                    return false;

                var separatorIndex = argument.IndexOf('=');
                if (separatorIndex >= 0)
                {
                    var name = argument[..separatorIndex].Trim();
                    var value = argument[(separatorIndex + 1)..].Trim();
                    if (!TrySetParameter(definitions, parameters, name, value))
                        return false;

                    continue;
                }

                if (TryFindDefinition(definitions, argument, out var namedDefinition))
                {
                    if (i + 1 >= parts.Count)
                        return false;

                    var value = parts[++i].Trim();
                    if (!TrySetParameter(namedDefinition, parameters, value))
                        return false;

                    continue;
                }

                if (i == 1 && positionalDefinition is not null)
                {
                    if (!TrySetParameter(positionalDefinition, parameters, argument))
                        return false;

                    continue;
                }

                return false;
            }

            return true;
        }

        private static TargetParameterDefinition? FindPositionalDefinition(IReadOnlyList<TargetParameterDefinition> definitions)
        {
            foreach (var definition in definitions)
            {
                if (definition.AllowPositional)
                    return definition;
            }

            return null;
        }

        private static bool TrySetParameter(
            IReadOnlyList<TargetParameterDefinition> definitions,
            Dictionary<string, string> parameters,
            string name,
            string value
        )
        {
            return TryFindDefinition(definitions, name, out var definition) && TrySetParameter(definition, parameters, value);
        }

        private static bool TrySetParameter(TargetParameterDefinition definition, Dictionary<string, string> parameters, string value)
        {
            if (parameters.ContainsKey(definition.Name))
                return false;

            if (!definition.TryNormalize(value, out var normalizedValue))
                return false;

            parameters[definition.Name] = normalizedValue;
            return true;
        }

        private static bool TryFindDefinition(
            IReadOnlyList<TargetParameterDefinition> definitions,
            string name,
            out TargetParameterDefinition definition
        )
        {
            foreach (var candidate in definitions)
            {
                if (candidate.MatchesName(name))
                {
                    definition = candidate;
                    return true;
                }
            }

            definition = null!;
            return false;
        }
    }
}
