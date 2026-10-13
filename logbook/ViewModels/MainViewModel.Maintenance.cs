using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using logbook.Helpers;
using logbook.Models;
using logbook.Services;

namespace logbook.ViewModels
{
    public class DefectTaskItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private readonly DefectTask _model;
        private readonly Action<DefectTaskItem> _onComplete;

        public DefectTaskItem(DefectTask model, string workInfo, Action<DefectTaskItem> onComplete)
        {
            _model = model;
            WorkInfo = workInfo;
            _onComplete = onComplete;
        }

        public DefectTask Model => _model;

        public string Id => _model.Id;
        public string Description => _model.Description;
        public string WorkId => _model.WorkId;
        public string WorkInfo { get; }
        public long CreatedAt => _model.CreatedAt;
        public long CompletedAt => _model.CompletedAt;

        public bool IsCompleted
        {
            get => _model.CompletedAt > 0L;
            set
            {
                if (value && _model.CompletedAt == 0L)
                {
                    _model.CompletedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    OnPropertyChanged(nameof(IsCompleted));
                    OnPropertyChanged(nameof(CanCheck));
                    _onComplete?.Invoke(this);
                }
            }
        }

        public bool CanCheck => _model.CompletedAt == 0L;
    }

    public partial class MainViewModel
    {
        public ObservableCollection<WorkDueItem> ScheduledWorks { get; } = new();
        public ObservableCollection<DefectTaskItem> VisibleDefects { get; } = new();

        public ICommand AddMaintenanceWorkCommand { get; private set; }
        public ICommand AddDefectTaskCommand { get; private set; }
        public ICommand MaintenanceHistoryCommand { get; private set; }
        public ICommand MaintenanceSyncCommand { get; private set; }

        public void LoadMaintenanceData()
        {
            ScheduledWorks.Clear();
            VisibleDefects.Clear();

            if (string.IsNullOrWhiteSpace(SelectedProfile)) return;

            var works = _storage.GetMaintenanceWorks(SelectedProfile);
            var tasks = _storage.GetDefectTasks(SelectedProfile);

            int fuselageMinutes = GetFuselageMinutes();
            var dues = MaintenanceJournalService.CalculateDue(works, fuselageMinutes, DateTime.Today);
            foreach (var due in dues)
            {
                ScheduledWorks.Add(due);
            }

            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var visibleTasks = tasks
                .Where(t => t.CompletedAt == 0L || (now - t.CompletedAt) < 600_000L)
                .OrderByDescending(t => t.CreatedAt)
                .ToList();

            foreach (var task in visibleTasks)
            {
                var linkedWork = works.FirstOrDefault(w => w.Id == task.WorkId);
                string workInfo = "";
                if (linkedWork != null)
                {
                    workInfo = $"{MaintenanceJournalService.GetKindTitle(linkedWork.Kind)} · {linkedWork.Date}";
                }

                VisibleDefects.Add(new DefectTaskItem(task, workInfo, OnDefectCompleted));
            }
        }

        public void RefreshMaintenanceCalculations()
        {
            if (string.IsNullOrWhiteSpace(SelectedProfile)) return;

            var works = _storage.GetMaintenanceWorks(SelectedProfile);
            int fuselageMinutes = GetFuselageMinutes();
            var dues = MaintenanceJournalService.CalculateDue(works, fuselageMinutes, DateTime.Today);

            ScheduledWorks.Clear();
            foreach (var due in dues)
            {
                ScheduledWorks.Add(due);
            }
        }

        private int GetFuselageMinutes()
        {
            string sneStr = GetLatestSNE();
            if (string.IsNullOrWhiteSpace(sneStr))
                sneStr = Chast1StartRecord?.ColSNE ?? "";

            var parsed = MaintenanceJournalService.ParseHours(sneStr);
            return parsed ?? 0;
        }

        private void OnDefectCompleted(DefectTaskItem item)
        {
            if (string.IsNullOrWhiteSpace(SelectedProfile)) return;

            var tasks = _storage.GetDefectTasks(SelectedProfile);
            var found = tasks.FirstOrDefault(t => t.Id == item.Id);
            if (found != null)
            {
                found.CompletedAt = item.CompletedAt;
                _storage.SaveDefectTasks(SelectedProfile, tasks);
            }

            RefreshMaintenanceCalculations();
        }

        private void InitializeMaintenanceCommands()
        {
            AddMaintenanceWorkCommand = new RelayCommand(OpenAddWorkDialog);
            AddDefectTaskCommand = new RelayCommand(OpenAddDefectDialog);
            MaintenanceHistoryCommand = new RelayCommand(OpenMaintenanceHistoryDialog);
            MaintenanceSyncCommand = new RelayCommand(() => DoSync(personalOnly: true));
        }

        private void OpenAddWorkDialog()
        {
            if (string.IsNullOrWhiteSpace(SelectedProfile)) return;

            int fuselageMinutes = GetFuselageMinutes();
            var dialog = new logbook.Views.AddWorkWindow(fuselageMinutes)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                var works = _storage.GetMaintenanceWorks(SelectedProfile);
                works.Add(new MaintenanceWork
                {
                    Kind = dialog.SelectedKind,
                    Date = dialog.SelectedDate.ToString("yyyy-MM-dd"),
                    CounterMinutes = dialog.CounterMinutes,
                    Description = dialog.Description?.Trim() ?? ""
                });

                _storage.SaveMaintenanceWorks(SelectedProfile, works);
                LoadMaintenanceData();
            }
        }

        private void OpenAddDefectDialog()
        {
            if (string.IsNullOrWhiteSpace(SelectedProfile)) return;

            var dialog = new logbook.Views.AddDefectWindow
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                var tasks = _storage.GetDefectTasks(SelectedProfile);
                tasks.Add(new DefectTask
                {
                    Description = dialog.Description.Trim(),
                    WorkId = ""
                });

                _storage.SaveDefectTasks(SelectedProfile, tasks);
                LoadMaintenanceData();
            }
        }

        private void OpenMaintenanceHistoryDialog()
        {
            if (string.IsNullOrWhiteSpace(SelectedProfile)) return;

            var works = _storage.GetMaintenanceWorks(SelectedProfile);
            var tasks = _storage.GetDefectTasks(SelectedProfile);

            var dialog = new logbook.Views.MaintenanceHistoryWindow(works, tasks, onClearHistory: () =>
            {
                var activeWorks = MaintenanceJournalService.FilterActiveWorks(_storage.GetMaintenanceWorks(SelectedProfile));
                _storage.SaveMaintenanceWorks(SelectedProfile, activeWorks);

                var allTasks = _storage.GetDefectTasks(SelectedProfile);
                var openTasks = allTasks.Where(t => t.CompletedAt == 0L).ToList();
                _storage.SaveDefectTasks(SelectedProfile, openTasks);

                LoadMaintenanceData();
            })
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            dialog.ShowDialog();
            LoadMaintenanceData();
        }
    }
}