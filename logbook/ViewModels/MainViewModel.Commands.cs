using logbook.Models;
using System.Windows.Input;

namespace logbook.ViewModels
{
    public partial class MainViewModel
    {
        /// <summary>
        /// Инициализация всех команд. Вызывается из конструктора.
        /// </summary>
        private void InitializeCommands()
        {
            AddLaunchCommand = new RelayCommand(AddLaunch);
            RemoveLaunchCommand = new RelayCommand<Launch>(RemoveLaunch);
            SaveCommand = new RelayCommand(Save);
            EndDayCommand = new RelayCommand(EndDay);
            AddProfileCommand = new RelayCommand(AddProfile);
            DeleteProfileCommand = new RelayCommand(DeleteProfile);
            HistoryCommand = new RelayCommand(ShowHistory);
            SyncCommand = new RelayCommand(SyncNow);
            SaveStartRecordCommand = new RelayCommand(SaveStartRecord);
            RemoveChast1RecordCommand = new RelayCommand<Chast1Record>(RemoveChast1Record);
            SaveVr80StartRecordCommand = new RelayCommand(SaveVr80StartRecord);
            RemoveVr80RecordCommand = new RelayCommand<Vr80Record>(RemoveVr80Record);
            SaveTa14StartRecordCommand = new RelayCommand(SaveTa14StartRecord);
            RemoveTa14RecordCommand = new RelayCommand<Ta14Record>(RemoveTa14Record);
            PrevDayCommand = new RelayCommand(() => ChangeDay(-1));
            NextDayCommand = new RelayCommand(() => ChangeDay(1));
            ShowFlightModeCommand = new RelayCommand(() => IsSnkMode = false);
            ShowSnkModeCommand = new RelayCommand(() => IsSnkMode = true);
            SaveVk250001StartRecordCommand = new RelayCommand(SaveVk250001StartRecord);
            RemoveVk250001RecordCommand = new RelayCommand<Vk250001Record>(RemoveVk250001Record);
            SaveVk250002StartRecordCommand = new RelayCommand(SaveVk250002StartRecord);
            RemoveVk250002RecordCommand = new RelayCommand<Vk250002Record>(RemoveVk250002Record);
            SaveVk250001CoefficientsCommand = new RelayCommand(SaveVk250001Coefficients);
            SaveVk250002CoefficientsCommand = new RelayCommand(SaveVk250002Coefficients);
            AddResourceRecordCommand = new RelayCommand(AddResourceRecord);
            RemoveResourceRecordCommand = new RelayCommand<ResourceRecord>(RemoveResourceRecord);
            SaveResourceRecordsCommand = new RelayCommand(SaveResourceRecords);
            AddOilRecordCommand = new RelayCommand(AddOilRecord);
            RemoveOilRecordCommand = new RelayCommand<OilRecord>(RemoveOilRecord);
            SaveAllCommand = new RelayCommand(SaveAll);
            UndoCommand = new RelayCommand(Undo);
            OpenSettingsCommand = new RelayCommand(OpenSettings);
            OpenNotificationsCommand = new RelayCommand(OpenNotifications);
            InitializeMaintenanceCommands();
            ToggleLeftPanelCommand = new RelayCommand(ToggleLeftPanel);

            // Агрегаты
            RefreshAggregateRowsCommand = new RelayCommand(RefreshAggregateRows);
            ReplaceAggregateCommand = new RelayCommand<AggregateAssignmentRow>(OnReplaceAggregate);
            OpenAggregateHistoryCommand = new RelayCommand(OpenAggregateHistory);
            OpenSetupAggregatesCommand = new RelayCommand(OpenSetupAggregates);
        }

        // ============================================================
        // ==================== ОБРАБОТЧИКИ ===========================
        // ============================================================

        private void AddLaunch()
        {
            TakeSnapshot();
            AddLaunchInternal();
        }

        private void RemoveLaunch(Launch launch)
        {
            if (launch == null) return;

            TakeSnapshot();

            if (Launches.Count > 1)
            {
                Launches.Remove(launch);
            }
            else
            {
                // Строка одна — очищаем поля
                launch.Vsu = 0;
                launch.Zemlya = 0;
                launch.Vozduh = 0;
            }

            // Сохраняем DayData на диск — чтобы HistoryWindow видел актуальные данные
            Save();
        }
    }
}