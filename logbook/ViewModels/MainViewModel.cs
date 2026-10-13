using logbook.Helpers;
using logbook.Models;
using logbook.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace logbook.ViewModels
{
    public class SnkRecord : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private string _leftValue = "";
        public string LeftValue
        {
            get => _leftValue;
            set { _leftValue = value; OnPropertyChanged(nameof(LeftValue)); }
        }

        private string _rightValue = "";
        public string RightValue
        {
            get => _rightValue;
            set { _rightValue = value; OnPropertyChanged(nameof(RightValue)); }
        }

        private string _label = "";
        public string Label
        {
            get => _label;
            set { _label = value; OnPropertyChanged(nameof(Label)); }
        }
    }

    public partial class MainViewModel : ViewModelBase
    {
        // === Сервисы ===
        private readonly SettingsService _settingsService;
        private readonly StorageService _storage;

        public SettingsService SettingsService => _settingsService;

        // === Дата / экипаж / профиль ===
        private DateTime _selectedDate = DateTime.Today;
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (value.Date > DateTime.Today)
                {
                    MessageBox.Show(
                        LocalizationManager.Get("Msg_FutureDate"),
                        LocalizationManager.Get("Msg_Attention"),
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    OnPropertyChanged();
                    return;
                }

                if (value.Date < DateTime.Today.AddDays(-30))
                {
                    MessageBox.Show(
                        LocalizationManager.Get("Msg_HistoryUnavailable"),
                        LocalizationManager.Get("Msg_Attention"),
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    OnPropertyChanged();
                    return;
                }

                _selectedDate = value;
                OnPropertyChanged();
                LoadDayData();
            }
        }

        private bool _loadingProfile = true;
        private string _crew = "";
        public string Crew
        {
            get => _crew;
            set { if (_crew != value && !_loadingProfile && !_exchangeOpen) IsDirty = true; _crew = value; OnPropertyChanged(); }
        }

        private string _selectedProfile;
        public string SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    _selectedProfile = value;
                    OnPropertyChanged();
                    return;
                }

                // 1. ОТПИСЫВАЕМСЯ от старых объектов
                UnsubscribeStartRecord(Chast1StartRecord);
                UnsubscribeStartRecord(Vr80StartRecord);
                UnsubscribeStartRecord(Ta14StartRecord);
                UnsubscribeStartRecord(Vk250001StartRecord);
                UnsubscribeStartRecord(Vk250002StartRecord);

                UnsubscribeItems(Chast1Records);
                UnsubscribeItems(Vr80Records);
                UnsubscribeItems(Ta14Records);
                UnsubscribeItems(Vk250001Records);
                UnsubscribeItems(Vk250002Records);
                UnsubscribeItems(ResourceRecords);
                UnsubscribeItems(OilRecords);

                _selectedProfile = value;
                OnPropertyChanged();

                var wasLoading = _loadingProfile;
                _loadingProfile = true;
                try {
                // 2. ЗАГРУЖАЕМ новые данные
                LoadDayData();
                LoadChast1Records();
                LoadVr80Records();
                LoadTa14Records();
                LoadVk2500Coefficients();
                LoadVk250001Records();
                LoadVk250002Records();
                LoadResourceRecords();
                LoadMaintenanceData();

                // 3. ПОДПИСЫВАЕМСЯ на новые объекты
                SubscribeStartRecord(Chast1StartRecord);
                SubscribeStartRecord(Vr80StartRecord);
                SubscribeStartRecord(Ta14StartRecord);
                SubscribeStartRecord(Vk250001StartRecord);
                SubscribeStartRecord(Vk250002StartRecord);

                SubscribeItems(Chast1Records);
                SubscribeItems(Vr80Records);
                SubscribeItems(Ta14Records);
                SubscribeItems(Vk250001Records);
                SubscribeItems(Vk250002Records);
                SubscribeItems(ResourceRecords);
                SubscribeItems(OilRecords);

                UpdateDaysInProfile();
                UpdateNotifications();
                RefreshAggregateRows();
                } finally { _loadingProfile = wasLoading; _recalcTimer?.Stop(); }

            }
        }

        // === Коллекции ===
        public ObservableCollection<string> Profiles { get; set; }
            = new ObservableCollection<string>();

        public ObservableCollection<Launch> Launches { get; set; }
            = new ObservableCollection<Launch>();

        // === Часть 1 ===
        public ObservableCollection<Chast1Record> Chast1Records { get; set; }
            = new ObservableCollection<Chast1Record>();

        private Chast1Record _chast1StartRecord = new Chast1Record();
        public Chast1Record Chast1StartRecord
        {
            get => _chast1StartRecord;
            set { _chast1StartRecord = value; OnPropertyChanged(); }
        }

        // === ВР-80 ===
        public ObservableCollection<Vr80Record> Vr80Records { get; set; }
            = new ObservableCollection<Vr80Record>();

        private Vr80Record _vr80StartRecord = new Vr80Record();
        public Vr80Record Vr80StartRecord
        {
            get => _vr80StartRecord;
            set { _vr80StartRecord = value; OnPropertyChanged(); }
        }

        // === Та-14 ===
        public ObservableCollection<Ta14Record> Ta14Records { get; set; }
            = new ObservableCollection<Ta14Record>();

        private Ta14Record _ta14StartRecord = new Ta14Record();
        public Ta14Record Ta14StartRecord
        {
            get => _ta14StartRecord;
            set { _ta14StartRecord = value; OnPropertyChanged(); }
        }

        // === СНК ===
        public ObservableCollection<SnkRecord> SnkRecords { get; set; }
            = new ObservableCollection<SnkRecord>();

        // === ВК-2500-01 ===
        public ObservableCollection<Vk250001Record> Vk250001Records { get; set; }
            = new ObservableCollection<Vk250001Record>();

        private Vk250001Record _vk250001StartRecord = new Vk250001Record();
        public Vk250001Record Vk250001StartRecord
        {
            get => _vk250001StartRecord;
            set { _vk250001StartRecord = value; OnPropertyChanged(); }
        }

        private string _vk250001CoeffA = "";
        public string Vk250001CoeffA
        {
            get => _vk250001CoeffA;
            set
            {
                _vk250001CoeffA = value;
                OnPropertyChanged();
                if (!_isRecalculating) ScheduleRecalc();
            }
        }

        private string _vk250001CoeffH = "";
        public string Vk250001CoeffH
        {
            get => _vk250001CoeffH;
            set
            {
                _vk250001CoeffH = value;
                OnPropertyChanged();
                if (!_isRecalculating) ScheduleRecalc();
            }
        }

        private bool _isLeftPanelCollapsed;
        public bool IsLeftPanelCollapsed
        {
            get => _isLeftPanelCollapsed;
            set
            {
                _isLeftPanelCollapsed = value;
                OnPropertyChanged();
            }
        }

        // === ВК-2500-02 ===
        public ObservableCollection<Vk250002Record> Vk250002Records { get; set; }
            = new ObservableCollection<Vk250002Record>();

        private Vk250002Record _vk250002StartRecord = new Vk250002Record();
        public Vk250002Record Vk250002StartRecord
        {
            get => _vk250002StartRecord;
            set { _vk250002StartRecord = value; OnPropertyChanged(); }
        }

        private string _vk250002CoeffA = "";
        public string Vk250002CoeffA
        {
            get => _vk250002CoeffA;
            set
            {
                _vk250002CoeffA = value;
                OnPropertyChanged();
                if (!_isRecalculating) ScheduleRecalc();
            }
        }

        private string _vk250002CoeffH = "";
        public string Vk250002CoeffH
        {
            get => _vk250002CoeffH;
            set
            {
                _vk250002CoeffH = value;
                OnPropertyChanged();
                if (!_isRecalculating) ScheduleRecalc();
            }
        }

        // === Ресурсы ===
        public ObservableCollection<ResourceRecord> ResourceRecords { get; set; }
            = new ObservableCollection<ResourceRecord>();

        public ObservableCollection<OilRecord> OilRecords { get; set; }
            = new ObservableCollection<OilRecord>();

        public ObservableCollection<string> TermOptions { get; set; }
            = new ObservableCollection<string>
            {
                "0,5", "1", "1,5", "2", "2,5",
                "3", "3,5", "4", "4,5", "5"
            };

        // === Итоги за день ===
        private int _totalVsu;
        public int TotalVsu
        {
            get => _totalVsu;
            set { _totalVsu = value; OnPropertyChanged(); }
        }

        private int _totalZemlya;
        public int TotalZemlya
        {
            get => _totalZemlya;
            set { _totalZemlya = value; OnPropertyChanged(); }
        }

        private int _totalVozduh;
        public int TotalVozduh
        {
            get => _totalVozduh;
            set { _totalVozduh = value; OnPropertyChanged(); }
        }

        // === Команды (объявления) ===
        public ICommand AddLaunchCommand { get; private set; }
        public ICommand RemoveLaunchCommand { get; private set; }
        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand EndDayCommand { get; private set; }
        public ICommand AddProfileCommand { get; private set; }
        public ICommand DeleteProfileCommand { get; private set; }
        public ICommand HistoryCommand { get; private set; }
        public ICommand SyncCommand { get; private set; }
        public ICommand SaveStartRecordCommand { get; private set; }
        public ICommand RemoveChast1RecordCommand { get; private set; }
        public ICommand SaveVr80StartRecordCommand { get; private set; }
        public ICommand RemoveVr80RecordCommand { get; private set; }
        public ICommand SaveTa14StartRecordCommand { get; private set; }
        public ICommand RemoveTa14RecordCommand { get; private set; }
        public ICommand PrevDayCommand { get; private set; }
        public ICommand NextDayCommand { get; private set; }
        public ICommand ShowFlightModeCommand { get; private set; }
        public ICommand ShowSnkModeCommand { get; private set; }
        public ICommand SaveVk250001StartRecordCommand { get; private set; }
        public ICommand RemoveVk250001RecordCommand { get; private set; }
        public ICommand SaveVk250002StartRecordCommand { get; private set; }
        public ICommand RemoveVk250002RecordCommand { get; private set; }
        public ICommand SaveVk250001CoefficientsCommand { get; private set; }
        public ICommand SaveVk250002CoefficientsCommand { get; private set; }
        public ICommand AddResourceRecordCommand { get; private set; }
        public ICommand RemoveResourceRecordCommand { get; private set; }
        public ICommand SaveResourceRecordsCommand { get; private set; }
        public ICommand AddOilRecordCommand { get; private set; }
        public ICommand RemoveOilRecordCommand { get; private set; }
        public ICommand SaveAllCommand { get; private set; }
        public ICommand UndoCommand { get; private set; }
        public ICommand OpenSettingsCommand { get; private set; }
        public ICommand OpenNotificationsCommand { get; private set; }
        public ICommand RefreshAggregateRowsCommand { get; private set; }
        public ICommand ReplaceAggregateCommand { get; private set; }
        public ICommand OpenAggregateHistoryCommand { get; private set; }
        public ICommand OpenSetupAggregatesCommand { get; private set; }
        public ICommand ToggleLeftPanelCommand { get; private set; }

        // === Таймеры / флаги ===
        private readonly DispatcherTimer _autoSaveTimer;
        private readonly DispatcherTimer _recalcTimer;
        private bool _isRecalculating = false;

        private readonly Stack<AppState> _history = new Stack<AppState>();
        private bool _isRestoringHistory = false;

        private bool _isSnkMode;
        public bool IsSnkMode
        {
            get => _isSnkMode;
            set
            {
                _isSnkMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsFlightMode));
            }
        }

        public bool IsFlightMode => !_isSnkMode;

        // === Статус-строка ===
        private int _daysInProfile;
        public int DaysInProfile
        {
            get => _daysInProfile;
            set { _daysInProfile = value; OnPropertyChanged(); }
        }

        private string _lastSaveStatus = "";
        public string LastSaveStatus
        {
            get => _lastSaveStatus;
            set { _lastSaveStatus = value; OnPropertyChanged(); }
        }

        private void UpdateSaveStatus()
        {
            LastSaveStatus = IsDirty
                ? LocalizationManager.Get("Main_Saved")
                : LocalizationManager.Get("Main_NoSaves");
        }

        private bool _isDirty;
        public bool IsDirty
        {
            get => _isDirty;
            set { _isDirty = value; OnPropertyChanged(); }
        }

        private int _notificationLevel;
        public int NotificationLevel
        {
            get => _notificationLevel;
            set { _notificationLevel = value; OnPropertyChanged(); }
        }

        // === Вкладки ===
        private int _selectedTabIndex;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                _selectedTabIndex = value;
                OnPropertyChanged();

                if (value == 0)
                {
                    RefreshMaintenanceCalculations();
                }
                else if (value == 6)
                {
                    RefreshSNE();
                    RecalculateResources();
                }
            }
        }

        private object _selectedTabContent;
        public object SelectedTabContent
        {
            get => _selectedTabContent;
            set { _selectedTabContent = value; OnPropertyChanged(); }
        }

        // === Конструктор ===
        public MainViewModel() : this(new SettingsService()) { }

        public MainViewModel(SettingsService settings)
        {
            _settingsService = settings;

            ThemeManager.Apply(
                _settingsService.Settings.Theme,
                _settingsService.Settings.FontFamily,
                _settingsService.Settings.FontSize);

            _storage = new StorageService(_settingsService);

            var savedProfiles = _storage.GetAllProfiles();
            if (savedProfiles.Count > 0)
            {
                foreach (var profile in savedProfiles)
                    Profiles.Add(profile);
            }
            else
            {
                Profiles.Add("Новая техника");
            }

            SelectedProfile = Profiles[0];

            Launches.CollectionChanged += Launches_CollectionChanged;
            AddLaunchInternal();

            // Команды
            InitializeCommands();

            UpdateSaveStatus();

            LocalizationManager.LanguageChanged += () =>
            {
                UpdateSaveStatus();
                RefreshAggregateRows();
                RebuildTotalLabels();
            };

            // Автосохранение
            _autoSaveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(5)
            };
            _autoSaveTimer.Tick += (s, e) => AutoSaveTick();
            _autoSaveTimer.Start();

            // Пересчёт (debounce 500 мс)
            _recalcTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _recalcTimer.Tick += (s, e) =>
            {
                _recalcTimer.Stop();
                RecalculateAll();
            };

            SubscribeToRecalcSources();

            UpdateDaysInProfile();
            UpdateNotifications();

            RefreshAggregateRows();
            InitializeSnkRecords();
            LoadVk2500Coefficients();
            LoadVk250001Records();
            LoadVk250002Records();
            LoadResourceRecords();
                LoadMaintenanceData();

            if (!IsExchangeProfile) { RefreshSNE(); RecalculateResources(); }

            IsDirty = false;
            _loadingProfile = false;
            _recalcTimer?.Stop();
        }
    }
}