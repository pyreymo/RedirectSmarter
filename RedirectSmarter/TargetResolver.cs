using System;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace RedirectSmarter
{
    internal class TargetResolver
    {
        private static ITargetManager TargetManager => Services.TargetManager;

        public IGameObject? Resolve(string target)
        {
            return target switch
            {
                RedirectTargets.Self => Services.ObjectTable.LocalPlayer,
                RedirectTargets.Target => TargetManager.Target,
                RedirectTargets.Focus => TargetManager.FocusTarget,
                RedirectTargets.TargetOfTarget => TargetManager.Target?.TargetObject,
                RedirectTargets.SoftTarget => TargetManager.SoftTarget,
                RedirectTargets.Chocobo => ResolvePlaceholder("<b>"),
                RedirectTargets.Party2 => ResolvePlaceholder(RedirectTargets.Party2),
                RedirectTargets.Party3 => ResolvePlaceholder(RedirectTargets.Party3),
                RedirectTargets.Party4 => ResolvePlaceholder(RedirectTargets.Party4),
                RedirectTargets.Party5 => ResolvePlaceholder(RedirectTargets.Party5),
                RedirectTargets.Party6 => ResolvePlaceholder(RedirectTargets.Party6),
                RedirectTargets.Party7 => ResolvePlaceholder(RedirectTargets.Party7),
                RedirectTargets.Party8 => ResolvePlaceholder(RedirectTargets.Party8),
                _ => null,
            };
        }

        private unsafe IGameObject? ResolvePlaceholder(string placeholder)
        {
            try
            {
                var pronounModule = PronounModule.Instance();
                var objectAddress = (IntPtr)pronounModule->ResolvePlaceholder(placeholder, 0, 0);
                return Services.ObjectTable.CreateObjectReference(objectAddress);
            }
            catch (Exception ex)
            {
                Services.PluginLog.Error(
                    $"Unable to resolve placeholder ({placeholder}): {ex.Message}"
                );
                return null;
            }
        }
    }
}
