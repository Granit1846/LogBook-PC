using logbook.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using logbook.Models;

namespace logbook.Services
{
    public class StorageService : IDesktopStore
    {
        private string _filePath;
        private AppData _data;
        public string LastSyncBackup { get; private set; } = "";
        public SyncHistory History => new SyncHistory(_settingsService.Settings.DataFolder);

        private readonly SettingsService _settingsService;

        /// <summary>
        /// Тестовая синхронизация через локальную папку `sync`.
        /// </summary>
        public SyncService Sync { get; }

        public StorageService(SettingsService settingsService)
        {
            _settingsService = settingsService;
            _settingsService.EnsureFoldersExist();

            _filePath = _settingsService.GetDataFilePath();
            LoadFromFile();

            Sync = new SyncService(_settingsService.Settings.DataFolder);
        }

        // ============================================================
        // ==================== ЗАГРУЗКА / СОХРАНЕНИЕ ================
        // ============================================================

        private void LoadFromFile()
        {
            if (File.Exists(_filePath))
            {
                string json = File.ReadAllText(_filePath);
                try
                {
                    _data = JsonConvert.DeserializeObject<AppData>(json)
                            ?? throw new JsonSerializationException("Empty application data");

                    if (_data.Profiles == null || _data.Profiles.Count == 0)
                    {
                        try
                        {
                            var oldData = JsonConvert.DeserializeObject<Dictionary<string, ProfileData>>(json);
                            if (oldData != null && oldData.Count > 0 && !oldData.ContainsKey("Profiles"))
                                _data.Profiles = oldData;
                        }
                        catch (JsonException)
                        {
                            // Valid current-format data need not be a legacy profile dictionary.
                        }
                    }

                    _data.Profiles ??= new();
                    _data.Aggregates ??= new();
                    SanitizeSnkValues(_data);
                }
                catch (JsonException ex)
                {
                    throw new InvalidDataException($"Cannot read the LogBook data file: {_filePath}", ex);
                }
            }
            else
            {
                _data = new AppData();
            }
        }
        public static void SanitizeSnkValues(AppData data)
        {
            if (data?.Profiles == null) return;
            foreach (var profile in data.Profiles.Values)
            {
                if (profile.Days == null) continue;
                foreach (var day in profile.Days.Values)
                {
                    if (day.SnkValues == null) continue;
                    foreach (var sv in day.SnkValues)
                    {
                        sv.LeftValue = ExchangeCodec.CleanSnk(sv.LeftValue, sv.Label);
                        sv.RightValue = ExchangeCodec.CleanSnk(sv.RightValue, sv.Label);
                    }
                }
            }
        }

        private void SaveToFile()
        {
            string json = JsonConvert.SerializeObject(_data,
                Newtonsoft.Json.Formatting.Indented);
            AtomicFile.Write(_filePath, System.Text.Encoding.UTF8.GetBytes(json));
        }

        public bool HasExchangeProfile(string name) => _data.Profiles.TryGetValue(name, out var profile) && !string.IsNullOrEmpty(profile.ExchangeId);

        public AppData Snapshot() => JsonConvert.DeserializeObject<AppData>(JsonConvert.SerializeObject(_data))!;

        public void Save(AppData data)
        {
            SanitizeSnkValues(data);
            var json = JsonConvert.SerializeObject(data, Formatting.Indented);
            AtomicFile.Write(_filePath, System.Text.Encoding.UTF8.GetBytes(json));
            _data = data;
        }

