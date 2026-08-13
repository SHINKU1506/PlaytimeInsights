using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using PlaytimeInsights.Services;

namespace PlaytimeInsights.Converters
{
    public sealed class CoverImageConverter : IValueConverter
    {
        private const int DecodePixelWidth = 96;
        private static readonly CoverImageCache cache = new CoverImageCache(
            512,
            new CoverFileStampProvider(),
            new CoverImageDecoder());

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

        private sealed class CoverImageDecoder : ICoverImageDecoder
        {
            public BitmapSource Decode(string path, int decodePixelWidth)
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                image.DecodePixelWidth = decodePixelWidth;
                image.UriSource = new Uri(path, UriKind.Absolute);
                image.EndInit();
                image.Freeze();
                return image;
            }
        }
    }
}
