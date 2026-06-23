using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using RedirectSmarter.Targeting.Parameters;

namespace RedirectSmarter.Targeting.Selectors
{
    /// <summary>
    /// Selects an enemy that is a good anchor point for enemy-targeted circular AoE actions.
    /// </summary>
    internal sealed class AoeEnemyTargetSelector : IRedirectTargetSelector
    {
        private const string RadiusParameterName = "radius";
        private const string MinTargetsParameterName = "min";
        private const string MaxRangeParameterName = "maxRange";
        private const string RadiusParameterDisplayNameKey = "RedirectTarget.AoeEnemy.Parameter.Radius";
        private const string MinTargetsParameterDisplayNameKey = "RedirectTarget.AoeEnemy.Parameter.Min";
        private const string MaxRangeParameterDisplayNameKey = "RedirectTarget.AoeEnemy.Parameter.MaxRange";
        private const int DefaultRadius = 5;
        private const int DefaultMinTargets = 2;
        private const int DefaultMaxRange = 30;
        private const int MaxReasonableDistance = 100;
        private const float CurrentTargetDistanceBonus = 0.5f;

        public static IReadOnlyList<TargetParameterDefinition> Parameters { get; } =
        [
            TargetParameter.Int(
                RadiusParameterName,
                RadiusParameterDisplayNameKey,
                defaultValue: DefaultRadius,
                min: 1,
                max: MaxReasonableDistance,
                suffix: "y",
                allowPositional: true,
                aliases: ["r"]
            ),
            TargetParameter.Int(
                MinTargetsParameterName,
                MinTargetsParameterDisplayNameKey,
                defaultValue: DefaultMinTargets,
                min: 1,
                max: 32
            ),
            TargetParameter.Int(
                MaxRangeParameterName,
                MaxRangeParameterDisplayNameKey,
                defaultValue: DefaultMaxRange,
                min: 1,
                max: MaxReasonableDistance,
                suffix: "y"
            ),
        ];

        public IGameObject? Resolve(TargetSelectionContext context)
        {
            var localPlayer = Services.ObjectTable.LocalPlayer;
            if (localPlayer is null)
            {
                return null;
            }

            var radius = context.GetInt(RadiusParameterName, DefaultRadius);
            var minTargets = context.GetInt(MinTargetsParameterName, DefaultMinTargets);
            var maxRange = context.GetInt(MaxRangeParameterName, DefaultMaxRange);
            var radiusSquared = radius * radius;
            var maxRangeSquared = maxRange * maxRange;
            var enemies = CollectEnemies(localPlayer, maxRangeSquared);

            if (enemies.Count == 0)
            {
                return null;
            }

            var currentTargetId = Services.TargetManager.Target?.GameObjectId;
            CandidateScore? bestScore = null;

            foreach (var candidate in enemies)
            {
                var score = ScoreCandidate(candidate, enemies, localPlayer, radiusSquared, currentTargetId);
                if (score.HitCount < minTargets)
                {
                    continue;
                }

                if (bestScore is null || IsBetter(score, bestScore.Value))
                {
                    bestScore = score;
                }
            }

            if (bestScore is null)
            {
                return null;
            }

            return bestScore.Value.Target;
        }

        private static List<IGameObject> CollectEnemies(IGameObject localPlayer, float maxRangeSquared)
        {
            var enemies = new List<IGameObject>();

            foreach (var gameObject in Services.ObjectTable)
            {
                if (!IsEligibleEnemy(gameObject))
                {
                    continue;
                }

                if (DistanceSquared2D(localPlayer, gameObject) > maxRangeSquared)
                {
                    continue;
                }

                enemies.Add(gameObject);
            }

            return enemies;
        }

        private static bool IsEligibleEnemy(IGameObject? gameObject)
        {
            return gameObject is IBattleNpc { BattleNpcKind: BattleNpcSubKind.Combatant } && gameObject.IsTargetable && !gameObject.IsDead;
        }

        private static CandidateScore ScoreCandidate(
            IGameObject candidate,
            IReadOnlyList<IGameObject> enemies,
            IGameObject localPlayer,
            float radiusSquared,
            ulong? currentTargetId
        )
        {
            var hitCount = 0;
            var totalCoveredDistance = 0f;

            foreach (var enemy in enemies)
            {
                var distanceSquared = DistanceSquared2D(candidate, enemy);
                if (distanceSquared > radiusSquared)
                {
                    continue;
                }

                hitCount++;
                totalCoveredDistance += MathF.Sqrt(distanceSquared);
            }

            var playerDistance = Distance2D(localPlayer, candidate);
            var isCurrentTarget = currentTargetId == candidate.GameObjectId;

            return new CandidateScore(candidate, hitCount, totalCoveredDistance, playerDistance, isCurrentTarget);
        }

        private static bool IsBetter(CandidateScore candidate, CandidateScore currentBest)
        {
            if (candidate.HitCount != currentBest.HitCount)
                return candidate.HitCount > currentBest.HitCount;

            if (!NearlyEqual(candidate.TotalCoveredDistance, currentBest.TotalCoveredDistance))
                return candidate.TotalCoveredDistance < currentBest.TotalCoveredDistance;

            var candidateDistance = candidate.PlayerDistance - (candidate.IsCurrentTarget ? CurrentTargetDistanceBonus : 0);
            var currentBestDistance = currentBest.PlayerDistance - (currentBest.IsCurrentTarget ? CurrentTargetDistanceBonus : 0);
            if (!NearlyEqual(candidateDistance, currentBestDistance))
                return candidateDistance < currentBestDistance;

            if (candidate.IsCurrentTarget != currentBest.IsCurrentTarget)
                return candidate.IsCurrentTarget;

            return candidate.Target.GameObjectId < currentBest.Target.GameObjectId;
        }

        private static bool NearlyEqual(float left, float right)
        {
            return MathF.Abs(left - right) < 0.001f;
        }

        private static float Distance2D(IGameObject first, IGameObject second)
        {
            return MathF.Sqrt(DistanceSquared2D(first, second));
        }

        private static float DistanceSquared2D(IGameObject first, IGameObject second)
        {
            var dx = first.Position.X - second.Position.X;
            var dz = first.Position.Z - second.Position.Z;
            return dx * dx + dz * dz;
        }

        private readonly record struct CandidateScore(
            IGameObject Target,
            int HitCount,
            float TotalCoveredDistance,
            float PlayerDistance,
            bool IsCurrentTarget
        );
    }
}
