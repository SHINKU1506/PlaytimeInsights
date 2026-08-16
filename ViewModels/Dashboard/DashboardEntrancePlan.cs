using System.Collections.Generic;

namespace PlaytimeInsights.ViewModels
{
    public sealed class DashboardEntranceStep
    {
        public DashboardEntranceStep(
            string hostName,
            double delayMilliseconds,
            double durationMilliseconds,
            double offsetY)
        {
            HostName = hostName;
            DelayMilliseconds = delayMilliseconds;
            DurationMilliseconds = durationMilliseconds;
            OffsetY = offsetY;
        }

        public string HostName { get; }

        public double DelayMilliseconds { get; }

        public double DurationMilliseconds { get; }

        public double OffsetY { get; }
    }

    public sealed class DashboardEntrancePlan
    {
        private static readonly DashboardEntranceStep[] FullSteps =
        {
            new DashboardEntranceStep("MetricCardsHost", 0d, 160d, 6d),
            new DashboardEntranceStep("TrendModule", 24d, 160d, 5d),
            new DashboardEntranceStep("RankingModule", 24d, 160d, 5d),
            new DashboardEntranceStep("DistributionModule", 48d, 160d, 5d),
            new DashboardEntranceStep("AnomalyModule", 48d, 160d, 5d)
        };

        private DashboardEntrancePlan(
            DashboardPresentationTransition transition,
            IReadOnlyList<DashboardEntranceStep> steps)
        {
            Transition = transition;
            Steps = steps;
        }

        public DashboardPresentationTransition Transition { get; }

        public IReadOnlyList<DashboardEntranceStep> Steps { get; }

        public static DashboardEntrancePlan Create(
            DashboardPresentationTransition transition,
            bool animationsEnabled)
        {
            switch (transition)
            {
                case DashboardPresentationTransition.Trend:
                    return CreateSteps(
                        transition,
                        animationsEnabled,
                        new[]
                        {
                            new DashboardEntranceStep(
                                "TrendModule",
                                0d,
                                140d,
                                4d)
                        });
                case DashboardPresentationTransition.Ranking:
                    return CreateSteps(
                        transition,
                        animationsEnabled,
                        new[]
                        {
                            new DashboardEntranceStep(
                                "RankingModule",
                                0d,
                                140d,
                                4d)
                        });
                case DashboardPresentationTransition.Full:
                    return CreateSteps(
                        transition,
                        animationsEnabled,
                        FullSteps);
                case DashboardPresentationTransition.None:
                default:
                    return new DashboardEntrancePlan(
                        transition,
                        new DashboardEntranceStep[0]);
            }
        }

        private static DashboardEntrancePlan CreateSteps(
            DashboardPresentationTransition transition,
            bool animationsEnabled,
            IEnumerable<DashboardEntranceStep> steps)
        {
            var result = new List<DashboardEntranceStep>();
            foreach (var step in steps)
            {
                result.Add(animationsEnabled
                    ? step
                    : new DashboardEntranceStep(
                        step.HostName,
                        0d,
                        0d,
                        0d));
            }

            return new DashboardEntrancePlan(transition, result);
        }
    }
}
