namespace AutoInstaller;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;

    // ── Color Palette ♥ puppy girl pink Y2K ♥ ──────────────
    static readonly Color BG          = Color.FromArgb(30, 10, 22);       // deep plum bg
    static readonly Color Surface     = Color.FromArgb(45, 18, 35);       // dark rose surface
    static readonly Color Panel_      = Color.FromArgb(55, 22, 42);       // panel berry
    static readonly Color ControlBg   = Color.FromArgb(65, 28, 50);       // control mauve
    static readonly Color Border      = Color.FromArgb(120, 50, 90);      // pink border
    static readonly Color TextPrimary = Color.FromArgb(255, 220, 240);    // soft pink white
    static readonly Color TextDim     = Color.FromArgb(190, 130, 165);    // muted rose
    static readonly Color Accent      = Color.FromArgb(255, 105, 180);    // hot pink ♥
    static readonly Color AccentDim   = Color.FromArgb(200, 70, 140);     // deeper pink
    static readonly Color Green       = Color.FromArgb(255, 150, 200);    // pastel pink (play)
    static readonly Color GreenDim    = Color.FromArgb(200, 100, 155);    // muted play
    static readonly Color Red         = Color.FromArgb(255, 80, 120);     // coral red
    static readonly Color RedDim      = Color.FromArgb(180, 50, 80);      // dark coral
    static readonly Color Purple      = Color.FromArgb(220, 130, 255);    // lilac sparkle
    static readonly Color LogGreen    = Color.FromArgb(255, 180, 220);    // pink log text
    static readonly Color LogBg       = Color.FromArgb(22, 8, 16);        // deep dark plum

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    // Helper: styled flat button
    private Button MakeBtn(string text, Color bg, Color? border = null, int w = 80, int h = 30)
    {
        var b = new Button
        {
            Text = text,
            Size = new Size(w, h),
            FlatStyle = FlatStyle.Flat,
            BackColor = bg,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
            Cursor = Cursors.Hand
        };
        b.FlatAppearance.BorderColor = border ?? Border;
        b.FlatAppearance.BorderSize = 1;
        b.FlatAppearance.MouseOverBackColor = Color.FromArgb(
            Math.Min(bg.R + 20, 255), Math.Min(bg.G + 20, 255), Math.Min(bg.B + 20, 255));
        b.FlatAppearance.MouseDownBackColor = Color.FromArgb(
            Math.Max(bg.R - 10, 0), Math.Max(bg.G - 10, 0), Math.Max(bg.B - 10, 0));
        return b;
    }

    // Helper: styled GroupBox replacement (Panel with painted header)
    private Panel MakeSection(string title, int x, int y, int w, int h)
    {
        var pnl = new Panel
        {
            Location = new Point(x, y),
            Size = new Size(w, h),
            BackColor = Surface,
            Padding = new Padding(1)
        };
        // Accent top border
        var topBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 2,
            BackColor = Accent
        };
        // Title label
        var lbl = new Label
        {
            Text = title,
            Location = new Point(12, 8),
            AutoSize = true,
            ForeColor = Accent,
            BackColor = Surface,
            Font = new Font("Segoe UI Semibold", 9.5F)
        };
        pnl.Controls.Add(topBar);
        pnl.Controls.Add(lbl);
        return pnl;
    }

    // Helper: styled TextBox
    private TextBox MakeTextBox(int x, int y, int w, bool readOnly = false, string placeholder = "")
    {
        var t = new TextBox
        {
            Location = new Point(x, y),
            Size = new Size(w, 28),
            BackColor = ControlBg,
            ForeColor = TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10F),
            ReadOnly = readOnly
        };
        if (!string.IsNullOrEmpty(placeholder))
            t.PlaceholderText = placeholder;
        return t;
    }

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.AutoScaleMode = AutoScaleMode.Dpi;
        this.ClientSize = new Size(980, 780);
        this.Text = "  ♥ ~*~ Auto Installer ~*~ ♥";
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.MinimumSize = new Size(850, 700);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = BG;
        this.ForeColor = TextPrimary;
        this.Font = new Font("Segoe UI", 10F);

        // ═══════════════════════════════════════════════════
        //  Tab Control — owner-drawn for custom styling
        // ═══════════════════════════════════════════════════
        tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 11F),
            Padding = new Point(20, 8),
            DrawMode = TabDrawMode.OwnerDrawFixed,
            SizeMode = TabSizeMode.Fixed,
            ItemSize = new Size(140, 40)
        };
        tabControl.DrawItem += TabControl_DrawItem;

        tabInstaller = new TabPage
        {
            Text = "♥ Installer",
            BackColor = BG,
            ForeColor = TextPrimary,
            AutoScroll = true,
            Padding = new Padding(6)
        };

        tabQBittorrent = new TabPage
        {
            Text = "♥ Browser",
            BackColor = BG,
            ForeColor = TextPrimary
        };

        tabLibrary = new TabPage
        {
            Text = "♥ Library",
            BackColor = BG,
            ForeColor = TextPrimary
        };

        // ═══════════════════════════════════════════════════
        //  TAB 1 — Installer
        // ═══════════════════════════════════════════════════

        // -- Title --
        lblTitle = new Label
        {
            Text = "~*~ Auto Installer ~*~",
            Font = new Font("Segoe UI", 22F, FontStyle.Bold),
            ForeColor = Accent,
            AutoSize = true,
            Location = new Point(16, 8),
            BackColor = Color.Transparent
        };

        // -- Section: Watch Folder --
        grpFolder = MakeSection("♥  Watch Folder", 16, 52, 460, 75);
        txtFolderPath = MakeTextBox(12, 32, 350, readOnly: true, placeholder: "Select folder to watch...");
        btnBrowse = MakeBtn("Browse", ControlBg, Accent, 88, 28);
        btnBrowse.Location = new Point(368, 31);
        grpFolder.Controls.Add(txtFolderPath);
        grpFolder.Controls.Add(btnBrowse);

        // -- Quiet-wait option --
        chkWaitForQuiet = new CheckBox
        {
            Text = "Wait for folder to finish downloading (no new activity for",
            Location = new Point(18, 134),
            Size = new Size(380, 22),
            ForeColor = TextDim,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 9.5F),
            Checked = true
        };

        nudQuietSeconds = new NumericUpDown
        {
            Location = new Point(400, 132),
            Size = new Size(48, 24),
            Minimum = 3, Maximum = 120, Value = 10,
            BackColor = ControlBg, ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 9.5F)
        };

        lblQuietSuffix = new Label
        {
            Text = "sec)",
            Location = new Point(450, 134),
            Size = new Size(32, 22),
            ForeColor = TextDim,
            Font = new Font("Segoe UI", 9.5F)
        };

        // -- Section: Setup File --
        grpSetupFile = MakeSection("♥  Setup File Name", 16, 162, 460, 72);
        txtSetupFileName = MakeTextBox(12, 32, 436, placeholder: "e.g. setup.exe");
        txtSetupFileName.Text = "setup";
        grpSetupFile.Controls.Add(txtSetupFileName);

        // -- Section: Install Location --
        grpInstallLoc = MakeSection("♥  Install Location", 16, 240, 460, 108);

        lblDrive = new Label
        {
            Text = "Drive:",
            Location = new Point(12, 34), Size = new Size(48, 25),
            ForeColor = TextDim, Font = new Font("Segoe UI", 9.5F)
        };
        cmbDrive = new ComboBox
        {
            Location = new Point(64, 31), Size = new Size(384, 28),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = ControlBg, ForeColor = TextPrimary,
            FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10F)
        };
        lblSubPath = new Label
        {
            Text = "Path:",
            Location = new Point(12, 68), Size = new Size(48, 25),
            ForeColor = TextDim, Font = new Font("Segoe UI", 9.5F)
        };
        txtInstallPath = MakeTextBox(64, 65, 295);
        txtInstallPath.Text = "InstalledApps";
        btnBrowseInstall = MakeBtn("Browse", ControlBg, Accent, 80, 28);
        btnBrowseInstall.Location = new Point(368, 64);

        grpInstallLoc.Controls.Add(lblDrive);
        grpInstallLoc.Controls.Add(cmbDrive);
        grpInstallLoc.Controls.Add(lblSubPath);
        grpInstallLoc.Controls.Add(txtInstallPath);
        grpInstallLoc.Controls.Add(btnBrowseInstall);

        // -- Section: Delay --
        grpTimer = MakeSection("♥  Delay Before Install", 16, 354, 460, 72);
        nudDelay = new NumericUpDown
        {
            Location = new Point(12, 32), Size = new Size(310, 28),
            Minimum = 0, Maximum = 9999, Value = 10,
            BackColor = ControlBg, ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 10F)
        };
        cmbTimeUnit = new ComboBox
        {
            Location = new Point(328, 32), Size = new Size(120, 28),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = ControlBg, ForeColor = TextPrimary,
            FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10F)
        };
        cmbTimeUnit.Items.AddRange(new object[] { "Seconds", "Minutes", "Hours" });
        cmbTimeUnit.SelectedIndex = 0;
        grpTimer.Controls.Add(nudDelay);
        grpTimer.Controls.Add(cmbTimeUnit);

        // -- Section: Notifications --
        grpNotify = MakeSection("♥  Mobile Notification (ntfy.sh)", 16, 432, 460, 72);
        txtNtfyTopic = MakeTextBox(12, 32, 350, placeholder: "your-secret-topic-name");
        btnTestNotify = MakeBtn("Test", Purple, Purple, 80, 28);
        btnTestNotify.Location = new Point(368, 31);
        grpNotify.Controls.Add(txtNtfyTopic);
        grpNotify.Controls.Add(btnTestNotify);

        // -- Section: Install Options --
        grpInstallOpts = MakeSection("♥  Install Options", 16, 510, 460, 100);
        chkInstallDirectX = new CheckBox
        {
            Text = "Install DirectX",
            Location = new Point(12, 32), Size = new Size(200, 24),
            ForeColor = TextPrimary, Checked = true,
            Font = new Font("Segoe UI", 9.5F)
        };
        chkInstallVCRedist = new CheckBox
        {
            Text = "Install Visual C++ Redists",
            Location = new Point(220, 32), Size = new Size(228, 24),
            ForeColor = TextPrimary, Checked = true,
            Font = new Font("Segoe UI", 9.5F)
        };
        chkAddToSteam = new CheckBox
        {
            Text = "♥ Add installed games to Steam library",
            Location = new Point(12, 58), Size = new Size(436, 24),
            ForeColor = TextPrimary, Checked = false,
            Font = new Font("Segoe UI", 9.5F)
        };
        grpInstallOpts.Controls.Add(chkInstallDirectX);
        grpInstallOpts.Controls.Add(chkInstallVCRedist);
        grpInstallOpts.Controls.Add(chkAddToSteam);

        // -- Status + Progress --
        lblStatus = new Label
        {
            Text = "● Status: Idle",
            Location = new Point(16, 620),
            Size = new Size(460, 26),
            ForeColor = TextDim,
            Font = new Font("Segoe UI Semibold", 10.5F)
        };

        progressBar = new ProgressBar
        {
            Location = new Point(16, 648),
            Size = new Size(460, 8),
            Style = ProgressBarStyle.Continuous
        };

        // -- Log Console --
        txtLog = new TextBox
        {
            Location = new Point(16, 664),
            Size = new Size(460, 120),
            BackColor = LogBg,
            ForeColor = LogGreen,
            BorderStyle = BorderStyle.FixedSingle,
            ReadOnly = true, Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            WordWrap = true,
            Font = new Font("Cascadia Code, Consolas", 9F)
        };

        // -- Action Buttons --
        btnStart = MakeBtn("♥  Start Watching", Green, GreenDim, 224, 40);
        btnStart.Location = new Point(16, 792);
        btnStart.Font = new Font("Segoe UI Semibold", 11.5F);

        btnStop = MakeBtn("×  Stop", Red, RedDim, 224, 40);
        btnStop.Location = new Point(252, 792);
        btnStop.Font = new Font("Segoe UI Semibold", 11.5F);
        btnStop.Enabled = false;

        btnTestSetup = MakeBtn("♥  Test Setup Exe...", Panel_, Accent, 460, 34);
        btnTestSetup.Location = new Point(16, 840);
        btnTestSetup.Font = new Font("Segoe UI", 10F);

        // -- Add all to Tab 1 --
        tabInstaller.Controls.Add(lblTitle);
        tabInstaller.Controls.Add(grpFolder);
        tabInstaller.Controls.Add(chkWaitForQuiet);
        tabInstaller.Controls.Add(nudQuietSeconds);
        tabInstaller.Controls.Add(lblQuietSuffix);
        tabInstaller.Controls.Add(grpSetupFile);
        tabInstaller.Controls.Add(grpInstallLoc);
        tabInstaller.Controls.Add(grpTimer);
        tabInstaller.Controls.Add(grpNotify);
        tabInstaller.Controls.Add(grpInstallOpts);
        tabInstaller.Controls.Add(lblStatus);
        tabInstaller.Controls.Add(progressBar);
        tabInstaller.Controls.Add(txtLog);
        tabInstaller.Controls.Add(btnStart);
        tabInstaller.Controls.Add(btnStop);
        tabInstaller.Controls.Add(btnTestSetup);

        // ═══════════════════════════════════════════════════
        //  TAB 2 — Browser
        // ═══════════════════════════════════════════════════

        // --- Top settings panel ---
        pnlQbtTop = new Panel
        {
            Dock = DockStyle.Top,
            Height = 68,
            BackColor = Surface,
            Padding = new Padding(10, 8, 10, 8)
        };

        lblQbtExe = new Label
        {
            Text = "♥  Built-in Torrent Client",
            Location = new Point(10, 8), Size = new Size(220, 25),
            ForeColor = Accent, Font = new Font("Segoe UI Semibold", 10F)
        };
        lblQbtExeStatus = new Label
        {
            Text = "Ready",
            Location = new Point(240, 10), Size = new Size(200, 25),
            ForeColor = Green,
            Font = new Font("Segoe UI Semibold", 9F)
        };

        btnAddTorrentFile = MakeBtn("+ .torrent", ControlBg, Accent, 88, 28);
        btnAddTorrentFile.Location = new Point(700, 6);
        btnAddMagnetLink = MakeBtn("+ Magnet", ControlBg, Accent, 88, 28);
        btnAddMagnetLink.Location = new Point(794, 6);

        chkClipboardMagnet = new CheckBox
        {
            Text = "🧲 Auto-detect magnet links from clipboard",
            Location = new Point(10, 40), Size = new Size(320, 22),
            ForeColor = TextPrimary, Checked = true,
            Font = new Font("Segoe UI", 9.5F)
        };

        lblQbtStatus = new Label
        {
            Text = "",
            Location = new Point(340, 42), Size = new Size(560, 20),
            ForeColor = Accent,
            Font = new Font("Segoe UI", 9F)
        };

        pnlQbtTop.Controls.Add(lblQbtExe);
        pnlQbtTop.Controls.Add(lblQbtExeStatus);
        pnlQbtTop.Controls.Add(btnAddTorrentFile);
        pnlQbtTop.Controls.Add(btnAddMagnetLink);
        pnlQbtTop.Controls.Add(chkClipboardMagnet);
        pnlQbtTop.Controls.Add(lblQbtStatus);

        // --- Bookmarks carousel ---
        pnlBookmarks = new Panel
        {
            Dock = DockStyle.Top,
            Height = 88,
            BackColor = Panel_,
            Padding = new Padding(8, 4, 8, 4)
        };

        lblBookmarks = new Label
        {
            Text = "★ Bookmarks",
            Location = new Point(8, 4), Size = new Size(100, 18),
            ForeColor = Accent,
            Font = new Font("Segoe UI Semibold", 8.5F)
        };

        flpBookmarks = new FlowLayoutPanel
        {
            Location = new Point(4, 22),
            Size = new Size(900, 60),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            AutoScroll = true, WrapContents = false,
            BackColor = Panel_
        };

        btnAddBookmark = new Button
        {
            Text = "+ Add",
            Size = new Size(60, 52),
            FlatStyle = FlatStyle.Flat,
            BackColor = ControlBg,
            ForeColor = TextDim,
            Font = new Font("Segoe UI Semibold", 9F),
            Margin = new Padding(3),
            Cursor = Cursors.Hand
        };
        btnAddBookmark.FlatAppearance.BorderColor = Border;
        btnAddBookmark.FlatAppearance.MouseOverBackColor = Color.FromArgb(55, 58, 78);
        flpBookmarks.Controls.Add(btnAddBookmark);

        pnlBookmarks.Controls.Add(lblBookmarks);
        pnlBookmarks.Controls.Add(flpBookmarks);

        // --- Navigation bar ---
        pnlNavBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 38,
            BackColor = Surface,
            Padding = new Padding(4)
        };

        btnNavBack = MakeBtn("◀", ControlBg, Border, 34, 28);
        btnNavBack.Location = new Point(4, 5);
        btnNavBack.Font = new Font("Segoe UI", 10F);

        btnNavForward = MakeBtn("▶", ControlBg, Border, 34, 28);
        btnNavForward.Location = new Point(42, 5);
        btnNavForward.Font = new Font("Segoe UI", 10F);

        txtNavUrl = new TextBox
        {
            Location = new Point(82, 6), Size = new Size(790, 26),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = ControlBg, ForeColor = TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9.5F),
            Text = "https://www.google.com"
        };
        btnNavGo = MakeBtn("Go", Green, GreenDim, 44, 28);
        btnNavGo.Location = new Point(878, 5);
        btnNavGo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnNavGo.Font = new Font("Segoe UI Semibold", 9.5F);

        pnlNavBar.Controls.Add(btnNavBack);
        pnlNavBar.Controls.Add(btnNavForward);
        pnlNavBar.Controls.Add(txtNavUrl);
        pnlNavBar.Controls.Add(btnNavGo);

        // --- Torrent Dashboard panel (built-in engine) ---
        pnlTorrentDash = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 200,
            BackColor = Panel_,
            Padding = new Padding(8, 4, 8, 4)
        };

        // Torrent dashboard header + speed display
        lblTorrentDashTitle = new Label
        {
            Text = "♥ Downloads",
            Dock = DockStyle.Top,
            Height = 22,
            ForeColor = Accent,
            Font = new Font("Segoe UI Semibold", 9.5F),
            Padding = new Padding(0, 2, 0, 0)
        };

        lblTransferSpeeds = new Label
        {
            Text = "",
            Location = new Point(140, 4), Size = new Size(400, 18),
            ForeColor = TextDim,
            Font = new Font("Segoe UI", 8.5F),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        // Collapse / expand button
        btnToggleDash = MakeBtn("▼", ControlBg, Border, 28, 22);
        btnToggleDash.Location = new Point(pnlTorrentDash.Width - 36, 2);
        btnToggleDash.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnToggleDash.Font = new Font("Segoe UI", 8F);

        // Torrent list (scrollable)
        flpTorrents = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            WrapContents = false,
            FlowDirection = FlowDirection.TopDown,
            BackColor = Panel_,
            Padding = new Padding(0, 4, 0, 0)
        };

        pnlTorrentDash.Controls.Add(flpTorrents);
        pnlTorrentDash.Controls.Add(lblTransferSpeeds);
        pnlTorrentDash.Controls.Add(lblTorrentDashTitle);
        pnlTorrentDash.Controls.Add(btnToggleDash);

        // --- WebView2 browser ---
        wvBrowser = new Microsoft.Web.WebView2.WinForms.WebView2
        {
            Dock = DockStyle.Fill,
            DefaultBackgroundColor = BG
        };

        // Order: Fill first, then Top panels in reverse, then Bottom
        tabQBittorrent.Controls.Add(wvBrowser);
        tabQBittorrent.Controls.Add(pnlNavBar);
        tabQBittorrent.Controls.Add(pnlBookmarks);
        tabQBittorrent.Controls.Add(pnlQbtTop);
        tabQBittorrent.Controls.Add(pnlTorrentDash);

        // ═══════════════════════════════════════════════════
        //  TAB 3 — Library
        // ═══════════════════════════════════════════════════

        pnlLibraryHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 90,
            BackColor = Surface,
            Padding = new Padding(12, 8, 12, 8)
        };

        lblLibraryTitle = new Label
        {
            Text = "♥  Game Library  ♥",
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            ForeColor = Accent,
            AutoSize = true,
            Location = new Point(12, 12),
            BackColor = Color.Transparent
        };

        btnRefreshLibrary = MakeBtn("↻ Refresh", ControlBg, Accent, 90, 30);
        btnRefreshLibrary.Location = new Point(860, 10);
        btnRefreshLibrary.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnRefreshLibrary.Font = new Font("Segoe UI Semibold", 9.5F);

        lblLibraryPath = new Label
        {
            Text = "",
            Location = new Point(220, 18),
            Size = new Size(620, 20),
            ForeColor = TextDim,
            Font = new Font("Segoe UI", 9F),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        lblSgdbApiKey = new Label
        {
            Text = "SteamGridDB API Key:",
            Location = new Point(12, 52),
            AutoSize = true,
            ForeColor = TextDim,
            Font = new Font("Segoe UI", 9F),
            BackColor = Color.Transparent
        };

        txtSgdbApiKey = MakeTextBox(160, 50, 400, placeholder: "Paste your API key from steamgriddb.com/profile/preferences/api");
        txtSgdbApiKey.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtSgdbApiKey.UseSystemPasswordChar = true;

        pnlLibraryHeader.Controls.Add(lblLibraryTitle);
        pnlLibraryHeader.Controls.Add(btnRefreshLibrary);
        pnlLibraryHeader.Controls.Add(lblLibraryPath);
        pnlLibraryHeader.Controls.Add(lblSgdbApiKey);
        pnlLibraryHeader.Controls.Add(txtSgdbApiKey);

        flpLibrary = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = BG,
            Padding = new Padding(16, 12, 16, 12),
            WrapContents = true
        };

        tabLibrary.Controls.Add(flpLibrary);
        tabLibrary.Controls.Add(pnlLibraryHeader);

        // === Assemble ===
        tabControl.TabPages.Add(tabLibrary);
        tabControl.TabPages.Add(tabInstaller);
        tabControl.TabPages.Add(tabQBittorrent);
        this.Controls.Add(tabControl);
    }

    // ── Custom Tab Painting ────────────────────────────────
    private void TabControl_DrawItem(object? sender, DrawItemEventArgs e)
    {
        var tc = (TabControl)sender!;
        var tab = tc.TabPages[e.Index];
        bool selected = (e.Index == tc.SelectedIndex);
        var bounds = tc.GetTabRect(e.Index);

        // Background
        using var bgBrush = new SolidBrush(selected ? Surface : BG);
        e.Graphics.FillRectangle(bgBrush, bounds);

        // Accent underline for selected tab
        if (selected)
        {
            using var accentPen = new Pen(Accent, 3);
            e.Graphics.DrawLine(accentPen, bounds.Left + 4, bounds.Bottom - 2, bounds.Right - 4, bounds.Bottom - 2);
        }

        // Text
        var textColor = selected ? Accent : TextDim;
        using var textBrush = new SolidBrush(textColor);
        var font = selected
            ? new Font("Segoe UI Semibold", 10.5F)
            : new Font("Segoe UI", 10.5F);
        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        e.Graphics.DrawString(tab.Text, font, textBrush, bounds, sf);
        font.Dispose();
        sf.Dispose();
    }

    // --- Tab 0: Library ---
    private TabPage tabLibrary;
    private Panel pnlLibraryHeader;
    private Label lblLibraryTitle;
    private Label lblLibraryPath;
    private Button btnRefreshLibrary;
    private FlowLayoutPanel flpLibrary;
    private Label lblSgdbApiKey;
    private TextBox txtSgdbApiKey;

    // --- Tab 1: Installer ---
    private TabControl tabControl;
    private TabPage tabInstaller;
    private TabPage tabQBittorrent;
    private Label lblTitle;
    private Panel grpFolder;
    private TextBox txtFolderPath;
    private Button btnBrowse;
    private CheckBox chkWaitForQuiet;
    private NumericUpDown nudQuietSeconds;
    private Label lblQuietSuffix;
    private Panel grpSetupFile;
    private TextBox txtSetupFileName;
    private Panel grpInstallLoc;
    private Label lblDrive;
    private ComboBox cmbDrive;
    private Label lblSubPath;
    private TextBox txtInstallPath;
    private Button btnBrowseInstall;
    private Panel grpTimer;
    private NumericUpDown nudDelay;
    private ComboBox cmbTimeUnit;
    private Label lblStatus;
    private ProgressBar progressBar;
    private TextBox txtLog;
    private Button btnStart;
    private Button btnStop;
    private Button btnTestSetup;
    private Panel grpNotify;
    private TextBox txtNtfyTopic;
    private Button btnTestNotify;
    private Panel grpInstallOpts;
    private CheckBox chkInstallDirectX;
    private CheckBox chkInstallVCRedist;
    private CheckBox chkAddToSteam;

    // --- Tab 2: Browser ---
    private Panel pnlQbtTop;
    private Label lblQbtExe;
    private Label lblQbtExeStatus;
    private Button btnAddTorrentFile;
    private Button btnAddMagnetLink;
    private CheckBox chkClipboardMagnet;
    private Label lblQbtStatus;
    private Panel pnlBookmarks;
    private Label lblBookmarks;
    private FlowLayoutPanel flpBookmarks;
    private Button btnAddBookmark;
    private Panel pnlNavBar;
    private Button btnNavBack;
    private Button btnNavForward;
    private TextBox txtNavUrl;
    private Button btnNavGo;
    private Microsoft.Web.WebView2.WinForms.WebView2 wvBrowser;
    // Torrent Dashboard (built-in)
    private Panel pnlTorrentDash;
    private Label lblTransferSpeeds;
    private FlowLayoutPanel flpTorrents;
    private Label lblTorrentDashTitle;
    private Button btnToggleDash;
}
