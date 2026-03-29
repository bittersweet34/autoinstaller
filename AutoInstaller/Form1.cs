using System.Diagnostics;

namespace AutoInstaller;

public partial class Form1 : Form
{
    private FileSystemWatcher? _watcher;
    private System.Windows.Forms.Timer? _countdownTimer;
    private System.Windows.Forms.Timer? _quietTimer;
    private int _countdownRemaining;
    private string? _detectedSetupPath;
    private CancellationTokenSource? _cts;

    public Form1()
    {
        InitializeComponent();
        LoadDrives();
        WireEvents();
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
        cmbDrive.SelectedIndexChanged += CmbDrive_SelectedIndexChanged;
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
        // When drive changes, reset the path to default subfolder on that drive
        if (cmbDrive.SelectedItem is DriveItem drive)
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

    private void StartFileWatch(string setupName)
    {
        // Check if setup already exists in folder
        string existingPath = Path.Combine(txtFolderPath.Text, setupName);
        if (File.Exists(existingPath))
        {
            Log($"Found existing {setupName}");
            OnSetupDetected(existingPath);
            return;
        }

        Log($"Watching for {setupName} in {txtFolderPath.Text}");
        SetStatus("Watching for setup file...");

        _watcher = new FileSystemWatcher(txtFolderPath.Text)
        {
            Filter = setupName,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime,
            EnableRaisingEvents = true
        };

        _watcher.Created += (s, ev) =>
        {
            this.BeginInvoke(() =>
            {
                Log($"Detected: {ev.Name}");
                OnSetupDetected(ev.FullPath);
            });
        };
    }

    private void StartFolderQuietWatch(string setupName)
    {
        int quietSec = (int)nudQuietSeconds.Value;
        Log($"Watching folder — will trigger after {quietSec}s of no activity");
        SetStatus($"Watching for folder activity to stop ({quietSec}s quiet)...");

        _watcher = new FileSystemWatcher(txtFolderPath.Text)
        {
            Filter = "*.*",
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName
                         | NotifyFilters.Size | NotifyFilters.LastWrite
                         | NotifyFilters.CreationTime,
            EnableRaisingEvents = true
        };

        // Quiet timer: resets every time we see activity
        _quietTimer = new System.Windows.Forms.Timer { Interval = quietSec * 1000 };
        _quietTimer.Tick += (s, ev) =>
        {
            _quietTimer?.Stop();
            _quietTimer?.Dispose();
            _quietTimer = null;

            // Folder is quiet — look for setup file
            string setupPath = Path.Combine(txtFolderPath.Text, setupName);
            if (File.Exists(setupPath))
            {
                Log($"Folder quiet — found {setupName}");
                OnSetupDetected(setupPath);
            }
            else
            {
                Log($"Folder quiet but {setupName} not found — still watching");
                SetStatus($"Waiting for {setupName}...");
                // Fall back to file-specific watch
                _watcher?.Dispose();
                StartFileWatch(setupName);
            }
        };

        void ResetQuiet(object sender, FileSystemEventArgs ev)
        {
            this.BeginInvoke(() =>
            {
                Log($"Activity: {ev.ChangeType} {ev.Name}");
                SetStatus($"Download active — last: {ev.Name}");
                _quietTimer?.Stop();
                _quietTimer?.Start();
            });
        }

        _watcher.Created += ResetQuiet;
        _watcher.Changed += ResetQuiet;
        _watcher.Renamed += (s, ev) => ResetQuiet(s!, ev);

        // Start the quiet timer (if folder already has content, timer starts now)
        _quietTimer.Start();
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
        _countdownTimer?.Stop();
        _countdownTimer?.Dispose();
        _countdownTimer = null;
        _detectedSetupPath = null;
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
        for (int i = 0; i < 120; i++) // try for up to 2 minutes
        {
            if (ct.IsCancellationRequested) return false;
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
                return true;
            }
            catch (IOException)
            {
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
        string installDir = txtInstallPath.Text.Trim();
        if (string.IsNullOrWhiteSpace(installDir))
        {
            installDir = Path.Combine(drive.DrivePath, "InstalledApps");
        }

        SetStatus("Launching installer...");
        Log($"Running: {_detectedSetupPath} → {installDir}");
        progressBar.Value = progressBar.Maximum;

        try
        {
            // Run as admin with silent install flags
            var psi = new ProcessStartInfo
            {
                FileName = _detectedSetupPath,
                Arguments = $"/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /DIR=\"{installDir}\"",
                UseShellExecute = true,
                Verb = "runas"
            };

            var proc = Process.Start(psi);
            if (proc != null)
            {
                SetStatus("Installer running...");
                Log("Setup launched with /VERYSILENT");

                // Monitor in background
                Task.Run(async () =>
                {
                    await proc.WaitForExitAsync();
                    this.BeginInvoke(() =>
                    {
                        int code = proc.ExitCode;
                        if (code == 0)
                        {
                            SetStatus("Install complete!");
                            Log($"Finished — exit code {code}");
                            progressBar.Value = progressBar.Maximum;
                        }
                        else
                        {
                            SetStatus($"Installer exited with code {code}");
                            Log($"Exit code: {code}");
                        }
                        SetWatching(false);
                    });
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
        string installDir = txtInstallPath.Text.Trim();
        if (string.IsNullOrWhiteSpace(installDir) && drive != null)
        {
            installDir = Path.Combine(drive.DrivePath, "InstalledApps");
        }

        Log($"TEST: Launching {testPath}");
        Log($"TEST: Install dir → {installDir}");
        SetStatus("Testing setup...");

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = testPath,
                Arguments = $"/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /DIR=\"{installDir}\"",
                UseShellExecute = true,
                Verb = "runas"
            };

            var proc = Process.Start(psi);
            if (proc != null)
            {
                Log("TEST: Setup launched with elevation");
                Task.Run(async () =>
                {
                    await proc.WaitForExitAsync();
                    this.BeginInvoke(() =>
                    {
                        Log($"TEST: Exited with code {proc.ExitCode}");
                        SetStatus(proc.ExitCode == 0 ? "Test complete!" : $"Test exited code {proc.ExitCode}");
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

    private void Log(string msg)
    {
        string line = $"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}";
        txtLog.AppendText(line);
    }

    private record DriveItem(string DrivePath, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }
}
