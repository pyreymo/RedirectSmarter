using System.Collections.Generic;
using Lumina.Excel.Sheets;

namespace RedirectSmarter.Actions.Classification
{
    internal sealed class ActionClassifier
    {
        public static ActionClassification Classify(Action action)
        {
            var tags = new List<string> { $"Range:{action.Range}", $"EffectRange:{action.EffectRange}", $"CastType:{action.CastType}" };

            if (action.CanTargetHostile)
            {
                tags.Add("Enemy");
            }

            if (CanTargetFriendly(action))
            {
                tags.Add("Friendly");
            }

            if (action.EffectRange > 0)
            {
                tags.Add("AoE");
            }

            if (action.TargetArea)
            {
                tags.Add("TargetArea");
            }

            if (action.CanTargetHostile && action.EffectRange > 0 && action.Range > 0)
            {
                return new ActionClassification(
                    action.RowId,
                    RedirectUseCase.EnemyTargetedAoE,
                    action.Range,
                    action.EffectRange,
                    true,
                    "Enemy target with effect range",
                    tags
                );
            }

            if (action.CanTargetHostile)
            {
                return new ActionClassification(
                    action.RowId,
                    RedirectUseCase.EnemySingleTarget,
                    action.Range,
                    action.EffectRange,
                    true,
                    "Enemy target",
                    tags
                );
            }

            if (CanTargetFriendly(action) && action.EffectRange > 0)
            {
                return new ActionClassification(
                    action.RowId,
                    RedirectUseCase.FriendlyAoE,
                    action.Range,
                    action.EffectRange,
                    true,
                    "Friendly target with effect range",
                    tags
                );
            }

            if (CanTargetFriendly(action))
            {
                return new ActionClassification(
                    action.RowId,
                    RedirectUseCase.FriendlySingleTarget,
                    action.Range,
                    action.EffectRange,
                    true,
                    "Friendly target",
                    tags
                );
            }

            if (action.CanTargetSelf || !action.HasConfigurableTarget())
            {
                return new ActionClassification(
                    action.RowId,
                    RedirectUseCase.SelfOrNoTarget,
                    action.Range,
                    action.EffectRange,
                    false,
                    "Self or no configurable target",
                    tags
                );
            }

            return new ActionClassification(
                action.RowId,
                RedirectUseCase.Unknown,
                action.Range,
                action.EffectRange,
                false,
                "No matching redirect use case",
                tags
            );
        }

        private static bool CanTargetFriendly(Action action)
        {
            return action.CanTargetAlly || action.CanTargetParty;
        }
    }
}
