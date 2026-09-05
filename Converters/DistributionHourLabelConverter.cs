using System;
using System.Globalization;
using System.Windows.Data;

namespace PlaytimeInsights.Converters
{
    // Shows an hour axis label only when its slot index divides by the layout
    // step. The label text keeps the hour part ("00"-"23"); the full "HH:mm"
    // stays in the cell tooltip. Unlabelled hours keep their bar and hit area.
    public sealed class DistributionHourLabelConverter : IMultiValueConverter
    {
        public object Convert(
            object[] values,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            var label = values[0] as string ?? string.Empty;
            var index = values.Length > 1 ? AsInt(values[1]) : 0;
            var step = values.Length > 2 ? AsInt(values[2]) : 1;
            if (parameter != null)
            {
                step = AsInt(parameter);
            }

            if (step < 1)
            {
                step = 1;
            }

            if (index % step != 0)
            {
                return string.Empty;
            }

            var separator = label.IndexOf(':');
            return separator > 0 ? label.Substring(0, separator) : label;
        }

        public object[] ConvertBack(
            object value,
            Type[] targetTypes,
            object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private static int AsInt(object value)
        {
            if (value is int)
            {
                return (int)value;
            }

            return int.TryParse(value as string, out var parsed) ? parsed : 1;
        }
    }
}
