# Auto Installer

Automatically installs setup files when a folder finishes downloading. Set it and forget it.

## Download

Grab the latest **AutoInstaller.exe** from the [Releases](https://github.com/bittersweet34/autoinstaller/releases) page. No install needed — just run it.

## How To Use

### 1. Pick the game folder to watch

Click **Browse** and select the **game folder itself** — the one that contains (or will contain) `setup.exe`.

> ⚠️ **You MUST select the game folder, NOT the drive root or parent folder.**
>
> **Correct:** `Z:\repacks\Crimson Desert [FitGirl HV Repack]\`
>
> **Wrong:** `Z:\repacks\` &nbsp;&nbsp;←&nbsp; selecting this will watch everything on the drive
>
> **Wrong:** `Z:\` &nbsp;&nbsp;←&nbsp; this is the drive root, do not use this

The app watches this folder for file activity and waits for `setup.exe` to appear.

### 2. Set the setup file name
Defaults to `setup.exe`. Change it if your installer has a different name.

### 3. Wait for download to finish (optional)
**"Wait for folder to finish downloading"** is on by default. It watches all file activity in the folder and waits until nothing changes for 10 seconds (adjustable) before starting the install. This way it won't try to run a half-downloaded setup.

The app needs **both** conditions before it starts:
- The folder has been quiet (no new files) for the full quiet period
- `setup.exe` exists in the folder

If `setup.exe` downloads last, no problem — the app keeps watching until it appears AND the folder is quiet.

Uncheck this if you just want it to trigger as soon as the setup file appears.

### 4. Choose where to install
- Pick a **drive** from the dropdown
- Set the **path** (defaults to `<Drive>\InstalledApps`) or click **Browse** to pick any folder
- The installer auto-creates a subfolder named after the game (e.g. `Crimson Desert [FitGirl HV Repack]` → installs to `Crimson Desert`)

### 5. Set a delay (optional)
Set a countdown before the install starts (seconds, minutes, or hours). You can change it even while the countdown is running.

### 6. Click Start Watching
That's it. The app will:
1. Watch the game folder for file activity
2. Detect when the download is fully done (all files quiet + setup.exe present)
3. Wait the countdown
4. Launch setup.exe — the installer window opens **alongside AutoInstaller** so you can see progress
5. AutoInstaller detects when the installer finishes and shows the installed size and elapsed time

### 7. Test Setup (optional)
Use the **Test Setup Exe** button at the bottom to pick any setup file and test-run it with your current install settings.

## Status Bar

The status bar shows what's happening in real time:
- `Downloading — quiet for 3s / 10s needed` — files still changing
- `Found setup.exe — waiting for folder to settle` — setup.exe is there but other files are still downloading
- `Folder quiet but waiting for setup.exe...` — other files done, setup.exe hasn't arrived yet
- `Installing... 2m 30s elapsed — 12.4 GB written` — install in progress

## Debug Log
The log box at the bottom shows everything happening in real time — file activity, countdown, install progress, errors.

## Notes
- The app asks for **admin permissions** on launch (required to run installers)
- Installers are launched with `/SILENT` — they run automatically and show their own progress window alongside AutoInstaller
- AutoInstaller detects when the installer finishes, even for repacks that spawn child processes (e.g. FitGirl)
- The app is self-contained — no .NET install required
- Works on Windows 10/11 (x64)

## Building From Source

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```
cd AutoInstaller
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ..\build
```
