using PlaytimeInsights.Models;
using System;

namespace PlaytimeInsights.ViewModels
{
    public sealed class SessionDetailViewModel
    {
        public Guid GameId { get; set; }

        public string GameName { get; set; }

        public string CoverImagePath { get; set; }

        public string StartedText { get; set; }

        public string DurationText { get; set; }

        public SessionSource Source { get; set; }

        public string SourceText { get; set; }
    }
}
