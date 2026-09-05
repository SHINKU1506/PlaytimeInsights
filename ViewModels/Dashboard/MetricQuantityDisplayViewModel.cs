namespace PlaytimeInsights.ViewModels
{
    // Structured display for counted metrics (sessions, active days, streaks,
    // anomaly rows). Built from the raw count and localized unit resources at the
    // same place the legacy text is produced, so nothing ever parses the formatted
    // string. The unit stays empty when a metric has no visible unit - the session
    // count must not grow a fabricated "次".
    public sealed class MetricQuantityDisplayViewModel
    {
        public MetricQuantityDisplayViewModel(
            string valueText,
            string unitText,
            string automationText)
        {
            ValueText = valueText ?? string.Empty;
            UnitText = unitText ?? string.Empty;
            AutomationText = automationText ?? string.Empty;
        }

        public string ValueText { get; }

        public string UnitText { get; }

        public string AutomationText { get; }

        public bool HasUnit => UnitText.Length > 0;

        // Separator in front of the unit. A unitless metric renders empty Runs
        // instead of a trailing space after the value.
        public string UnitSeparator => HasUnit ? " " : string.Empty;
    }
}
