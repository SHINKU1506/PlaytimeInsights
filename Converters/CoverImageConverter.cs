using System;
using System.Globalization;
using System.Windows.Data;
using PlaytimeInsights.Services;

namespace PlaytimeInsights.Converters
{
    public sealed class CoverImageConverter : IValueConverter
    {
        private const int DecodePixelWidth = 96;
        private static readonly CoverImageCache cache = new CoverImageCache(512);

        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            return cache.GetOrLoad(value as string, DecodePixelWidth);
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
