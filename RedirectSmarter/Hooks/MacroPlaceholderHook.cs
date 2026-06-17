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
                return resolvePlaceholderHook.Original(pronounModule, placeholder, a3, a4, a5);
            }

            try
            {
                var placeholderText = placeholder.ToString();
                var resolvedTarget = targetResolver.ResolveMacroPlaceholder(placeholderText);

                if (resolvedTarget is not null)
                {
                    return (GameObject*)resolvedTarget.Address;
                }
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
