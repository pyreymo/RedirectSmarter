using System.Collections.Generic;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using RedirectSmarter.Configuration;
using RedirectSmarter.Localization;
using RedirectSmarter.Targeting;

namespace RedirectSmarter.BatchApply
{
    internal sealed class BatchApplyWindow(RedirectTargetCatalog targetCatalog, BatchApplyService batchApplyService)
    {
        private const string PopupId = "BatchApplyRulesPopup";

        private RuleTemplateSnapshot? template;
        private Action sourceAction;
        private IReadOnlyList<BatchApplyCandidate> candidates = [];
        private readonly HashSet<uint> selectedActionIds = [];
        private BatchApplyMode applyMode = BatchApplyMode.SkipConfigured;
        private bool shouldOpen;
        private string? lastResultText;

        public bool HasUndo => batchApplyService.LastUndoSnapshot is not null;

        public void Open(Action action, Redirection redirection, IEnumerable<Action> actions)
        {
            sourceAction = action;
            template = new RuleTemplateSnapshot(action.RowId, redirection.Clone(action.RowId));
            candidates = SimilarActionFinder.FindCandidates(action, redirection, actions);

            selectedActionIds.Clear();
            foreach (var candidate in candidates.Where(candidate => candidate.Classification.HighConfidence))
            {
                selectedActionIds.Add(candidate.Action.RowId);
            }

            applyMode = BatchApplyMode.SkipConfigured;
            lastResultText = null;
            shouldOpen = true;
        }

        public void Draw()
        {
            if (shouldOpen)
            {
                ImGui.OpenPopup(PopupLabel());
                shouldOpen = false;
            }

            var open = true;
            if (!ImGui.BeginPopupModal(PopupLabel(), ref open, ImGuiWindowFlags.AlwaysAutoResize))
            {
                return;
            }

            if (template is null)
            {
                ImGui.TextDisabled(Loc.Text("BatchApply.NoSource"));
                DrawCloseButton();
                ImGui.EndPopup();
                return;
            }

            DrawSourceSummary();
            ImGui.Separator();
            DrawApplyOptions();
            ImGui.Separator();
            DrawCandidates();
            ImGui.Separator();
            DrawFooter();

            ImGui.EndPopup();
        }

        public bool UndoLastApply()
        {
            return batchApplyService.UndoLastApply();
        }

        public void ClearUndo()
        {
            batchApplyService.ClearUndo();
        }

        private void DrawSourceSummary()
        {
            ImGui.TextUnformatted(Loc.Text("BatchApply.Source", sourceAction.Name.ToString()));
            ImGui.TextDisabled(SummarizeRedirection(template!.Redirection));
        }

        private void DrawApplyOptions()
        {
            ImGui.TextUnformatted(Loc.Text("BatchApply.ConfiguredActions"));

            var skipConfigured = applyMode == BatchApplyMode.SkipConfigured;
            if (ImGui.RadioButton(Loc.Text("BatchApply.SkipConfigured"), skipConfigured))
            {
                applyMode = BatchApplyMode.SkipConfigured;
            }

            ImGui.SameLine();

            var overwrite = applyMode == BatchApplyMode.Overwrite;
            if (ImGui.RadioButton(Loc.Text("BatchApply.Overwrite"), overwrite))
            {
                applyMode = BatchApplyMode.Overwrite;
            }
        }

