using System.Collections.Generic;
using RedirectSmarter.Configuration;

namespace RedirectSmarter.BatchApply
{
    internal sealed class BatchApplyService(PluginConfiguration configuration)
    {
        public BatchApplyUndoSnapshot? LastUndoSnapshot { get; private set; }

        public BatchApplyResult Apply(RuleTemplateSnapshot template, IEnumerable<uint> actionIds, BatchApplyMode mode)
        {
            var previousRedirections = new Dictionary<uint, Redirection?>();
            var appliedCount = 0;
            var skippedCount = 0;

            foreach (var actionId in actionIds)
            {
                var hasExisting = configuration.Redirections.TryGetValue(actionId, out var existing);
                if (hasExisting && mode == BatchApplyMode.SkipConfigured)
                {
                    skippedCount++;
                    continue;
                }

                previousRedirections[actionId] = existing?.Clone(actionId);
                configuration.Redirections[actionId] = template.CreateRedirection(actionId);
                appliedCount++;
            }

            LastUndoSnapshot = previousRedirections.Count > 0 ? new BatchApplyUndoSnapshot(previousRedirections) : null;
            configuration.Save();

            return new BatchApplyResult(appliedCount, skippedCount);
        }

        public bool UndoLastApply()
        {
            if (LastUndoSnapshot is null)
            {
                return false;
            }

            foreach (var (actionId, previousRedirection) in LastUndoSnapshot.PreviousRedirections)
            {
                if (previousRedirection is null)
                {
                    configuration.Redirections.Remove(actionId);
                    continue;
                }

                configuration.Redirections[actionId] = previousRedirection.Clone(actionId);
            }

            LastUndoSnapshot = null;
            configuration.Save();
            return true;
        }

        public void ClearUndo()
        {
            LastUndoSnapshot = null;
        }
    }
}
