using PlaytimeInsights.ViewModels;

namespace PlaytimeInsights.Services
{
    public static class HeatmapIntensityScale
    {
        public static HeatmapIntensityLevel FromSeconds(ulong seconds)
        {
            if (seconds == 0)
            {
                return HeatmapIntensityLevel.None;
            }

            if (seconds < 3600)
            {
                return HeatmapIntensityLevel.Low;
            }

            if (seconds <= 10800)
            {
                return HeatmapIntensityLevel.Medium;
            }

            return HeatmapIntensityLevel.High;
        }
    }
}
