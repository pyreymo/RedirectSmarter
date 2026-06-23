using RedirectSmarter.Localization;
using RedirectSmarter.Targeting.Validation;

namespace RedirectSmarter.Redirecting
{
    internal sealed class RedirectErrorNotifier
    {
        public static void ShowNoRedirectTarget()
        {
            Services.ToastGui.ShowError(Loc.Text("Error.NoRedirectTarget"));
        }

        public static void ShowTargetError(TargetValidationError error)
        {
            Services.ToastGui.ShowError(
                error switch
                {
                    TargetValidationError.NotInLineOfSight => Loc.Text("Error.TargetNotInLineOfSight"),
                    TargetValidationError.NotInRange => Loc.Text("Error.TargetNotInRange"),
                    _ => Loc.Text("Error.InvalidTarget"),
                }
            );
        }
    }
}
