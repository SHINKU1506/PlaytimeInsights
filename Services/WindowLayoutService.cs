using System;
using System.Windows;

namespace PlaytimeInsights.Services
{
    public static class WindowLayoutService
    {
        public static Size CalculateConstrainedSize(
            double desiredWidth,
            double desiredHeight,
            double workAreaWidth,
            double workAreaHeight,
            double margin = 32)
        {
            var availableWidth = Math.Max(320, workAreaWidth - margin);
            var availableHeight = Math.Max(280, workAreaHeight - margin);
            return new Size(
                Math.Min(desiredWidth, availableWidth),
                Math.Min(desiredHeight, availableHeight));
        }

        public static void ConstrainToWorkArea(Window window, double margin = 32)
        {
            if (window == null)
            {
                return;
            }

            var workArea = SystemParameters.WorkArea;
            var size = CalculateConstrainedSize(
                window.Width,
                window.Height,
                workArea.Width,
                workArea.Height,
                margin);
            window.MaxWidth = Math.Max(window.MinWidth, workArea.Width - margin);
            window.MaxHeight = Math.Max(window.MinHeight, workArea.Height - margin);
            window.Width = Math.Max(window.MinWidth, size.Width);
            window.Height = Math.Max(window.MinHeight, size.Height);
        }
    }
}