        private void DrawCandidates()
        {
            ImGui.TextUnformatted(Loc.Text("BatchApply.Candidates", candidates.Count));

            if (candidates.Count == 0)
            {
                ImGui.TextDisabled(Loc.Text("BatchApply.NoCandidates"));
                return;
            }

            if (ImGui.Button(Loc.Text("BatchApply.SelectAll")))
            {
                selectedActionIds.Clear();
                foreach (var candidate in candidates)
                {
                    selectedActionIds.Add(candidate.Action.RowId);
                }
            }

            ImGui.SameLine();

            if (ImGui.Button(Loc.Text("BatchApply.SelectNone")))
            {
                selectedActionIds.Clear();
            }

            var tableSize = new System.Numerics.Vector2(640f, 300f);
            if (
                !ImGui.BeginTable(
                    "batch-apply-candidates",
                    4,
                    ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY,
                    tableSize
                )
            )
            {
                return;
            }

            ImGui.TableSetupColumn("##select", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn(Loc.Text("BatchApply.Action"), ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn(Loc.Text("BatchApply.UseCase"), ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn(Loc.Text("BatchApply.Range"), ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableHeadersRow();

            foreach (var candidate in candidates)
            {
                DrawCandidateRow(candidate);
            }

            ImGui.EndTable();
        }

        private void DrawCandidateRow(BatchApplyCandidate candidate)
        {
            var actionId = candidate.Action.RowId;
            var selected = selectedActionIds.Contains(actionId);

            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            if (ImGui.Checkbox($"##candidate-{actionId}", ref selected))
            {
                if (selected)
                {
                    selectedActionIds.Add(actionId);
                }
                else
                {
                    selectedActionIds.Remove(actionId);
                }
            }

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(candidate.Action.Name.ToString());

            ImGui.TableNextColumn();
            ImGui.TextDisabled(Loc.Text($"RedirectUseCase.{candidate.Classification.UseCase}"));

            ImGui.TableNextColumn();
            ImGui.TextDisabled($"r={candidate.Classification.EffectRange}, range={candidate.Classification.Range}");
        }

        private void DrawFooter()
        {
            if (lastResultText is not null)
            {
                ImGui.TextDisabled(lastResultText);
            }

            var selectedCount = selectedActionIds.Count;
            if (selectedCount == 0)
            {
                ImGui.BeginDisabled();
            }

            if (ImGui.Button(Loc.Text("BatchApply.Apply", selectedCount)))
            {
                var result = batchApplyService.Apply(template!, selectedActionIds, applyMode);
                lastResultText = Loc.Text("BatchApply.Result", result.AppliedCount, result.SkippedCount);
            }

            if (selectedCount == 0)
            {
                ImGui.EndDisabled();
            }

            ImGui.SameLine();
            DrawCloseButton();
        }

        private static void DrawCloseButton()
        {
            if (ImGui.Button(Loc.Text("BatchApply.Close")))
            {
                ImGui.CloseCurrentPopup();
            }
        }

        private static string PopupLabel()
        {
            return $"{Loc.Text("BatchApply.Title")}##{PopupId}";
        }

        private string SummarizeRedirection(Redirection redirection)
        {
            var parts = new List<string>();
            for (var i = 0; i < redirection.Count; i++)
            {
                parts.Add(SummarizeTarget(redirection, i));
            }

            var summary = parts.Count == 0 ? Loc.Text("Redirect.None") : string.Join(" -> ", parts);
            return redirection.PreventDefault ? $"{summary} ({Loc.Text("Redirect.BlocksDefaultSummary")})" : summary;
        }

        private string SummarizeTarget(Redirection redirection, int index)
        {
            var displayName = targetCatalog.DisplayName(redirection[index]);
            if (!targetCatalog.TryGetDefinition(redirection[index], out var definition) || definition.Parameters.Count == 0)
            {
                return displayName;
            }

            var options = redirection.GetTargetOptions(index);
            var parameters = definition.Parameters.Select(parameter =>
            {
                var name = parameter.Aliases.Count > 0 ? parameter.Aliases[0] : parameter.Name;
                var value = options.Parameters.TryGetValue(parameter.Name, out var configuredValue)
                    ? configuredValue
                    : parameter.DefaultValue;
                var suffix = parameter.Kind == Targeting.Parameters.TargetParameterKind.Int ? parameter.Suffix : null;
                return $"{name}={value}{suffix}";
            });

            return $"{displayName} ({string.Join(", ", parameters)})";
        }
    }
}
