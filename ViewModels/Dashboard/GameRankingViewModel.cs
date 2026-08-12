using System;

namespace PlaytimeInsights.ViewModels
{
    public sealed class GameRankingViewModel
    {
        public Guid GameId { get; set; }

        public int Position { get; set; }

        public string Name { get; set; }

        public string CoverImagePath { get; set; }

        public string PrimaryValueText { get; set; }

        public string DetailText { get; set; }

        public double ProgressPercent { get; set; }

        public string ProgressTooltipText { get; set; }
    }
}
