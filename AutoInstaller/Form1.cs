using System.Diagnostics;

namespace AutoInstaller;

public partial class Form1 : Form
{
    private FileSystemWatcher? _watcher;
    private System.Windows.Forms.Timer? _countdownTimer;
    private System.Windows.Forms.Timer? _quietTimer;
    private System.Windows.Forms.Timer? _installTimer;
    private System.Windows.Forms.Timer? _pollTimer;
    private DateTime _installStartTime;
    private DateTime _lastFolderActivity;
    private string? _installDir;
    private int _countdownRemaining;
    private string? _detectedSetupPath;
    private CancellationTokenSource? _cts;
    private Process? _installProc;
    private bool _installCompleted;
    private static readonly HttpClient _httpClient = new();

    // qBittorrent (local)
    private string? _qbtExePath;

    // Clipboard magnet
    private System.Windows.Forms.Timer? _clipboardTimer;
    private string? _lastClipboardText;

    // Bookmarks
    private List<Bookmark> _bookmarks = [];

    public Form1()
    {
        InitializeComponent();
        LoadDrives();
        WireEvents();
        LoadSettings();
        DetectQBittorrent();
        InitBrowser();
        LoadBookmarks();
        StartClipboardMonitor();
        FormClosing += (s, e) => SaveSettings();
        Load += (s, e) => EnsureClipboardMonitorRunning();
    }

    private void LoadSettings()
    {
        var s = SettingsStore.Load();
        if (!string.IsNullOrEmpty(s.WatchFolder))
            txtFolderPath.Text = s.WatchFolder;
        if (!string.IsNullOrEmpty(s.SetupFileName))
            txtSetupFileName.Text = s.SetupFileName;
        // Set drive first — its SelectedIndexChanged would overwrite install path,
        // so we set install path AFTER
        if (!string.IsNullOrEmpty(s.DrivePath))
        {
            for (int i = 0; i < cmbDrive.Items.Count; i++)
            {
                if (cmbDrive.Items[i] is DriveItem d && d.DrivePath.Equals(s.DrivePath, StringComparison.OrdinalIgnoreCase))
                {
                    cmbDrive.SelectedIndex = i;
                    break;
                }
            }
        }
        // Apply saved install path after drive selection so it isn't overwritten
        if (!string.IsNullOrEmpty(s.InstallPath))
            txtInstallPath.Text = s.InstallPath;
        chkWaitForQuiet.Checked = s.WaitForQuiet;
        nudQuietSeconds.Value = Math.Clamp(s.QuietSeconds, (int)nudQuietSeconds.Minimum, (int)nudQuietSeconds.Maximum);
        nudDelay.Value = Math.Clamp(s.DelayValue, nudDelay.Minimum, nudDelay.Maximum);
        if (s.TimeUnitIndex >= 0 && s.TimeUnitIndex < cmbTimeUnit.Items.Count)
            cmbTimeUnit.SelectedIndex = s.TimeUnitIndex;
        if (!string.IsNullOrEmpty(s.NtfyTopic))
            txtNtfyTopic.Text = s.NtfyTopic;
        if (!string.IsNullOrEmpty(s.QbtExePath) && File.Exists(s.QbtExePath))
        {
            _qbtExePath = s.QbtExePath;
            txtQbtExe.Text = s.QbtExePath;
        }
        chkClipboardMagnet.Checked = s.ClipboardMagnet;
        chkInstallDirectX.Checked = s.InstallDirectX;
        chkInstallVCRedist.Checked = s.InstallVCRedist;
    }

    private void SaveSettings()
    {
        var s = new AppSettings
        {
            WatchFolder = txtFolderPath.Text.Trim(),
            SetupFileName = txtSetupFileName.Text.Trim(),
            InstallPath = txtInstallPath.Text.Trim(),
            DrivePath = cmbDrive.SelectedItem is DriveItem d ? d.DrivePath : "",
            WaitForQuiet = chkWaitForQuiet.Checked,
            QuietSeconds = (int)nudQuietSeconds.Value,
            DelayValue = nudDelay.Value,
            TimeUnitIndex = cmbTimeUnit.SelectedIndex,
            NtfyTopic = txtNtfyTopic.Text.Trim(),
            QbtExePath = _qbtExePath ?? "",
            ClipboardMagnet = chkClipboardMagnet.Checked,
            InstallDirectX = chkInstallDirectX.Checked,
            InstallVCRedist = chkInstallVCRedist.Checked
        };
        SettingsStore.Save(s);
    }

