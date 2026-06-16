using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace RedirectSmarter
{
    class ConfigWindow : Window, IDisposable
    {
        private Configuration Configuration { get; }

        public ConfigWindow(Configuration configuration)
            : base($"{Plugin.Name} Settings")
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

        public override void Draw()
        {
            DrawConfigCheckbox(
                "Ignore range and target type errors",
                Configuration.IgnoreErrors,
                value => Configuration.IgnoreErrors = value
            );

            DrawConfigCheckbox(
                "Actions from macros",
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
