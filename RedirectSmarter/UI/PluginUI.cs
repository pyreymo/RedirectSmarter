using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Windowing;
using Lumina.Excel.Sheets;
using RedirectSmarter.Actions;
using RedirectSmarter.Configuration;
using RedirectSmarter.Localization;
using RedirectSmarter.Targeting;
using RedirectSmarter.Targeting.Parameters;
using LuminaAction = Lumina.Excel.Sheets.Action;

namespace RedirectSmarter.UI
{
    class PluginUI : Window, IDisposable
    {
        private const float IconSize = 32f;
        private const float JobListWidth = 140f;
        private const float RedirectComboWidth = 135f;
        private const float IntParameterWidth = 54f;
        private const string WindowId = "RedirectSmarter.Main";

        private PluginConfiguration Configuration { get; }
        private ActionCatalog ActionCatalog { get; }
        private RedirectTargetCatalog TargetCatalog { get; }
        private RedirectionEditor RedirectionEditor { get; }

        private List<uint> Jobs => ActionCatalog.GetJobInfo();

        private bool selectedRoleActions;
        private uint selectedJob;
        private string search = string.Empty;
        private MainTab? requestedTab;

        private enum MainTab
        {
            Actions,
            Settings,
        }

        public PluginUI(PluginConfiguration config, ActionCatalog actions, RedirectTargetCatalog targetCatalog)
            : base(Plugin.Name)
        {
            Configuration = config;
            ActionCatalog = actions;
            TargetCatalog = targetCatalog;
            RedirectionEditor = new RedirectionEditor(config);

            Size = new Vector2(760, 560);
            SizeCondition = ImGuiCond.FirstUseEver;
            UpdateWindowTitle();
        }

        public void Dispose() { }

        public override void Draw()
        {
            UpdateWindowTitle();

            if (!ImGui.BeginTabBar("main-tabs"))
            {
                return;
            }

            if (ImGui.BeginTabItem(Loc.Text("Tab.Actions"), GetTabFlags(MainTab.Actions)))
            {
                DrawMainLayout();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.Text("Tab.Settings"), GetTabFlags(MainTab.Settings)))
            {
                DrawSettings();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
            requestedTab = null;
        }

        public void ToggleSettings()
        {
            requestedTab = MainTab.Settings;
            Toggle();
        }

        public void UpdateLanguage()
        {
            UpdateWindowTitle();
        }

        private void UpdateWindowTitle()
        {
            WindowName = $"{Plugin.Name} - {Loc.Text(Configuration.EnableRedirects ? "Status.Enabled" : "Status.Disabled")}###{WindowId}";
        }

        private ImGuiTabItemFlags GetTabFlags(MainTab tab)
        {
            return requestedTab == tab ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None;
        }

        private void DrawMainLayout()
        {
            var contentSize = ImGui.GetContentRegionAvail();

            if (ImGui.BeginChild("job-list", new Vector2(JobListWidth, contentSize.Y), true))
            {
                DrawJobList();
                ImGui.EndChild();
            }

            ImGui.SameLine();

            if (ImGui.BeginChild("action-pane", new Vector2(0, contentSize.Y), false))
            {
                DrawActionPane();
                ImGui.EndChild();
            }
        }

        private void DrawJobList()
        {
            ImGui.TextUnformatted(Loc.Text("Section.Jobs"));
            ImGui.Separator();

            if (ImGui.Selectable(Loc.Text("Section.RoleActions"), selectedRoleActions))
            {
                selectedRoleActions = true;
                selectedJob = 0;
            }

            ImGui.Spacing();

            var cjSheet = Services.DataManager.GetExcelSheet<ClassJob>()!;

            foreach (var job in Jobs)
            {
                var jobRow = cjSheet.GetRow(job)!;
                var jobName = jobRow.Name.ExtractText();

                if (ImGui.Selectable($"{jobName}##job-{job}", selectedJob == job))
                {
                    selectedJob = job;
                    selectedRoleActions = false;
                }
            }
        }

        private void DrawActionPane()
        {
            if (!selectedRoleActions && selectedJob == 0)
            {
                DrawEmptyState();
                return;
            }

            var actions = selectedRoleActions ? ActionCatalog.GetRoleActions() : ActionCatalog.GetJobActions(selectedJob);

            var filtered = actions.Where(action => !action.IsPvP).Where(MatchesSearch).ToList();

            DrawActionToolbar(filtered.Count);
            ImGui.Spacing();
            DrawActionTable(filtered);
        }

        private static void DrawEmptyState()
        {
            var region = ImGui.GetContentRegionAvail();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + region.Y * 0.42f);

