using System.Net;
using MonoTorrent;
using MonoTorrent.Client;

namespace AutoInstaller;

/// <summary>
/// Built-in BitTorrent client powered by MonoTorrent — no external qBittorrent needed.
/// </summary>
public sealed class TorrentEngine : IDisposable
{
    private ClientEngine? _engine;
    private readonly string _cacheDir;
    private bool _disposed;

    public record DownloadInfo(
        TorrentManager Manager,
        string SavePath,
        DateTime AddedTime);

    private readonly List<DownloadInfo> _downloads = [];
    public IReadOnlyList<DownloadInfo> Downloads => _downloads;

    /// <summary>Fired when any download's state or progress changes.</summary>
    public event Action? DownloadsChanged;

    public TorrentEngine()
    {
        _cacheDir = Path.Combine(AppContext.BaseDirectory, "torrent_cache");
        Directory.CreateDirectory(_cacheDir);
    }

    private async Task<ClientEngine> GetEngineAsync()
    {
        if (_engine != null) return _engine;

        var settings = new EngineSettingsBuilder
        {
            AllowPortForwarding = true,
            AutoSaveLoadDhtCache = true,
            AutoSaveLoadFastResume = true,
            AutoSaveLoadMagnetLinkMetadata = true,
            CacheDirectory = _cacheDir,
            ListenEndPoints = new Dictionary<string, IPEndPoint>
            {
                { "ipv4", new IPEndPoint(IPAddress.Any, 0) },
                { "ipv6", new IPEndPoint(IPAddress.IPv6Any, 0) }
            },
            DhtEndPoint = new IPEndPoint(IPAddress.Any, 0)
        }.ToSettings();

        _engine = new ClientEngine(settings);
        // Small delay to let DHT bootstrap
        await Task.Delay(500);
        return _engine;
    }

    /// <summary>
    /// Add a magnet link and start downloading to the specified folder.
    /// </summary>
    public async Task<TorrentManager> AddMagnetAsync(string magnetUri, string savePath)
    {
        if (!MagnetLink.TryParse(magnetUri, out var magnet))
            throw new ArgumentException("Invalid magnet link");

        Directory.CreateDirectory(savePath);
        var engine = await GetEngineAsync();

        var torrentSettings = new TorrentSettingsBuilder
        {
            MaximumConnections = 60
        }.ToSettings();

        var manager = await engine.AddAsync(magnet, savePath, torrentSettings);

        manager.TorrentStateChanged += (_, _) => DownloadsChanged?.Invoke();
        manager.PieceHashed += (_, _) => DownloadsChanged?.Invoke();

        _downloads.Add(new DownloadInfo(manager, savePath, DateTime.Now));
        await manager.StartAsync();

        DownloadsChanged?.Invoke();
        return manager;
    }

    /// <summary>
    /// Add a .torrent file and start downloading.
    /// </summary>
    public async Task<TorrentManager> AddTorrentFileAsync(string torrentPath, string savePath)
    {
        Directory.CreateDirectory(savePath);
        var engine = await GetEngineAsync();

        var torrentSettings = new TorrentSettingsBuilder
        {
            MaximumConnections = 60
        }.ToSettings();

        var manager = await engine.AddAsync(torrentPath, savePath, torrentSettings);

        manager.TorrentStateChanged += (_, _) => DownloadsChanged?.Invoke();
        manager.PieceHashed += (_, _) => DownloadsChanged?.Invoke();

        _downloads.Add(new DownloadInfo(manager, savePath, DateTime.Now));
        await manager.StartAsync();

        DownloadsChanged?.Invoke();
        return manager;
    }

    /// <summary>Pause a download.</summary>
    public async Task PauseAsync(TorrentManager manager)
    {
        await manager.PauseAsync();
        DownloadsChanged?.Invoke();
    }

    /// <summary>Resume a paused download.</summary>
    public async Task ResumeAsync(TorrentManager manager)
    {
        await manager.StartAsync();
        DownloadsChanged?.Invoke();
    }

    /// <summary>Remove a download (optionally delete data).</summary>
    public async Task RemoveAsync(TorrentManager manager, bool deleteData = false)
    {
        var engine = await GetEngineAsync();
        await manager.StopAsync();
        await engine.RemoveAsync(manager);

        var info = _downloads.FirstOrDefault(d => d.Manager == manager);
        if (info != null)
            _downloads.Remove(info);

        if (deleteData && info != null && Directory.Exists(info.SavePath))
        {
            try { Directory.Delete(info.SavePath, true); } catch { }
        }

        DownloadsChanged?.Invoke();
    }

    /// <summary>Total download rate in bytes/sec.</summary>
    public long TotalDownloadRate => _engine?.TotalDownloadRate ?? 0;

    /// <summary>Total upload rate in bytes/sec.</summary>
    public long TotalUploadRate => _engine?.TotalUploadRate ?? 0;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_engine != null)
        {
            // Stop all torrents gracefully
            foreach (var mgr in _engine.Torrents)
            {
                try { mgr.StopAsync().GetAwaiter().GetResult(); } catch { }
            }
            _engine.Dispose();
            _engine = null;
        }
    }

    /// <summary>Format bytes into human-readable size.</summary>
    public static string FormatBytes(long bytes)
    {
        if (bytes >= 1_073_741_824) return $"{bytes / 1_073_741_824.0:F1} GB";
        if (bytes >= 1_048_576) return $"{bytes / 1_048_576.0:F1} MB";
        if (bytes >= 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes} B";
    }

    /// <summary>Format speed in bytes/sec to human-readable.</summary>
    public static string FormatSpeed(long bytesPerSec)
    {
        if (bytesPerSec >= 1_048_576) return $"{bytesPerSec / 1_048_576.0:F1} MB/s";
        if (bytesPerSec >= 1024) return $"{bytesPerSec / 1024.0:F1} KB/s";
        return $"{bytesPerSec} B/s";
    }
}
