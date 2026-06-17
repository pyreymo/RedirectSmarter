namespace RedirectSmarter.Targeting
{
    internal enum TargetValidationError
    {
        InvalidTarget,
        NotInRange,
        NotInLineOfSight,
    }

    internal static class TargetValidationErrors
    {
        public static TargetValidationError FromActionStatus(uint status)
        {
            return status switch
            {
                566 => TargetValidationError.NotInLineOfSight,
                562 => TargetValidationError.NotInRange,
                _ => TargetValidationError.InvalidTarget,
            };
        }
    }
}
