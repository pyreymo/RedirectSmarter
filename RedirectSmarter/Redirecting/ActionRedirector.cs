using RedirectSmarter.Actions;
using RedirectSmarter.Configuration;
using RedirectSmarter.Targeting;
using LuminaAction = Lumina.Excel.Sheets.Action;
using UseActionMode = FFXIVClientStructs.FFXIV.Client.Game.ActionManager.UseActionMode;

namespace RedirectSmarter.Redirecting
{
    internal sealed class ActionRedirector(PluginConfiguration configuration, TargetResolver targetResolver)
    {
        public RedirectResult Resolve(LuminaAction requestedAction, LuminaAction adjustedAction, UseActionMode mode)
        {
            if (!ShouldRedirect(adjustedAction, mode))
            {
                return RedirectResult.ContinueOriginal();
            }

            var configurationId = GetConfigurationId(requestedAction, adjustedAction);
            if (!configuration.Redirections.TryGetValue(configurationId, out var redirection))
            {
                return RedirectResult.ContinueOriginal();
            }

            return ResolveConfiguredTarget(adjustedAction, redirection);
        }

        private RedirectResult ResolveConfiguredTarget(LuminaAction adjustedAction, Redirection redirection)
        {
            for (var i = 0; i < redirection.Count; i++)
            {
                var targetName = redirection[i];
                var resolvedTarget = targetResolver.Resolve(targetName, redirection.GetTargetOptions(i).Parameters);
                if (resolvedTarget is null)
                {
                    continue;
                }

                var validation = TargetValidator.Validate(adjustedAction, resolvedTarget);
                if (validation.IsValid)
                {
                    return RedirectResult.UseTarget(resolvedTarget.GameObjectId);
                }

                if (!configuration.IgnoreErrors)
                {
                    RedirectErrorNotifier.ShowTargetError(validation.Error);
                    return RedirectResult.Block();
                }
            }

            if (redirection.PreventDefault)
            {
                if (!configuration.IgnoreErrors)
                {
                    RedirectErrorNotifier.ShowNoRedirectTarget();
                }

                return RedirectResult.Block();
            }

            return RedirectResult.ContinueOriginal();
        }

        private static bool ShouldRedirect(LuminaAction adjustedAction, UseActionMode mode)
        {
            return mode != UseActionMode.Queue && adjustedAction.HasConfigurableTarget();
        }

        private static uint GetConfigurationId(LuminaAction requestedAction, LuminaAction adjustedAction)
        {
            return adjustedAction.IsPlayerAction ? adjustedAction.RowId : requestedAction.RowId;
        }
    }
}
