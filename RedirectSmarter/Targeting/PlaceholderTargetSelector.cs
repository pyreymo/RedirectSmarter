using System;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace RedirectSmarter.Targeting
{
    /// <summary>
    /// Resolves a native game placeholder, such as &lt;2&gt;, through the client's pronoun module.
    /// </summary>
    internal sealed class PlaceholderTargetSelector(string placeholder) : IRedirectTargetSelector
    {
        public unsafe IGameObject? Resolve()
        {
            try
            {
                var pronounModule = PronounModule.Instance();
                var objectAddress = (IntPtr)pronounModule->ResolvePlaceholder(placeholder, 0, 0);
                return Services.ObjectTable.CreateObjectReference(objectAddress);
            }
            catch (Exception ex)
            {
                Services.PluginLog.Error($"Unable to resolve placeholder ({placeholder}): {ex.Message}");
                return null;
            }
        }
    }
}
