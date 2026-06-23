using System.Collections.Generic;
using RedirectSmarter.Targeting.Parameters;
using RedirectSmarter.Targeting.Selectors;

namespace RedirectSmarter.Targeting
{
    /// <summary>
    /// Describes one selectable redirect target, including its persisted id, display key, resolver, and optional macro placeholder.
    /// </summary>
    internal sealed record RedirectTargetDefinition(
        string Id,
        string DisplayNameKey,
        IRedirectTargetSelector Selector,
        string? MacroPlaceholder = null,
        IReadOnlyList<TargetParameterDefinition>? Parameters = null
    )
    {
        public IReadOnlyList<TargetParameterDefinition> Parameters { get; init; } = Parameters ?? [];
    }
}
