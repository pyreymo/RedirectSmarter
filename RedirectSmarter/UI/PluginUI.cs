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
        private const float ActionListMinWidth = 240f;
        private const float ActionListMaxWidth = 340f;
        private const float StackedActionLayoutBreakpoint = 680f;
        private const float ParameterLabelWidth = 120f;
        private const float IntParameterWidth = 54f;
        private const string WindowId = "RedirectSmarter.Main";

        private PluginConfiguration Configuration { get; }
        private ActionCatalog ActionCatalog { get; }
        private RedirectTargetCatalog TargetCatalog { get; }
        private RedirectionEditor RedirectionEditor { get; }

        private List<uint> Jobs => ActionCatalog.GetJobInfo();

        private bool selectedRoleActions;
        private uint selectedJob;
        private uint? selectedActionId;
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
                selectedActionId = null;
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
                    selectedActionId = null;
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
            DrawActionBrowser(filtered);
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

        private void DrawActionBrowser(IReadOnlyList<LuminaAction> actions)
        {
            EnsureSelectedAction(actions);

            if (actions.Count == 0)
            {
                DrawNoActionsState();
                return;
            }

            var available = ImGui.GetContentRegionAvail();
            var save = false;

            if (available.X < StackedActionLayoutBreakpoint)
            {
                var listHeight = Math.Min(220f, Math.Max(140f, available.Y * 0.38f));
                if (ImGui.BeginChild("action-list", new Vector2(0, listHeight), true))
                {
                    DrawActionList(actions);
                    ImGui.EndChild();
                }

                ImGui.Spacing();

                if (ImGui.BeginChild("selected-action-editor", new Vector2(0, 0), false))
                {
                    save |= DrawSelectedActionEditor(actions);
                    ImGui.EndChild();
                }
            }
            else
            {
                var listWidth = Math.Clamp(available.X * 0.34f, ActionListMinWidth, ActionListMaxWidth);
                if (ImGui.BeginChild("action-list", new Vector2(listWidth, 0), true))
                {
                    DrawActionList(actions);
                    ImGui.EndChild();
                }

                ImGui.SameLine();

                if (ImGui.BeginChild("selected-action-editor", new Vector2(0, 0), false))
                {
                    save |= DrawSelectedActionEditor(actions);
                    ImGui.EndChild();
                }
            }

            if (save)
            {
                Configuration.Save();
            }
        }

        private void DrawActionList(IReadOnlyList<LuminaAction> actions)
        {
            ImGui.TextUnformatted(Loc.Text("Section.ActionList"));
            ImGui.Separator();

            foreach (var action in actions)
            {
                DrawActionListItem(action);
            }
        }

        private void DrawActionListItem(LuminaAction action)
        {
            var redirection = RedirectionEditor.GetRedirection(action.RowId);
            var selected = selectedActionId == action.RowId;
            var iconSize = new Vector2(IconSize);

            ImGui.PushID($"action-{action.RowId}");
            DrawIcon(action.Icon, iconSize);
            ImGui.SameLine();

            var label = action.Name.ToString();
            if (redirection.Count > 0 || redirection.PreventDefault)
            {
                label = $"{label}  {Loc.Text("Redirect.EnabledBadge")}";
            }

            if (ImGui.Selectable($"{label}##select", selected))
            {
                selectedActionId = action.RowId;
            }

            if (HasVisibleSummary(redirection))
            {
                ImGui.Indent(IconSize + ImGui.GetStyle().ItemSpacing.X);
                DrawDisabledWrapped(SummarizeRedirection(redirection));
                ImGui.Unindent(IconSize + ImGui.GetStyle().ItemSpacing.X);
            }

            ImGui.PopID();
        }

        private bool DrawSelectedActionEditor(IReadOnlyList<LuminaAction> actions)
        {
            if (!TryGetSelectedAction(actions, out var action))
            {
                DrawSelectActionState();
                return false;
            }

            var redirection = RedirectionEditor.GetRedirection(action.RowId);
            var save = false;

            DrawActionEditorHeader(action);
            ImGui.Spacing();
            save |= DrawPreventDefaultCheckbox(action.RowId, redirection);
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.TextUnformatted(Loc.Text("Section.TargetSelectionOrder"));
            ImGui.SameLine();
            save |= DrawAddTargetRuleButton(action.RowId, redirection);
            ImGui.Spacing();

            save |= DrawTargetRuleStack(action.RowId, redirection);
            RedirectionEditor.Apply(action.RowId, redirection);

            return save;
        }

        private static void DrawActionEditorHeader(LuminaAction action)
        {
            DrawIcon(action.Icon, new Vector2(IconSize));
            ImGui.SameLine();

            ImGui.BeginGroup();
            ImGui.TextUnformatted(action.Name.ToString());
            ImGui.TextDisabled($"#{action.RowId}");
            ImGui.EndGroup();
        }

        private static bool DrawPreventDefaultCheckbox(uint actionId, Redirection redirection)
        {
            var preventDefault = redirection.PreventDefault;
            if (ImGui.Checkbox($"{Loc.Text("Redirect.BlockOriginalTarget")}##prevent-default-{actionId}", ref preventDefault))
            {
                return RedirectionEditor.SetPreventDefault(redirection, preventDefault);
            }

            return false;
        }

        private bool DrawAddTargetRuleButton(uint actionId, Redirection redirection)
        {
            var canAdd = RedirectionEditor.CanAdd(redirection);

            if (!canAdd)
            {
                ImGui.BeginDisabled();
            }

            var save =
                ImGui.Button($"{Loc.Text("Redirect.AddTargetRule")}##add-{actionId}") && RedirectionEditor.AddDefaultTarget(redirection);

            if (!canAdd)
            {
                ImGui.EndDisabled();
            }

            return save;
        }

        private bool DrawTargetRuleStack(uint actionId, Redirection redirection)
        {
            var save = false;
            var removeIndex = -1;
            var moveFrom = -1;
            var moveTo = -1;

            if (redirection.Count == 0)
            {
                ImGui.TextDisabled(Loc.Text("Redirect.NoTargetRules"));
                return false;
            }

            for (var i = 0; i < redirection.Count; i++)
            {
                save |= DrawTargetRuleBlock(actionId, redirection, i, out var requestedMoveTo, out var requestedRemove);
                if (requestedMoveTo >= 0)
                {
                    moveFrom = i;
                    moveTo = requestedMoveTo;
                }

                if (requestedRemove)
                {
                    removeIndex = i;
                }
            }

            if (removeIndex >= 0)
            {
                save |= RedirectionEditor.RemoveAt(redirection, removeIndex);
            }
            else if (moveFrom >= 0 && moveTo >= 0)
            {
                save |= RedirectionEditor.Move(redirection, moveFrom, moveTo);
            }

            return save;
        }

        private bool DrawTargetRuleBlock(
            uint actionId,
            Redirection redirection,
            int index,
            out int requestedMoveTo,
            out bool requestedRemove
        )
        {
            requestedMoveTo = -1;
            requestedRemove = false;

            TargetCatalog.TryGetDefinition(redirection[index], out var definition);
            var parameterCount = definition?.Parameters.Count ?? 0;
            var blockHeight = GetRuleBlockHeight(parameterCount);

            ImGui.PushID($"rule-{actionId}-{index}");
            if (ImGui.BeginChild("rule-block", new Vector2(0, blockHeight), true))
            {
                var save = DrawRuleHeader(actionId, redirection, index, out requestedMoveTo, out requestedRemove);

                if (definition is { Parameters.Count: > 0 })
                {
                    ImGui.Spacing();
                    save |= DrawTargetOptions(actionId, redirection, index);
                }

                ImGui.EndChild();
                ImGui.PopID();
                return save;
            }

            ImGui.EndChild();
            ImGui.PopID();
            return false;
        }

        private bool DrawRuleHeader(uint actionId, Redirection redirection, int index, out int requestedMoveTo, out bool requestedRemove)
        {
            requestedMoveTo = -1;
            requestedRemove = false;
            var save = false;

            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted($"{index + 1}.");
            ImGui.SameLine();

            ImGui.SetNextItemWidth(GetRuleTargetComboWidth());
            if (ImGui.BeginCombo("##target", TargetCatalog.DisplayName(redirection[index])))
            {
                foreach (var option in TargetCatalog.Definitions)
                {
                    var selected = option.Id == redirection[index];
                    if (ImGui.Selectable($"{TargetCatalog.DisplayName(option.Id)}##{option.Id}", selected))
                    {
                        save |= RedirectionEditor.SetTarget(redirection, index, option.Id);
                    }

                    if (selected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.SameLine();
            if (DrawMoveButton(FontAwesomeIcon.ArrowUp, "up", index == 0))
            {
                requestedMoveTo = index - 1;
            }

            ImGui.SameLine();
            if (DrawMoveButton(FontAwesomeIcon.ArrowDown, "down", index == redirection.Count - 1))
            {
                requestedMoveTo = index + 1;
            }

            ImGui.SameLine();

            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button($"{FontAwesomeIcon.Trash.ToIconString()}##remove"))
            {
                requestedRemove = true;
                save = true;
            }
            ImGui.PopFont();

            return save;
        }

        private static bool DrawMoveButton(FontAwesomeIcon icon, string id, bool disabled)
        {
            if (disabled)
            {
                ImGui.BeginDisabled();
            }

            ImGui.PushFont(UiBuilder.IconFont);
            var clicked = ImGui.Button($"{icon.ToIconString()}##{id}");
            ImGui.PopFont();

            if (disabled)
            {
                ImGui.EndDisabled();
            }

            return clicked && !disabled;
        }

        private static float GetRuleTargetComboWidth()
        {
            var style = ImGui.GetStyle();
            var buttonWidth = ImGui.GetFrameHeight();
            var reservedWidth = buttonWidth * 3 + style.ItemSpacing.X * 4;
            return Math.Max(160f, ImGui.GetContentRegionAvail().X - reservedWidth);
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
            var startX = ImGui.GetCursorPosX();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(Loc.Text(parameter.DisplayNameKey));

            ImGui.SameLine(startX + ParameterLabelWidth);
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
            var startX = ImGui.GetCursorPosX();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(Loc.Text(parameter.DisplayNameKey));

            ImGui.SameLine(startX + ParameterLabelWidth);
            var value = GetBoolParameterValue(options, parameter);
            if (ImGui.Checkbox($"##target-param-{actionId}-{index}-{parameter.Name}", ref value))
            {
                return RedirectionEditor.SetTargetParameter(redirection, index, parameter, value.ToString().ToLowerInvariant());
            }

            return false;
        }

        private void EnsureSelectedAction(IReadOnlyList<LuminaAction> actions)
        {
            if (actions.Count == 0)
            {
                selectedActionId = null;
                return;
            }

            if (selectedActionId is not null && actions.Any(action => action.RowId == selectedActionId.Value))
            {
                return;
            }

            selectedActionId = actions[0].RowId;
        }

        private bool TryGetSelectedAction(IReadOnlyList<LuminaAction> actions, out LuminaAction selectedAction)
        {
            if (selectedActionId is not null)
            {
                foreach (var action in actions)
                {
                    if (action.RowId == selectedActionId.Value)
                    {
                        selectedAction = action;
                        return true;
                    }
                }
            }

            selectedAction = default;
            return false;
        }

        private static void DrawNoActionsState()
        {
            ImGui.TextDisabled(Loc.Text("Empty.NoActions"));
        }

        private static void DrawSelectActionState()
        {
            ImGui.TextDisabled(Loc.Text("Empty.SelectAction"));
        }

        private static void DrawDisabledWrapped(string text)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled]);
            ImGui.PushTextWrapPos();
            ImGui.TextUnformatted(text);
            ImGui.PopTextWrapPos();
            ImGui.PopStyleColor();
        }

        private static float GetRuleBlockHeight(int parameterCount)
        {
            return Math.Max(70f, ImGui.GetFrameHeightWithSpacing() * (parameterCount + 2) + ImGui.GetStyle().WindowPadding.Y);
        }

        private string SummarizeRedirection(Redirection redirection)
        {
            var parts = new List<string>();

            for (var i = 0; i < redirection.Count; i++)
            {
                parts.Add(SummarizeTarget(redirection, i));
            }

            if (parts.Count == 0)
            {
                return redirection.PreventDefault ? Loc.Text("Redirect.BlocksDefaultSummary") : Loc.Text("Redirect.None");
            }

            var summary = string.Join(" -> ", parts);
            return redirection.PreventDefault ? $"{summary} ({Loc.Text("Redirect.BlocksDefaultSummary")})" : summary;
        }

        private static bool HasVisibleSummary(Redirection redirection)
        {
            return redirection.Count > 0 || redirection.PreventDefault;
        }

        private string SummarizeTarget(Redirection redirection, int index)
        {
            var displayName = TargetCatalog.DisplayName(redirection[index]);
            if (!TargetCatalog.TryGetDefinition(redirection[index], out var definition) || definition.Parameters.Count == 0)
            {
                return displayName;
            }

            var options = redirection.GetTargetOptions(index);
            var parameters = definition.Parameters.Select(parameter => SummarizeParameter(options, parameter));
            return $"{displayName} ({string.Join(", ", parameters)})";
        }

        private static string SummarizeParameter(RedirectionTargetOptions options, TargetParameterDefinition parameter)
        {
            var name = parameter.Aliases.Count > 0 ? parameter.Aliases[0] : parameter.Name;
            var value = GetParameterValue(options, parameter);
            var suffix = parameter.Kind == TargetParameterKind.Int ? parameter.Suffix : null;
            return $"{name}={value}{suffix}";
        }

        private static int GetIntParameterValue(RedirectionTargetOptions options, TargetParameterDefinition parameter)
        {
            return int.Parse(GetParameterValue(options, parameter), CultureInfo.InvariantCulture);
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

        private static string GetParameterValue(RedirectionTargetOptions options, TargetParameterDefinition parameter)
        {
            var value = options.Parameters.TryGetValue(parameter.Name, out var configuredValue) ? configuredValue : parameter.DefaultValue;
            return parameter.TryNormalize(value, out var normalizedValue) ? normalizedValue : parameter.DefaultValue;
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