        public void Download(string packet, string expectedLocal, long revision, string endpoint, string account) => Download(packet, expectedLocal, revision, endpoint, account, false);
        public void Download(string packet, string expectedLocal, long revision, string endpoint, string account, bool preserveSyncState, Action<DesktopExchangeState>? configure = null)
        {
            var current = Snapshot();
            if (!ExchangeCodec.Same(ExchangeCodec.Encode(current).ToString(Formatting.None), expectedLocal))
                throw new InvalidOperationException(SyncText.T("Sync_93155fad6e"));
            var restored = ExchangeCodec.Decode(ExchangeCodec.Parse(packet), current.Exchange);
            SanitizeSnkValues(restored);
            if (!preserveSyncState) {
            restored.Exchange.Endpoint = endpoint; restored.Exchange.Account = account;
            restored.Exchange.BaseRevision = revision; restored.Exchange.BaseJson = packet;
            restored.Exchange.PendingId = ""; restored.Exchange.PendingJson = ""; restored.Exchange.PendingRevision = 0;
            restored.Exchange.LastSuccess = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
            if (!preserveSyncState) restored.Exchange.SaveCheckpoint();
            configure?.Invoke(restored.Exchange);
            // Validate and serialize before replacing anything; a failed backup aborts the download.
            var replacement = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(restored, Formatting.Indented));
            var backup = Path.Combine(_settingsService.GetBackupFolder(), "before-sync-" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + "-" + Guid.NewGuid().ToString("N") + ".json");
            AtomicFile.Write(backup, System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(_data, Formatting.Indented)));
            AtomicFile.Write(_filePath, replacement);
            _data = restored;
            LastSyncBackup = backup;
        }

        // ============================================================
        // ==================== ПРОФИЛИ ===============================
        // ============================================================

        public List<string> GetAllProfiles()
        {
            return _data.Profiles.Keys.OrderBy(p => p).ToList();
        }

        public ProfileData GetProfile(string profile)
        {
            return GetOrCreateProfile(profile);
        }

        public void DeleteProfile(string profile)
        {
            if (_data.Profiles.ContainsKey(profile))
            {
                _data.Profiles.Remove(profile);
                SaveToFile();
            }
        }

        public string GetProfileIcon(string profile)
        {
            var p = GetOrCreateProfile(profile);
            return string.IsNullOrWhiteSpace(p.IconName) ? "1.ico" : p.IconName;
        }

        public void SetProfileIcon(string profile, string iconName)
        {
            var p = GetOrCreateProfile(profile);
            p.IconName = iconName;
            SaveToFile();
        }

        public bool GetHasBeenSynced(string profile)
        {
            var p = GetOrCreateProfile(profile);
            return p.HasBeenSynced;
        }

        public void SetHasBeenSynced(string profile, bool value)
        {
            var p = GetOrCreateProfile(profile);
            p.HasBeenSynced = value;
            SaveToFile();
        }

        /// <summary>
        /// Возвращает SettingsService (для доступа к настройкам URL/ключа Supabase).
        /// </summary>
        public SettingsService GetSettingsService()
        {
            return _settingsService;
        }

        public bool CreateProfile(string profile, string iconName)
        {
            if (string.IsNullOrWhiteSpace(profile)) return false;
            if (_data.Profiles.ContainsKey(profile)) return false;

            _data.Profiles[profile] = new ProfileData
            {
                ProfileName = profile,
                IconName = iconName
            };
            SaveToFile();
            return true;
        }

        private ProfileData GetOrCreateProfile(string profile)
        {
            if (string.IsNullOrWhiteSpace(profile))
                return new ProfileData { ProfileName = "" };

            if (!_data.Profiles.ContainsKey(profile))
                _data.Profiles[profile] = new ProfileData { ProfileName = profile };

            return _data.Profiles[profile];
        }

        // ============================================================
        // ==================== ДАННЫЕ ПО ДНЮ =========================
        // ============================================================

        public DayData GetDayData(string profile, string date)
        {
            var p = GetOrCreateProfile(profile);
            return p.Days.ContainsKey(date) ? p.Days[date] : null;
        }

        public void SaveDayData(DayData dayData)
        {
            var p = GetOrCreateProfile(dayData.Profile);
            if (string.IsNullOrEmpty(dayData.ExchangeId) && p.Days.TryGetValue(dayData.Date, out var previous)) dayData.ExchangeId = previous.ExchangeId;
            if (dayData.SnkValues != null)
            {
                foreach (var sv in dayData.SnkValues)
                {
                    sv.LeftValue = ExchangeCodec.CleanSnk(sv.LeftValue, sv.Label);
                    sv.RightValue = ExchangeCodec.CleanSnk(sv.RightValue, sv.Label);
                }
            }
            p.Days[dayData.Date] = dayData;
            SaveToFile();
        }

