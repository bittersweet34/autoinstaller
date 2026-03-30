using Microsoft.Win32;
using System.Text;

namespace AutoInstaller;

public static class SteamIntegration
{
    // Known non-game executables to skip when searching for the main game exe
    private static readonly string[] ExcludedExePrefixes =
    [
        "unins", "uninst", "uninstall",
        "UnityCrashHandler", "CrashReporter", "CrashHandler",
        "dxsetup", "dxwebsetup", "DXSETUP",
        "vcredist", "vc_redist",
        "dotNetFx", "NDP", "ndp",
        "UE4PrereqSetup", "UEPrereqSetup",
        "installscript", "installutil",
        "7z", "rar"
    ];

    /// <summary>Locate Steam install folder via registry or default path.</summary>
    public static string? FindSteamPath()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam")
                             ?? Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Valve\Steam");
            var path = key?.GetValue("InstallPath") as string;
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                return path;
        }
        catch { }

        var defaultPath = @"C:\Program Files (x86)\Steam";
        return Directory.Exists(defaultPath) ? defaultPath : null;
    }

    /// <summary>Find shortcuts.vdf for the most-recently-active Steam user.</summary>
    public static string? FindShortcutsVdf()
    {
        var steam = FindSteamPath();
        if (steam == null) return null;

        var userdata = Path.Combine(steam, "userdata");
        if (!Directory.Exists(userdata)) return null;

        var userDir = new DirectoryInfo(userdata)
            .GetDirectories()
            .Where(d => d.Name.All(char.IsDigit) && d.Name != "0")
            .OrderByDescending(d => d.LastWriteTime)
            .FirstOrDefault();

        if (userDir == null) return null;

        var configDir = Path.Combine(userDir.FullName, "config");
        if (!Directory.Exists(configDir))
            Directory.CreateDirectory(configDir);

        return Path.Combine(configDir, "shortcuts.vdf");
    }

    /// <summary>
    /// Find the likely main game executable in an install directory.
    /// Prefers root-level exes, then name-matching, then largest.
    /// </summary>
    public static string? FindGameExe(string installDir)
    {
        if (!Directory.Exists(installDir)) return null;

        var exes = new DirectoryInfo(installDir)
            .EnumerateFiles("*.exe", SearchOption.AllDirectories)
            .Where(f => !IsExcludedExe(f.Name))
            .OrderByDescending(f => f.Length)
            .ToList();

        if (exes.Count == 0) return null;

        // Prefer exe in root directory
        var rootExes = exes
            .Where(f => string.Equals(f.DirectoryName, installDir, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (rootExes.Count > 0) return rootExes[0].FullName;

        // Prefer exe matching folder name
        var folderName = Path.GetFileName(installDir);
        var match = exes.FirstOrDefault(f =>
            Path.GetFileNameWithoutExtension(f.Name)
                .Equals(folderName, StringComparison.OrdinalIgnoreCase));
        if (match != null) return match.FullName;

        // Fallback to largest exe
        return exes[0].FullName;
    }

    private static bool IsExcludedExe(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        return ExcludedExePrefixes.Any(p =>
            name.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Add a non-Steam game shortcut to Steam's shortcuts.vdf.
    /// Returns true on success (or if already present).
    /// </summary>
    public static bool AddNonSteamGame(string gameName, string exePath)
    {
        try
        {
            var vdfPath = FindShortcutsVdf();
            if (vdfPath == null) return false;

            byte[] data;
            if (File.Exists(vdfPath))
            {
                data = File.ReadAllBytes(vdfPath);
                if (data.Length == 0 || data[^1] != 0x08)
                    data = CreateEmptyShortcutsVdf();
            }
            else
            {
                data = CreateEmptyShortcutsVdf();
            }

            // Check for duplicate
            if (ContainsExePath(data, exePath))
                return true;

            int nextIndex = FindNextIndex(data);
            byte[] entry = BuildShortcutEntry(nextIndex, gameName, exePath);

            // Insert new entry just before the final \x08 (end-of-root marker)
            var result = new byte[data.Length + entry.Length];
            Array.Copy(data, 0, result, 0, data.Length - 1);
            Array.Copy(entry, 0, result, data.Length - 1, entry.Length);
            result[^1] = 0x08;

            // Backup existing file
            if (File.Exists(vdfPath))
                File.Copy(vdfPath, vdfPath + ".bak", true);

            File.WriteAllBytes(vdfPath, result);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ── Binary VDF helpers ──────────────────────────────

    private static byte[] CreateEmptyShortcutsVdf()
    {
        using var ms = new MemoryStream();
        ms.WriteByte(0x00);
        WriteCString(ms, "shortcuts");
        ms.WriteByte(0x08);
        return ms.ToArray();
    }

    private static bool ContainsExePath(byte[] data, string exePath)
    {
        var search = Encoding.UTF8.GetBytes(exePath);
        return FindBytesCaseInsensitive(data, search) >= 0;
    }

    private static int FindBytesCaseInsensitive(byte[] haystack, byte[] needle)
    {
        for (int i = 0; i <= haystack.Length - needle.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < needle.Length; j++)
            {
                byte a = haystack[i + j];
                byte b = needle[j];
                if (a >= 65 && a <= 90) a += 32;
                if (b >= 65 && b <= 90) b += 32;
                if (a != b) { match = false; break; }
            }
            if (match) return i;
        }
        return -1;
    }

    private static int FindNextIndex(byte[] data)
    {
        int maxIndex = -1;
        int pos = 0;

        if (pos >= data.Length || data[pos] != 0x00) return 0;
        pos++;
        SkipCString(data, ref pos); // skip "shortcuts"

        while (pos < data.Length && data[pos] == 0x00)
        {
            pos++;
            string indexStr = ReadCString(data, ref pos);
            if (int.TryParse(indexStr, out int idx) && idx > maxIndex)
                maxIndex = idx;
            SkipVdfObject(data, ref pos);
        }

        return maxIndex + 1;
    }

    private static byte[] BuildShortcutEntry(int index, string gameName, string exePath)
    {
        string startDir = Path.GetDirectoryName(exePath) ?? "";

        using var ms = new MemoryStream();

        ms.WriteByte(0x00);
        WriteCString(ms, index.ToString());

        WriteStringField(ms, "AppName", gameName);
        WriteStringField(ms, "Exe", $"\"{exePath}\"");
        WriteStringField(ms, "StartDir", $"\"{startDir}\"");
        WriteStringField(ms, "icon", "");
        WriteStringField(ms, "ShortcutPath", "");
        WriteStringField(ms, "LaunchOptions", "");

        WriteIntField(ms, "IsHidden", 0);
        WriteIntField(ms, "AllowDesktopConfig", 1);
        WriteIntField(ms, "AllowOverlay", 1);
        WriteIntField(ms, "OpenVR", 0);
        WriteIntField(ms, "Devkit", 0);
        WriteStringField(ms, "DevkitGameID", "");
        WriteIntField(ms, "DevkitOverrideAppID", 0);
        WriteIntField(ms, "LastPlayTime", 0);

        // Empty tags sub-object
        ms.WriteByte(0x00);
        WriteCString(ms, "tags");
        ms.WriteByte(0x08);

        // End of entry
        ms.WriteByte(0x08);

        return ms.ToArray();
    }

    private static string ReadCString(byte[] data, ref int pos)
    {
        int start = pos;
        while (pos < data.Length && data[pos] != 0x00) pos++;
        var str = Encoding.UTF8.GetString(data, start, pos - start);
        if (pos < data.Length) pos++;
        return str;
    }

    private static void SkipCString(byte[] data, ref int pos)
    {
        while (pos < data.Length && data[pos] != 0x00) pos++;
        if (pos < data.Length) pos++;
    }

    private static void SkipVdfObject(byte[] data, ref int pos)
    {
        while (pos < data.Length)
        {
            byte type = data[pos];
            if (type == 0x08) { pos++; return; }
            pos++;
            SkipCString(data, ref pos);
            switch (type)
            {
                case 0x01:
                    SkipCString(data, ref pos);
                    break;
                case 0x02:
                    pos += 4;
                    break;
                case 0x00:
                    SkipVdfObject(data, ref pos);
                    break;
                default:
                    return;
            }
        }
    }

    private static void WriteCString(MemoryStream ms, string s)
    {
        var bytes = Encoding.UTF8.GetBytes(s);
        ms.Write(bytes, 0, bytes.Length);
        ms.WriteByte(0x00);
    }

    private static void WriteStringField(MemoryStream ms, string key, string value)
    {
        ms.WriteByte(0x01);
        WriteCString(ms, key);
        WriteCString(ms, value);
    }

    private static void WriteIntField(MemoryStream ms, string key, int value)
    {
        ms.WriteByte(0x02);
        WriteCString(ms, key);
        ms.Write(BitConverter.GetBytes(value), 0, 4);
    }
}
