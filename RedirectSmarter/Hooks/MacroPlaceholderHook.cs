using System;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using InteropGenerator.Runtime;
using RedirectSmarter.Configuration;
using RedirectSmarter.Targeting;

namespace RedirectSmarter.Hooks
{
    internal sealed unsafe class MacroPlaceholderHook : IDisposable
    {
        private readonly PluginConfiguration configuration;
        private readonly TargetResolver targetResolver = new();
        private readonly Hook<ResolvePlaceholderDelegate> resolvePlaceholderHook;

        private delegate GameObject* ResolvePlaceholderDelegate(
            PronounModule* pronounModule,
            CStringPointer placeholder,
            byte a3,
            byte a4,
            bool a5
        );

        public MacroPlaceholderHook(PluginConfiguration configuration)
        {
            this.configuration = configuration;
            resolvePlaceholderHook =
                Services.InteropProvider.HookFromAddress<ResolvePlaceholderDelegate>(
                    PronounModule.Addresses.ResolvePlaceholder.Value,
                    ResolvePlaceholderDetour
                );

            resolvePlaceholderHook.Enable();
            Services.PluginLog.Debug("ResolvePlaceholder hook enabled.");
        }

        private GameObject* ResolvePlaceholderDetour(
            PronounModule* pronounModule,
            CStringPointer placeholder,
            byte a3,
            byte a4,
            bool a5
        )
        {
            if (!configuration.EnableRedirects)
            {
                Services.PluginLog.Debug("Macro placeholder bypassed: redirects disabled.");
                return resolvePlaceholderHook.Original(pronounModule, placeholder, a3, a4, a5);
            }

            try
            {
                var placeholderText = placeholder.ToString();
                Services.PluginLog.Debug(
                    "Macro placeholder intercepted: placeholder={Placeholder}, a3={A3}, a4={A4}, a5={A5}",
                    placeholderText,
                    a3,
                    a4,
                    a5
                );
                var resolvedTarget = targetResolver.ResolveMacroPlaceholder(placeholderText);

                if (resolvedTarget is not null)
                {
                    Services.PluginLog.Debug(
                        "Macro placeholder resolved: placeholder={Placeholder}, result={Result}, gameObjectId={GameObjectId}",
                        placeholderText,
                        resolvedTarget.Name.ToString(),
                        resolvedTarget.GameObjectId
                    );
                    return (GameObject*)resolvedTarget.Address;
                }

                Services.PluginLog.Debug(
                    "Macro placeholder falling back: placeholder={Placeholder}",
                    placeholderText
                );
            }
            catch (Exception ex)
            {
                Services.PluginLog.Error(ex, "Unable to resolve custom macro placeholder.");
            }

            return resolvePlaceholderHook.Original(pronounModule, placeholder, a3, a4, a5);
        }

        public void Dispose()
        {
            resolvePlaceholderHook.Dispose();
        }
    }
}