        /// <summary>
        /// Удалить DayData за указанную дату.
        /// </summary>
                public void ClearDaysHistory(string profile)
        {
            var p = GetOrCreateProfile(profile);
            p.Days.Clear();
            SaveToFile();
        }

        public void DeleteDayData(string profile, string date)
        {
            var p = GetOrCreateProfile(profile);
            if (p.Days.ContainsKey(date))
            {
                p.Days.Remove(date);
                SaveToFile();
            }
        }

        public List<DayData> GetAllDays(string profile)
        {
            var p = GetOrCreateProfile(profile);
            return p.Days.Values
                .OrderBy(d =>
                {
                    DateTime dt;
                    return DateTime.TryParseExact(d.Date, "dd.MM.yyyy",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out dt)
                        ? dt : DateTime.MinValue;
                })
                .ToList();
        }

        // ============================================================
        // ==================== ЧАСТЬ 1 ===============================
        // ============================================================

        public List<Chast1Record> GetChast1Records(string profile)
        {
            var p = GetOrCreateProfile(profile);
            return p.Chast1Records;
        }

        public void SaveChast1Records(string profile, List<Chast1Record> records)
        {
            var p = GetOrCreateProfile(profile);
            p.Chast1Records = records;
            SaveToFile();
        }

        public Chast1Record GetChast1StartRecord(string profile)
        {
            var p = GetOrCreateProfile(profile);
            return p.Chast1StartRecord ?? new Chast1Record();
        }

        public void SaveChast1StartRecord(string profile, Chast1Record record)
        {
            var p = GetOrCreateProfile(profile);
            p.Chast1StartRecord = record;
            SaveToFile();
        }

        // ============================================================
        // ==================== ВР-80 =================================
        // ============================================================

        public List<Vr80Record> GetVr80Records(string profile)
        {
            var p = GetOrCreateProfile(profile);
            return p.Vr80Records;
        }

        public void SaveVr80Records(string profile, List<Vr80Record> records)
        {
            var p = GetOrCreateProfile(profile);
            p.Vr80Records = records;
            SaveToFile();
        }

        public Vr80Record GetVr80StartRecord(string profile)
        {
            var p = GetOrCreateProfile(profile);
            return p.Vr80StartRecord ?? new Vr80Record();
        }

        public void SaveVr80StartRecord(string profile, Vr80Record record)
        {
            var p = GetOrCreateProfile(profile);
            p.Vr80StartRecord = record;
            SaveToFile();
        }

        // ============================================================
        // ==================== Та-14 =================================
        // ============================================================

        public List<Ta14Record> GetTa14Records(string profile)
        {
            var p = GetOrCreateProfile(profile);
            return p.Ta14Records;
        }

        public void SaveTa14Records(string profile, List<Ta14Record> records)
        {
            var p = GetOrCreateProfile(profile);
            p.Ta14Records = records;
            SaveToFile();
        }

        public Ta14Record GetTa14StartRecord(string profile)
        {
            var p = GetOrCreateProfile(profile);
            return p.Ta14StartRecord ?? new Ta14Record();
        }

        public void SaveTa14StartRecord(string profile, Ta14Record record)
        {
            var p = GetOrCreateProfile(profile);
            p.Ta14StartRecord = record;
            SaveToFile();
        }

        // ============================================================
        // ==================== ВК-2500-01 ============================
        // ============================================================

        public List<Vk250001Record> GetVk250001Records(string profile)
        {
            var p = GetOrCreateProfile(profile);
            return p.Vk250001Records;
        }

        public void SaveVk250001Records(string profile, List<Vk250001Record> records)
        {
            var p = GetOrCreateProfile(profile);
            p.Vk250001Records = records;
            SaveToFile();
        }

