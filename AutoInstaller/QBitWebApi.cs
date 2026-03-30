using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoInstaller;

public class TorrentInfo
{
    [JsonPropertyName("hash")] public string Hash { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("size")] public long Size { get; set; }
    [JsonPropertyName("progress")] public double Progress { get; set; }
    [JsonPropertyName("dlspeed")] public long DlSpeed { get; set; }
    [JsonPropertyName("upspeed")] public long UpSpeed { get; set; }
    [JsonPropertyName("state")] public string State { get; set; } = "";
    [JsonPropertyName("eta")] public long Eta { get; set; }
    [JsonPropertyName("num_seeds")] public int NumSeeds { get; set; }
    [JsonPropertyName("num_leechs")] public int NumLeechs { get; set; }
    [JsonPropertyName("added_on")] public long AddedOn { get; set; }
    [JsonPropertyName("save_path")] public string SavePath { get; set; } = "";
}

public class TransferInfo
{
    [JsonPropertyName("dl_info_speed")] public long DlSpeed { get; set; }
    [JsonPropertyName("up_info_speed")] public long UpSpeed { get; set; }
    [JsonPropertyName("dl_info_data")] public long DlData { get; set; }
    [JsonPropertyName("up_info_data")] public long UpData { get; set; }
    [JsonPropertyName("connection_status")] public string ConnectionStatus { get; set; } = "";
}

/// <summary>
/// Client for qBittorrent Web API v2 (localhost).
/// </summary>
public sealed class QBitWebApi : IDisposable
{
    private readonly HttpClient _http;
    private readonly CookieContainer _cookies;
    private string _baseUrl;
    private bool _loggedIn;

    public bool IsLoggedIn => _loggedIn;
    public string BaseUrl => _baseUrl;

    public QBitWebApi()
    {
        _cookies = new CookieContainer();
        var handler = new HttpClientHandler
        {
            CookieContainer = _cookies,
            UseCookies = true
        };
        _http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
        _baseUrl = "http://localhost:8080";
    }

    public void SetBaseUrl(string host, int port)
    {
        _baseUrl = $"http://{host}:{port}";
        _loggedIn = false;
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        try
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("password", password)
            });
            _http.DefaultRequestHeaders.Remove("Referer");
            _http.DefaultRequestHeaders.Add("Referer", _baseUrl);

            var resp = await _http.PostAsync($"{_baseUrl}/api/v2/auth/login", content);
            if (!resp.IsSuccessStatusCode) return false;

            var body = await resp.Content.ReadAsStringAsync();
            _loggedIn = body.Contains("Ok", StringComparison.OrdinalIgnoreCase);
            return _loggedIn;
        }
        catch
        {
            _loggedIn = false;
            return false;
        }
    }

    public async Task<List<TorrentInfo>> GetTorrentsAsync(string filter = "all")
    {
        try
        {
            var resp = await _http.GetAsync($"{_baseUrl}/api/v2/torrents/info?filter={filter}");
            if (!resp.IsSuccessStatusCode) return [];
            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<TorrentInfo>>(json) ?? [];
        }
        catch { return []; }
    }

    public async Task<TransferInfo?> GetTransferInfoAsync()
    {
        try
        {
            var resp = await _http.GetAsync($"{_baseUrl}/api/v2/transfer/info");
            if (!resp.IsSuccessStatusCode) return null;
            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TransferInfo>(json);
        }
        catch { return null; }
    }

    public async Task<bool> AddMagnetAsync(string magnetUri, string? savePath = null)
    {
        try
        {
            var form = new MultipartFormDataContent();
            form.Add(new StringContent(magnetUri), "urls");
            if (!string.IsNullOrWhiteSpace(savePath))
                form.Add(new StringContent(savePath), "savepath");

            var resp = await _http.PostAsync($"{_baseUrl}/api/v2/torrents/add", form);
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<bool> PauseTorrentsAsync(params string[] hashes)
    {
        return await PostHashesAsync("pause", hashes);
    }

    public async Task<bool> ResumeTorrentsAsync(params string[] hashes)
    {
        return await PostHashesAsync("resume", hashes);
    }

    public async Task<bool> DeleteTorrentsAsync(bool deleteFiles, params string[] hashes)
    {
        try
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("hashes", string.Join("|", hashes)),
                new KeyValuePair<string, string>("deleteFiles", deleteFiles.ToString().ToLower())
            });
            var resp = await _http.PostAsync($"{_baseUrl}/api/v2/torrents/delete", content);
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            var resp = await _http.GetAsync($"{_baseUrl}/api/v2/app/version");
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    private async Task<bool> PostHashesAsync(string action, string[] hashes)
    {
        try
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("hashes", string.Join("|", hashes))
            });
            var resp = await _http.PostAsync($"{_baseUrl}/api/v2/torrents/{action}", content);
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public static string FormatSpeed(long bytesPerSecond)
    {
        if (bytesPerSecond <= 0) return "0 B/s";
        double kb = bytesPerSecond / 1024.0;
        if (kb < 1024) return $"{kb:F1} KB/s";
        double mb = kb / 1024.0;
        return $"{mb:F2} MB/s";
    }

    public static string FormatSize(long bytes)
    {
        if (bytes <= 0) return "0 B";
        double kb = bytes / 1024.0;
        if (kb < 1024) return $"{kb:F1} KB";
        double mb = kb / 1024.0;
        if (mb < 1024) return $"{mb:F1} MB";
        double gb = mb / 1024.0;
        return $"{gb:F2} GB";
    }

    public static string FormatEta(long seconds)
    {
        if (seconds <= 0 || seconds >= 8640000) return "∞";
        var ts = TimeSpan.FromSeconds(seconds);
        if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h {ts.Minutes}m";
        if (ts.TotalMinutes >= 1) return $"{ts.Minutes}m {ts.Seconds}s";
        return $"{ts.Seconds}s";
    }

    public static string FriendlyState(string state) => state switch
    {
        "downloading" => "Downloading",
        "stalledDL" => "Stalled (DL)",
        "metaDL" => "Getting metadata",
        "pausedDL" => "Paused",
        "queuedDL" => "Queued",
        "forcedDL" => "Downloading [F]",
        "uploading" => "Seeding",
        "stalledUP" => "Seeding (idle)",
        "pausedUP" => "Completed",
        "queuedUP" => "Queued (seed)",
        "forcedUP" => "Seeding [F]",
        "checkingDL" or "checkingUP" => "Checking",
        "checkingResumeData" => "Resuming",
        "allocating" => "Allocating",
        "moving" => "Moving",
        "error" => "Error",
        "missingFiles" => "Missing files",
        _ => state
    };

    public void Dispose() => _http.Dispose();
}
