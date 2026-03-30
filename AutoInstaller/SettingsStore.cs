using System.Text.Json;

namespace AutoInstaller;

public class AppSettings
{
    public string WatchFolder { get; set; } = "";
    public string SetupFileName { get; set; } = "setup.exe";
    public string InstallPath { get; set; } = "";
    public string DrivePath { get; set; } = "";
    public bool WaitForQuiet { get; set; } = true;
    public int QuietSeconds { get; set; } = 10;
    public decimal DelayValue { get; set; } = 10;
    public int TimeUnitIndex { get; set; } // 0=Seconds, 1=Minutes, 2=Hours
    public string NtfyTopic { get; set; } = "";
    public string QbtExePath { get; set; } = "";
    public bool ClipboardMagnet { get; set; } = true;
    public bool InstallDirectX { get; set; } = true;
    public bool InstallVCRedist { get; set; } = true;
}

public static class SettingsStore
{
    private static readonly string FilePath = Path.Combine(
        AppContext.BaseDirectory, "settings.json");

    public static AppSettings Load()
    {
        if (!File.Exists(FilePath)) return new AppSettings();
        try
        {
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch { return new AppSettings(); }
    }

    public static void Save(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings,
            new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json);
    }
}