        public Vk250001Record GetVk250001StartRecord(string profile)
        {
            var p = GetOrCreateProfile(profile);
            return p.Vk250001StartRecord ?? new Vk250001Record();
        }

        public void SaveVk250001StartRecord(string profile, Vk250001Record record)
        {
            var p = GetOrCreateProfile(profile);
            p.Vk250001StartRecord = record;
            SaveToFile();
        }

        public (string coeffA, string coeffH) GetVk250001Coefficients(string profile)
        {
            var p = GetOrCreateProfile(profile);
            return (p.Vk250001CoeffA ?? "", p.Vk250001CoeffH ?? "");
        }

        public void SaveVk250001Coefficients(string profile, string coeffA, string coeffH)
        {
            var p = GetOrCreateProfile(profile);
            p.Vk250001CoeffA = coeffA;
            p.Vk250001CoeffH = coeffH;
            SaveToFile();
        }

        // ============================================================
        // ==================== ВК-2500-02 ============================
        // ============================================================

        public List<Vk250002Record> GetVk250002Records(string profile)
        {
            var p = GetOrCreateProfile(profile);
            return p.Vk250002Records;
        }

        public void SaveVk250002Records(string profile, List<Vk250002Record> records)
        {
            var p = GetOrCreateProfile(profile);
            p.Vk250002Records = records;
            SaveToFile();
        }

        public Vk250002Record GetVk250002StartRecord(string profile)
        {
            var p = GetOrCreateProfile(profile);
            return p.Vk250002StartRecord ?? new Vk250002Record();
        }

        public void SaveVk250002StartRecord(string profile, Vk250002Record record)
        {
            var p = GetOrCreateProfile(profile);
            p.Vk250002StartRecord = record;
            SaveToFile();
        }

        public (string coeffA, string coeffH) GetVk250002Coefficients(string profile)
        {
            var p = GetOrCreateProfile(profile);
            return (p.Vk250002CoeffA ?? "", p.Vk250002CoeffH ?? "");
        }

        public void SaveVk250002Coefficients(string profile, string coeffA, string coeffH)
        {
            var p = GetOrCreateProfile(profile);
            p.Vk250002CoeffA = coeffA;
            p.Vk250002CoeffH = coeffH;
            SaveToFile();
        }

        // ============================================================
        // ==================== РЕСУРСЫ ===============================
        // ============================================================

        public List<ResourceRecord> GetResourceRecords(string profile)
        {
            var p = GetOrCreateProfile(profile);
            return p.ResourceRecords;
        }

        public void SaveResourceRecords(string profile, List<ResourceRecord> records)
        {
            var p = GetOrCreateProfile(profile);
            p.ResourceRecords = records;
            p.ResourcesUpdatedAt = DateTime.UtcNow;
            SaveToFile();
        }

        public List<OilRecord> GetOilRecords(string profile)
        {
            var p = GetOrCreateProfile(profile);
            return p.OilRecords;
        }

        public void SaveOilRecords(string profile, List<OilRecord> records)
        {
            var p = GetOrCreateProfile(profile);
            p.OilRecords = records;
            p.ResourcesUpdatedAt = DateTime.UtcNow;
            SaveToFile();
        }

        public DateTime GetResourcesUpdatedAt(string profile)
        {
            var p = GetOrCreateProfile(profile);
            return p.ResourcesUpdatedAt;
        }

        // ============================================================
        // ==================== АГРЕГАТЫ (каталог) ====================
        // ============================================================

        public List<AggregateData> GetAllAggregates()
        {
            return _data.Aggregates.Values.ToList();
        }

        public AggregateData GetAggregate(string number)
        {
            if (string.IsNullOrWhiteSpace(number)) return null;
            return _data.Aggregates.ContainsKey(number) ? _data.Aggregates[number] : null;
        }

        public void SaveAggregate(AggregateData aggregate)
        {
            if (aggregate == null || string.IsNullOrWhiteSpace(aggregate.Number)) return;
            _data.Aggregates[aggregate.Number] = aggregate;
            SaveToFile();
        }

