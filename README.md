# Win Widget

Minimalist desktop widgets designed for Windows 11 and inspired by the supplied ocean desktop reference.

The application will provide three independently configurable widgets:

- date and time;
- monthly calendar;
- text notes.

Each widget will support its own background color, background opacity, and text color. The default visual language is typography-first: Segoe UI Variable, deep blue text (`#23478B`), transparent surfaces, and no persistent chrome.

## Technology

- Windows 11 (build 22000 or newer)
- .NET 8
- WPF

## Install on Windows 11

1. Open the repository's [Releases page](https://github.com/rosnrock/win-widget/releases) and download `WinWidget-Setup.exe` from the latest release.
2. Run the downloaded file and follow the installer prompts.
3. Optionally enable launch at Windows sign-in or create a desktop shortcut.

The installer contains the required .NET runtime, so .NET does not need to be installed separately. Win Widget is available from the Start menu after installation and can also be controlled from its system tray icon.

To update, download and run the installer from the newer release. It installs over the existing version and keeps the widget settings and notes.

To uninstall, open **Settings > Apps > Installed apps**, find **Win Widget**, and select **Uninstall**. User settings and notes are stored in `%LOCALAPPDATA%\WinWidget\settings.json`. Remove the `%LOCALAPPDATA%\WinWidget` folder manually only if you also want to erase that data.

## Development

Open `WinWidget.sln` in Visual Studio 2022 with the **.NET desktop development** workload, or build from a Windows terminal:

```powershell
dotnet restore
dotnet build WinWidget.sln
dotnet run --project src/WinWidget/WinWidget.csproj
```

The repository is organized into `Views`, `Models`, and `Services` so widget presentation, state, and Windows integration can evolve independently.

## Build the installer

On Windows 11, install the .NET 8 SDK and [Inno Setup 6](https://jrsoftware.org/isinfo.php), then run:

```powershell
.\scripts\build-installer.ps1 -Version 0.1.0
```

The self-contained Windows x64 installer is written to `artifacts\installer\WinWidget-Setup.exe`.

GitHub Actions verifies pull requests and pushes to `main` and uploads the installer as the `WinWidget-Setup-win-x64` workflow artifact. Pushing a version tag such as `v0.1.0` also creates a GitHub Release containing `WinWidget-Setup.exe`.
