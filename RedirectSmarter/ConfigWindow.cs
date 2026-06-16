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

            Size = new Vector2(460, 360);
            SizeCondition = ImGuiCond.FirstUseEver;
        }

        public void Dispose() { }

        public new void Toggle()
        {
            IsOpen = !IsOpen;
        }

        public override void Draw()
        {
            ImGui.TextUnformatted("Default redirection");
            ImGui.Separator();
            ImGui.Spacing();

            DrawRedirectionOptions();

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.TextUnformatted("Action queueing");
            ImGui.Separator();
            ImGui.Spacing();

            DrawQueueOptions();
        }

        private void DrawRedirectionOptions()
        {
            DrawConfigCheckbox(
                "Ignore range and target type errors",
                Configuration.IgnoreErrors,
                value => Configuration.IgnoreErrors = value
            );

            DrawConfigCheckbox(
                "Treat all friendly actions as mouseovers",
                Configuration.DefaultMouseoverFriendly,
                value => Configuration.DefaultMouseoverFriendly = value
            );

            if (Configuration.DefaultMouseoverFriendly)
            {
                ImGui.Indent();
                DrawConfigCheckbox(
                    "Include friendly target models",
                    Configuration.DefaultModelMouseoverFriendly,
                    value => Configuration.DefaultModelMouseoverFriendly = value
                );
                ImGui.Unindent();
            }

            DrawConfigCheckbox(
                "Treat all hostile actions as mouseovers",
                Configuration.DefaultMouseoverHostile,
                value => Configuration.DefaultMouseoverHostile = value
            );

            if (Configuration.DefaultMouseoverHostile)
            {
                ImGui.Indent();
                DrawConfigCheckbox(
                    "Include hostile target models",
                    Configuration.DefaultModelMouseoverHostile,
                    value => Configuration.DefaultModelMouseoverHostile = value
                );
                ImGui.Unindent();
            }

            DrawConfigCheckbox(
                "Treat all ground-targeted actions as mouseovers",
                Configuration.DefaultMouseoverGround,
                value => Configuration.DefaultMouseoverGround = value
            );

            DrawConfigCheckbox(
                "Place all ground targets at the cursor",
                Configuration.DefaultCursorMouseover,
                value => Configuration.DefaultCursorMouseover = value
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
