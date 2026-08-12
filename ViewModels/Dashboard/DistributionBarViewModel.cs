using System.Collections.Generic;

namespace PlaytimeInsights.ViewModels
{
    public sealed class DistributionBarViewModel : ObservableObject
    {
        private bool isSelected;

        public string Label { get; set; }

        public ulong Seconds { get; set; }

        public string DurationText { get; set; }

        public string TooltipText { get; set; }

        public double BarHeight { get; set; }

        public string AutomationName { get; set; }

        public bool IsSelected
        {
            get => isSelected;
            set => SetValue(ref isSelected, value);
        }
    }
}
