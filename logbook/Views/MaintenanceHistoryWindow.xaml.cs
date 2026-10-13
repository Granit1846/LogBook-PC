using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using logbook.Helpers;
using logbook.Models;
using logbook.Services;

namespace logbook.Views
{
    public partial class MaintenanceHistoryWindow : Window
    {
        public class WorkHistoryDisplayItem
        {
            public string KindTitle { get; set; } = "";
            public string Details { get; set; } = "";
            public string Description { get; set; } = "";
            public Visibility DescriptionVisibility => string.IsNullOrWhiteSpace(Description) ? Visibility.Collapsed : Visibility.Visible;
        }

        public class CompletedTaskDisplayItem
        {
            public string Description { get; set; } = "";
            public string WorkInfo { get; set; } = "";
            public Visibility WorkInfoVisibility => string.IsNullOrWhiteSpace(WorkInfo) ? Visibility.Collapsed : Visibility.Visible;
        }

        private readonly Action _onClearHistory;
        private readonly List<MaintenanceWork> _works;
        private readonly List<DefectTask> _tasks;

        public MaintenanceHistoryWindow(List<MaintenanceWork> works, List<DefectTask> tasks, Action onClearHistory)
        {
            InitializeComponent();
            _works = works ?? new List<MaintenanceWork>();
            _tasks = tasks ?? new List<DefectTask>();
            _onClearHistory = onClearHistory;

            Loaded += (s, e) =>
            {
                DarkTitleBarHelper.Apply(this);
                BindData();
            };
        }

        private void BindData()
        {
            var sortedWorks = _works
                .OrderByDescending(w => DateTime.TryParse(w.Date, out var dt) ? dt : DateTime.MinValue)
                .ThenByDescending(w => w.CreatedAt)
                .Select(w =>
                {
                    string dateStr = DateTime.TryParse(w.Date, out var dt) ? dt.ToString("dd.MM.yyyy") : w.Date;
                    return new WorkHistoryDisplayItem
                    {
                        KindTitle = MaintenanceJournalService.GetKindTitle(w.Kind),
                        Details = $"{dateStr} · {MaintenanceJournalService.FormatHours(w.CounterMinutes)}",
                        Description = w.Description
                    };
                })
                .ToList();

            WorksItemsControl.ItemsSource = sortedWorks;
            EmptyWorksText.Visibility = sortedWorks.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            var completedTasks = _tasks
                .Where(t => t.CompletedAt > 0L)
                .OrderByDescending(t => t.CompletedAt)
                .Select(t =>
                {
                    var linked = _works.FirstOrDefault(w => w.Id == t.WorkId);
                    string linkedInfo = linked != null ? $"{MaintenanceJournalService.GetKindTitle(linked.Kind)} · {linked.Date}" : "";
                    return new CompletedTaskDisplayItem
                    {
                        Description = t.Description,
                        WorkInfo = linkedInfo
                    };
                })
                .ToList();

            CompletedTasksItemsControl.ItemsSource = completedTasks;
            EmptyDefectsText.Visibility = completedTasks.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            ClearHistoryButton.Visibility = (sortedWorks.Count > 0 || completedTasks.Count > 0) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                LocalizationManager.Get("Journal_ClearHistoryConfirm"),
                LocalizationManager.Get("Journal_History"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _onClearHistory?.Invoke();
                Close();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}