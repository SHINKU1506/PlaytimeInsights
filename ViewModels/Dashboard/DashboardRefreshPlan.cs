namespace PlaytimeInsights.ViewModels
{
    public enum DashboardRefreshReason
    {
        DataReload,
        Range,
        MetadataDimension,
        MetadataValue,
        Aggregation,
        Ranking
    }

    public enum DashboardRefreshMode
    {
        FullAnalysis,
        TrendOnly,
        RankingOnly
    }

    public sealed class DashboardRefreshPlan
    {
        public DashboardRefreshReason Reason { get; private set; }

        public DashboardRefreshMode Mode { get; private set; }

        public bool ReloadData { get; private set; }

        public bool RefreshMetadataOptions { get; private set; }

        public bool RebuildFilter { get; private set; }

        public static DashboardRefreshPlan Create(
            DashboardRefreshReason reason,
            bool cacheReady)
        {
            if (!cacheReady && reason != DashboardRefreshReason.DataReload)
            {
                reason = DashboardRefreshReason.DataReload;
            }

            switch (reason)
            {
                case DashboardRefreshReason.Aggregation:
                    return Create(reason, DashboardRefreshMode.TrendOnly);
                case DashboardRefreshReason.Ranking:
                    return Create(reason, DashboardRefreshMode.RankingOnly);
                case DashboardRefreshReason.MetadataDimension:
                    return Create(
                        reason,
                        DashboardRefreshMode.FullAnalysis,
                        false,
                        true,
                        true);
                case DashboardRefreshReason.MetadataValue:
                    return Create(
                        reason,
                        DashboardRefreshMode.FullAnalysis,
                        false,
                        false,
                        true);
                case DashboardRefreshReason.Range:
                    return Create(reason, DashboardRefreshMode.FullAnalysis);
                case DashboardRefreshReason.DataReload:
                default:
                    return Create(
                        DashboardRefreshReason.DataReload,
                        DashboardRefreshMode.FullAnalysis,
                        true,
                        true,
                        true);
            }
        }

        private static DashboardRefreshPlan Create(
            DashboardRefreshReason reason,
            DashboardRefreshMode mode,
            bool reloadData = false,
            bool refreshMetadataOptions = false,
            bool rebuildFilter = false)
        {
            return new DashboardRefreshPlan
            {
                Reason = reason,
                Mode = mode,
                ReloadData = reloadData,
                RefreshMetadataOptions = refreshMetadataOptions,
                RebuildFilter = rebuildFilter
            };
        }
    }
}
