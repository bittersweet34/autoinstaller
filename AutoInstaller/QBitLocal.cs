using Microsoft.Win32;

namespace AutoInstaller;

/// <summary>
/// Finds and interacts with a locally installed qBittorrent — no Web UI required.
/// </summary>
public static class QBitLocal
{
    /// <summary>
    /// Try to find qbittorrent.exe: registry first, then common locations.
    /// </summary>
    public static string? FindExePath()
    {
        // 1. Check registry (both 64-bit and 32-bit)
        string?[] regPaths =
        [
            @"SOFTWARE\qBittorrent",
            @"SOFTWARE\WOW6432Node\qBittorrent"
        ];

        foreach (var regPath in regPaths)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(regPath);
                var installDir = key?.GetValue("InstallLocation") as string
                              ?? key?.GetValue("InstallDir") as string;
                if (!string.IsNullOrEmpty(installDir))
                {
                    string exe = Path.Combine(installDir, "qbittorrent.exe");
                    if (File.Exists(exe)) return exe;
                }
            }
            catch { }
        }

        // 2. Try Uninstall registry entries
        string[] uninstallPaths =
        [
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
        ];

        foreach (var uPath in uninstallPaths)
        {
            try
            {
                using var baseKey = Registry.LocalMachine.OpenSubKey(uPath);
                if (baseKey == null) continue;
                foreach (var subName in baseKey.GetSubKeyNames())
                {
                    using var sub = baseKey.OpenSubKey(subName);
                    var display = sub?.GetValue("DisplayName") as string;
                    if (display != null && display.Contains("qBittorrent", StringComparison.OrdinalIgnoreCase))
                    {
                        var loc = sub?.GetValue("InstallLocation") as string;
                        if (!string.IsNullOrEmpty(loc))
                        {
                            string exe = Path.Combine(loc, "qbittorrent.exe");
                            if (File.Exists(exe)) return exe;
                        }
                    }
                }
            }
            catch { }
        }

        // 3. Common paths
        string[] commonPaths =
        [
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "qBittorrent", "qbittorrent.exe"),
            @"C:\Program Files\qBittorrent\qbittorrent.exe",
            @"C:\Program Files (x86)\qBittorrent\qbittorrent.exe",
        ];

        foreach (var p in commonPaths)
        {
            if (File.Exists(p)) return p;
        }

        return null;
    }

    /// <summary>
    /// Read the default save path from qBittorrent's INI config.
    /// </summary>
    public static string? GetDownloadFolder()
    {
        string iniPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "qBittorrent", "qBittorrent.ini");

        if (!File.Exists(iniPath)) return null;

        try
        {
            foreach (var line in File.ReadLines(iniPath))
            {
                var trimmed = line.Trim();
                // Look for: Downloads\SavePath=...  or  Session\DefaultSavePath=...
                if (trimmed.StartsWith(@"Downloads\SavePath=", StringComparison.OrdinalIgnoreCase)
                    || trimmed.StartsWith(@"Session\DefaultSavePath=", StringComparison.OrdinalIgnoreCase))
                {
                    string val = trimmed[(trimmed.IndexOf('=') + 1)..].Trim();
                    if (!string.IsNullOrWhiteSpace(val) && Directory.Exists(val))
                        return val;
                }
            }
        }
        catch { }

        return null;
    }

    /// <summary>
    /// Send a magnet link to qBittorrent by launching its exe.
    /// If qBittorrent is already running, it passes the magnet to the running instance.
    /// </summary>
    public static bool AddMagnet(string exePath, string magnetUri, string? savePath = null)
    {
        try
        {
            string args = !string.IsNullOrWhiteSpace(savePath)
                ? $"--skip-dialog=true --save-path=\"{savePath}\" \"{magnetUri}\""
                : $"--skip-dialog=true \"{magnetUri}\"";

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = exePath,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            System.Diagnostics.Process.Start(psi);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get subfolders in the download directory.
    /// </summary>
    public static List<string> GetDownloadSubfolders(string downloadFolder)
    {
        try
        {
            if (!Directory.Exists(downloadFolder)) return [];
            return Directory.GetDirectories(downloadFolder)
                .OrderByDescending(d => Directory.GetLastWriteTime(d))
                .ToList();
        }
        catch { return []; }
    }
}
