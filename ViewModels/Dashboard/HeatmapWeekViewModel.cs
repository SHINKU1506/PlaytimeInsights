using System.Collections.Generic;

namespace PlaytimeInsights.ViewModels
{
    public sealed class HeatmapWeekViewModel
    {
        public int ColumnIndex { get; set; }

        public string WeekLabel { get; set; }

        public IReadOnlyList<HeatmapCellViewModel> Days { get; set; }
    }
}
