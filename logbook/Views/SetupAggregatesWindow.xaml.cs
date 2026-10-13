using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using logbook.Helpers;
using logbook.Models;
using logbook.Services;

namespace logbook.Views
{
    public partial class SetupAggregatesWindow : Window
    {
        private readonly StorageService _storage;
        private readonly SettingsService _settingsService;
        private readonly string _profile;

        /// <summary>
        /// true — пользователь нажал «Подтвердить».
        /// </summary>
        public bool Saved { get; private set; }

        /// <summary>
        /// Соответствие: TextBox → ключ привязки.
        /// </summary>
        private Dictionary<TextBox, string> _boxes;

        /// <summary>
        /// Был ли выполнен Pull с сервера (сценарий 1.1).
        /// </summary>
        private bool _boardLoadedFromServer = false;

        /// <summary>
        /// Данные борта с сервера (для сохранения при «Подтвердить»).
        /// </summary>
        private BoardData _loadedBoardData = null;

        public SetupAggregatesWindow(StorageService storage, string profile, SettingsService settingsService)
        {
            InitializeComponent();

            _storage = storage;
            _profile = profile;
            _settingsService = settingsService;

            _boxes = new Dictionary<TextBox, string>
            {
                { TxtVr80,   "Vr80" },
                { TxtTa14,   "Ta14" },
                { TxtVk01,   "Vk250001" },
                { TxtVk02,   "Vk250002" }
            };

            SourceInitialized += (s, e) => DarkTitleBarHelper.Apply(this);
            Loaded += SetupAggregatesWindow_Loaded;
        }

        private void SetupAggregatesWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Предзаполняем номер борта, если уже есть
            string boardNumber = _storage.GetAggregateAssignment(_profile, "Chast1");
            if (!string.IsNullOrWhiteSpace(boardNumber))
                TxtChast1.Text = boardNumber;

            UpdateBoardStatus();

            // Тёмный системный заголовок
            DarkTitleBarHelper.Apply(this);
        }

        // ============================================================
        // ==================== ЭТАП 1 ================================
        // ============================================================

        private void OnBoardNumberChanged(object sender, TextChangedEventArgs e)
        {
            UpdateBoardStatus();
        }

        private void UpdateBoardStatus()
        {
            string number = TxtChast1.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(number))
            {
                StatusChast1.Text = "";
                StatusChast1.Foreground = Brushes.Gray;
                return;
            }

