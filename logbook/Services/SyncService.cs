using System;
using System.IO;
using Newtonsoft.Json;
using logbook.Models;

namespace logbook.Services
{
    /// <summary>
    /// Тестовая синхронизация через локальную папку `sync`.
    /// Позже будет заменена на реальный сервер.
    /// </summary>
    public class SyncService
    {
        private readonly string _syncFolder;

        public SyncService(string dataFolder)
        {
            _syncFolder = Path.Combine(dataFolder, "sync");

            if (!Directory.Exists(_syncFolder))
                Directory.CreateDirectory(_syncFolder);

            // Подпапки по типам (Chast1 — отдельно, агрегаты — отдельно)
            foreach (var type in new[] { "VR80", "Ta14", "VK2500", "Chast1", "Resources" })
            {
                string sub = Path.Combine(_syncFolder, type);
                if (!Directory.Exists(sub))
                    Directory.CreateDirectory(sub);
            }
        }

        // ============================================================
        // ==================== АГРЕГАТЫ ==============================
        // ============================================================

        /// <summary>
        /// Сохранить агрегат в папку sync.
        /// </summary>
        public void SaveAggregate(AggregateData aggregate)
        {
            if (aggregate == null || string.IsNullOrWhiteSpace(aggregate.Number))
                return;

            string path = GetAggregatePath(aggregate.Type, aggregate.Number);
            if (path == null) return;

            string json = JsonConvert.SerializeObject(aggregate, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        /// <summary>
        /// Загрузить агрегат из папки sync (или null).
        /// </summary>
        public AggregateData LoadAggregate(string type, string number)
        {
            string path = GetAggregatePath(type, number);
            if (path == null || !File.Exists(path))
                return null;

            try
            {
                string json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<AggregateData>(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Удалить агрегат из папки sync.
        /// </summary>
        public void DeleteAggregate(string type, string number)
        {
            string path = GetAggregatePath(type, number);
            if (path != null && File.Exists(path))
            {
                try { File.Delete(path); } catch { }
            }
        }

        /// <summary>
        /// Проверить, есть ли агрегат в sync.
        /// </summary>
        public bool HasAggregate(string type, string number)
        {
            string path = GetAggregatePath(type, number);
            return path != null && File.Exists(path);
        }

        /// <summary>
        /// Путь к файлу агрегата.
        /// </summary>
        private string GetAggregatePath(string type, string number)
        {
            if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(number))
                return null;

            string sub = type switch
            {
                "Vr80" => "VR80",
                "Ta14" => "Ta14",
                "Vk250001" => "VK2500",   // ← оба ВК → одна папка
                "Vk250002" => "VK2500",   // ←
                _ => null
            };

            if (sub == null) return null;

            return Path.Combine(_syncFolder, sub, $"{number}.json");
        }

        // ============================================================
        // ==================== ЧАСТЬ 1 (БОРТ) ========================
        // ============================================================

        /// <summary>
        /// Сохранить формуляры Части 1 борта в sync.
        /// </summary>
        public void SaveBoard(BoardData board)
        {
            if (board == null || string.IsNullOrWhiteSpace(board.Number))
                return;

            string path = GetBoardPath(board.Number);
            if (path == null) return;

            string json = JsonConvert.SerializeObject(board, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        /// <summary>
        /// Загрузить формуляры Части 1 борта из sync (или null).
        /// </summary>
        public BoardData LoadBoard(string number)
        {
            string path = GetBoardPath(number);
            if (path == null || !File.Exists(path))
                return null;

            try
            {
                string json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<BoardData>(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Удалить формуляры Части 1 борта из sync.
        /// </summary>
        public void DeleteBoard(string number)
        {
            string path = GetBoardPath(number);
            if (path != null && File.Exists(path))
            {
                try { File.Delete(path); } catch { }
            }
        }

        /// <summary>
        /// Проверить, есть ли борт в sync.
        /// </summary>
        public bool HasBoard(string number)
        {
            string path = GetBoardPath(number);
            return path != null && File.Exists(path);
        }

        /// <summary>
        /// Путь к файлу борта (Часть 1).
        /// </summary>
        private string GetBoardPath(string number)
        {
            if (string.IsNullOrWhiteSpace(number))
                return null;

            return Path.Combine(_syncFolder, "Chast1", $"{number}.json");
        }

        // ============================================================
        // ==================== РЕСУРСЫ (БОРТ) ========================
        // ============================================================

        public void SaveResources(ResourcesData res)
        {
            if (res == null || string.IsNullOrWhiteSpace(res.BoardNumber))
                return;

            string path = GetResourcesPath(res.BoardNumber);
            if (path == null) return;

            string json = JsonConvert.SerializeObject(res, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        public ResourcesData LoadResources(string boardNumber)
        {
            string path = GetResourcesPath(boardNumber);
            if (path == null || !File.Exists(path))
                return null;

            try
            {
                string json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<ResourcesData>(json);
            }
            catch
            {
                return null;
            }
        }

        public void DeleteResources(string boardNumber)
        {
            string path = GetResourcesPath(boardNumber);
            if (path != null && File.Exists(path))
            {
                try { File.Delete(path); } catch { }
            }
        }

        public bool HasResources(string boardNumber)
        {
            string path = GetResourcesPath(boardNumber);
            return path != null && File.Exists(path);
        }

        private string GetResourcesPath(string boardNumber)
        {
            if (string.IsNullOrWhiteSpace(boardNumber))
                return null;

            return Path.Combine(_syncFolder, "Resources", $"{boardNumber}.json");
        }
    }
}