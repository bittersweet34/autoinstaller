# Auto Installer

Automatically installs setup files when a folder finishes downloading. Set it and forget it.

## Download

Grab the latest **AutoInstaller.exe** from the [Releases](https://github.com/bittersweet34/autoinstaller/releases) page. No install needed — just run it.

## How To Use

### 1. Pick the folder to watch
Click **Browse** and select the folder where your download will land (or is already downloading to).

### 2. Set the setup file name
Defaults to `setup.exe`. Change it if your installer has a different name.

### 3. Wait for download to finish (optional)
**"Wait for folder to finish downloading"** is on by default. It watches all file activity in the folder and waits until nothing changes for 10 seconds (adjustable) before starting the install. This way it won't try to run a half-downloaded setup.

Uncheck this if you just want it to trigger as soon as the setup file appears.

### 4. Choose where to install
- Pick a **drive** from the dropdown
- Set the **path** (defaults to `<Drive>\InstalledApps`) or click **Browse** to pick any folder
- The installer auto-creates a subfolder named after the game (e.g. `Crimson Desert`)

### 5. Set a delay (optional)
Set a countdown before the install starts (seconds, minutes, or hours). You can change it even while the countdown is running.

### 6. Click Start Watching
That's it. The app will:
1. Watch the folder for activity
2. Detect when the download is done
3. Wait the countdown
4. Launch the setup with the install path you picked
5. Show elapsed time and install size while it runs

### 7. Test Setup (optional)
Use the **Test Setup Exe** button at the bottom to pick any setup file and test-run it with your current install settings.

## Debug Log
The log box at the bottom shows everything happening in real time — file activity, countdown, install progress, errors.

## Notes
- The app asks for **admin permissions** on launch (required to run installers)
- Installs are run silently using Inno Setup flags (`/VERYSILENT`)
- The app is self-contained — no .NET install required
- Works on Windows 10/11 (x64)

## Building From Source

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```
cd AutoInstaller
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ..\build
```
