using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Windowing;
using Lumina.Excel.Sheets;
using LuminaAction = Lumina.Excel.Sheets.Action;

namespace RedirectSmarter
{
    class PluginUI : Window, IDisposable
    {
        private const float IconSize = 32f;
        private const uint MaxRedirects = 12;
        private const float JobListWidth = 140f;
        private const float RedirectComboWidth = 135f;

        private System.Action ToggleConfigWindow { get; }
        private Configuration Configuration { get; }
        private ActionCatalog ActionCatalog { get; }

        private List<uint> Jobs => ActionCatalog.GetJobInfo();

        private bool selectedRoleActions;
        private uint selectedJob;
        private string search = string.Empty;

        public PluginUI(
            Configuration config,
            ActionCatalog actions,
            System.Action toggleConfigWindow
        )
            : base(Plugin.Name)
        {
            Configuration = config;
            ActionCatalog = actions;
            ToggleConfigWindow = toggleConfigWindow;

            Size = new Vector2(760, 560);
            SizeCondition = ImGuiCond.FirstUseEver;

            TitleBarButtons.Add(
                new TitleBarButton
                {
                    Icon = FontAwesomeIcon.Cog,
                    IconOffset = new Vector2(2, 1),
                    Click = _ => ToggleConfigWindow(),
                    ShowTooltip = () => ImGui.SetTooltip("Settings"),
                }
            );
        }

        public void Dispose() { }

        public override void Draw()
        {
            DrawMainLayout();
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
            ImGui.TextUnformatted("Jobs");
            ImGui.Separator();

            if (ImGui.Selectable("Role Actions", selectedRoleActions))
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

            var actions = selectedRoleActions
                ? ActionCatalog.GetRoleActions()
                : ActionCatalog.GetJobActions(selectedJob);

            var filtered = actions.Where(action => !action.IsPvP).Where(MatchesSearch).ToList();

            DrawActionToolbar(filtered.Count);
            ImGui.Spacing();
            DrawActionTable(filtered);
        }

        private static void DrawEmptyState()
        {
            var region = ImGui.GetContentRegionAvail();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + region.Y * 0.42f);

            var text = "Select a job to get started.";
            var textSize = ImGui.CalcTextSize(text);
            ImGui.SetCursorPosX(
                ImGui.GetCursorPosX() + Math.Max(0, (region.X - textSize.X) * 0.5f)
            );
            ImGui.TextUnformatted(text);
        }

        private void DrawActionToolbar(int actionCount)
        {
            ImGui.TextUnformatted(GetSelectionTitle());
            ImGui.SameLine();
            ImGui.TextDisabled($"{actionCount} actions");

            ImGui.SetNextItemWidth(-1);
            ImGui.InputTextWithHint("##search", "Search actions", ref search, 250);
        }

        private string GetSelectionTitle()
        {
            if (selectedRoleActions)
            {
                return "Role Actions";
            }

            var cjSheet = Services.DataManager.GetExcelSheet<ClassJob>()!;
            var jobRow = cjSheet.GetRow(selectedJob);
            return jobRow is { } row ? row.Name.ExtractText() : "Actions";
        }

        private bool MatchesSearch(LuminaAction action)
        {
            return search.Length == 0
                || action
                    .Name.ToString()
                    .Contains(search, StringComparison.CurrentCultureIgnoreCase);
        }

        private void DrawActionTable(IReadOnlyList<LuminaAction> actions)
        {
            var flags =
                ImGuiTableFlags.BordersInnerH
                | ImGuiTableFlags.RowBg
                | ImGuiTableFlags.Resizable
                | ImGuiTableFlags.ScrollY
                | ImGuiTableFlags.SizingFixedFit;

            if (!ImGui.BeginTable("actions", 4, flags, new Vector2(0, 0)))
            {
                return;
            }

            ImGui.TableSetupColumn("##icon", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Action", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Add", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Redirect priority", ImGuiTableColumnFlags.WidthStretch);
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

            Configuration.Redirections.TryGetValue(action.RowId, out var redirection);
            redirection ??= new() { ID = action.RowId };

            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            DrawIcon(action.Icon, iconSize);

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(action.Name.ToString());

            ImGui.TableNextColumn();
            save |= DrawAddRedirectionButton(action.RowId, redirection);

            ImGui.TableNextColumn();
            save |= DrawRedirectionPriority(action, redirection);

            if (redirection.Count > 0)
            {
                Configuration.Redirections[action.RowId] = redirection;
            }
            else
            {
                Configuration.Redirections.Remove(action.RowId);
            }

            return save;
        }

        private bool DrawAddRedirectionButton(uint actionId, Redirection redirection)
        {
            var save = false;
            var canAdd = redirection.Count < MaxRedirects;

            if (!canAdd)
            {
                ImGui.BeginDisabled();
            }

            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button($"{FontAwesomeIcon.PlusCircle.ToIconString()}##add-{actionId}"))
            {
                redirection.Priority.Add(Configuration.DefaultRedirection);
                save = true;
            }
            ImGui.PopFont();

            if (!canAdd)
            {
                ImGui.EndDisabled();
            }

            return save;
        }

        private static bool DrawRedirectionPriority(LuminaAction action, Redirection redirection)
        {
            var save = false;
            var removeIndex = -1;

            if (redirection.Count == 0)
            {
                ImGui.AlignTextToFramePadding();
                ImGui.TextDisabled("No redirects");
                return false;
            }

            for (var i = 0; i < redirection.Count; i++)
            {
                if (i > 0)
                {
                    ImGui.SameLine();
                }

                ImGui.SetNextItemWidth(RedirectComboWidth);
                if (ImGui.BeginCombo($"##redirection-{action.RowId}-{i}", redirection[i]))
                {
                    foreach (var option in RedirectTargets.All)
                    {
                        var selected = option == redirection[i];
                        if (ImGui.Selectable(option, selected))
                        {
                            redirection[i] = option;
                            save = true;
                        }

                        if (selected)
                        {
                            ImGui.SetItemDefaultFocus();
                        }
                    }

                    ImGui.EndCombo();
                }

                ImGui.SameLine();
                ImGui.PushFont(UiBuilder.IconFont);
                if (
                    ImGui.Button(
                        $"{FontAwesomeIcon.Trash.ToIconString()}##remove-{action.RowId}-{i}"
                    )
                )
                {
                    removeIndex = i;
                    save = true;
                }
                ImGui.PopFont();
            }

            if (removeIndex >= 0)
            {
                redirection.RemoveAt(removeIndex);
            }

            return save;
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
    }
}
