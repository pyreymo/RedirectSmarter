using System.Collections.Generic;

namespace RedirectSmarter.Actions.Classification
{
    internal sealed record ActionClassification(
        uint ActionId,
        RedirectUseCase UseCase,
        sbyte Range,
        byte EffectRange,
        bool HighConfidence,
        string Reason,
        IReadOnlyList<string> Tags
    );
}