        public void DeleteAggregate(string number)
        {
            if (_data.Aggregates.ContainsKey(number))
            {
                _data.Aggregates.Remove(number);
                SaveToFile();
            }
        }

        public AggregateData GetOrCreateAggregate(string number, string type, string displayName)
        {
            if (_data.Aggregates.ContainsKey(number))
                return _data.Aggregates[number];

            var agg = new AggregateData
            {
                Number = number,
                Type = type,
                DisplayName = displayName
            };
            _data.Aggregates[number] = agg;
            SaveToFile();
            return agg;
        }

        // ============================================================
        // ==================== ПРИВЯЗКИ АГРЕГАТОВ ====================
        // ============================================================

        public string GetAggregateAssignment(string profile, string type)
        {
            var p = GetOrCreateProfile(profile);
            if (p.AggregateAssignments == null) p.AggregateAssignments = new();
            return p.AggregateAssignments.ContainsKey(type)
                ? p.AggregateAssignments[type]
                : "";
        }

        public void SetAggregateAssignment(string profile, string type, string number)
        {
            var p = GetOrCreateProfile(profile);
            if (p.AggregateAssignments == null) p.AggregateAssignments = new();
            p.AggregateAssignments[type] = number;
            SaveToFile();
        }

        public Dictionary<string, string> GetAggregateAssignments(string profile)
        {
            var p = GetOrCreateProfile(profile);
            if (p.AggregateAssignments == null) p.AggregateAssignments = new();
            return p.AggregateAssignments;
        }

        public bool GetSyncEnabled(string profile)
        {
            var p = GetOrCreateProfile(profile);
            return p.SyncEnabled;
        }

        public void SetSyncEnabled(string profile, bool enabled)
        {
            var p = GetOrCreateProfile(profile);
            p.SyncEnabled = enabled;
            SaveToFile();
        }

        // ============================================================
        // ==================== ИСТОРИЯ ЗАМЕН =========================
        // ============================================================

        public List<AggregateReplacement> GetAggregateHistory(string profile)
        {
            var p = GetOrCreateProfile(profile);
            if (p.AggregateHistory == null) p.AggregateHistory = new();
            return p.AggregateHistory;
        }

        public void SaveAggregateHistory(string profile, List<AggregateReplacement> history)
        {
            var p = GetOrCreateProfile(profile);
            p.AggregateHistory = history;
            SaveToFile();
        }

                public void ClearAggregateHistory(string profile)
        {
            var p = GetOrCreateProfile(profile);
            p.AggregateHistory.Clear();
            SaveToFile();
        }

        public void AddAggregateReplacement(string profile, AggregateReplacement replacement)
        {
            var p = GetOrCreateProfile(profile);
            if (p.AggregateHistory == null) p.AggregateHistory = new();
            p.AggregateHistory.Add(replacement);
            SaveToFile();
        }

        // ============================================================
        // ==================== ИСТОРИЯ УСТАНОВОК =====================
        // ============================================================

        /// <summary>
        /// Добавить запись в PlacementHistory агрегата (новый — вверху).
        /// </summary>
        public void AddPlacement(string aggregateNumber, AggregatePlacement placement)
        {
            if (string.IsNullOrWhiteSpace(aggregateNumber) || placement == null)
                return;

            if (!_data.Aggregates.ContainsKey(aggregateNumber))
                return;

            var agg = _data.Aggregates[aggregateNumber];
            if (agg.PlacementHistory == null)
                agg.PlacementHistory = new();

            agg.PlacementHistory.Insert(0, placement);
            SaveToFile();
        }

        /// <summary>
        /// Найти существующую запись установки для борта.
        /// </summary>
        public AggregatePlacement FindPlacement(string aggregateNumber, string profileName)
        {
            if (string.IsNullOrWhiteSpace(aggregateNumber) || string.IsNullOrWhiteSpace(profileName))
                return null;

            if (!_data.Aggregates.ContainsKey(aggregateNumber))
                return null;

            var agg = _data.Aggregates[aggregateNumber];
            if (agg.PlacementHistory == null)
                return null;

            return agg.PlacementHistory
                .FirstOrDefault(pl => pl.ProfileName == profileName
                                      && pl.RemovedAt == null);
        }

