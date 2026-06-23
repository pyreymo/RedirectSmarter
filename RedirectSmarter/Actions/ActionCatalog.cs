using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace RedirectSmarter.Actions
{
    internal class ActionCatalog
    {
        private ExcelSheet<Action> actionSheet = null!;
        private ExcelSheet<RawRow> classJobCategories = null!;
        private IReadOnlyList<Action> roleActions = [];
        private List<uint> jobIds = [];
        private readonly Dictionary<uint, IReadOnlyList<Action>> jobActions = [];
        private bool initialized;

        public bool IsReady => initialized;

        public List<uint> GetJobInfo() => initialized ? jobIds : [];

        public IEnumerable<Action> GetJobActions(uint job) => initialized && jobActions.TryGetValue(job, out var actions) ? actions : [];

        public IEnumerable<Action> GetRoleActions() => initialized ? roleActions : [];

        public Action GetRow(uint id) => actionSheet.GetRow(id);

        public ActionCatalog()
        {
            Task.Run(Initialize)
                .ContinueWith(
                    task => Services.PluginLog.Error(task.Exception, "Failed to initialize action catalog."),
                    TaskContinuationOptions.OnlyOnFaulted
                );
        }

        private void Initialize()
        {
            actionSheet = Services.DataManager.GetExcelSheet<Action>()!;
            roleActions =
            [
                .. actionSheet.Where(action => action.IsRoleAction && action.ClassJobLevel != 0 && action.HasConfigurableTarget()),
            ];
            jobIds =
            [
                .. Services
                    .DataManager.GetExcelSheet<ClassJob>()!
                    .Where(j => j.Role > 0 && j.ItemSoulCrystal.Value.RowId > 0)
                    .Select(j => j.RowId),
            ];
            classJobCategories = Services.DataManager.GetExcelSheet<RawRow>(name: "ClassJobCategory");

            foreach (var job in jobIds)
            {
                jobActions[job] = [.. actionSheet.Where(action => IsJobActionFor(action, job))];
            }
            initialized = true;
        }

        private bool IsJobActionFor(Action action, uint job)
        {
            if (action.ClassJob.RowId + 1 == 0 || !action.IsPlayerAction || action.IsRoleAction)
            {
                return false;
            }

            var category = classJobCategories.GetRow(action.ClassJobCategory.RowId);

            return category.ReadBoolColumn((int)job + 1) && (action.HasConfigurableTarget() || action.IsExplicitlyAllowed());
        }
    }
}