            var text = Loc.Text("Empty.SelectJob");
            var textSize = ImGui.CalcTextSize(text);
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Math.Max(0, (region.X - textSize.X) * 0.5f));
            ImGui.TextUnformatted(text);
        }

        private void DrawActionToolbar(int actionCount)
        {
            ImGui.TextUnformatted(GetSelectionTitle());
            ImGui.SameLine();
            ImGui.TextDisabled(Loc.Text("Action.Count", actionCount));

            ImGui.SetNextItemWidth(-1);
            ImGui.InputTextWithHint("##search", Loc.Text("Search.Actions"), ref search, 250);
        }

        private string GetSelectionTitle()
        {
            if (selectedRoleActions)
            {
                return Loc.Text("Section.RoleActions");
            }

            var cjSheet = Services.DataManager.GetExcelSheet<ClassJob>()!;
            var jobRow = cjSheet.GetRow(selectedJob);
            return jobRow is { } row ? row.Name.ExtractText() : Loc.Text("Section.Actions");
        }

        private bool MatchesSearch(LuminaAction action)
        {
            return search.Length == 0 || action.Name.ToString().Contains(search, StringComparison.CurrentCultureIgnoreCase);
        }

        private void DrawActionTable(IReadOnlyList<LuminaAction> actions)
        {
            var flags =
                ImGuiTableFlags.BordersInnerH
                | ImGuiTableFlags.RowBg
                | ImGuiTableFlags.Resizable
                | ImGuiTableFlags.ScrollY
                | ImGuiTableFlags.SizingFixedFit;

            if (!ImGui.BeginTable("actions", 5, flags, new Vector2(0, 0)))
            {
                return;
            }

            ImGui.TableSetupColumn("##icon", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn(Loc.Text("Table.Action"), ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn(Loc.Text("Table.PreventDefault"), ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn(Loc.Text("Table.Add"), ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn(Loc.Text("Table.RedirectPriority"), ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableHeadersRow();

            var save = false;

            foreach (var action in actions)
            {
                save |= DrawActionRow(action);
            }

            if (save)
            {
                Configuration.Save();
            }

            ImGui.EndTable();
        }

        private bool DrawActionRow(LuminaAction action)
        {
            var save = false;
            var iconSize = new Vector2(IconSize);

            var redirection = RedirectionEditor.GetRedirection(action.RowId);

            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            DrawIcon(action.Icon, iconSize);

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(action.Name.ToString());

            ImGui.TableNextColumn();
            save |= DrawPreventDefaultCheckbox(action.RowId, redirection);

            ImGui.TableNextColumn();
            save |= DrawAddRedirectionButton(action.RowId, redirection);

            ImGui.TableNextColumn();
            save |= DrawRedirectionPriority(action, redirection);

            RedirectionEditor.Apply(action.RowId, redirection);

            return save;
        }

        private static bool DrawPreventDefaultCheckbox(uint actionId, Redirection redirection)
        {
            var preventDefault = redirection.PreventDefault;
            if (ImGui.Checkbox($"##prevent-default-{actionId}", ref preventDefault))
            {
                return RedirectionEditor.SetPreventDefault(redirection, preventDefault);
            }

            return false;
        }

        private bool DrawAddRedirectionButton(uint actionId, Redirection redirection)
        {
            var save = false;
            var canAdd = RedirectionEditor.CanAdd(redirection);

            if (!canAdd)
            {
                ImGui.BeginDisabled();
            }

            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button($"{FontAwesomeIcon.PlusCircle.ToIconString()}##add-{actionId}"))
            {
                save = RedirectionEditor.AddDefaultTarget(redirection);
            }
            ImGui.PopFont();

            if (!canAdd)
            {
                ImGui.EndDisabled();
            }

            return save;
        }

        private bool DrawRedirectionPriority(LuminaAction action, Redirection redirection)
        {
            var save = false;
            var removeIndex = -1;

            if (redirection.Count == 0)
            {
                ImGui.AlignTextToFramePadding();
                ImGui.TextDisabled(Loc.Text("Redirect.None"));
                return false;
            }

            for (var i = 0; i < redirection.Count; i++)
            {
                if (i > 0)
                {
                    ImGui.SameLine();
                }

                ImGui.SetNextItemWidth(RedirectComboWidth);
                if (ImGui.BeginCombo($"##redirection-{action.RowId}-{i}", TargetCatalog.DisplayName(redirection[i])))
                {
                    foreach (var option in TargetCatalog.Definitions)
                    {
                        var selected = option.Id == redirection[i];
                        if (ImGui.Selectable($"{TargetCatalog.DisplayName(option.Id)}##{option.Id}", selected))
                        {
                            save |= RedirectionEditor.SetTarget(redirection, i, option.Id);
                        }

                        if (selected)
                        {
                            ImGui.SetItemDefaultFocus();
                        }
                    }

                    ImGui.EndCombo();
                }

                save |= DrawTargetOptions(action.RowId, redirection, i);

                ImGui.SameLine();
                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGui.Button($"{FontAwesomeIcon.Trash.ToIconString()}##remove-{action.RowId}-{i}"))
                {
                    removeIndex = i;
                    save = true;
                }
                ImGui.PopFont();
            }

            if (removeIndex >= 0)
            {
                save |= RedirectionEditor.RemoveAt(redirection, removeIndex);
            }

            return save;
        }

        private bool DrawTargetOptions(uint actionId, Redirection redirection, int index)
        {
            if (!TargetCatalog.TryGetDefinition(redirection[index], out var definition) || definition.Parameters.Count == 0)
            {
                return false;
            }

            var save = false;
            var options = redirection.GetTargetOptions(index);
            foreach (var parameter in definition.Parameters)
            {
                save |= DrawTargetParameter(actionId, redirection, index, options, parameter);
            }

            return save;
        }

        private static bool DrawTargetParameter(
            uint actionId,
            Redirection redirection,
            int index,
            RedirectionTargetOptions options,
            TargetParameterDefinition parameter
        )
        {
            return parameter.Kind switch
            {
                TargetParameterKind.Int => DrawIntTargetParameter(actionId, redirection, index, options, parameter),
                TargetParameterKind.Bool => DrawBoolTargetParameter(actionId, redirection, index, options, parameter),
                _ => false,
            };
        }

        private static bool DrawIntTargetParameter(
            uint actionId,
            Redirection redirection,
            int index,
            RedirectionTargetOptions options,
            TargetParameterDefinition parameter
        )
        {
            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(Loc.Text(parameter.DisplayNameKey));

            ImGui.SameLine();
            ImGui.SetNextItemWidth(IntParameterWidth);
            var value = GetIntParameterValue(options, parameter);
            if (ImGui.InputInt($"##target-param-{actionId}-{index}-{parameter.Name}", ref value, 0, 0))
            {
                return RedirectionEditor.SetTargetParameter(redirection, index, parameter, value.ToString(CultureInfo.InvariantCulture));
            }

            if (parameter.Suffix is not null)
            {
                ImGui.SameLine();
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted(parameter.Suffix);
            }

            return false;
        }

        private static bool DrawBoolTargetParameter(
            uint actionId,
            Redirection redirection,
            int index,
            RedirectionTargetOptions options,
            TargetParameterDefinition parameter
        )
        {
            ImGui.SameLine();
            var value = GetBoolParameterValue(options, parameter);
            if (ImGui.Checkbox($"{Loc.Text(parameter.DisplayNameKey)}##target-param-{actionId}-{index}-{parameter.Name}", ref value))
            {
                return RedirectionEditor.SetTargetParameter(redirection, index, parameter, value.ToString().ToLowerInvariant());
            }

            return false;
        }

        private static int GetIntParameterValue(RedirectionTargetOptions options, TargetParameterDefinition parameter)
        {
            var value = options.Parameters.TryGetValue(parameter.Name, out var configuredValue) ? configuredValue : parameter.DefaultValue;

            if (!parameter.TryNormalize(value, out var normalizedValue))
            {
                normalizedValue = parameter.DefaultValue;
            }

            return int.Parse(normalizedValue, CultureInfo.InvariantCulture);
        }

        private static bool GetBoolParameterValue(RedirectionTargetOptions options, TargetParameterDefinition parameter)
        {
            var value = options.Parameters.TryGetValue(parameter.Name, out var configuredValue) ? configuredValue : parameter.DefaultValue;

            if (!parameter.TryNormalize(value, out var normalizedValue))
            {
                normalizedValue = parameter.DefaultValue;
            }

            return bool.Parse(normalizedValue);
        }

        private static void DrawIcon(ushort id, Vector2 size = default)
        {
            var icon = new GameIconLookup(id);
            var texture = Services.TextureProvider.GetFromGameIcon(icon);
            var wrap = texture.GetWrapOrDefault();

            if (wrap is null)
            {
                return;
            }

            var drawSize = size == default ? new Vector2(wrap.Width, wrap.Height) : size;
            ImGui.Image(wrap.Handle, drawSize);
        }

        private void DrawSettings()
        {
            DrawConfigCheckbox(
                Loc.Text("Config.EnableRedirects"),
                Configuration.EnableRedirects,
                value => Configuration.EnableRedirects = value
            );

            DrawConfigCheckbox(Loc.Text("Config.IgnoreErrors"), Configuration.IgnoreErrors, value => Configuration.IgnoreErrors = value);

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
                UpdateWindowTitle();
            }
        }
    }
}
