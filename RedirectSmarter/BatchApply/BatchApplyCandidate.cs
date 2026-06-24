using Lumina.Excel.Sheets;
using RedirectSmarter.Actions.Classification;

namespace RedirectSmarter.BatchApply
{
    internal sealed record BatchApplyCandidate(Action Action, ActionClassification Classification);
}
