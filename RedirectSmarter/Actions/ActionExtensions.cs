using System.Collections.Generic;
using Lumina.Excel.Sheets;

namespace RedirectSmarter.Actions
{
    internal static class ActionExtensions
    {
        private static readonly HashSet<uint> ActionAllowlist =
        [
            25822, // "Astral Flow",
            37019, // "Play I",
            37020, // "Play II",
            37021, // "Play III",
        ];

        public static bool IsExplicitlyAllowed(this Action a) => ActionAllowlist.Contains(a.RowId);

        public static bool HasConfigurableTarget(this Action a) => a.CanTargetAlly || a.CanTargetHostile || a.CanTargetParty;
    }
}
