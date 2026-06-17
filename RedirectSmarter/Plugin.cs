using System;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using RedirectSmarter.Actions;
using RedirectSmarter.Configuration;
using RedirectSmarter.Hooks;
using RedirectSmarter.Localization;
using RedirectSmarter.UI;

namespace RedirectSmarter
{
    public class Plugin : IDalamudPlugin, IDisposable
    {
        public static string Name => "Redirect Smarter";
        private const string CommandName = "/rs";
        private PluginConfiguration Configuration { get; set; }
        private PluginUI PluginUi { get; } = null!;
        private ConfigWindow ConfigWindow { get; } = null!;
        private ActionCatalog ActionCatalog { get; } = null!;
        private GameHooks Hooks { get; } = null!;

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
                Configuration =
                    Interface.GetPluginConfig() as PluginConfiguration ?? new PluginConfiguration();
            }
            catch (Exception)
            {
                Services.PluginLog.Error(
                    "Failed to load plugin configuration. A new configuration file has been created."
                );
                Configuration = new PluginConfiguration();
            }

            if (Configuration.PruneUnsupportedRedirections())
            {
                Configuration.Save();
            }

            ActionCatalog = new();
            Hooks = new(Configuration, ActionCatalog);
            ConfigWindow = new ConfigWindow(Configuration);
            PluginUi = new PluginUI(Configuration, ActionCatalog, ConfigWindow.Toggle);

            WindowSystem.AddWindow(PluginUi);
            WindowSystem.AddWindow(ConfigWindow);

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
            PluginUi.Dispose();
            ConfigWindow.Dispose();
            Configuration.Save();

            CommandManager.RemoveHandler(CommandName);
        }

        private void RegisterCommand()
        {
            CommandManager.AddHandler(
                CommandName,
                new CommandInfo(OnCommand) { HelpMessage = Loc.Text("Command.OpenConfig") }
            );
        }

        private void OnLanguageChanged(string langCode)
        {
            Loc.Load(Interface, langCode);
            ConfigWindow.UpdateLanguage();

            CommandManager.RemoveHandler(CommandName);
            RegisterCommand();
        }

        private void OnCommand(string command, string args)
        {
            PluginUi.Toggle();
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
            ConfigWindow.Toggle();
        }
    }
}
