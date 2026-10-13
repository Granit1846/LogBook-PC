using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Supabase;
using logbook.Models;

namespace logbook.Services
{
    public class SupabaseSyncService
    {
        private readonly Supabase.Client _client;

        public SupabaseSyncService(string url, string key)
        {
            var options = new Supabase.SupabaseOptions
            {
                AutoRefreshToken = false,
                AutoConnectRealtime = false
            };

            _client = new Supabase.Client(url, key, options);
        }

        public async Task InitializeAsync()
        {
            await _client.InitializeAsync();
        }

        // ============================================================
        // АГРЕГАТЫ
        // ============================================================

        public async Task PushAggregateAsync(AggregateData agg)
        {
            var record = new SyncRecord
            {
                Kind = "aggregate",
                Type = agg.Type,
                Number = agg.Number,
                Data = JsonConvert.SerializeObject(agg),
                UpdatedAt = DateTime.UtcNow
            };

            // Ищем существующую запись
            var existing = await _client.From<SyncRecord>()
                .Where(x => x.Kind == "aggregate")
                .Where(x => x.Type == agg.Type)
                .Where(x => x.Number == agg.Number)
                .Get();

            if (existing.Models.Count > 0)
            {
                // Обновляем существующую
                await _client.From<SyncRecord>()
                    .Where(x => x.Kind == "aggregate")
                    .Where(x => x.Type == agg.Type)
                    .Where(x => x.Number == agg.Number)
                    .Set(x => x.Data, record.Data)
                    .Set(x => x.UpdatedAt, record.UpdatedAt)
                    .Update();
            }
            else
            {
                // Вставляем новую
                await _client.From<SyncRecord>().Insert(record);
            }
        }

        public async Task<AggregateData> PullAggregateAsync(string type, string number)
        {
            var result = await _client.From<SyncRecord>()
                .Where(x => x.Kind == "aggregate")
                .Where(x => x.Type == type)
                .Where(x => x.Number == number)
                .Get();

            var rec = result.Models.FirstOrDefault();
            if (rec == null) return null;

            return JsonConvert.DeserializeObject<AggregateData>(rec.Data);
        }

        // ============================================================
        // БОРТА
        // ============================================================

        public async Task PushBoardAsync(BoardData board)
        {
            var record = new SyncRecord
            {
                Kind = "board",
                Type = "",
                Number = board.Number,
                Data = JsonConvert.SerializeObject(board),
                UpdatedAt = DateTime.UtcNow
            };

            var existing = await _client.From<SyncRecord>()
                .Where(x => x.Kind == "board")
                .Where(x => x.Number == board.Number)
                .Get();

            if (existing.Models.Count > 0)
            {
                await _client.From<SyncRecord>()
                    .Where(x => x.Kind == "board")
                    .Where(x => x.Number == board.Number)
                    .Set(x => x.Data, record.Data)
                    .Set(x => x.UpdatedAt, record.UpdatedAt)
                    .Update();
            }
            else
            {
                await _client.From<SyncRecord>().Insert(record);
            }
        }

        public async Task<BoardData> PullBoardAsync(string number)
        {
            var result = await _client.From<SyncRecord>()
                .Where(x => x.Kind == "board")
                .Where(x => x.Number == number)
                .Get();

            var rec = result.Models.FirstOrDefault();
            if (rec == null) return null;

            return JsonConvert.DeserializeObject<BoardData>(rec.Data);
        }

        // ============================================================
        // РЕСУРСЫ
        // ============================================================

        public async Task PushResourcesAsync(ResourcesData res)
        {
            var record = new SyncRecord
            {
                Kind = "resources",
                Type = "",
                Number = res.BoardNumber,
                Data = JsonConvert.SerializeObject(res),
                UpdatedAt = DateTime.UtcNow
            };

            var existing = await _client.From<SyncRecord>()
                .Where(x => x.Kind == "resources")
                .Where(x => x.Number == res.BoardNumber)
                .Get();

            if (existing.Models.Count > 0)
            {
                await _client.From<SyncRecord>()
                    .Where(x => x.Kind == "resources")
                    .Where(x => x.Number == res.BoardNumber)
                    .Set(x => x.Data, record.Data)
                    .Set(x => x.UpdatedAt, record.UpdatedAt)
                    .Update();
            }
            else
            {
                await _client.From<SyncRecord>().Insert(record);
            }
        }

        public async Task<ResourcesData> PullResourcesAsync(string boardNumber)
        {
            var result = await _client.From<SyncRecord>()
                .Where(x => x.Kind == "resources")
                .Where(x => x.Number == boardNumber)
                .Get();

            var rec = result.Models.FirstOrDefault();
            if (rec == null) return null;

            return JsonConvert.DeserializeObject<ResourcesData>(rec.Data);
        }
    }
}