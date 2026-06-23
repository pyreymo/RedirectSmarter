using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Configuration;
using RedirectSmarter.Targeting;

namespace RedirectSmarter.Configuration
{
    [Serializable]
    public class PluginConfiguration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;
        public bool EnableRedirects { get; set; } = true;
        public bool EnableMacroQueueing { get; set; } = false;
        public bool IgnoreErrors { get; set; } = true;
        public string DefaultRedirection { get; set; } = RedirectTargets.Target;
        public Dictionary<uint, Redirection> Redirections { get; set; } = [];

        public bool PruneUnsupportedRedirections(IReadOnlySet<string> validTargets)
        {
            var changed = false;

            if (!validTargets.Contains(DefaultRedirection))
            {
                DefaultRedirection = RedirectTargets.Target;
                changed = true;
            }

            foreach (var (actionId, redirection) in Redirections.ToList())
            {
                redirection.NormalizeTargetOptions();

                for (var i = redirection.Count - 1; i >= 0; i--)
                {
                    if (validTargets.Contains(redirection[i]))
                    {
                        continue;
                    }

                    redirection.RemoveAt(i);
                    changed = true;
                }

                if (redirection.Count == 0)
                {
                    Redirections.Remove(actionId);
                    changed = true;
                }
            }

            return changed;
        }

        public void Save()
        {
            Services.Interface.SavePluginConfig(this);
        }
    }
}
