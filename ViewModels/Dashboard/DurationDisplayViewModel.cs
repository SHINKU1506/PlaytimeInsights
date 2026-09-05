using System.Linq;

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

        // True when a minor value/unit pair exists, so the presentation layer can
        // choose a template without leaving a dangling separator space behind.
        public bool HasMinorPart =>
            !string.IsNullOrEmpty(MinorValue) ||
            !string.IsNullOrEmpty(MinorUnit);

        // Separator in front of the minor group. An absent minor group renders
        // empty Runs instead of leftover spaces after the major unit.
        public string MinorSeparator => HasMinorPart ? " " : string.Empty;

        public string CompactText => string.Join(
            " ",
            new[]
            {
                MajorValue,
                MajorUnit,
                MinorValue,
                MinorUnit
            }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }
}
