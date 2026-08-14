namespace PlaytimeInsights.ViewModels
{
    public sealed class DurationDisplayViewModel
    {
        public DurationDisplayViewModel(
            string majorValue,
            string majorUnit,
            string minorValue,
            string minorUnit,
            string automationText)
        {
            MajorValue = majorValue ?? string.Empty;
            MajorUnit = majorUnit ?? string.Empty;
            MinorValue = minorValue ?? string.Empty;
            MinorUnit = minorUnit ?? string.Empty;
            AutomationText = automationText ?? string.Empty;
        }

        public string MajorValue { get; }

        public string MajorUnit { get; }

        public string MinorValue { get; }

        public string MinorUnit { get; }

        public string AutomationText { get; }
    }
}
