using RedirectSmarter.Configuration;

namespace RedirectSmarter.BatchApply
{
    internal sealed record RuleTemplateSnapshot(uint SourceActionId, Redirection Redirection)
    {
        public Redirection CreateRedirection(uint actionId)
        {
            return Redirection.Clone(actionId);
        }
    }
}
