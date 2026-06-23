namespace RedirectSmarter.Targeting.Validation
{
    /// <summary>
    /// Carries the result of target validation together with the failure reason when validation fails.
    /// </summary>
    internal readonly record struct TargetValidationResult(bool IsValid, TargetValidationError Error)
    {
        public static TargetValidationResult Valid { get; } = new(true, TargetValidationError.InvalidTarget);

        public static TargetValidationResult Invalid(TargetValidationError error) => new(false, error);
    }
}
