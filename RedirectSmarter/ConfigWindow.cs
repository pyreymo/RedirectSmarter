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

            Size = new Vector2(460, 260);
            SizeCondition = ImGuiCond.FirstUseEver;
        }

        public void Dispose() { }

        public new void Toggle()
        {
            IsOpen = !IsOpen;
        }

        public override void Draw()
        {
            ImGui.TextUnformatted("Targeting");
            ImGui.Separator();
            ImGui.Spacing();

            DrawTargetingOptions();

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.TextUnformatted("Action queueing");
            ImGui.Separator();
            ImGui.Spacing();

            DrawQueueOptions();
        }

        private void DrawTargetingOptions()
        {
            DrawConfigCheckbox(
                "Ignore range and target type errors",
                Configuration.IgnoreErrors,
                value => Configuration.IgnoreErrors = value
            );

            DrawConfigCheckbox(
                "Place all ground targets at the cursor",
                Configuration.DefaultCursorPlacement,
                value => Configuration.DefaultCursorPlacement = value
            );
        }

        private void DrawQueueOptions()
        {
            DrawConfigCheckbox(
                "Ground targeted actions",
                Configuration.QueueGroundActions,
                value => Configuration.QueueGroundActions = value
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
