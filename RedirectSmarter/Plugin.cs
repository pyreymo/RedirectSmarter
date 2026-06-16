using System;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace RedirectSmarter
{
    public class Plugin : IDalamudPlugin, IDisposable
    {
        public static string Name => "Redirect Smarter";
        private const string CommandName = "/rs";
        private Configuration Configuration { get; set; }
        private PluginUI PluginUi { get; } = null!;
        private ConfigWindow ConfigWindow { get; } = null!;
        private Actions Actions { get; } = null!;
        private GameHooks Hooks { get; } = null!;

        private readonly WindowSystem WindowSystem = new(Name);

        public static IDalamudPluginInterface Interface => Services.Interface;
        public static IDataManager DataManager => Services.DataManager;
        public static ICommandManager CommandManager => Services.CommandManager;

        public Plugin(IDalamudPluginInterface i)
        {
            Services.Initialize(i);

            try
            {
                Configuration = Interface.GetPluginConfig() as Configuration ?? new Configuration();
            }
            catch (Exception)
            {
                Services.PluginLog.Error(
                    "Failed to load plugin configuration. A new configuration file has been created."
                );
                Configuration = new Configuration();
            }

            Actions = new();
            Hooks = new(Configuration, Actions);
            ConfigWindow = new ConfigWindow(Configuration);
            PluginUi = new PluginUI(Configuration, Actions, ConfigWindow.Toggle);

            WindowSystem.AddWindow(PluginUi);
            WindowSystem.AddWindow(ConfigWindow);

            CommandManager.AddHandler(
                CommandName,
                new CommandInfo(OnCommand) { HelpMessage = "Opens the configuration menu" }
            );

            Interface.UiBuilder.Draw += OnDraw;
            Interface.UiBuilder.OpenMainUi += OpenMainUi;
            Interface.UiBuilder.OpenConfigUi += OpenConfigUi;
        }

        public void Dispose()
        {
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