        // ============================================================
        // ==================== ЧАСТЬ 1 В SYNC ========================
        // ============================================================

        public void SaveBoardToSync(string profile, string boardNumber)
        {
            if (string.IsNullOrWhiteSpace(boardNumber)) return;

            var board = new BoardData
            {
                Number = boardNumber,
                Chast1Records = GetChast1Records(profile),
                Chast1StartRecord = GetChast1StartRecord(profile)
            };

            Sync.SaveBoard(board);
        }

        public BoardData LoadBoardFromSync(string boardNumber)
        {
            return Sync.LoadBoard(boardNumber);
        }

        public bool HasBoardInSync(string boardNumber)
        {
            return Sync.HasBoard(boardNumber);
        }

        // ============================================================
        // ==================== РЕСУРСЫ В SYNC ========================
        // ============================================================

        public void SaveResourcesToSync(string profile, string boardNumber)
        {
            if (string.IsNullOrWhiteSpace(boardNumber)) return;

            var res = new ResourcesData
            {
                BoardNumber = boardNumber,
                ResourceRecords = GetResourceRecords(profile),
                OilRecords = GetOilRecords(profile),
                UpdatedAt = GetResourcesUpdatedAt(profile)
            };

            Sync.SaveResources(res);
        }

        public ResourcesData LoadResourcesFromSync(string boardNumber)
        {
            return Sync.LoadResources(boardNumber);
        }

        public bool HasResourcesInSync(string boardNumber)
        {
            return Sync.HasResources(boardNumber);
        }

        // ============================================================
        // ==================== ПУТЬ К ДАННЫМ =========================
        // ============================================================

        public void ChangeDataFolder(string newFolder)
        {
            string oldFolder = _settingsService.Settings.DataFolder;
            if (string.Equals(Path.GetFullPath(oldFolder), Path.GetFullPath(newFolder), StringComparison.OrdinalIgnoreCase))
                return;

            Directory.CreateDirectory(newFolder);
            string newFile = Path.Combine(newFolder, "work_data.json");
            if (File.Exists(newFile))
                throw new IOException($"The selected folder already contains a LogBook data file: {newFile}");

            if (File.Exists(_filePath))
                File.Copy(_filePath, newFile);
            else
                AtomicFile.Write(newFile, System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(_data, Formatting.Indented)));

            var oldFile = _filePath;
            var oldData = _data;
            try
            {
                _settingsService.Settings.DataFolder = newFolder;
                _settingsService.Save();
                _filePath = newFile;
                LoadFromFile();
                Directory.CreateDirectory(Path.Combine(newFolder, "backup"));
            }
            catch
            {
                _settingsService.Settings.DataFolder = oldFolder;
                _filePath = oldFile;
                _data = oldData;
                try { _settingsService.Save(); } catch { }
                throw;
            }
            // Keep the previous data file as a recovery copy after switching folders.
        }
        // ============================================================
        // ==================== ЖУРНАЛ ТО =============================
        // ============================================================

        public List<MaintenanceWork> GetMaintenanceWorks(string profile)
        {
            var p = GetOrCreateProfile(profile);
            p.MaintenanceWorks ??= new List<MaintenanceWork>();
            return p.MaintenanceWorks;
        }

        public void SaveMaintenanceWorks(string profile, List<MaintenanceWork> works)
        {
            var p = GetOrCreateProfile(profile);
            p.MaintenanceWorks = works ?? new List<MaintenanceWork>();
            SaveToFile();
        }

        public List<DefectTask> GetDefectTasks(string profile)
        {
            var p = GetOrCreateProfile(profile);
            p.DefectTasks ??= new List<DefectTask>();
            return p.DefectTasks;
        }

        public void SaveDefectTasks(string profile, List<DefectTask> tasks)
        {
            var p = GetOrCreateProfile(profile);
            p.DefectTasks = tasks ?? new List<DefectTask>();
            SaveToFile();
        }
    }
}