            if (_storage.HasBoardInSync(number))
            {
                StatusChast1.Text = LocalizationManager.Get("SetupAgg_Status_InSync");
                StatusChast1.Foreground = Brushes.SeaGreen;
            }
            else
            {
                StatusChast1.Text = LocalizationManager.Get("SetupAgg_Status_New");
                StatusChast1.Foreground = Brushes.Gray;
            }
        }

        // ============================================================
        // ==================== КНОПКА «СИНХРОНИЗАЦИЯ» ================
        // ============================================================

        private async void OnSyncClick(object sender, RoutedEventArgs e)
        {
            string boardNumber = TxtChast1.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(boardNumber))
            {
                MessageBox.Show(
                    LocalizationManager.Get("SetupAgg_EnterBoardNumber"),
                    LocalizationManager.Get("Msg_Attention"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Готовим сервис
                string url = _settingsService.Settings.SupabaseUrl;
                string key = _settingsService.Settings.SupabaseKey;

                if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(key))
                {
                    MessageBox.Show(
                        LocalizationManager.Get("Sync_NoCredentials"),
                        LocalizationManager.Get("Sync_Title"),
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var service = new SupabaseSyncService(url, key);
                await service.InitializeAsync();

                // Pull BoardData
                var remoteBoard = await service.PullBoardAsync(boardNumber);

                if (remoteBoard != null)
                {
                    // === Сценарий 1.1: борт найден ===
                    _boardLoadedFromServer = true;
                    _loadedBoardData = remoteBoard;

                    // Заполняем поля агрегатов
                    if (remoteBoard.AggregateAssignments != null)
                    {
                        if (remoteBoard.AggregateAssignments.TryGetValue("Vr80", out string vr80))
                            TxtVr80.Text = vr80;
                        if (remoteBoard.AggregateAssignments.TryGetValue("Ta14", out string ta14))
                            TxtTa14.Text = ta14;
                        if (remoteBoard.AggregateAssignments.TryGetValue("Vk250001", out string vk1))
                            TxtVk01.Text = vk1;
                        if (remoteBoard.AggregateAssignments.TryGetValue("Vk250002", out string vk2))
                            TxtVk02.Text = vk2;
                    }
                }
                else
                {
                    // === Сценарий 1.2: борт не найден ===
                    _boardLoadedFromServer = false;
                    _loadedBoardData = null;

                    // Поля пустые
                    TxtVr80.Text = "";
                    TxtTa14.Text = "";
                    TxtVk01.Text = "";
                    TxtVk02.Text = "";
                }

                // Переключаемся на этап 2
                ExpandToStage2();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{LocalizationManager.Get("Sync_Error")}:\n{ex.Message}",
                    LocalizationManager.Get("Sync_Title"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Расширяет окно, показывает блок агрегатов, меняет кнопки.
        /// </summary>
        private void ExpandToStage2()
        {
            // Заголовок
            HeaderText.Text = LocalizationManager.Get("SetupAgg_AggregatesHeader");

            // Показываем блок агрегатов
            AggregatesPanel.Visibility = Visibility.Visible;

            // Увеличиваем окно
            Height = 420;

            // Скрываем кнопки этапа 1
            SyncButton.Visibility = Visibility.Collapsed;
            CancelButton.Visibility = Visibility.Collapsed;

            // Показываем кнопки этапа 2
            EditButton.Visibility = Visibility.Visible;
            ConfirmButton.Visibility = Visibility.Visible;

            // Поля заблокированы, пока не «Редактировать»
            SetFieldsReadOnly(true);

            // Кнопка «Подтвердить» — активна в сценарии 1.1, неактивна в 1.2
            ConfirmButton.IsEnabled = _boardLoadedFromServer;

            UpdateAllStatuses();
        }

        /// <summary>
        /// Переключает поля между «только для чтения» и «редактируемыми».
        /// Использует ресурсы темы (без хардкода цветов).
        /// </summary>
        private void SetFieldsReadOnly(bool readOnly)
        {
            foreach (var box in _boxes.Keys)
            {
                box.IsReadOnly = readOnly;
                box.Opacity = readOnly ? 0.6 : 1.0;

                // Меняем фон на «стартовый» (жёлтоватый) — но из темы, не хардкод
                if (readOnly)
                {
                    box.SetResourceReference(TextBox.BackgroundProperty, "StartCellBackgroundBrush");
                }
                else
                {
                    box.SetResourceReference(TextBox.BackgroundProperty, "CellBackgroundBrush");
                }
            }
        }

        // ============================================================
        // ==================== КНОПКА «РЕДАКТИРОВАТЬ» ================
        // ============================================================

        private void OnEditClick(object sender, RoutedEventArgs e)
        {
            // Разблокируем поля
            SetFieldsReadOnly(false);

            // Кнопка «Редактировать» становится недоступной
            EditButton.IsEnabled = false;

            // «Подтвердить» — разблокируем только после ввода хотя бы одного символа
            ConfirmButton.IsEnabled = false;

            // Подписываемся на изменения всех полей
            foreach (var box in _boxes.Keys)
                box.TextChanged += OnAggregateFieldChanged;

            TxtVr80.Focus();
        }

        private void OnAggregateFieldChanged(object sender, TextChangedEventArgs e)
        {
            // Как только введён хотя бы один символ в любое поле — разблокируем «Подтвердить»
            bool anyFilled = _boxes.Keys.Any(b => !string.IsNullOrWhiteSpace(b.Text));
            ConfirmButton.IsEnabled = anyFilled;

            UpdateAllStatuses();
        }

        // ============================================================
        // ==================== СТАТУСЫ ===============================
        // ============================================================

        private void UpdateAllStatuses()
        {
            UpdateAggregateStatus(TxtVr80, StatusVr80, "Vr80");
            UpdateAggregateStatus(TxtTa14, StatusTa14, "Ta14");
            UpdateAggregateStatus(TxtVk01, StatusVk01, "Vk250001");
            UpdateAggregateStatus(TxtVk02, StatusVk02, "Vk250002");
        }

        private void UpdateAggregateStatus(TextBox box, TextBlock status, string type)
        {
            string number = box.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(number))
            {
                status.Text = "";
                status.Foreground = Brushes.Gray;
                return;
            }

            if (_storage.Sync.HasAggregate(type, number))
            {
                status.Text = LocalizationManager.Get("SetupAgg_Status_InSync");
                status.Foreground = Brushes.SeaGreen;
                return;
            }

            if (_storage.GetAggregate(number) != null)
            {
                status.Text = LocalizationManager.Get("SetupAgg_Status_InCatalog");
                status.Foreground = Brushes.SeaGreen;
                return;
            }

            status.Text = LocalizationManager.Get("SetupAgg_Status_New");
            status.Foreground = Brushes.Gray;
        }

        // ============================================================
        // ==================== КНОПКА «ПОДТВЕРДИТЬ» ==================
        // ============================================================

        private async void OnConfirmClick(object sender, RoutedEventArgs e)
        {
            // Собираем номера
            var entered = new Dictionary<string, string>();
            foreach (var kv in _boxes)
            {
                string num = kv.Key.Text?.Trim() ?? "";
                entered[kv.Value] = num;
            }

            // Проверяем заполненность
            var empty = entered
                .Where(kv => string.IsNullOrWhiteSpace(kv.Value))
                .Select(kv => GetDisplayName(kv.Key))
                .ToList();

            if (empty.Count > 0)
            {
                MessageBox.Show(
                    LocalizationManager.Format("SetupAgg_AllRequired",
                        string.Join(", ", empty)),
                    LocalizationManager.Get("Msg_Attention"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка дубликатов
            var dupes = entered
                .GroupBy(kv => kv.Value)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (dupes.Count > 0)
            {
                MessageBox.Show(
                    LocalizationManager.Format("SetupAgg_Duplicate",
                        string.Join(", ", dupes)),
                    LocalizationManager.Get("Msg_Attention"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string boardNumber = TxtChast1.Text?.Trim() ?? "";

            try
            {
                // Сохраняем номер борта
                _storage.SetAggregateAssignment(_profile, "Chast1", boardNumber);

                // Сохраняем привязки агрегатов
                foreach (var kv in entered)
                {
                    _storage.SetAggregateAssignment(_profile, kv.Key, kv.Value);
                }

                // Загружаем формуляры с сервера
                string url = _settingsService.Settings.SupabaseUrl;
                string key = _settingsService.Settings.SupabaseKey;

                if (!string.IsNullOrWhiteSpace(url) && !string.IsNullOrWhiteSpace(key))
                {
                    var service = new SupabaseSyncService(url, key);
                    await service.InitializeAsync();

                    // Скачиваем агрегаты
                    foreach (var kv in entered)
                    {
                        string type = kv.Key;
                        string number = kv.Value;

                        var remote = await service.PullAggregateAsync(type, number);
                        if (remote != null)
                        {
                            _storage.SaveAggregate(remote);
                            _storage.Sync.SaveAggregate(remote);
                        }
                        else
                        {
                            // Новый агрегат — создаём пустой
                            var newAgg = _storage.GetOrCreateAggregate(
                                number, type, GetDisplayName(type));
                            _storage.Sync.SaveAggregate(newAgg);
                        }
                    }

                    // Сохраняем борта (BoardData)
                    if (_loadedBoardData != null)
                    {
                        _storage.SaveChast1Records(_profile, _loadedBoardData.Chast1Records);
                        _storage.SaveChast1StartRecord(_profile, _loadedBoardData.Chast1StartRecord);
                    }
                    else
                    {
                        _storage.SaveBoardToSync(_profile, boardNumber);
                    }
                }

                // SyncEnabled = true
                _storage.SetSyncEnabled(_profile, true);
                _storage.SetHasBeenSynced(_profile, true);

                Saved = true;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{LocalizationManager.Get("Sync_Error")}:\n{ex.Message}",
                    LocalizationManager.Get("Sync_Title"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ============================================================
        // ==================== КНОПКА «ОТМЕНА» =======================
        // ============================================================

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            _storage.SetSyncEnabled(_profile, false);
            Saved = false;
            DialogResult = false;
            Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Если окно закрывается крестиком — тоже блокируем синхронизацию
            if (!Saved)
            {
                _storage.SetSyncEnabled(_profile, false);
            }
            base.OnClosing(e);
        }

        // ============================================================
        // ==================== ВСПОМОГАТЕЛЬНЫЕ =======================
        // ============================================================

        private string GetDisplayName(string type)
        {
            return type switch
            {
                "Vr80" => "ВР-80",
                "Ta14" => "Та-14",
                "Vk250001" => "ВК-2500-01",
                "Vk250002" => "ВК-2500-02",
                "Chast1" => LocalizationManager.Get("SetupAgg_Chast1"),
                _ => type
            };
        }
    }
}