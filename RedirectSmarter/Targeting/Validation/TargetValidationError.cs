namespace RedirectSmarter.Targeting.Validation
{
    /// <summary>
    /// Represents the user-facing reason a resolved target cannot receive a redirected action.
    /// </summary>
    internal enum TargetValidationError
    {
        InvalidTarget,
        NotInRange,
        NotInLineOfSight,
    }
}
