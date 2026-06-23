namespace RedirectSmarter.Redirecting
{
    internal enum RedirectResultKind
    {
        ContinueOriginal,
        UseTarget,
        Block,
    }

    internal readonly record struct RedirectResult(RedirectResultKind Kind, ulong TargetId = 0)
    {
        public static RedirectResult ContinueOriginal() => new(RedirectResultKind.ContinueOriginal);

        public static RedirectResult UseTarget(ulong targetId) => new(RedirectResultKind.UseTarget, targetId);

        public static RedirectResult Block() => new(RedirectResultKind.Block);
    }
}
