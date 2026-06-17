using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using RedirectSmarter.Configuration;
using RedirectSmarter.Localization;

namespace RedirectSmarter.UI
{
    class ConfigWindow : Window, IDisposable
    {
        private PluginConfiguration Configuration { get; }

        public ConfigWindow(PluginConfiguration configuration)
            : base(Loc.Text("Window.Settings"))
        {
            Configuration = configuration;

            Size = new Vector2(460, 220);
            SizeCondition = ImGuiCond.FirstUseEver;
        }

        public void Dispose() { }

        public new void Toggle()
        {
            IsOpen = !IsOpen;
        }

        public void UpdateLanguage()
        {
            WindowName = Loc.Text("Window.Settings");
        }

        public override void Draw()
        {
            DrawConfigCheckbox(
                Loc.Text("Config.IgnoreErrors"),
                Configuration.IgnoreErrors,
                value => Configuration.IgnoreErrors = value
            );

            DrawConfigCheckbox(
                Loc.Text("Config.ActionsFromMacros"),
                Configuration.EnableMacroQueueing,
                value => Configuration.EnableMacroQueueing = value
            );
        }

        private void DrawConfigCheckbox(string label, bool currentValue, Action<bool> setValue)
        {
            var value = currentValue;

            if (ImGui.Checkbox(label, ref value))
            {
                setValue(value);
                Configuration.Save();
            }
        }
    }
}
