using System.Collections.Generic;
using System.Linq;
using Lumina.Excel.Sheets;
using RedirectSmarter.Actions.Classification;
using RedirectSmarter.Configuration;
using RedirectSmarter.Targeting;

namespace RedirectSmarter.BatchApply
{
    internal sealed class SimilarActionFinder()
    {
        public static IReadOnlyList<BatchApplyCandidate> FindCandidates(
            Action sourceAction,
            Redirection sourceRedirection,
            IEnumerable<Action> actions
        )
        {
            var useCase = GetDesiredUseCase(sourceAction, sourceRedirection);

            return
            [
                .. actions
                    .Where(action => action.RowId != sourceAction.RowId && !action.IsPvP)
                    .Select(action => new BatchApplyCandidate(action, ActionClassifier.Classify(action)))
                    .Where(candidate => candidate.Classification.UseCase == useCase)
                    .OrderByDescending(candidate => candidate.Classification.HighConfidence)
                    .ThenBy(candidate => candidate.Action.Name.ToString())
                    .ThenBy(candidate => candidate.Action.RowId),
            ];
        }

        private static RedirectUseCase GetDesiredUseCase(Action sourceAction, Redirection sourceRedirection)
        {
            for (var i = 0; i < sourceRedirection.Count; i++)
            {
                if (sourceRedirection[i] == RedirectTargets.AoeEnemy)
                {
                    return RedirectUseCase.EnemyTargetedAoE;
                }
            }

            return ActionClassifier.Classify(sourceAction).UseCase;
        }
    }
}
