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
using RedirectSmarter.BatchApply;
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
        private BatchApplyWindow BatchApplyWindow { get; }

        private List<uint> Jobs => ActionCatalog.GetJobInfo();

        private bool selectedRoleActions;
        private uint selectedJob;
        private uint? selectedActionId;
        private string search = string.Empty;
        private MainTab? requestedTab;
        private bool selectCurrentJobOnOpen;

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
            var batchApplyService = new BatchApplyService(config);
            BatchApplyWindow = new BatchApplyWindow(targetCatalog, batchApplyService);

            Size = new Vector2(760, 560);
            SizeCondition = ImGuiCond.FirstUseEver;
            UpdateWindowTitle();
        }

        public void Dispose() { }

        public override void Draw()
        {
            UpdateWindowTitle();
            ApplyOpenSelection();

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
            BatchApplyWindow.Draw();
        }

        public void ToggleMain()
        {
            if (!IsOpen)
            {
                requestedTab = MainTab.Actions;
                selectCurrentJobOnOpen = true;
            }

            Toggle();
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

        private void ApplyOpenSelection()
        {
            if (!selectCurrentJobOnOpen || !ActionCatalog.IsReady)
            {
                return;
            }

            selectCurrentJobOnOpen = false;
            SelectCurrentJob();
        }

        private void SelectCurrentJob()
        {
            var currentJobId = Services.ObjectTable.LocalPlayer?.ClassJob.RowId ?? 0;
            if (currentJobId == 0 || !Jobs.Contains(currentJobId))
            {
                return;
            }

            if (!selectedRoleActions && selectedJob == currentJobId)
            {
                return;
            }

            selectedRoleActions = false;
            selectedJob = currentJobId;
            selectedActionId = null;
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
                    save |= DrawActionList(actions);
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
                    save |= DrawActionList(actions);
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

        private bool DrawActionList(IReadOnlyList<LuminaAction> actions)
        {
            var save = false;
            ImGui.TextUnformatted(Loc.Text("Section.ActionList"));
            ImGui.Separator();

            foreach (var action in actions)
            {
                save |= DrawActionListItem(action);
            }

            return save;
        }

        private bool DrawActionListItem(LuminaAction action)
        {
            var redirection = RedirectionEditor.GetRedirection(action.RowId);
            var selected = selectedActionId == action.RowId;
            var hasConfiguration = HasVisibleSummary(redirection);
            var style = ImGui.GetStyle();
            var rowPos = ImGui.GetCursorScreenPos();
            var rowWidth = ImGui.GetContentRegionAvail().X;
            var iconButtonSize = GetIconButtonSize();
            var rowHeight = Math.Max(IconSize, iconButtonSize.Y);
            var deleteReserve = hasConfiguration ? iconButtonSize.X + style.ItemSpacing.X : 0f;
            var selectableWidth = Math.Max(1f, rowWidth - deleteReserve);

            ImGui.PushID($"action-{action.RowId}");

            var clicked = ImGui.InvisibleButton("##select", new Vector2(selectableWidth, rowHeight));
            if (clicked)
            {
                selectedActionId = action.RowId;
            }

            var rowMax = rowPos + new Vector2(rowWidth, rowHeight);
            var rowHovered = ImGui.IsMouseHoveringRect(rowPos, rowMax);
            DrawActionListItemBackground(rowPos, rowMax, selected, rowHovered);
            DrawActionListItemContent(action, redirection, rowPos, rowWidth, rowHeight, deleteReserve);

            if (hasConfiguration && rowHovered)
            {
                var deleteButtonPos = new Vector2(rowMax.X - iconButtonSize.X, rowPos.Y + (rowHeight - iconButtonSize.Y) * 0.5f);
                ImGui.SetCursorScreenPos(deleteButtonPos);
                if (DrawOverlayIconButton(FontAwesomeIcon.Trash, "clear"))
                {
                    Configuration.Redirections.Remove(action.RowId);
                    BatchApplyWindow.ClearUndo();
                    ImGui.PopID();
                    return true;
                }
            }

            ImGui.SetCursorScreenPos(new Vector2(rowPos.X, rowMax.Y));

            if (hasConfiguration)
            {
                ImGui.Indent(IconSize + style.ItemSpacing.X);
                DrawDisabledWrapped(SummarizeRedirection(redirection));
                ImGui.Unindent(IconSize + style.ItemSpacing.X);
            }

            ImGui.PopID();
            return false;
        }

        private static void DrawActionListItemBackground(Vector2 min, Vector2 max, bool selected, bool hovered)
        {
            if (!selected && !hovered)
            {
                return;
            }

            var color = selected
                ? ImGui.GetColorU32(hovered ? ImGuiCol.HeaderHovered : ImGuiCol.Header)
                : ImGui.GetColorU32(ImGuiCol.HeaderHovered);

            ImGui.GetWindowDrawList().AddRectFilled(min, max, color, ImGui.GetStyle().FrameRounding);
        }

        private void DrawActionListItemContent(
            LuminaAction action,
            Redirection redirection,
            Vector2 rowPos,
            float rowWidth,
            float rowHeight,
            float trailingReserve
        )
        {
            var style = ImGui.GetStyle();
            var iconPos = rowPos + new Vector2(0f, (rowHeight - IconSize) * 0.5f);
            DrawIconAt(action.Icon, iconPos, new Vector2(IconSize));

            var text = action.Name.ToString();
            var textPos = new Vector2(rowPos.X + IconSize + style.ItemSpacing.X, rowPos.Y + (rowHeight - ImGui.GetTextLineHeight()) * 0.5f);

            var drawList = ImGui.GetWindowDrawList();
            var clipMin = new Vector2(textPos.X, rowPos.Y);
            var clipMax = new Vector2(
                Math.Max(clipMin.X + 1f, rowPos.X + rowWidth - trailingReserve - style.ItemSpacing.X),
                rowPos.Y + rowHeight
            );
            drawList.PushClipRect(clipMin, clipMax, true);
            drawList.AddText(textPos, ImGui.GetColorU32(ImGuiCol.Text), text);

            if (HasVisibleSummary(redirection))
            {
                var textSize = ImGui.CalcTextSize(text);
                drawList.AddText(
                    new Vector2(textPos.X + textSize.X + style.ItemSpacing.X, textPos.Y),
                    ImGui.GetColorU32(ImGuiCol.TextDisabled),
                    Loc.Text("Redirect.EnabledBadge")
                );
            }

            drawList.PopClipRect();
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
            ImGui.SameLine();
            DrawBatchApplyButton(action, redirection);
            ImGui.SameLine();
            save |= DrawUndoBatchApplyButton();
            ImGui.Spacing();

            save |= DrawTargetRuleStack(action.RowId, redirection);
            RedirectionEditor.Apply(action.RowId, redirection);

            return save;
        }

        private void DrawBatchApplyButton(LuminaAction action, Redirection redirection)
        {
            var canApply = redirection.Count > 0 || redirection.PreventDefault;
            if (DrawButtonDisabledIf($"{Loc.Text("BatchApply.Open")}##batch-apply-{action.RowId}", !canApply))
            {
                BatchApplyWindow.Open(action, redirection, ActionCatalog.GetAllActions());
            }
        }

        private bool DrawUndoBatchApplyButton()
        {
            return DrawButtonDisabledIf(Loc.Text("BatchApply.Undo"), !BatchApplyWindow.HasUndo) && BatchApplyWindow.UndoLastApply();
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
            return DrawButtonDisabledIf($"{Loc.Text("Redirect.AddTargetRule")}##add-{actionId}", !RedirectionEditor.CanAdd(redirection))
                && RedirectionEditor.AddDefaultTarget(redirection);
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

            var ruleItemSpacing = ImGui.GetStyle().ItemSpacing;
            for (var i = 0; i < redirection.Count; i++)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(ruleItemSpacing.X, 0f));
                save |= DrawTargetRuleBlock(actionId, redirection, i, ruleItemSpacing, out var requestedMoveTo, out var requestedRemove);
                ImGui.PopStyleVar();

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
            Vector2 contentItemSpacing,
            out int requestedMoveTo,
            out bool requestedRemove
        )
        {
            requestedMoveTo = -1;
            requestedRemove = false;

            TargetCatalog.TryGetDefinition(redirection[index], out var definition);
            var style = ImGui.GetStyle();
            var blockMin = ImGui.GetCursorScreenPos();
            var blockWidth = ImGui.GetContentRegionAvail().X;

            ImGui.PushID($"rule-{actionId}-{index}");
            ImGui.BeginGroup();
            ImGui.SetCursorPos(new Vector2(ImGui.GetCursorPosX() + style.WindowPadding.X, ImGui.GetCursorPosY() + style.WindowPadding.Y));

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, contentItemSpacing);
            var save = DrawRuleHeader(actionId, redirection, index, out requestedMoveTo, out requestedRemove);

            if (definition is { Parameters.Count: > 0 })
            {
                save |= DrawTargetOptions(actionId, redirection, index);
            }

            ImGui.PopStyleVar();
            ImGui.Dummy(new Vector2(0, style.WindowPadding.Y));
            ImGui.EndGroup();

            var blockMax = new Vector2(blockMin.X + blockWidth, ImGui.GetItemRectMax().Y);
            ImGui.GetWindowDrawList().AddRect(blockMin, blockMax, ImGui.GetColorU32(ImGuiCol.Border));
            ImGui.PopID();
            return save;
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
            if (DrawIconButton(FontAwesomeIcon.ArrowUp, "up", index == 0))
            {
                requestedMoveTo = index - 1;
            }

            ImGui.SameLine();
            if (DrawIconButton(FontAwesomeIcon.ArrowDown, "down", index == redirection.Count - 1))
            {
                requestedMoveTo = index + 1;
            }

            ImGui.SameLine();
            if (DrawIconButton(FontAwesomeIcon.Trash, "remove"))
            {
                requestedRemove = true;
                save = true;
            }

            return save;
        }

        private static Vector2 GetIconButtonSize() => new(ImGui.GetFrameHeight());

        private static bool DrawButtonDisabledIf(string label, bool disabled)
        {
            if (disabled)
            {
                ImGui.BeginDisabled();
            }

            var clicked = ImGui.Button(label);

            if (disabled)
            {
                ImGui.EndDisabled();
            }

            return clicked && !disabled;
        }

        private static bool DrawIconButton(FontAwesomeIcon icon, string id, bool disabled = false)
        {
            if (disabled)
            {
                ImGui.BeginDisabled();
            }

            ImGui.PushFont(UiBuilder.IconFont);
            var clicked = ImGui.Button($"{icon.ToIconString()}##{id}", GetIconButtonSize());
            ImGui.PopFont();

            if (disabled)
            {
                ImGui.EndDisabled();
            }

            return clicked && !disabled;
        }

        private static bool DrawOverlayIconButton(FontAwesomeIcon icon, string id)
        {
            var style = ImGui.GetStyle();
            var size = GetIconButtonSize();
            var pos = ImGui.GetCursorScreenPos();
            var iconText = icon.ToIconString();

            var clicked = ImGui.InvisibleButton($"##{id}", size);
            var hovered = ImGui.IsItemHovered();
            var active = ImGui.IsItemActive();
            var drawList = ImGui.GetWindowDrawList();

            if (hovered || active)
            {
                drawList.AddRectFilled(
                    pos,
                    pos + size,
                    ImGui.GetColorU32(active ? ImGuiCol.FrameBgActive : ImGuiCol.FrameBgHovered),
                    style.FrameRounding
                );
            }

            ImGui.PushFont(UiBuilder.IconFont);
            var iconSize = ImGui.CalcTextSize(iconText);
            ImGui.PopFont();

            var iconPos = pos + (size - iconSize) * 0.5f;
            var iconColor = ImGui.GetColorU32(hovered || active ? ImGuiCol.Text : ImGuiCol.TextDisabled);

            ImGui.PushFont(UiBuilder.IconFont);
            drawList.AddText(iconPos, iconColor, iconText);
            ImGui.PopFont();

            return clicked;
        }

        private static bool DrawDestructiveButton(string label, string id)
        {
            PushDestructiveButtonColors();
            var clicked = ImGui.Button($"{label}##{id}");
            ImGui.PopStyleColor(3);
            return clicked;
        }

        private static float GetRuleTargetComboWidth()
        {
            var style = ImGui.GetStyle();
            var buttonWidth = ImGui.GetFrameHeight();
            var reservedWidth = buttonWidth * 3 + style.ItemSpacing.X * 4;
            return Math.Max(160f, ImGui.GetContentRegionAvail().X - reservedWidth - style.WindowPadding.X);
        }

        private bool DrawTargetOptions(uint actionId, Redirection redirection, int index)
        {
            if (!TargetCatalog.TryGetDefinition(redirection[index], out var definition) || definition.Parameters.Count == 0)
            {
                return false;
            }

            var save = false;
            var options = redirection.GetTargetOptions(index);
            var indent = GetTargetParameterIndent();

            ImGui.Indent(indent);
            if (ImGui.BeginTable("target-params", 3, ImGuiTableFlags.SizingFixedFit))
            {
                ImGui.TableSetupColumn("label", ImGuiTableColumnFlags.WidthFixed, ParameterLabelWidth);
                ImGui.TableSetupColumn("value", ImGuiTableColumnFlags.WidthFixed, IntParameterWidth);
                ImGui.TableSetupColumn("suffix", ImGuiTableColumnFlags.WidthStretch);

                foreach (var parameter in definition.Parameters)
                {
                    save |= DrawTargetParameterRow(actionId, redirection, index, options, parameter);
                }

                ImGui.EndTable();
            }
            ImGui.Unindent(indent);

            return save;
        }

        private static float GetTargetParameterIndent()
        {
            var style = ImGui.GetStyle();
            return Math.Max(style.FramePadding.X * 2f, style.IndentSpacing * 0.75f);
        }

        private static bool DrawTargetParameterRow(
            uint actionId,
            Redirection redirection,
            int index,
            RedirectionTargetOptions options,
            TargetParameterDefinition parameter
        )
        {
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(Loc.Text(parameter.DisplayNameKey));

            ImGui.TableSetColumnIndex(1);
            var save = parameter.Kind switch
            {
                TargetParameterKind.Int => DrawIntTargetParameterControl(actionId, redirection, index, options, parameter),
                TargetParameterKind.Bool => DrawBoolTargetParameterControl(actionId, redirection, index, options, parameter),
                _ => false,
            };

            if (parameter.Kind == TargetParameterKind.Int && parameter.Suffix is not null)
            {
                ImGui.TableSetColumnIndex(2);
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted(parameter.Suffix);
            }

            return save;
        }

        private static bool DrawIntTargetParameterControl(
            uint actionId,
            Redirection redirection,
            int index,
            RedirectionTargetOptions options,
            TargetParameterDefinition parameter
        )
        {
            ImGui.SetNextItemWidth(IntParameterWidth);
            var value = GetIntParameterValue(options, parameter);
            return ImGui.InputInt($"##target-param-{actionId}-{index}-{parameter.Name}", ref value, 0, 0)
                && RedirectionEditor.SetTargetParameter(redirection, index, parameter, value.ToString(CultureInfo.InvariantCulture));
        }

        private static bool DrawBoolTargetParameterControl(
            uint actionId,
            Redirection redirection,
            int index,
            RedirectionTargetOptions options,
            TargetParameterDefinition parameter
        )
        {
            var value = GetBoolParameterValue(options, parameter);
            return ImGui.Checkbox($"##target-param-{actionId}-{index}-{parameter.Name}", ref value)
                && RedirectionEditor.SetTargetParameter(redirection, index, parameter, value.ToString().ToLowerInvariant());
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

        private static void DrawIconAt(ushort id, Vector2 pos, Vector2 size)
        {
            var icon = new GameIconLookup(id);
            var texture = Services.TextureProvider.GetFromGameIcon(icon);
            var wrap = texture.GetWrapOrDefault();

            if (wrap is null)
            {
                return;
            }

            ImGui.GetWindowDrawList().AddImage(wrap.Handle, pos, pos + size);
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

            ImGui.Separator();
            DrawClearAllRedirectionsButton();
        }

        private void DrawClearAllRedirectionsButton()
        {
            var redirectionCount = Configuration.Redirections.Count;
            if (redirectionCount == 0)
            {
                ImGui.BeginDisabled();
            }

            if (DrawDestructiveButton(Loc.Text("Config.ClearAllRedirections", redirectionCount), "clear-all-redirections"))
            {
                Configuration.Redirections.Clear();
                BatchApplyWindow.ClearUndo();
                Configuration.Save();
            }

            if (redirectionCount == 0)
            {
                ImGui.EndDisabled();
            }
        }

        private static void PushDestructiveButtonColors()
        {
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.62f, 0.16f, 0.16f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.78f, 0.22f, 0.22f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.50f, 0.10f, 0.10f, 1f));
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
