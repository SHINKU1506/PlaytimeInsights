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

        public string ShareText { get; set; }

        public string LastPlayedText { get; set; }

        public string AverageSessionText { get; set; }

        public string AverageSessionLabelText { get; set; }

        public string LongestSessionText { get; set; }

        public string LongestSessionLabelText { get; set; }

        public bool IsSparseLayout { get; set; }

        public double ProgressPercent { get; set; }
    }
}