    private void LoadDrives()
    {
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (drive.IsReady)
            {
                string label = string.IsNullOrEmpty(drive.VolumeLabel)
                    ? drive.Name
                    : $"{drive.Name}  ({drive.VolumeLabel})";
                cmbDrive.Items.Add(new DriveItem(drive.Name, label));
            }
        }
        if (cmbDrive.Items.Count > 0)
        {
            cmbDrive.SelectedIndex = 0;
            // Set initial install path
            var firstDrive = (DriveItem)cmbDrive.Items[0]!;
            txtInstallPath.Text = Path.Combine(firstDrive.DrivePath, "InstalledApps");
        }
    }

    private void WireEvents()
    {
        btnBrowse.Click += BtnBrowse_Click;
        btnBrowseInstall.Click += BtnBrowseInstall_Click;
        btnStart.Click += BtnStart_Click;
        btnStop.Click += BtnStop_Click;
        btnTestSetup.Click += BtnTestSetup_Click;
        btnTestNotify.Click += BtnTestNotify_Click;
        cmbDrive.SelectedIndexChanged += CmbDrive_SelectedIndexChanged;

        // qBittorrent events
        btnQbtBrowseExe.Click += BtnQbtBrowseExe_Click;

        // Browser navigation events
        btnNavBack.Click += (s, e) => { try { wvBrowser.GoBack(); } catch { } };
        btnNavForward.Click += (s, e) => { try { wvBrowser.GoForward(); } catch { } };
        btnNavGo.Click += (s, e) => NavigateBrowser();
        txtNavUrl.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { NavigateBrowser(); e.SuppressKeyPress = true; } };

        // Bookmark events
        btnAddBookmark.Click += BtnAddBookmark_Click;
        chkClipboardMagnet.CheckedChanged += ChkClipboardMagnet_CheckedChanged;
    }

    private void BtnBrowse_Click(object? sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Select the folder to watch for setup.exe"
        };
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            txtFolderPath.Text = dlg.SelectedPath;
        }
    }

    private void CmbDrive_SelectedIndexChanged(object? sender, EventArgs e)
    {
        // Only auto-fill install path if it's empty or still the default for any drive
        // (i.e. don't overwrite a user-customized path)
        string current = txtInstallPath.Text.Trim();
        bool isDefault = string.IsNullOrEmpty(current)
            || DriveInfo.GetDrives().Any(d =>
                current.Equals(Path.Combine(d.Name, "InstalledApps"), StringComparison.OrdinalIgnoreCase));

        if (isDefault && cmbDrive.SelectedItem is DriveItem drive)
        {
            txtInstallPath.Text = Path.Combine(drive.DrivePath, "InstalledApps");
        }
    }

    private void BtnBrowseInstall_Click(object? sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Select install location",
            ShowNewFolderButton = true
        };
        if (cmbDrive.SelectedItem is DriveItem drive)
        {
            dlg.SelectedPath = drive.DrivePath;
        }
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            txtInstallPath.Text = dlg.SelectedPath;
        }
    }

    private void BtnStart_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtFolderPath.Text))
        {
            MessageBox.Show("Please select a folder to watch.", "Missing Folder",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (cmbDrive.SelectedItem is not DriveItem)
        {
            MessageBox.Show("Please select an install drive.", "Missing Drive",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string setupName = txtSetupFileName.Text.Trim();
        if (string.IsNullOrWhiteSpace(setupName))
        {
            MessageBox.Show("Please enter the setup file name.", "Missing File Name",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _cts = new CancellationTokenSource();
        SetWatching(true);

        if (chkWaitForQuiet.Checked)
        {
            // Watch entire folder for any activity, trigger when quiet
            StartFolderQuietWatch(setupName);
        }
        else
        {
            // Original behavior: just watch for the specific file
            StartFileWatch(setupName);
        }
    }

    private string? ResolveSetupPath(string folder, string setupName)
    {
        if (!Directory.Exists(folder)) return null;

        // Build list of name patterns to match
        var names = new List<string> { setupName };
        if (!setupName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            names.Add(setupName + ".exe");

        // Helper: check a single directory for a matching setup file
        FileInfo? FindSetupIn(string dir)
        {
            try
            {
                foreach (var n in names)
                {
                    string path = Path.Combine(dir, n);
                    if (File.Exists(path)) return new FileInfo(path);
                }
            }
            catch { }
            return null;
        }

        // 1. Check root of watch folder
        var rootMatch = FindSetupIn(folder);
        if (rootMatch != null) return rootMatch.FullName;

        // 2. Find the NEWEST subfolder (= the current download) and only search there
        try
        {
            var subDirs = new DirectoryInfo(folder)
                .GetDirectories()
                .OrderByDescending(d => d.CreationTime)
                .ToArray();

            foreach (var sub in subDirs)
            {
                // Check this subfolder
                var match = FindSetupIn(sub.FullName);
                if (match != null)
                {
                    Log($"Found setup in newest folder: {sub.Name}");
                    return match.FullName;
                }

                // Check one level deeper inside this subfolder
                try
                {
                    foreach (var deep in sub.GetDirectories())
                    {
                        var deepMatch = FindSetupIn(deep.FullName);
                        if (deepMatch != null)
                        {
                            Log($"Found setup in: {sub.Name}\\{deep.Name}");
                            return deepMatch.FullName;
                        }
                    }
                }
                catch { }

                // Only check the newest subfolder — don't fall through to older ones
                break;
            }
        }
        catch { }

        return null;
    }

    private void StartFileWatch(string setupName)
    {
        // Check if setup already exists in folder (or any subfolder)
        string? existingPath = ResolveSetupPath(txtFolderPath.Text, setupName);
        if (existingPath != null)
        {
            Log($"Found existing {Path.GetFileName(existingPath)}");
            OnSetupDetected(existingPath);
            return;
        }

        Log($"Watching for {setupName} in {txtFolderPath.Text} (and subfolders)");
        SetStatus("Watching for setup file...");

        _watcher = new FileSystemWatcher(txtFolderPath.Text)
        {
            Filter = "*",
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime,
            EnableRaisingEvents = true
        };

        _watcher.Created += (s, ev) =>
        {
            this.BeginInvoke(() =>
            {
                // ev.Name may be "SubFolder\setup.exe" — compare just the filename
                string name = Path.GetFileName(ev.Name ?? "");
                if (name.Equals(setupName, StringComparison.OrdinalIgnoreCase)
                    || name.Equals(setupName + ".exe", StringComparison.OrdinalIgnoreCase))
                {
                    Log($"Detected: {ev.Name}");
                    OnSetupDetected(ev.FullPath);
                }
            });
        };
    }

    private void StartFolderQuietWatch(string setupName)
    {
        int quietSec = (int)nudQuietSeconds.Value;
        Log($"Watching folder — will trigger after {quietSec}s of no activity");
        SetStatus($"Watching for folder activity to stop ({quietSec}s quiet)...");

        _lastFolderActivity = DateTime.Now;

        _watcher = new FileSystemWatcher(txtFolderPath.Text)
        {
            Filter = "*.*",
            IncludeSubdirectories = true,
            InternalBufferSize = 65536, // 64KB buffer to avoid missing events
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName
                         | NotifyFilters.Size | NotifyFilters.LastWrite
                         | NotifyFilters.CreationTime,
            EnableRaisingEvents = true
        };

        // Log if buffer overflows so user knows events were missed
        _watcher.Error += (s, ev) =>
        {
            this.BeginInvoke(() =>
            {
                Log($"Watcher buffer overflow — polling will catch up");
            });
        };

        void ResetQuiet(object sender, FileSystemEventArgs ev)
        {
            this.BeginInvoke(() =>
            {
                _lastFolderActivity = DateTime.Now;
                Log($"Activity: {ev.ChangeType} {ev.Name}");
                SetStatus($"Download active — last: {ev.Name}");
            });
        }

        _watcher.Created += ResetQuiet;
        _watcher.Changed += ResetQuiet;
        _watcher.Deleted += ResetQuiet;
        _watcher.Renamed += (s, ev) => ResetQuiet(s!, ev);

        // Poll every 2 seconds as a backup — checks actual folder write time
        // This catches activity even if FileSystemWatcher misses events
        _pollTimer = new System.Windows.Forms.Timer { Interval = 2000 };
        _pollTimer.Tick += (s, ev) =>
        {
            try
            {
                // Check if any file was recently modified
                var dir = new DirectoryInfo(txtFolderPath.Text);
                if (dir.Exists)
                {
                    var newest = dir.EnumerateFiles("*", SearchOption.AllDirectories)
                        .OrderByDescending(f => f.LastWriteTime)
                        .FirstOrDefault();

                    if (newest != null && newest.LastWriteTime > _lastFolderActivity)
                    {
                        _lastFolderActivity = newest.LastWriteTime;
                        Log($"Poll detected activity: {newest.Name}");
                        SetStatus($"Download active — last: {newest.Name}");
                    }
                }
            }
            catch { /* folder may be busy */ }

            // Check if quiet period has elapsed since last activity
            int currentQuietSec = (int)nudQuietSeconds.Value;
            double silentFor = (DateTime.Now - _lastFolderActivity).TotalSeconds;

            // Both conditions must be true: folder quiet AND setup file exists
            string? setupPath = ResolveSetupPath(txtFolderPath.Text, setupName);

            if (silentFor >= currentQuietSec && setupPath != null)
            {
                _pollTimer?.Stop();
                _pollTimer?.Dispose();
                _pollTimer = null;
                _watcher?.Dispose();
                _watcher = null;

                Log($"Folder quiet for {currentQuietSec}s — found {Path.GetFileName(setupPath)}");
                OnSetupDetected(setupPath);
            }
            else if (silentFor >= currentQuietSec && setupPath == null)
            {
                // Folder is quiet but setup file not here yet — keep watching
                SetStatus($"Folder quiet but waiting for {setupName}...");
            }
            else
            {
                if (setupPath != null)
                    SetStatus($"Found {Path.GetFileName(setupPath)} — waiting for folder to settle ({(int)silentFor}s / {currentQuietSec}s)");
                else
                    SetStatus($"Downloading — quiet for {(int)silentFor}s / {currentQuietSec}s needed");
            }
        };

        _pollTimer.Start();
    }

    private void BtnStop_Click(object? sender, EventArgs e)
    {
        Cancel();
    }

    private void Cancel()
    {
        _cts?.Cancel();
        _watcher?.Dispose();
        _watcher = null;
        _quietTimer?.Stop();
        _quietTimer?.Dispose();
        _quietTimer = null;
        _pollTimer?.Stop();
        _pollTimer?.Dispose();
        _pollTimer = null;
        _installTimer?.Stop();
        _installTimer?.Dispose();
        _installTimer = null;
        _countdownTimer?.Stop();
        _countdownTimer?.Dispose();
        _countdownTimer = null;
        _detectedSetupPath = null;
        _installProc = null;
        _installCompleted = false;
        progressBar.Value = 0;
        SetStatus("Stopped");
        Log("Cancelled by user");
        SetWatching(false);
    }

    private async void OnSetupDetected(string setupPath)
    {
        _detectedSetupPath = setupPath;
        _watcher?.Dispose();
        _watcher = null;

        // Wait for the file to be fully written / not locked
        SetStatus("Waiting for file to finish downloading...");
        Log("Waiting for file to be ready...");

        bool ready = await WaitForFileReady(setupPath, _cts!.Token);
        if (!ready)
        {
            if (!_cts.IsCancellationRequested)
            {
                Log("File never became ready");
                SetStatus("Error: file locked");
            }
            return;
        }

        Log("File is ready!");

        int delaySec = GetDelayInSeconds();
        if (delaySec > 0)
        {
            StartCountdown(delaySec);
        }
        else
        {
            LaunchInstaller();
        }
    }

    private int GetDelayInSeconds()
    {
        int val = (int)nudDelay.Value;
        return cmbTimeUnit.SelectedIndex switch
        {
            1 => val * 60,    // Minutes
            2 => val * 3600,  // Hours
            _ => val          // Seconds
        };
    }

    private static string FormatTime(int totalSeconds)
    {
        if (totalSeconds >= 3600)
        {
            int h = totalSeconds / 3600;
            int m = (totalSeconds % 3600) / 60;
            int s = totalSeconds % 60;
            return $"{h}h {m}m {s}s";
        }
        if (totalSeconds >= 60)
        {
            int m = totalSeconds / 60;
            int s = totalSeconds % 60;
            return $"{m}m {s}s";
        }
        return $"{totalSeconds}s";
    }

    private async Task<bool> WaitForFileReady(string path, CancellationToken ct)
    {
        for (int i = 0; i < 300; i++) // try for up to 5 minutes
        {
            if (ct.IsCancellationRequested) return false;
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
                return true;
            }
            catch (IOException)
            {
                if (i % 10 == 0) // log every 10 seconds
                {
                    this.BeginInvoke(() =>
                    {
                        Log($"File still locked — retrying ({i}s)...");
                        SetStatus($"Waiting for file to be ready ({i}s)...");
                    });
                }
                await Task.Delay(1000, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }
        return false;
    }

    private void StartCountdown(int seconds)
    {
        _countdownRemaining = seconds;
        progressBar.Maximum = seconds;
        progressBar.Value = 0;
        SetStatus($"Installing in {FormatTime(_countdownRemaining)}...");
        Log($"Countdown: {FormatTime(seconds)} — adjust the delay spinner to change");

        // Lock folder/file name but keep delay editable
        txtFolderPath.Enabled = false;
        txtSetupFileName.Enabled = false;
        cmbDrive.Enabled = false;
        txtInstallPath.Enabled = false;
        btnBrowseInstall.Enabled = false;
        nudDelay.Enabled = true; // keep editable!
        cmbTimeUnit.Enabled = true; // keep editable!

        _countdownTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _countdownTimer.Tick += CountdownTick;
        _countdownTimer.Start();
    }

    private void CountdownTick(object? sender, EventArgs e)
    {
        if (_cts?.IsCancellationRequested == true)
        {
            _countdownTimer?.Stop();
            return;
        }

        // Allow live adjustment: if user changes nudDelay or unit, respect it
        int configuredDelaySec = GetDelayInSeconds();
        int elapsed = progressBar.Maximum - _countdownRemaining + 1;

        if (configuredDelaySec != progressBar.Maximum)
        {
            // User changed the delay mid-countdown — recalculate remaining
            _countdownRemaining = Math.Max(0, configuredDelaySec - elapsed);
            progressBar.Maximum = configuredDelaySec;
        }
        else
        {
            _countdownRemaining--;
        }

        int progressVal = Math.Min(progressBar.Maximum - _countdownRemaining, progressBar.Maximum);
        progressBar.Value = Math.Max(0, progressVal);

        if (_countdownRemaining <= 0)
        {
            _countdownTimer?.Stop();
            _countdownTimer?.Dispose();
            _countdownTimer = null;
            LaunchInstaller();
        }
        else
        {
            SetStatus($"Installing in {FormatTime(_countdownRemaining)}...");
        }
    }

    private void LaunchInstaller()
    {
        if (_detectedSetupPath == null) return;

        var drive = (DriveItem)cmbDrive.SelectedItem!;
        string baseDir = txtInstallPath.Text.Trim();
        if (string.IsNullOrWhiteSpace(baseDir))
        {
            baseDir = Path.Combine(drive.DrivePath, "InstalledApps");
        }

        // Create a game-named subfolder from the setup's parent folder
        string gameName = GetGameName(_detectedSetupPath);
        string installDir = Path.Combine(baseDir, gameName);

        Log($"Game detected: {gameName}");
        Log($"Install folder: {installDir}");
        SetStatus("Launching installer...");
        progressBar.Value = progressBar.Maximum;

        try
        {
            // Run as admin (app already elevated via manifest)
            var psi = new ProcessStartInfo
            {
                FileName = _detectedSetupPath,
                Arguments = BuildInstallerArgs(installDir),
                UseShellExecute = true
            };

            var proc = Process.Start(psi);
            if (proc != null)
            {
                Log("Setup launched — installer window running alongside");

                _installProc = proc;
                _installCompleted = false;

                // Start elapsed timer
                _installStartTime = DateTime.Now;
                _installDir = installDir;
                progressBar.Style = ProgressBarStyle.Marquee;
                _installTimer = new System.Windows.Forms.Timer { Interval = 1000 };
                _installTimer.Tick += InstallTimerTick;
                _installTimer.Start();

                // Primary completion detection — runs on background thread.
                // Uses blocking WaitForExit() which is more reliable than async
                // for installers that spawn child processes (e.g. FitGirl repacks).
                Task.Run(() =>
                {
                    try { proc.WaitForExit(); } catch { }
                    this.BeginInvoke(HandleInstallComplete);
                });
            }
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            SetStatus("UAC prompt declined");
            Log("User cancelled the UAC elevation prompt");
            SetWatching(false);
        }
        catch (Exception ex)
        {
            SetStatus("Error launching installer");
            Log($"Error: {ex.Message}");
            SetWatching(false);
        }
    }

    private void SetWatching(bool watching)
    {
        btnStart.Enabled = !watching;
        btnStop.Enabled = watching;
        btnBrowse.Enabled = !watching;
        txtSetupFileName.Enabled = !watching;
        cmbDrive.Enabled = !watching;
        txtFolderPath.Enabled = !watching;
        txtInstallPath.Enabled = !watching;
        btnBrowseInstall.Enabled = !watching;
        chkWaitForQuiet.Enabled = !watching;
        nudQuietSeconds.Enabled = !watching;
        nudDelay.Enabled = true; // always allow changing delay
        cmbTimeUnit.Enabled = true; // always allow changing unit
    }

    private void SetStatus(string text)
    {
        lblStatus.Text = $"Status: {text}";
    }

    private string BuildInstallerArgs(string installDir)
    {
        var args = $"/SILENT /DIR=\"{installDir}\"";

        // Build /MERGETASKS to suppress unchecked redistributables
        var suppress = new List<string>();
        if (!chkInstallDirectX.Checked)
        {
            suppress.AddRange(new[] { "!directx", "!dx", "!redist\\directx" });
        }
        if (!chkInstallVCRedist.Checked)
        {
            suppress.AddRange(new[] {
                "!vcredist", "!vcredist2005", "!vcredist2008", "!vcredist2010",
                "!vcredist2012", "!vcredist2013", "!vcredist2015", "!vcredist2017",
                "!vcredist2019", "!vcredist2022", "!vcpp",
                "!redist\\vcredist", "!redist\\vcredist2005x86",
                "!redist\\vcredist2005x64", "!redist\\vcredist2008x86",
                "!redist\\vcredist2008x64", "!redist\\vcredist2010x86",
                "!redist\\vcredist2010x64", "!redist\\vcredist2012x86",
                "!redist\\vcredist2012x64", "!redist\\vcredist2013x86",
                "!redist\\vcredist2013x64", "!redist\\vcredist2015_2022x86",
                "!redist\\vcredist2015_2022x64"
            });
        }
        if (suppress.Count > 0)
        {
            args += $" /MERGETASKS=\"{string.Join(",", suppress)}\"";
        }

        return args;
    }

    private void BtnTestSetup_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Select a setup.exe to test",
            Filter = "Executables (*.exe)|*.exe|All files (*.*)|*.*"
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        string testPath = dlg.FileName;
        var drive = cmbDrive.SelectedItem as DriveItem;
        string baseDir = txtInstallPath.Text.Trim();
        if (string.IsNullOrWhiteSpace(baseDir) && drive != null)
        {
            baseDir = Path.Combine(drive.DrivePath, "InstalledApps");
        }

        string gameName = GetGameName(testPath);
        string installDir = Path.Combine(baseDir, gameName);

        Log($"TEST: Game name: {gameName}");
        Log($"TEST: Launching {testPath}");
        Log($"TEST: Install dir → {installDir}");
        SetStatus("Testing setup...");

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = testPath,
                Arguments = BuildInstallerArgs(installDir),
                UseShellExecute = true
            };

            var proc = Process.Start(psi);
            if (proc != null)
            {
                Log("TEST: Setup launched — installer window running alongside");
                Task.Run(() =>
                {
                    try { proc.WaitForExit(); } catch { }
                    int code = 0;
                    try { code = proc.ExitCode; } catch { }
                    this.BeginInvoke(() =>
                    {
                        Log($"TEST: Exited with code {code}");
                        SetStatus(code == 0 ? "Test complete!" : $"Test exited code {code}");
                    });
                });
            }
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            Log("TEST: User cancelled UAC prompt");
            SetStatus("Test cancelled (UAC declined)");
        }
        catch (Exception ex)
        {
            Log($"TEST: Error — {ex.Message}");
            SetStatus("Test error");
        }
    }

    private void BtnTestNotify_Click(object? sender, EventArgs e)
    {
        string topic = txtNtfyTopic.Text.Trim();
        if (string.IsNullOrWhiteSpace(topic))
        {
            MessageBox.Show("Enter a topic name first.", "No Topic", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        Log("Sending test notification...");
        _ = SendNtfyNotificationAsync("\uD83D\uDD14 AutoInstaller test notification!");
    }

    private async Task SendNtfyNotificationAsync(string message)
    {
        string topic = txtNtfyTopic.Text.Trim();
        if (string.IsNullOrWhiteSpace(topic)) return;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"https://ntfy.sh/{Uri.EscapeDataString(topic)}");
            request.Content = new StringContent(message, System.Text.Encoding.UTF8, "text/plain");
            request.Headers.TryAddWithoutValidation("Title", "AutoInstaller");
            request.Headers.TryAddWithoutValidation("Tags", "computer");

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
                Log("Notification sent!");
            else
                Log($"Notification failed: HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            Log($"Notification error: {ex.Message}");
        }
    }

    private void Log(string msg)
    {
        string line = $"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}";
        txtLog.AppendText(line);
    }

    private record DriveItem(string DrivePath, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }

    private static string GetGameName(string setupPath)
    {
        // Get the parent folder name of the setup exe
        string? folderPath = Path.GetDirectoryName(setupPath);
        string folderName = Path.GetFileName(folderPath) ?? "Unknown";

        // Strip common bracket tags like [FitGirl Repack], (v1.2), etc.
        string cleaned = System.Text.RegularExpressions.Regex.Replace(folderName, @"\s*[\[\(][^\]\)]*[\]\)]", "");
        cleaned = cleaned.Trim();

        // Remove trailing dashes/underscores from cleanup
        cleaned = cleaned.TrimEnd('-', '_', ' ');

        // If nothing left after cleanup, use original folder name
        if (string.IsNullOrWhiteSpace(cleaned))
            cleaned = folderName;

        // Sanitize for filesystem
        foreach (char c in Path.GetInvalidFileNameChars())
            cleaned = cleaned.Replace(c, '_');

        return cleaned;
    }

    // Called when the install process exits (either via WaitForExit or timer backup).
    private void HandleInstallComplete()
    {
        if (_installCompleted) return; // guard against double-fire
        _installCompleted = true;

        _installTimer?.Stop();
        _installTimer?.Dispose();
        _installTimer = null;
        progressBar.Style = ProgressBarStyle.Continuous;
        progressBar.Value = progressBar.Maximum;

        var elapsed = DateTime.Now - _installStartTime;
        int code = 0;
        try { code = _installProc?.ExitCode ?? 0; } catch { }
        _installProc = null;

        if (code == 0)
        {
            string size = _installDir != null ? GetFolderSizeString(_installDir) : "?";
            SetStatus($"Install complete! ({FormatTime((int)elapsed.TotalSeconds)}, {size})");
            Log($"Finished in {FormatTime((int)elapsed.TotalSeconds)} — exit code {code}");
            Log($"Installed size: {size}");
            _ = SendNtfyNotificationAsync($"\u2705 Install complete! ({FormatTime((int)elapsed.TotalSeconds)}, {size})");
        }
        else
        {
            SetStatus($"Installer exited with code {code} ({FormatTime((int)elapsed.TotalSeconds)})");
            Log($"Exit code: {code} after {FormatTime((int)elapsed.TotalSeconds)}");
            _ = SendNtfyNotificationAsync($"\u26a0\ufe0f Installer exited with code {code}");
        }
        SetWatching(false);
    }

    private void InstallTimerTick(object? sender, EventArgs e)
    {
        // Backup completion detection: catches installers that spawn a child
        // process and exit the parent immediately (e.g. FitGirl repacks).
        if (_installProc != null && !_installCompleted)
        {
            try
            {
                if (_installProc.HasExited)
                {
                    HandleInstallComplete();
                    return;
                }
            }
            catch { /* process handle may already be invalid */ }
        }

        var elapsed = DateTime.Now - _installStartTime;
        string elapsedStr = FormatTime((int)elapsed.TotalSeconds);

        if (_installDir != null && Directory.Exists(_installDir))
        {
            string size = GetFolderSizeString(_installDir);
            SetStatus($"Installing... {elapsedStr} elapsed \u2014 {size} written");
        }
        else
        {
            SetStatus($"Installing... {elapsedStr} elapsed");
        }
    }

    private static string GetFolderSizeString(string path)
    {
        try
        {
            if (!Directory.Exists(path)) return "0 MB";
            long bytes = new DirectoryInfo(path)
                .EnumerateFiles("*", SearchOption.AllDirectories)
                .Sum(f => f.Length);

            if (bytes >= 1_073_741_824)
                return $"{bytes / 1_073_741_824.0:F1} GB";
            else
                return $"{bytes / 1_048_576.0:F1} MB";
        }
        catch
        {
            return "? MB";
        }
    }

    // ======================================================
    //  Embedded Browser
    // ======================================================

    private async void InitBrowser()
    {
        try
        {
            await wvBrowser.EnsureCoreWebView2Async();
            wvBrowser.CoreWebView2.SourceChanged += (s, e) =>
            {
                this.BeginInvoke(() =>
                {
                    txtNavUrl.Text = wvBrowser.CoreWebView2.Source;
                });
            };
            // Keep all link clicks inside the embedded browser (prevent opening external window)
            wvBrowser.CoreWebView2.NewWindowRequested += (s, e) =>
            {
                e.Handled = true;
                wvBrowser.CoreWebView2.Navigate(e.Uri);
            };
            wvBrowser.CoreWebView2.Navigate("https://www.google.com");
        }
        catch (Exception ex)
        {
            Log($"Browser init failed: {ex.Message}");
        }
    }

    private void NavigateBrowser()
    {
        string url = txtNavUrl.Text.Trim();
        if (string.IsNullOrEmpty(url)) return;
        if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            url = "https://" + url;
        try
        {
            wvBrowser.CoreWebView2?.Navigate(url);
        }
        catch { }
    }

    // ======================================================
    //  qBittorrent Integration (local)
    // ======================================================

    private void DetectQBittorrent()
    {
        // Skip auto-detect if already loaded from saved settings
        if (_qbtExePath != null)
        {
            lblQbtExeStatus.Text = "\u2714 Found";
            lblQbtExeStatus.ForeColor = Green;
            return;
        }

        _qbtExePath = QBitLocal.FindExePath();
        if (_qbtExePath != null)
        {
            txtQbtExe.Text = _qbtExePath;
            lblQbtExeStatus.Text = "\u2714 Found";
            lblQbtExeStatus.ForeColor = Green;
            Log($"qBittorrent found: {_qbtExePath}");
        }
        else
        {
            lblQbtExeStatus.Text = "\u2718 Not found — browse manually";
            lblQbtExeStatus.ForeColor = Red;
        }

    }

    private void BtnQbtBrowseExe_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Select qbittorrent.exe",
            Filter = "qBittorrent (qbittorrent.exe)|qbittorrent.exe|All files (*.*)|*.*"
        };
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _qbtExePath = dlg.FileName;
            txtQbtExe.Text = _qbtExePath;
            lblQbtExeStatus.Text = "\u2714 Selected";
            lblQbtExeStatus.ForeColor = Green;
            Log($"qBittorrent set to: {_qbtExePath}");
        }
    }



    // ===== Clipboard Magnet Monitor =====

    private void StartClipboardMonitor()
    {
        _clipboardTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _clipboardTimer.Tick += ClipboardTick;
        // Actual start is deferred to Form.Load via EnsureClipboardMonitorRunning()
    }

    private void EnsureClipboardMonitorRunning()
    {
        if (!chkClipboardMagnet.Checked) return;
        if (_clipboardTimer == null || _clipboardTimer.Enabled) return;
        try
        {
            // Capture current clipboard so first tick doesn't re-send stale content
            _lastClipboardText = Clipboard.ContainsText() ? Clipboard.GetText() : null;
        }
        catch { _lastClipboardText = null; }
        _clipboardTimer.Start();
    }

    private void ChkClipboardMagnet_CheckedChanged(object? sender, EventArgs e)
    {
        if (chkClipboardMagnet.Checked)
        {
            _lastClipboardText = null;
            _clipboardTimer?.Start();
            Log("Clipboard magnet monitoring enabled");
        }
        else
        {
            _clipboardTimer?.Stop();
            Log("Clipboard magnet monitoring disabled");
        }
    }

    private void ClipboardTick(object? sender, EventArgs e)
    {
        if (_qbtExePath == null) return;

        try
        {
            if (!Clipboard.ContainsText()) return;
            string text = Clipboard.GetText().Trim();
            if (string.IsNullOrEmpty(text)) return;
            if (text == _lastClipboardText) return;
            _lastClipboardText = text;

            if (text.StartsWith("magnet:?", StringComparison.OrdinalIgnoreCase))
            {
                // Use the watch folder as download destination so QB downloads right where we're watching
                string? savePath = !string.IsNullOrWhiteSpace(txtFolderPath.Text) ? txtFolderPath.Text : null;

                Log($"Clipboard magnet detected — sending to qBittorrent...");
                if (savePath != null)
                    Log($"Download path: {savePath}");
                lblQbtStatus.Text = "Adding magnet from clipboard...";
                bool ok = QBitLocal.AddMagnet(_qbtExePath, text, savePath);
                if (ok)
                {
                    Log("Magnet sent to qBittorrent");
                    lblQbtStatus.Text = savePath != null
                        ? $"Magnet sent — downloading to: {savePath}"
                        : "Magnet sent to qBittorrent!";
                    lblQbtStatus.ForeColor = Green;

                    // Auto-start watching if not already active
                    if (savePath != null && btnStart.Enabled)
                    {
                        Log("Auto-starting watch for downloaded content...");
                        tabControl.SelectedTab = tabInstaller;
                        BtnStart_Click(null, EventArgs.Empty);
                    }
                }
                else
                {
                    Log("Failed to send magnet");
                    lblQbtStatus.Text = "Failed to send magnet";
                    lblQbtStatus.ForeColor = Red;
                }
            }
        }
        catch { /* clipboard may be locked by another app */ }
    }

    // ===== Bookmarks =====

    private void LoadBookmarks()
    {
        _bookmarks = BookmarkStore.Load();
        RebuildBookmarkCarousel();
    }

    private void RebuildBookmarkCarousel()
    {
        // Keep only the "+ Add" button, remove bookmark buttons
        for (int i = flpBookmarks.Controls.Count - 1; i >= 0; i--)
        {
            if (flpBookmarks.Controls[i] != btnAddBookmark)
            {
                flpBookmarks.Controls[i].Dispose();
            }
        }

        // Insert bookmark buttons before the "+ Add" button
        int insertIdx = 0;
        foreach (var bm in _bookmarks)
        {
            var btn = CreateBookmarkButton(bm);
            flpBookmarks.Controls.Add(btn);
            flpBookmarks.Controls.SetChildIndex(btn, insertIdx++);
        }
    }

    private Button CreateBookmarkButton(Bookmark bm)
    {
        var btn = new Button
        {
            Text = bm.Title.Length > 8 ? bm.Title[..8] + "…" : bm.Title,
            Size = new Size(80, 56),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(40, 42, 62),
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 8F),
            Margin = new Padding(3),
            Tag = bm,
            TextAlign = ContentAlignment.BottomCenter,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = Border;
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(55, 58, 78);

        // Try to load favicon
        _ = LoadFaviconAsync(btn, bm.Url);

        // Left-click navigates in embedded browser, right-click removes
        btn.Click += (s, e) =>
        {
            try { wvBrowser.CoreWebView2?.Navigate(bm.Url); }
            catch { }
        };

        btn.MouseUp += (s, e) =>
        {
            if (e.Button == MouseButtons.Right)
            {
                var result = MessageBox.Show(
                    $"Remove bookmark \"{bm.Title}\"?",
                    "Remove Bookmark",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    _bookmarks.Remove(bm);
                    BookmarkStore.Save(_bookmarks);
                    RebuildBookmarkCarousel();
                }
            }
        };

        return btn;
    }

    private async Task LoadFaviconAsync(Button btn, string url)
    {
        try
        {
            var uri = new Uri(url);
            string faviconUrl = $"{uri.Scheme}://{uri.Host}/favicon.ico";
            using var resp = await _httpClient.GetAsync(faviconUrl);
            if (resp.IsSuccessStatusCode)
            {
                var data = await resp.Content.ReadAsByteArrayAsync();
                using var ms = new MemoryStream(data);
                var img = Image.FromStream(ms);
                var bmp = new Bitmap(img, 32, 32);
                if (!btn.IsDisposed)
                {
                    btn.Image = bmp;
                    btn.ImageAlign = ContentAlignment.TopCenter;
                    btn.TextAlign = ContentAlignment.BottomCenter;
                }
            }
        }
        catch { /* favicon not available */ }
    }

    private void BtnAddBookmark_Click(object? sender, EventArgs e)
    {
        string currentUrl = "";
        try { currentUrl = wvBrowser.CoreWebView2?.Source ?? ""; }
        catch { }

        using var form = new Form
        {
            Text = "Add Bookmark",
            Size = new Size(400, 180),
            StartPosition = FormStartPosition.CenterParent,
            BackColor = BG,
            ForeColor = TextPrimary,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            Font = new Font("Segoe UI", 10F)
        };

        var lblT = new Label { Text = "Title:", Location = new Point(12, 18), Size = new Size(50, 25) };
        var txtTitle = new TextBox
        {
            Location = new Point(65, 15), Size = new Size(300, 25),
            BackColor = ControlBg, ForeColor = TextPrimary,
            BorderStyle = BorderStyle.FixedSingle
        };

        var lblU = new Label { Text = "URL:", Location = new Point(12, 55), Size = new Size(50, 25) };
        var txtUrl = new TextBox
        {
            Text = currentUrl,
            Location = new Point(65, 52), Size = new Size(300, 25),
            BackColor = ControlBg, ForeColor = TextPrimary,
            BorderStyle = BorderStyle.FixedSingle
        };

        var btnOk = MakeBtn("Add", Green, GreenDim, 80, 32);
        btnOk.Location = new Point(200, 95);
        btnOk.DialogResult = DialogResult.OK;

        var btnCancel = MakeBtn("Cancel", Panel_, Border, 80, 32);
        btnCancel.Location = new Point(285, 95);
        btnCancel.DialogResult = DialogResult.Cancel;

        form.Controls.AddRange([lblT, txtTitle, lblU, txtUrl, btnOk, btnCancel]);
        form.AcceptButton = btnOk;
        form.CancelButton = btnCancel;

        if (form.ShowDialog(this) == DialogResult.OK)
        {
            string title = txtTitle.Text.Trim();
            string url = txtUrl.Text.Trim();
            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(url)) return;
            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                url = "https://" + url;

            var bm = new Bookmark(title, url);
            _bookmarks.Add(bm);
            BookmarkStore.Save(_bookmarks);
            RebuildBookmarkCarousel();
            Log($"Bookmark added: {title}");
        }
    }
}
