namespace AutoInstaller;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.AutoScaleMode = AutoScaleMode.Dpi;
        this.ClientSize = new Size(480, 685);
        this.Text = "Auto Installer";
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.FromArgb(30, 30, 30);
        this.ForeColor = Color.White;
        this.Font = new Font("Segoe UI", 10F);

        // === Title Label ===
        lblTitle = new Label
        {
            Text = "Auto Installer",
            Font = new Font("Segoe UI", 18F, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 180, 255),
            AutoSize = true,
            Location = new Point(150, 12)
        };

        // === Watch Folder Group ===
        grpFolder = new GroupBox
        {
            Text = "Watch Folder (drop setup.exe here or browse)",
            ForeColor = Color.FromArgb(180, 180, 180),
            Location = new Point(16, 52),
            Size = new Size(445, 70)
        };

        txtFolderPath = new TextBox
        {
            Location = new Point(12, 28),
            Size = new Size(340, 30),
            BackColor = Color.FromArgb(45, 45, 45),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            ReadOnly = true
        };

        btnBrowse = new Button
        {
            Text = "Browse",
            Location = new Point(358, 26),
            Size = new Size(75, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White
        };

        grpFolder.Controls.Add(txtFolderPath);
        grpFolder.Controls.Add(btnBrowse);

        // === Folder Activity Watch ===
        chkWaitForQuiet = new CheckBox
        {
            Text = "Wait for folder to finish downloading (no new activity for",
            Location = new Point(16, 128),
            Size = new Size(370, 22),
            ForeColor = Color.FromArgb(200, 200, 200),
            Checked = true
        };

        nudQuietSeconds = new NumericUpDown
        {
            Location = new Point(390, 126),
            Size = new Size(45, 22),
            Minimum = 3,
            Maximum = 120,
            Value = 10,
            BackColor = Color.FromArgb(45, 45, 45),
            ForeColor = Color.White
        };

        lblQuietSuffix = new Label
        {
            Text = "sec)",
            Location = new Point(437, 128),
            Size = new Size(30, 22),
            ForeColor = Color.FromArgb(200, 200, 200)
        };

        // === Setup File Name ===
        grpSetupFile = new GroupBox
        {
            Text = "Setup File Name",
            ForeColor = Color.FromArgb(180, 180, 180),
            Location = new Point(16, 155),
            Size = new Size(445, 70)
        };

        txtSetupFileName = new TextBox
        {
            Text = "setup.exe",
            Location = new Point(12, 28),
            Size = new Size(420, 30),
            BackColor = Color.FromArgb(45, 45, 45),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        grpSetupFile.Controls.Add(txtSetupFileName);

        // === Install Location ===
        grpInstallLoc = new GroupBox
        {
            Text = "Install Location",
            ForeColor = Color.FromArgb(180, 180, 180),
            Location = new Point(16, 231),
            Size = new Size(445, 105)
        };

        lblDrive = new Label
        {
            Text = "Drive:",
            Location = new Point(12, 25),
            Size = new Size(50, 25),
            ForeColor = Color.FromArgb(200, 200, 200)
        };

        cmbDrive = new ComboBox
        {
            Location = new Point(65, 22),
            Size = new Size(365, 30),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(45, 45, 45),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };

        lblSubPath = new Label
        {
            Text = "Path:",
            Location = new Point(12, 62),
            Size = new Size(50, 25),
            ForeColor = Color.FromArgb(200, 200, 200)
        };

        txtInstallPath = new TextBox
        {
            Text = "InstalledApps",
            Location = new Point(65, 59),
            Size = new Size(280, 30),
            BackColor = Color.FromArgb(45, 45, 45),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        btnBrowseInstall = new Button
        {
            Text = "Browse",
            Location = new Point(355, 57),
            Size = new Size(75, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White
        };

        grpInstallLoc.Controls.Add(lblDrive);
        grpInstallLoc.Controls.Add(cmbDrive);
        grpInstallLoc.Controls.Add(lblSubPath);
        grpInstallLoc.Controls.Add(txtInstallPath);
        grpInstallLoc.Controls.Add(btnBrowseInstall);

        // === Timer Delay ===
        grpTimer = new GroupBox
        {
            Text = "Delay Before Install",
            ForeColor = Color.FromArgb(180, 180, 180),
            Location = new Point(16, 342),
            Size = new Size(445, 70)
        };

        nudDelay = new NumericUpDown
        {
            Location = new Point(12, 28),
            Size = new Size(300, 30),
            Minimum = 0,
            Maximum = 9999,
            Value = 10,
            BackColor = Color.FromArgb(45, 45, 45),
            ForeColor = Color.White
        };

        cmbTimeUnit = new ComboBox
        {
            Location = new Point(318, 28),
            Size = new Size(115, 30),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(45, 45, 45),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        cmbTimeUnit.Items.AddRange(new object[] { "Seconds", "Minutes", "Hours" });
        cmbTimeUnit.SelectedIndex = 0;

        grpTimer.Controls.Add(nudDelay);
        grpTimer.Controls.Add(cmbTimeUnit);

        // === Status ===
        lblStatus = new Label
        {
            Text = "Status: Idle",
            Location = new Point(16, 422),
            Size = new Size(445, 25),
            ForeColor = Color.FromArgb(200, 200, 200)
        };

        // === Progress Bar ===
        progressBar = new ProgressBar
        {
            Location = new Point(16, 449),
            Size = new Size(445, 22),
            Style = ProgressBarStyle.Continuous
        };

        // === Debug Log ===
        txtLog = new TextBox
        {
            Location = new Point(16, 475),
            Size = new Size(445, 120),
            BackColor = Color.FromArgb(20, 20, 20),
            ForeColor = Color.FromArgb(100, 255, 100),
            BorderStyle = BorderStyle.FixedSingle,
            ReadOnly = true,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            WordWrap = true,
            Font = new Font("Consolas", 9F)
        };

        // === Start / Stop Buttons ===
        btnStart = new Button
        {
            Text = "\u25B6 Start Watching",
            Location = new Point(16, 602),
            Size = new Size(215, 35),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(40, 120, 40),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold)
        };

        btnStop = new Button
        {
            Text = "\u25A0 Stop",
            Location = new Point(246, 602),
            Size = new Size(215, 35),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(150, 40, 40),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            Enabled = false
        };

        // === Test Setup Button ===
        btnTestSetup = new Button
        {
            Text = "\uD83D\uDD27 Test Setup Exe...",
            Location = new Point(16, 644),
            Size = new Size(445, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(60, 60, 90),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10F)
        };

        // === Add Controls ===
        this.Controls.Add(lblTitle);
        this.Controls.Add(grpFolder);
        this.Controls.Add(chkWaitForQuiet);
        this.Controls.Add(nudQuietSeconds);
        this.Controls.Add(lblQuietSuffix);
        this.Controls.Add(grpSetupFile);
        this.Controls.Add(grpInstallLoc);
        this.Controls.Add(grpTimer);
        this.Controls.Add(lblStatus);
        this.Controls.Add(progressBar);
        this.Controls.Add(txtLog);
        this.Controls.Add(btnStart);
        this.Controls.Add(btnStop);
        this.Controls.Add(btnTestSetup);
    }

    private Label lblTitle;
    private GroupBox grpFolder;
    private TextBox txtFolderPath;
    private Button btnBrowse;
    private CheckBox chkWaitForQuiet;
    private NumericUpDown nudQuietSeconds;
    private Label lblQuietSuffix;
    private GroupBox grpSetupFile;
    private TextBox txtSetupFileName;
    private GroupBox grpInstallLoc;
    private Label lblDrive;
    private ComboBox cmbDrive;
    private Label lblSubPath;
    private TextBox txtInstallPath;
    private Button btnBrowseInstall;
    private GroupBox grpTimer;
    private NumericUpDown nudDelay;
    private ComboBox cmbTimeUnit;
    private Label lblStatus;
    private ProgressBar progressBar;
    private TextBox txtLog;
    private Button btnStart;
    private Button btnStop;
    private Button btnTestSetup;
}
