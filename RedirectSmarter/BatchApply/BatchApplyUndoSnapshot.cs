using System.Collections.Generic;
using RedirectSmarter.Configuration;

namespace RedirectSmarter.BatchApply
{
    internal sealed record BatchApplyUndoSnapshot(IReadOnlyDictionary<uint, Redirection?> PreviousRedirections);
}
