using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace logbook.Helpers
{
    /// <summary>
    /// Информация об обновлении.
    /// </summary>
    public class UpdateInfo
    {
        public string Version { get; set; } = "";
        public string Url { get; set; } = "";
        private string notes = "";
        public string Notes { get => LocalizationManager.CurrentLanguage == "en" && !string.IsNullOrWhiteSpace(NotesEn) ? NotesEn : notes; set => notes = value; }
        public string NotesEn { get; set; } = "";
    }

    /// <summary>
    /// Проверка обновлений через JSON на сервере.
    /// </summary>
    public static class UpdateChecker
    {
        /// <summary>
        /// Текущая версия приложения (из FileVersion, т.е. &lt;FileVersion&gt; в .csproj).
        /// </summary>
        public static string CurrentVersion
        {
            get
            {
                try
                {
                    var asm = Assembly.GetExecutingAssembly();
                    // FileVersion is embedded in the assembly and remains available
                    // when PublishSingleFile makes Assembly.Location empty.
                    string fileVersion = asm.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "";
                    if (!string.IsNullOrWhiteSpace(fileVersion))
                    {
                        var parts = fileVersion.Split('.');
                        return parts.Length >= 3 ? $"{parts[0]}.{parts[1]}.{parts[2]}" : fileVersion;
                    }

                    // Fallback — AssemblyVersion
                    var ver = asm.GetName().Version;
                    return ver != null
                        ? $"{ver.Major}.{ver.Minor}.{ver.Build}"
                        : "1.0.0";
                }
                catch
                {
                    return "1.0.0";
                }
            }
        }

        /// <summary>
        /// Асинхронно проверить наличие обновления.
        /// URL к version.json строится из базового URL Supabase.
        /// Возвращает null только если обновлений нет; ошибки передаются вызывающему коду.
        /// </summary>
        public const string Website = "https://logbook-landing-page-cbo2.bolt.host/";
        public const string ManifestUrl = "https://zjagjauwmpuzcwzidhkn.supabase.co/storage/v1/object/public/updates/version.json";

        public static async Task<UpdateInfo?> CheckAsync(string supabaseUrl)
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            http.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true };
            string json = await http.GetStringAsync(ManifestUrl);
            var info = ParseManifest(json);
            return IsNewer(info.Version, CurrentVersion) ? info : null;
        }

        public static UpdateInfo ParseManifest(string json)
        {
            var info = JsonSerializer.Deserialize<UpdateInfo>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (info == null || !System.Text.RegularExpressions.Regex.IsMatch(info.Version ?? "", @"^\d+(?:\.\d+){0,3}$")
                || !Version.TryParse(Normalize(info.Version), out _))
                throw new InvalidDataException("Invalid release version");
            if (!string.IsNullOrWhiteSpace(info.Url) &&
                (!Uri.TryCreate(info.Url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps
                    || string.IsNullOrEmpty(uri.Host) || !string.IsNullOrEmpty(uri.UserInfo)))
                throw new InvalidDataException("Invalid release download URL");
            return info;
        }

        /// <summary>
        /// Сравнивает версии: true, если remote > local.
        /// </summary>
        private static bool IsNewer(string remote, string local)
        {
            try
            {
                var r = new Version(Normalize(remote));
                var l = new Version(Normalize(local));
                return r > l;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Приводит "1.0" → "1.0.0", "1" → "1.0.0".
        /// </summary>
        private static string Normalize(string v)
        {
            while (v.Split('.').Length < 3)
                v += ".0";
            return v;
        }
    }
}
