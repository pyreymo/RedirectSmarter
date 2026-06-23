namespace RedirectSmarter.Targeting
{
    internal readonly record struct TargetValidationResult(bool IsValid, TargetValidationError Error)
    {
        public static TargetValidationResult Valid { get; } = new(true, TargetValidationError.InvalidTarget);

        public static TargetValidationResult Invalid(TargetValidationError error) => new(false, error);
    }
}
