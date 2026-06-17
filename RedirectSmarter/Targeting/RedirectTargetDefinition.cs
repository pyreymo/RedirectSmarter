namespace RedirectSmarter.Targeting
{
    internal sealed record RedirectTargetDefinition(
        string Id,
        string DisplayNameKey,
        IRedirectTargetSelector Selector,
        string? MacroPlaceholder = null
    );
}
