using logbook.Helpers;
using logbook.Models;
using logbook.Services;
using logbook.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace logbook.ViewModels
{
    public partial class MainViewModel
    {
        /// <summary>
        /// Ключи привязок в AggregateAssignments.
        /// </summary>
        private static readonly string[] AggregateKeys =
            { "Chast1", "Vr80", "Ta14", "Vk250001", "Vk250002" };

        /// <summary>
        /// Флаг: окно SetupAggregatesWindow уже открыто.
        /// </summary>
        private bool _setupAggregatesOpen = false;

        /// <summary>
        /// Строки таблицы «Агрегаты на борту».
        /// </summary>
        public ObservableCollection<AggregateAssignmentRow> AggregateRows { get; set; }
            = new ObservableCollection<AggregateAssignmentRow>();

        // ============================================================
        // ==================== ПРОВЕРКА / ПОКАЗ ======================
        // ============================================================

        public void CheckAndShowSetupAggregates()
        {
            if (string.IsNullOrWhiteSpace(SelectedProfile))
                return;

            if (_setupAggregatesOpen)
                return;

            var main = Application.Current?.MainWindow;
            if (main == null || !main.IsLoaded)
                return;

            bool syncEnabled = _storage.GetSyncEnabled(SelectedProfile);
            if (!syncEnabled)
                return;

            var assignments = _storage.GetAggregateAssignments(SelectedProfile);

            bool anyEmpty = AggregateKeys.Any(k =>
                !assignments.TryGetValue(k, out string v) ||
                string.IsNullOrWhiteSpace(v));

            if (!anyEmpty)
                return;

            OpenSetupAggregates();
        }

        // ============================================================
        // ==================== КНОПКА «🔄» ===========================
        // ============================================================

        /// <summary>
        /// Кнопка «🔄» — синхронизация.
        /// </summary>
        private void SyncNow()
        {
            if (string.IsNullOrWhiteSpace(SelectedProfile))
                return;

            DoSync();
        }

        private bool _exchangeOpen;
        private bool IsExchangeProfile => !string.IsNullOrWhiteSpace(SelectedProfile) && _storage.HasExchangeProfile(SelectedProfile);

        private void DoSync(bool personalOnly = false)
        {
            if (!SaveAllSilent(recomputeResources: false)) { MessageBox.Show("Не удалось сохранить текущие правки. Синхронизация остановлена.", "Синхронизация"); return; }
            _exchangeOpen = true;
            _autoSaveTimer?.Stop();
            _recalcTimer?.Stop();
            try
            {
                var window = new DesktopSyncWindow(_settingsService, _storage, () =>
                {
                    _history.Clear();
                    RefreshProfiles();
                    var profile = _storage.GetAllProfiles().FirstOrDefault();
                    SelectedProfile = null;
                    if (profile != null) SelectedProfile = profile;
                    else {
                        Launches.Clear(); SnkRecords.Clear(); Crew = "";
                        Chast1Records.Clear(); Vr80Records.Clear(); Ta14Records.Clear();
                        Vk250001Records.Clear(); Vk250002Records.Clear();
                        ResourceRecords.Clear(); OilRecords.Clear(); AggregateRows.Clear();
                    }
                    IsDirty = false;
                }, personalOnly: personalOnly);
                window.Owner = Application.Current?.MainWindow;
                window.ShowDialog();
                // Refresh references after UUID generation as well as after downloading.
                if (_storage.GetAllProfiles().Contains(SelectedProfile)) SelectedProfile = SelectedProfile;
            }
            catch (Exception error) { MessageBox.Show(error.Message, "Синхронизация", MessageBoxButton.OK, MessageBoxImage.Error); }
            finally { _exchangeOpen = false; _recalcTimer?.Stop(); _autoSaveTimer?.Start(); }
        }

        private void DoLegacySync()
        {
            // Проверка привязок
            var assignments = _storage.GetAggregateAssignments(SelectedProfile);
            bool anyEmpty = AggregateKeys.Any(k =>
                !assignments.TryGetValue(k, out string v) ||
                string.IsNullOrWhiteSpace(v));

            if (anyEmpty)
            {
                MessageBox.Show(
                    LocalizationManager.Get("Sync_NoAssignments"),
                    LocalizationManager.Get("Sync_Title"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка URL и ключа
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

            // Открываем окно
            bool initialNotifications = _settingsService.Settings.ShowDuplicateAggregateNotifications;
            var window = new SyncWindow(initialNotifications);

            var main = Application.Current?.MainWindow;
            if (main != null && main.IsLoaded && main.IsVisible)
                window.Owner = main;

            bool? result = window.ShowDialog();

            if (result != true) return;

            // Сохраняем настройку галочки
            _settingsService.Settings.ShowDuplicateAggregateNotifications = window.ShowNotifications;
            _settingsService.Save();

            switch (window.Target)
            {
                case SyncTarget.Push:
                    _ = PushProfileToServerAsync(window.ShowNotifications);
                    break;
                case SyncTarget.Pull:
                    _ = PullProfileFromServerAsync(window.ShowNotifications);
                    break;
                case SyncTarget.Phone:
                    MessageBox.Show(
                        LocalizationManager.Get("Sync_PhoneNotImplemented"),
                        LocalizationManager.Get("Sync_Title"),
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
            }
        }

        // ============================================================
        // ==================== PUSH (на сервер) ======================
        // ============================================================

        private async Task PushProfileToServerAsync(bool showNotifications)
        {
            try
            {
                var service = CreateSyncService();
                if (service == null) return;

                await service.InitializeAsync();

                int pushed = 0;
                int skipped = 0;

                // === Борт ===
                string boardNumber = _storage.GetAggregateAssignment(SelectedProfile, "Chast1");
                if (!string.IsNullOrWhiteSpace(boardNumber))
                {
                    var board = BuildBoardDataFromProfile(boardNumber);

                    if (showNotifications)
                    {
                        var remote = await service.PullBoardAsync(boardNumber);
                        if (remote != null && !IsRemoteOlderOrEqual(remote, board))
                        {
                            var r = MessageBox.Show(
                                LocalizationManager.Format("Sync_ConflictBoardDate",
                                    boardNumber,
                                    GetLatestBoardDate(remote),
                                    GetLatestBoardDate(board)),
                                LocalizationManager.Get("Sync_Title"),
                                MessageBoxButton.YesNo, MessageBoxImage.Question);

                            if (r != MessageBoxResult.Yes)
                            {
                                skipped++;
                            }
                            else
                            {
                                await service.PushBoardAsync(board);
                                pushed++;
                            }
                        }
                        else
                        {
                            await service.PushBoardAsync(board);
                            pushed++;
                        }
                    }
                    else
                    {
                        await service.PushBoardAsync(board);
                        pushed++;
                    }
                }

                // === Ресурсы (привязаны к борту) ===
                if (!string.IsNullOrWhiteSpace(boardNumber))
                {
                    var localRes = BuildResourcesDataFromProfile(boardNumber);

                    if (showNotifications)
                    {
                        var remoteRes = await service.PullResourcesAsync(boardNumber);
                        if (remoteRes != null &&
                            GetLatestResourcesDate(remoteRes) > GetLatestResourcesDate(localRes))
                        {
                            var r = MessageBox.Show(
                                LocalizationManager.Format("Sync_ConflictResources",
                                    boardNumber,
                                    GetLatestResourcesDate(remoteRes).ToString("dd.MM.yyyy HH:mm"),
                                    GetLatestResourcesDate(localRes).ToString("dd.MM.yyyy HH:mm")),
                                LocalizationManager.Get("Sync_Title"),
                                MessageBoxButton.YesNo, MessageBoxImage.Question);

                            if (r != MessageBoxResult.Yes)
                            {
                                skipped++;
                            }
                            else
                            {
                                await service.PushResourcesAsync(localRes);
                                _storage.SaveResourcesToSync(SelectedProfile, boardNumber);
                                pushed++;
                            }
                        }
                        else
                        {
                            await service.PushResourcesAsync(localRes);
                            _storage.SaveResourcesToSync(SelectedProfile, boardNumber);
                            pushed++;
                        }
                    }
                    else
                    {
                        await service.PushResourcesAsync(localRes);
                        _storage.SaveResourcesToSync(SelectedProfile, boardNumber);
                        pushed++;
                    }
                }

                // === Агрегаты ===
                var assignments = _storage.GetAggregateAssignments(SelectedProfile);

                foreach (var type in new[] { "Vr80", "Ta14", "Vk250001", "Vk250002" })
                {
                    if (!assignments.TryGetValue(type, out string number)) continue;
                    if (string.IsNullOrWhiteSpace(number)) continue;

                    var agg = BuildAggregateDataFromProfile(type, number);
                    if (agg == null) continue;

                    if (showNotifications)
                    {
                        var remote = await service.PullAggregateAsync(type, number);
                        if (remote != null && !IsRemoteOlderOrEqual(remote, agg))
                        {
                            var r = MessageBox.Show(
                                LocalizationManager.Format("Sync_ConflictAggregateDate",
                                    number,
                                    GetLatestAggregateDate(remote),
                                    GetLatestAggregateDate(agg)),
                                LocalizationManager.Get("Sync_Title"),
                                MessageBoxButton.YesNo, MessageBoxImage.Question);

                            if (r != MessageBoxResult.Yes)
                            {
                                skipped++;
                                continue;
                            }
                        }
                    }

                    await service.PushAggregateAsync(agg);
                    pushed++;
                }

                MessageBox.Show(
                    LocalizationManager.Format("Sync_CompletePush", pushed, skipped),
                    LocalizationManager.Get("Sync_Title"),
                    MessageBoxButton.OK, MessageBoxImage.Information);
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
        // ==================== PULL (с сервера) ======================
        // ============================================================

        private async Task PullProfileFromServerAsync(bool showNotifications)
        {
            try
            {
                var service = CreateSyncService();
                if (service == null) return;

                await service.InitializeAsync();

                int pulled = 0;
                int skipped = 0;

                // === Борт ===
                string boardNumber = _storage.GetAggregateAssignment(SelectedProfile, "Chast1");
                if (!string.IsNullOrWhiteSpace(boardNumber))
                {
                    var remote = await service.PullBoardAsync(boardNumber);

                    if (remote == null)
                    {
                        // На сервере нет борта — пропускаем
                    }
                    else
                    {
                        var local = BuildBoardDataFromProfile(boardNumber);

                        if (showNotifications &&
                            !IsRemoteOlderOrEqual(remote, local) == false &&
                            !IsSameDate(local, remote))
                        {
                            var r = MessageBox.Show(
                                LocalizationManager.Format("Sync_ConflictBoardDate",
                                    boardNumber,
                                    GetLatestBoardDate(remote),
                                    GetLatestBoardDate(local)),
                                LocalizationManager.Get("Sync_Title"),
                                MessageBoxButton.YesNo, MessageBoxImage.Question);

                            if (r != MessageBoxResult.Yes)
                            {
                                skipped++;
                                goto aggregates;
                            }
                        }

                        ApplyBoardDataToProfile(remote);
                        // === Ресурсы (привязаны к борту) ===
                        if (!string.IsNullOrWhiteSpace(boardNumber))
                        {
                            var remoteRes = await service.PullResourcesAsync(boardNumber);

                            if (remoteRes != null)
                            {
                                var localRes = BuildResourcesDataFromProfile(boardNumber);

                                if (showNotifications &&
                                    GetLatestResourcesDate(remoteRes) > GetLatestResourcesDate(localRes))
                                {
                                    var r = MessageBox.Show(
                                        LocalizationManager.Format("Sync_ConflictResources",
                                            boardNumber,
                                            GetLatestResourcesDate(remoteRes).ToString("dd.MM.yyyy HH:mm"),
                                            GetLatestResourcesDate(localRes).ToString("dd.MM.yyyy HH:mm")),
                                        LocalizationManager.Get("Sync_Title"),
                                        MessageBoxButton.YesNo, MessageBoxImage.Question);

                                    if (r != MessageBoxResult.Yes)
                                    {
                                        skipped++;
                                    }
                                    else
                                    {
                                        ApplyResourcesDataToProfile(remoteRes);
                                        _storage.SaveResourcesToSync(SelectedProfile, boardNumber);
                                        pulled++;
                                    }
                                }
                                else
                                {
                                    ApplyResourcesDataToProfile(remoteRes);
                                    _storage.SaveResourcesToSync(SelectedProfile, boardNumber);
                                    pulled++;
                                }
                            }
                        }
                        pulled++;
                    }
                }

            aggregates:
                // === Агрегаты ===
                var assignments = _storage.GetAggregateAssignments(SelectedProfile);

                foreach (var type in new[] { "Vr80", "Ta14", "Vk250001", "Vk250002" })
                {
                    if (!assignments.TryGetValue(type, out string number)) continue;
                    if (string.IsNullOrWhiteSpace(number)) continue;

                    var remote = await service.PullAggregateAsync(type, number);

                    if (remote == null)
                    {
                        // На сервере нет — пропускаем
                        continue;
                    }

                    var local = BuildAggregateDataFromProfile(type, number);

                    if (showNotifications && local != null && !IsSameDate(local, remote))
                    {
                        var r = MessageBox.Show(
                            LocalizationManager.Format("Sync_ConflictAggregateDate",
                                number,
                                GetLatestAggregateDate(remote),
                                GetLatestAggregateDate(local)),
                            LocalizationManager.Get("Sync_Title"),
                            MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (r != MessageBoxResult.Yes)
                        {
                            skipped++;
                            continue;
                        }
                    }

                    ApplyAggregateDataToProfile(type, remote);
                    pulled++;
                }

                // Пересчёт и сохранение
                RecalculateAll();
                RefreshSNE();
                RecalculateResources();
                RefreshAggregateRows();
                UpdateNotifications();

                MessageBox.Show(
                    LocalizationManager.Format("Sync_CompletePull", pulled, skipped),
                    LocalizationManager.Get("Sync_Title"),
                    MessageBoxButton.OK, MessageBoxImage.Information);
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
        // ==================== ВСПОМОГАТЕЛЬНЫЕ =======================
        // ============================================================

        private SupabaseSyncService CreateSyncService()
        {
            string url = _settingsService.Settings.SupabaseUrl;
            string key = _settingsService.Settings.SupabaseKey;

            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(key))
            {
                MessageBox.Show(
                    LocalizationManager.Get("Sync_NoCredentials"),
                    LocalizationManager.Get("Sync_Title"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            return new SupabaseSyncService(url, key);
        }

        /// <summary>
        /// Собирает BoardData из текущего профиля (Часть 1 + привязки).
        /// </summary>
        private BoardData BuildBoardDataFromProfile(string boardNumber)
        {
            return new BoardData
            {
                Number = boardNumber,
                Chast1Records = Chast1Records.ToList(),
                Chast1StartRecord = Chast1StartRecord,
                AggregateAssignments = new Dictionary<string, string>(
                    _storage.GetAggregateAssignments(SelectedProfile))
            };
        }

        /// <summary>
        /// Применяет BoardData к текущему профилю.
        /// </summary>
        private void ApplyBoardDataToProfile(BoardData board)
        {
            if (board == null) return;

            _storage.SaveChast1Records(SelectedProfile, board.Chast1Records);
            _storage.SaveChast1StartRecord(SelectedProfile, board.Chast1StartRecord);

            if (board.AggregateAssignments != null)
            {
                foreach (var kv in board.AggregateAssignments)
                {
                    if (string.IsNullOrWhiteSpace(kv.Value)) continue;
                    _storage.SetAggregateAssignment(SelectedProfile, kv.Key, kv.Value);
                }
            }

            LoadChast1Records();
        }

        /// <summary>
        /// Собирает AggregateData из текущего профиля для указанного типа.
        /// </summary>
        private AggregateData BuildAggregateDataFromProfile(string type, string number)
        {
            if (string.IsNullOrWhiteSpace(number)) return null;

            var agg = _storage.GetAggregate(number)
                      ?? _storage.GetOrCreateAggregate(number, type, GetAggregateDisplayName(type));

            switch (type)
            {
                case "Vr80":
                    agg.Vr80Records = Vr80Records.ToList();
                    agg.Vr80StartRecord = Vr80StartRecord;
                    break;
                case "Ta14":
                    agg.Ta14Records = Ta14Records.ToList();
                    agg.Ta14StartRecord = Ta14StartRecord;
                    break;
                case "Vk250001":
                    agg.Vk250001Records = Vk250001Records.ToList();
                    agg.Vk250001StartRecord = Vk250001StartRecord;
                    agg.Vk250001CoeffA = Vk250001CoeffA;
                    agg.Vk250001CoeffH = Vk250001CoeffH;
                    break;
                case "Vk250002":
                    agg.Vk250002Records = Vk250002Records.ToList();
                    agg.Vk250002StartRecord = Vk250002StartRecord;
                    agg.Vk250002CoeffA = Vk250002CoeffA;
                    agg.Vk250002CoeffH = Vk250002CoeffH;
                    break;
            }

            return agg;
        }

        /// <summary>
        /// Применяет AggregateData к текущему профилю.
        /// </summary>
        private void ApplyAggregateDataToProfile(string type, AggregateData agg)
        {
            if (agg == null) return;

            _storage.SaveAggregate(agg);
            _storage.Sync.SaveAggregate(agg);

            switch (type)
            {
                case "Vr80":
                    LoadVr80FromAggregate(agg);
                    _storage.SaveVr80Records(SelectedProfile, Vr80Records.ToList());
                    _storage.SaveVr80StartRecord(SelectedProfile, Vr80StartRecord);
                    break;
                case "Ta14":
                    LoadTa14FromAggregate(agg);
                    _storage.SaveTa14Records(SelectedProfile, Ta14Records.ToList());
                    _storage.SaveTa14StartRecord(SelectedProfile, Ta14StartRecord);
                    break;
                case "Vk250001":
                    LoadVk250001FromAggregate(agg);
                    _storage.SaveVk250001Records(SelectedProfile, Vk250001Records.ToList());
                    _storage.SaveVk250001StartRecord(SelectedProfile, Vk250001StartRecord);
                    _storage.SaveVk250001Coefficients(SelectedProfile, Vk250001CoeffA, Vk250001CoeffH);
                    break;
                case "Vk250002":
                    LoadVk250002FromAggregate(agg);
                    _storage.SaveVk250002Records(SelectedProfile, Vk250002Records.ToList());
                    _storage.SaveVk250002StartRecord(SelectedProfile, Vk250002StartRecord);
                    _storage.SaveVk250002Coefficients(SelectedProfile, Vk250002CoeffA, Vk250002CoeffH);
                    break;
            }
        }

        /// <summary>
        /// Возвращает крайнюю дату из формуляра агрегата.
        /// </summary>
        private string GetLatestAggregateDate(AggregateData agg)
        {
            if (agg == null) return "";

            var dates = new List<string>();

            if (agg.Vr80Records != null)
                dates.AddRange(agg.Vr80Records
                    .Where(r => !string.IsNullOrWhiteSpace(r.Date))
                    .Select(r => r.Date));

            if (agg.Ta14Records != null)
                dates.AddRange(agg.Ta14Records
                    .Where(r => !string.IsNullOrWhiteSpace(r.Date))
                    .Select(r => r.Date));

            if (agg.Vk250001Records != null)
                dates.AddRange(agg.Vk250001Records
                    .Where(r => !string.IsNullOrWhiteSpace(r.Date))
                    .Select(r => r.Date));

            if (agg.Vk250002Records != null)
                dates.AddRange(agg.Vk250002Records
                    .Where(r => !string.IsNullOrWhiteSpace(r.Date))
                    .Select(r => r.Date));

            if (dates.Count == 0) return "";

            // Преобразуем в DateTime и берём максимальную
            var maxDate = dates
                .Select(d => TryParseDate(d, out var dt) ? (DateTime?)dt : null)
                .Where(d => d.HasValue)
                .Select(d => d.Value)
                .DefaultIfEmpty(DateTime.MinValue)
                .Max();

            return maxDate == DateTime.MinValue ? "" : maxDate.ToString("dd.MM.yyyy");
        }

        /// <summary>
        /// Возвращает крайнюю дату из BoardData (Часть 1).
        /// </summary>
        private string GetLatestBoardDate(BoardData board)
        {
            if (board?.Chast1Records == null) return "";

            var dates = board.Chast1Records
                .Where(r => !r.IsTotal && !string.IsNullOrWhiteSpace(r.Date))
                .Select(r => r.Date)
                .ToList();

            if (dates.Count == 0) return "";

            var maxDate = dates
                .Select(d => TryParseDate(d, out var dt) ? (DateTime?)dt : null)
                .Where(d => d.HasValue)
                .Select(d => d.Value)
                .DefaultIfEmpty(DateTime.MinValue)
                .Max();

            return maxDate == DateTime.MinValue ? "" : maxDate.ToString("dd.MM.yyyy");
        }

        /// <summary>
        /// Проверяет, что даты равны.
        /// </summary>
        private bool IsSameDate(AggregateData local, AggregateData remote)
        {
            return GetLatestAggregateDate(local) == GetLatestAggregateDate(remote);
        }

        private bool IsSameDate(BoardData local, BoardData remote)
        {
            return GetLatestBoardDate(local) == GetLatestBoardDate(remote);
        }

        /// <summary>
        /// Проверяет, что remote не новее local (т.е. серверная версия старая или та же).
        /// </summary>
        private bool IsRemoteOlderOrEqual(AggregateData remote, AggregateData local)
        {
            string remoteDate = GetLatestAggregateDate(remote);
            string localDate = GetLatestAggregateDate(local);

            if (string.IsNullOrWhiteSpace(remoteDate)) return true;
            if (string.IsNullOrWhiteSpace(localDate)) return false;

            if (TryParseDate(remoteDate, out var rd) &&
                TryParseDate(localDate, out var ld))
            {
                return rd <= ld;
            }
            return true;
        }

        private bool IsRemoteOlderOrEqual(BoardData remote, BoardData local)
        {
            string remoteDate = GetLatestBoardDate(remote);
            string localDate = GetLatestBoardDate(local);

            if (string.IsNullOrWhiteSpace(remoteDate)) return true;
            if (string.IsNullOrWhiteSpace(localDate)) return false;

            if (TryParseDate(remoteDate, out var rd) &&
                TryParseDate(localDate, out var ld))
            {
                return rd <= ld;
            }
            return true;
        }

        // ============================================================
        // ==================== ОКНО SETUP ============================
        // ============================================================

        private void OpenSetupAggregates()
        {
            if (string.IsNullOrWhiteSpace(SelectedProfile))
                return;

            if (_setupAggregatesOpen)
                return;

            _setupAggregatesOpen = true;
            try
            {
                var window = new SetupAggregatesWindow(_storage, SelectedProfile, _settingsService);

                var main = Application.Current?.MainWindow;
                if (main != null && main.IsLoaded && main.IsVisible)
                    window.Owner = main;

                bool? result = window.ShowDialog();

                if (result == true && window.Saved)
                {
                    LoadChast1Records();
                    UpdateDaysInProfile();

                    string boardNumber = _storage.GetAggregateAssignment(SelectedProfile, "Chast1");
                    if (!string.IsNullOrWhiteSpace(boardNumber))
                        ReloadResourcesFromBoard(boardNumber);

                    var assignments = _storage.GetAggregateAssignments(SelectedProfile);
                    foreach (var type in new[] { "Vr80", "Ta14", "Vk250001", "Vk250002" })
                    {
                        if (assignments.TryGetValue(type, out string number) &&
                            !string.IsNullOrWhiteSpace(number))
                        {
                            ReloadAggregateFormulars(type, number);
                        }
                    }

                    RefreshSNE();
                    RecalculateResources();
                    UpdateNotifications();
                    RefreshAggregateRows();
                }
            }
            finally
            {
                _setupAggregatesOpen = false;
            }
        }

        // ============================================================
        // ==================== ТАБЛИЦА «НА БОРТУ» ====================
        // ============================================================

        /// <summary>
        /// Пересобирает таблицу «Агрегаты на борту».
        /// </summary>
        public void RefreshAggregateRows()
        {
            AggregateRows.Clear();

            if (string.IsNullOrWhiteSpace(SelectedProfile))
            {
                // Уведомить WPF, что CanOpenSetupAggregates могло измениться
                OnPropertyChanged(nameof(CanOpenSetupAggregates));
                return;
            }

            var assignments = _storage.GetAggregateAssignments(SelectedProfile);

            AddAggregateRow("Chast1", LocalizationManager.Get("SetupAgg_Chast1"),
                assignments, canReplace: false);
            AddAggregateRow("Vr80", LocalizationManager.Get("SetupAgg_Vr80"),
                assignments, canReplace: true);
            AddAggregateRow("Ta14", LocalizationManager.Get("SetupAgg_Ta14"),
                assignments, canReplace: true);
            AddAggregateRow("Vk250001", LocalizationManager.Get("SetupAgg_Vk250001"),
                assignments, canReplace: true);
            AddAggregateRow("Vk250002", LocalizationManager.Get("SetupAgg_Vk250002"),
                assignments, canReplace: true);

            // Уведомить WPF, что CanOpenSetupAggregates могло измениться
            OnPropertyChanged(nameof(CanOpenSetupAggregates));
        }

        private void AddAggregateRow(
            string type, string displayName,
            Dictionary<string, string> assignments,
            bool canReplace)
        {
            assignments.TryGetValue(type, out string number);
            number = number ?? "";

            var row = new AggregateAssignmentRow
            {
                Type = type,
                DisplayName = displayName,
                Number = string.IsNullOrWhiteSpace(number) ? "—" : number,
                CanReplace = canReplace
            };

            row.Status = GetAggregateStatus(type, number);
            AggregateRows.Add(row);
        }

        private string GetAggregateStatus(string type, string number)
        {
            if (string.IsNullOrWhiteSpace(number))
                return "";

            if (type == "Chast1")
            {
                return _storage.HasBoardInSync(number)
                    ? LocalizationManager.Get("SetupAgg_Status_InSync")
                    : LocalizationManager.Get("SetupAgg_Status_New");
            }

            if (_storage.Sync.HasAggregate(type, number))
                return LocalizationManager.Get("SetupAgg_Status_InSync");

            if (_storage.GetAggregate(number) != null)
                return LocalizationManager.Get("SetupAgg_Status_InCatalog");

            return LocalizationManager.Get("SetupAgg_Status_New");
        }

        // ============================================================
        // ==================== ЗАМЕНА АГРЕГАТА =======================
        // ============================================================

        public void ReplaceAggregate(string type, string newNumber, string reason)
        {
            if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(newNumber))
                return;

            string oldNumber = _storage.GetAggregateAssignment(SelectedProfile, type);

            if (!string.IsNullOrWhiteSpace(oldNumber))
                SaveCurrentFormularsToSync(type, oldNumber);

            _storage.AddAggregateReplacement(SelectedProfile, new AggregateReplacement
            {
                Type = type,
                OldNumber = oldNumber ?? "",
                NewNumber = newNumber,
                Reason = reason ?? ""
            });

            _storage.SetAggregateAssignment(SelectedProfile, type, newNumber);

            if (!string.IsNullOrWhiteSpace(oldNumber))
            {
                var oldPlacement = _storage.FindPlacement(oldNumber, SelectedProfile);
                if (oldPlacement != null)
                {
                    oldPlacement.RemovedAt = DateTime.Today;
                    var oldAgg = _storage.GetAggregate(oldNumber);
                    if (oldAgg != null)
                        _storage.SaveAggregate(oldAgg);
                }
            }

            var newAgg = _storage.Sync.LoadAggregate(type, newNumber)
                         ?? _storage.GetAggregate(newNumber)
                         ?? _storage.GetOrCreateAggregate(
                             newNumber, type, GetAggregateDisplayName(type));

            _storage.AddPlacement(newNumber, new AggregatePlacement
            {
                ProfileName = SelectedProfile,
                InstalledAt = DateTime.Today,
                PreviousNumber = oldNumber ?? ""
            });

            _storage.Sync.SaveAggregate(newAgg);

            ReloadAggregateFormulars(type, newNumber);

            RefreshAggregateRows();
            UpdateNotifications();
        }

        // ============================================================
        // ==================== ФОРМУЛЯРЫ АГРЕГАТОВ ===================
        // ============================================================

        private void SaveCurrentFormularsToSync(string type, string number)
        {
            if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(number))
                return;

            var agg = _storage.GetAggregate(number)
                      ?? _storage.GetOrCreateAggregate(number, type, GetAggregateDisplayName(type));

            switch (type)
            {
                case "Vr80":
                    agg.Vr80Records = Vr80Records.ToList();
                    agg.Vr80StartRecord = Vr80StartRecord;
                    break;
                case "Ta14":
                    agg.Ta14Records = Ta14Records.ToList();
                    agg.Ta14StartRecord = Ta14StartRecord;
                    break;
                case "Vk250001":
                    agg.Vk250001Records = Vk250001Records.ToList();
                    agg.Vk250001StartRecord = Vk250001StartRecord;
                    agg.Vk250001CoeffA = Vk250001CoeffA;
                    agg.Vk250001CoeffH = Vk250001CoeffH;
                    break;
                case "Vk250002":
                    agg.Vk250002Records = Vk250002Records.ToList();
                    agg.Vk250002StartRecord = Vk250002StartRecord;
                    agg.Vk250002CoeffA = Vk250002CoeffA;
                    agg.Vk250002CoeffH = Vk250002CoeffH;
                    break;
            }

            _storage.SaveAggregate(agg);
            _storage.Sync.SaveAggregate(agg);
        }

        public void ReloadAggregateFormulars(string type, string number)
        {
            if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(SelectedProfile))
                return;

            UnsubscribeItems(Vr80Records);
            UnsubscribeItems(Ta14Records);
            UnsubscribeItems(Vk250001Records);
            UnsubscribeItems(Vk250002Records);

            AggregateData agg = string.IsNullOrWhiteSpace(number)
                ? null
                : _storage.Sync.LoadAggregate(type, number);

            if (agg == null && !string.IsNullOrWhiteSpace(number))
                agg = _storage.GetAggregate(number);

            if (agg == null && !string.IsNullOrWhiteSpace(number))
                agg = _storage.GetOrCreateAggregate(number, type, GetAggregateDisplayName(type));

            switch (type)
            {
                case "Vr80":
                    LoadVr80FromAggregate(agg);
                    break;
                case "Ta14":
                    LoadTa14FromAggregate(agg);
                    break;
                case "Vk250001":
                    LoadVk250001FromAggregate(agg);
                    break;
                case "Vk250002":
                    LoadVk250002FromAggregate(agg);
                    break;
            }

            SubscribeItems(Vr80Records);
            SubscribeItems(Ta14Records);
            SubscribeItems(Vk250001Records);
            SubscribeItems(Vk250002Records);

            _storage.SaveVr80Records(SelectedProfile, Vr80Records.ToList());
            _storage.SaveVr80StartRecord(SelectedProfile, Vr80StartRecord);
            _storage.SaveTa14Records(SelectedProfile, Ta14Records.ToList());
            _storage.SaveTa14StartRecord(SelectedProfile, Ta14StartRecord);
            _storage.SaveVk250001Records(SelectedProfile, Vk250001Records.ToList());
            _storage.SaveVk250001StartRecord(SelectedProfile, Vk250001StartRecord);
            _storage.SaveVk250001Coefficients(SelectedProfile, Vk250001CoeffA, Vk250001CoeffH);
            _storage.SaveVk250002Records(SelectedProfile, Vk250002Records.ToList());
            _storage.SaveVk250002StartRecord(SelectedProfile, Vk250002StartRecord);
            _storage.SaveVk250002Coefficients(SelectedProfile, Vk250002CoeffA, Vk250002CoeffH);

            RecalculateAll();
            RefreshSNE();
            RecalculateResources();
        }

        private void LoadVr80FromAggregate(AggregateData agg)
        {
            Vr80Records.Clear();

            if (agg != null && agg.Vr80Records != null)
            {
                foreach (var r in agg.Vr80Records)
                    Vr80Records.Add(r);
            }

            Vr80StartRecord = agg?.Vr80StartRecord ?? new Vr80Record();
        }

        private void LoadTa14FromAggregate(AggregateData agg)
        {
            Ta14Records.Clear();

            if (agg != null && agg.Ta14Records != null)
            {
                foreach (var r in agg.Ta14Records)
                    Ta14Records.Add(r);
            }

            Ta14StartRecord = agg?.Ta14StartRecord ?? new Ta14Record();
        }

        private void LoadVk250001FromAggregate(AggregateData agg)
        {
            Vk250001Records.Clear();

            if (agg != null && agg.Vk250001Records != null)
            {
                foreach (var r in agg.Vk250001Records)
                    Vk250001Records.Add(r);
            }

            Vk250001StartRecord = agg?.Vk250001StartRecord ?? new Vk250001Record();
            Vk250001CoeffA = agg?.Vk250001CoeffA ?? "";
            Vk250001CoeffH = agg?.Vk250001CoeffH ?? "";
        }

        private void LoadVk250002FromAggregate(AggregateData agg)
        {
            Vk250002Records.Clear();

            if (agg != null && agg.Vk250002Records != null)
            {
                foreach (var r in agg.Vk250002Records)
                    Vk250002Records.Add(r);
            }

            Vk250002StartRecord = agg?.Vk250002StartRecord ?? new Vk250002Record();
            Vk250002CoeffA = agg?.Vk250002CoeffA ?? "";
            Vk250002CoeffH = agg?.Vk250002CoeffH ?? "";
        }

        // ============================================================
        // ==================== ИСТОРИЯ ЗАМЕН =========================
        // ============================================================

        private void OpenAggregateHistory()
        {
            if (string.IsNullOrWhiteSpace(SelectedProfile))
                return;

            var window = new AggregateHistoryWindow(_storage, SelectedProfile);

            var main = Application.Current?.MainWindow;
            if (main != null && main.IsLoaded && main.IsVisible)
                window.Owner = main;

            window.ShowDialog();
        }

        // ============================================================
        // ==================== ВСПОМОГАТЕЛЬНЫЕ =======================
        // ============================================================

        public string GetAggregateDisplayName(string type)
        {
            return type switch
            {
                "Vr80" => LocalizationManager.Get("SetupAgg_Vr80"),
                "Ta14" => LocalizationManager.Get("SetupAgg_Ta14"),
                "Vk250001" => LocalizationManager.Get("SetupAgg_Vk250001"),
                "Vk250002" => LocalizationManager.Get("SetupAgg_Vk250002"),
                "Chast1" => LocalizationManager.Get("SetupAgg_Chast1"),
                _ => type
            };
        }

        public bool CanOpenSetupAggregates
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SelectedProfile))
                    return false;

                bool hasBeenSynced = _storage.GetHasBeenSynced(SelectedProfile);
                if (!hasBeenSynced)
                    return true;

                var assignments = _storage.GetAggregateAssignments(SelectedProfile);

                return AggregateKeys.Any(k =>
                    !assignments.TryGetValue(k, out string v) ||
                    string.IsNullOrWhiteSpace(v));
            }
        }

        private void OnReplaceAggregate(AggregateAssignmentRow row)
        {
            if (row == null || !row.CanReplace)
                return;

            if (string.IsNullOrWhiteSpace(SelectedProfile))
                return;

            var window = new ReplaceAggregateWindow(_storage, SelectedProfile, row.Type);

            var main = Application.Current?.MainWindow;
            if (main != null && main.IsLoaded && main.IsVisible)
                window.Owner = main;

            window.ShowDialog();

            if (window.Replaced)
            {
                ReplaceAggregate(row.Type, window.NewNumber, window.Reason);
                RefreshAggregateRows();
                UpdateNotifications();
            }
        }
    }
}