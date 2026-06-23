using System;
using Dalamud.Game.Command;
using Dalamud.Game.Gui.Toast;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using RedirectSmarter.Actions;
using RedirectSmarter.Configuration;
using RedirectSmarter.Hooks;
using RedirectSmarter.Localization;
using RedirectSmarter.Redirecting;
using RedirectSmarter.Targeting;
using RedirectSmarter.UI;

namespace RedirectSmarter
{
    public class Plugin : IDalamudPlugin, IDisposable
    {
        public static string Name => "Redirect Smarter";
        private const string CommandName = "/rs";
        private PluginConfiguration Configuration { get; set; }
        private RedirectTargetCatalog TargetCatalog { get; } = null!;
        private TargetResolver TargetResolver { get; } = null!;
        private ActionRedirector ActionRedirector { get; } = null!;
        private PluginUI PluginUi { get; } = null!;
        private ActionCatalog ActionCatalog { get; } = null!;
        private GameHooks Hooks { get; } = null!;
        private MacroPlaceholderHook MacroPlaceholderHook { get; } = null!;

        private readonly WindowSystem WindowSystem = new(Name);

        public static IDalamudPluginInterface Interface => Services.Interface;
        public static IDataManager DataManager => Services.DataManager;
        public static ICommandManager CommandManager => Services.CommandManager;

        public Plugin(IDalamudPluginInterface i)
        {
            Services.Initialize(i);
            Loc.Load(Interface);

            try
            {
                Configuration = Interface.GetPluginConfig() as PluginConfiguration ?? new PluginConfiguration();
            }
            catch (Exception)
            {
                Services.PluginLog.Error("Failed to load plugin configuration. A new configuration file has been created.");
                Configuration = new PluginConfiguration();
            }

            TargetCatalog = new RedirectTargetCatalog();
            TargetResolver = new TargetResolver(TargetCatalog);

            if (Configuration.PruneUnsupportedRedirections(TargetCatalog.ValidTargets))
            {
                Configuration.Save();
            }

            ActionCatalog = new();
            ActionRedirector = new ActionRedirector(Configuration, TargetResolver);
            Hooks = new(Configuration, ActionCatalog, ActionRedirector);
            MacroPlaceholderHook = new(Configuration, TargetResolver);
            PluginUi = new PluginUI(Configuration, ActionCatalog, TargetCatalog);

            WindowSystem.AddWindow(PluginUi);

            RegisterCommand();

            Interface.LanguageChanged += OnLanguageChanged;
            Interface.UiBuilder.Draw += OnDraw;
            Interface.UiBuilder.OpenMainUi += OpenMainUi;
            Interface.UiBuilder.OpenConfigUi += OpenConfigUi;
        }

        public void Dispose()
        {
            Interface.LanguageChanged -= OnLanguageChanged;
            Interface.UiBuilder.Draw -= OnDraw;
            Interface.UiBuilder.OpenMainUi -= OpenMainUi;
            Interface.UiBuilder.OpenConfigUi -= OpenConfigUi;

            WindowSystem.RemoveAllWindows();

            Hooks.Dispose();
            MacroPlaceholderHook.Dispose();
            PluginUi.Dispose();
            Configuration.Save();

            CommandManager.RemoveHandler(CommandName);
        }

        private void RegisterCommand()
        {
            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) { HelpMessage = Loc.Text("Command.Help") });
        }

        private void OnLanguageChanged(string langCode)
        {
            Loc.Load(Interface, langCode);
            PluginUi.UpdateLanguage();

            CommandManager.RemoveHandler(CommandName);
            RegisterCommand();
        }

        private void OnCommand(string command, string args)
        {
            if (args.Trim().Equals("toggle", StringComparison.OrdinalIgnoreCase))
            {
                ToggleRedirects();
                return;
            }

            PluginUi.Toggle();
        }

        private void ToggleRedirects()
        {
            Configuration.EnableRedirects = !Configuration.EnableRedirects;
            Configuration.Save();
            PluginUi.UpdateLanguage();

            // TODO: remove this annoying toast
            Services.ToastGui.ShowNormal(
                Loc.Text(Configuration.EnableRedirects ? "Command.RedirectsEnabled" : "Command.RedirectsDisabled"),
                new ToastOptions { Speed = ToastSpeed.Fast }
            );
        }

        private void OnDraw()
        {
            try
            {
                WindowSystem.Draw();
            }
            catch (Exception ex)
            {
                Services.PluginLog.Error(ex, " WindowSystem.Draw threw.");
            }
        }

        private void OpenMainUi()
        {
            PluginUi.Toggle();
        }

        private void OpenConfigUi()
        {
            PluginUi.ToggleSettings();
        }
    }
}
