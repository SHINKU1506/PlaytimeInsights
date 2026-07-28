using Playnite.SDK;
using Playnite.SDK.Data;
using PlaytimeInsights.Services;
using System.Collections.Generic;

namespace PlaytimeInsights
{
    public class PlaytimeInsightsSettings : ObservableObject
    {
        private bool enableSessionTracking = true;
        private bool recoverInterruptedSessions = true;
        private int recentDays = 7;
        private int topGames = 10;
        private bool useIsoWeekStart = true;

        public bool EnableSessionTracking
        {
            get => enableSessionTracking;
            set => SetValue(ref enableSessionTracking, value);
        }

        public int RecentDays
        {
            get => recentDays;
            set => SetValue(ref recentDays, value);
        }

        public bool RecoverInterruptedSessions
        {
            get => recoverInterruptedSessions;
            set => SetValue(ref recoverInterruptedSessions, value);
        }

        public int TopGames
        {
            get => topGames;
            set => SetValue(ref topGames, value);
        }

        public bool UseIsoWeekStart
        {
            get => useIsoWeekStart;
            set => SetValue(ref useIsoWeekStart, value);
        }
    }

    public class PlaytimeInsightsSettingsViewModel : ObservableObject, ISettings
    {
        private readonly PlaytimeInsights plugin;
        private PlaytimeInsightsSettings editingClone;
        private PlaytimeInsightsSettings settings;

        public PlaytimeInsightsSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                OnPropertyChanged();
            }
        }

        public PlaytimeInsightsSettingsViewModel(PlaytimeInsights plugin)
        {
            this.plugin = plugin;
            Settings = plugin.LoadPluginSettings<PlaytimeInsightsSettings>() ??
                new PlaytimeInsightsSettings();
        }

        public void BeginEdit()
        {
            editingClone = Serialization.GetClone(Settings);
        }

        public void CancelEdit()
        {
            Settings = editingClone;
        }

        public void EndEdit()
        {
            plugin.SavePluginSettings(Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();

            if (Settings.RecentDays < 1 || Settings.RecentDays > 366)
            {
                errors.Add(LocalizationService.Get(
                    "LOCPlaytimeInsightsRecentDaysValidation",
                    "自定义日期的默认回看天数必须在 1 到 366 之间。"));
            }

            if (Settings.TopGames < 1 || Settings.TopGames > 50)
            {
                errors.Add(LocalizationService.Get(
                    "LOCPlaytimeInsightsTopGamesValidation",
                    "排名数量必须在 1 到 50 之间。"));
            }

            return errors.Count == 0;
        }
    }
}
