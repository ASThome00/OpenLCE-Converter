# OpenLCE Converter

> Open, cross-platform Java ⇄ Legacy Console Edition world converter — Windows · macOS · Linux.

<p align="center">
  <img src="https://img.shields.io/github/license/ASThome00/OpenLCE-Converter?style=for-the-badge" alt="License" />
  <img src="https://img.shields.io/github/last-commit/ASThome00/OpenLCE-Converter?style=for-the-badge" alt="Last Commit" />
  <img src="https://img.shields.io/github/repo-size/ASThome00/OpenLCE-Converter?style=for-the-badge" alt="Repo Size" />
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8-512BD4?style=flat-square&logo=dotnet" alt=".NET 8" />
  <img src="https://img.shields.io/badge/CLI-Windows%20%7C%20Linux%20%7C%20macOS-2F855A?style=flat-square" alt="CLI cross-platform" />
  <img src="https://img.shields.io/badge/GUI-Windows%20%7C%20macOS%20%7C%20Linux-512BD4?style=flat-square&logo=avalonia" alt="GUI cross-platform" />
  <img src="https://img.shields.io/github/v/release/ASThome00/OpenLCE-Converter?style=flat-square&label=Release" alt="Release" />
  <img src="https://img.shields.io/github/downloads/ASThome00/OpenLCE-Converter/total?style=flat-square&label=Downloads" alt="Downloads" />
</p>

Convert Java Edition worlds into Minecraft Legacy Console Edition (LCE) `saveData.ms`, and convert LCE saves back into Java world folders reversibly.

## What It Does

- **Java → LCE:** packs a Java Edition world (folder or `.zip`) into a Windows64 LCE/TU19-compatible `saveData.ms`, recentred on the world spawn.
- **LCE → Java:** unpacks an LCE `saveData.ms` back into a standard Java world folder for a chosen target version.
- Converts the Overworld by default, with optional Nether and End.
- Optionally carries over player data, and (Java → LCE) entity/tile data.
- Detects the chunk format per chunk, so upgraded/mixed-version Java worlds convert without being treated as a single version.
- Ships both a cross-platform desktop GUI and a CLI that share the same conversion core.

## Quick Start (Prebuilt)

1. Download the latest release from [GitHub Releases](https://github.com/ASThome00/OpenLCE-Converter/releases).
2. Pick the right asset for what you want:
   - **Desktop app (recommended):** the `*-app.zip` (macOS/Linux) or `OpenLCE-Converter-GUI-*-win-x64.zip` / `*-setup.exe` (Windows).
   - **Command line:** the `*-cli.zip` for your platform.
3. Extract it and run the app or CLI executable.

### GUI (Recommended)

The desktop GUI is a cross-platform [Avalonia](https://avaloniaui.net/) app that runs on Windows, macOS, and Linux.

- **Windows:** open `LceWorldConverter.Gui.exe` (or install via the setup installer).
- **macOS:** unzip the `*-osx-<arch>-app.zip` and open `OpenLCE Converter.app`. The bundle is currently unsigned, so on first launch right-click the app and choose **Open** (or run `xattr -dr com.apple.quarantine "OpenLCE Converter.app"`) to clear Gatekeeper.
- **Linux:** extract the `*-linux-x64-app.tar.gz` and run `./LceWorldConverter.Gui`.

Then:

1. Choose `Java -> LCE` or `LCE -> Java`.
2. Select a Java world folder or `.zip`, or select an existing `saveData.ms`.
3. Choose an output folder and any extra options.
4. Review the summary and click `Convert`.

The GUI uses the same shared request model and validation as the CLI. The main behavior difference is the default LCE -> Java target version: the GUI starts at `1.21.11`, while the CLI defaults to `1.12.2` unless `--target-version` is supplied.

### CLI (Prebuilt)

The CLI ships as `LceWorldConverter.exe` on Windows and `LceWorldConverter` (no extension) on macOS and Linux. The arguments are identical across platforms; only the invocation differs.

```powershell
# Windows
.\LceWorldConverter.exe --from java <java_world_folder_or_zip> <output_dir> [--world-type <classic|small|medium|large|flat|flat-small|flat-medium|flat-large>] [--all-dimensions] [--copy-players] [--preserve-entities]
.\LceWorldConverter.exe --from lce <saveData.ms_path> <java_world_output_dir> [--all-dimensions] [--copy-players] [--target-version <version>]
```

```bash
# macOS / Linux
./LceWorldConverter --from java <java_world_folder_or_zip> <output_dir> [--world-type <classic|small|medium|large|flat|flat-small|flat-medium|flat-large>] [--all-dimensions] [--copy-players] [--preserve-entities]
./LceWorldConverter --from lce <saveData.ms_path> <java_world_output_dir> [--all-dimensions] [--copy-players] [--target-version <version>]
```

Common examples (Windows):

```powershell
# Folder input
.\LceWorldConverter.exe --from java "C:\Users\You\AppData\Roaming\.minecraft\saves\MyWorld" "D:\GameHDD\MySlot" --world-type large --all-dimensions

# Zip input
.\LceWorldConverter.exe --from java "C:\Users\You\Desktop\MyWorld.zip" "D:\GameHDD\MySlot"

# LCE to Java
.\LceWorldConverter.exe --from lce "D:\GameHDD\MySlot\saveData.ms" "C:\Users\You\Desktop\RecoveredJavaWorld" --all-dimensions --copy-players --target-version 1.21.11
```

The same commands on macOS/Linux use `./LceWorldConverter` and Unix paths, e.g.:

```bash
./LceWorldConverter --from java ~/worlds/MyWorld ~/converted/MySlot --world-type large --all-dimensions
./LceWorldConverter --from lce ~/converted/MySlot/saveData.ms ~/RecoveredJavaWorld --target-version 1.21.11
```

## What You Get

- `saveData.ms` for Windows64 LCE/TU19-compatible targets.
- Optional `unknown-modern-blocks.txt` in output when modern blocks are mapped to air.

Drop `saveData.ms` into your server world folder (for example `GameHDD/<worldname>/`) and set `level-name` in `server.properties`.

## Supported Inputs

| Java Version | Region Format | Notes |
|---|---|---|
| 1.6.4 | McRegion `.mcr` | Near 1:1 conversion |
| 1.7 - 1.12.2 | Anvil `.mca` | Minor remapping / flattening |
| 1.13 - 1.17 | Anvil `.mca` | Palette chunk remapping path |
| 1.18+ | Anvil `.mca` | Extended-height remapping path |
| Upgraded / mixed worlds | `.mca` | Chunk format is detected per chunk, not assumed per world |

## Flags

| Argument | Description |
|---|---|
| `--from java|lce` | Conversion direction |
| `java_world_folder_or_zip` | Java->LCE input path |
| `saveData.ms_path` | LCE->Java input path |
| `output_dir` | Java->LCE output directory for `saveData.ms` |
| `java_world_output_dir` | LCE->Java output world directory |
| `--world-type <...>` | Java->LCE only: `classic`, `small`, `medium`, `large`, `flat`, `flat-small`, `flat-medium`, `flat-large` |
| `--all-dimensions` | Convert Nether and End too |
| `--copy-players` | Java->LCE imports numeric `players/*.dat`; LCE->Java exports player data into `playerdata/` |
| `--preserve-entities` | Java->LCE only: keep entities/tile data (less compatibility-safe) |
| `--target-version <version>` | LCE->Java only: choose the minimum Java target version; defaults to `1.12.2` in CLI |

Legacy Java->LCE positional mode still exists: `.\LceWorldConverter.exe <java_world_folder_or_zip> [output_dir] [flags...]`.

Supported LCE->Java target versions currently include `1.12.2`, `1.13.2`, `1.14.4`, `1.15.2`, `1.16.5`, `1.17.1`, `1.18.2`, `1.19.4`, `1.20.4`, `1.21.4`, and `1.21.11`.

## Inspect Existing saveData.ms

```powershell
.\LceWorldConverter.exe --inspect <path_to_saveData.ms>
```

Additional inspection and debugging commands are also available from the CLI:

```powershell
.\LceWorldConverter.exe --scan-java-world <java_world_path>
.\LceWorldConverter.exe --inspect-region <region_file_path>
.\LceWorldConverter.exe --inspect-java-chunk <java_world_path> <chunk_x> <chunk_z> [overworld|nether|end]
.\LceWorldConverter.exe --scan-java-chest-items <java_world_path> [overworld|nether|end] [max_printed]
.\LceWorldConverter.exe --inspect-lce-chunk <saveData.ms_path> <chunk_x> <chunk_z> [overworld|nether|end]
.\LceWorldConverter.exe --scan-lce-coordinates <saveData.ms_path> [overworld|nether|end]
.\LceWorldConverter.exe --scan-lce-trailing-nbt <saveData.ms_path> [overworld|nether|end]
.\LceWorldConverter.exe --scan-lce-chest-item-mappings <saveData.ms_path> [overworld|nether|end]
```

## Notes and Limitations

- LCE height is 128; source blocks above Y=127 are dropped/remapped.
- Some modern blocks have no exact TU19 equivalent.
- Nether portal linkage should be re-established in-game after conversion.

## Project Layout

- `LceWorldConverter.csproj`: shared core conversion library
- `LceWorldConverter.Cli/`: command-line app that publishes as `LceWorldConverter.exe` in release packages
- `LceWorldConverter.Gui/`: cross-platform Avalonia desktop GUI (Windows, macOS, Linux)
- `src/Requests/`: shared request model, defaults, and validation
- `src/Services/`: focused conversion-side services
- `tests/`: unit and integration-oriented regression coverage
- `scripts/build-release.ps1`: Windows packaging script for the GUI exe, Inno Setup installer, multi-runtime CLI zips, and checksums
- `scripts/build-macos-app.sh`: builds a self-contained macOS `OpenLCE Converter.app` bundle (arm64/x64) and zip
- `.github/workflows/release.yml`: manually-triggered pipeline that computes the next version, tags `main`, builds macOS/Windows/Linux artifacts, and publishes a GitHub Release
- `.github/workflows/ci.yml`: pull-request build + test checks (publishes nothing)

## Related Repositories

- Hub repo: https://github.com/veroxsity/MinecraftLCE
- Bridge repo: https://github.com/veroxsity/LCEBridge
- Client repo: https://github.com/veroxsity/LCEClient
- Debug client repo: https://github.com/veroxsity/LCEDebug
- Launcher repo: https://github.com/veroxsity/LCELauncher
- Server repo: https://github.com/veroxsity/LCEServer

## Build From Source (Optional)

If you only want to use releases, you can ignore this section.

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download).

```bash
git clone https://github.com/ASThome00/OpenLCE-Converter.git
cd OpenLCE-Converter
dotnet build ./LceWorldConverter.sln -c Release
dotnet test ./tests/LceWorldConverter.Tests.csproj -c Release
```

Build GUI project:

```bash
dotnet build ./LceWorldConverter.Gui/LceWorldConverter.Gui.csproj
```

Run GUI from source (any OS):

```bash
dotnet run --project ./LceWorldConverter.Gui/LceWorldConverter.Gui.csproj
```

Run CLI from source (any OS):

```bash
dotnet run --project ./LceWorldConverter.Cli/LceWorldConverter.Cli.csproj -- --from java <java_world_folder_or_zip> <output_dir>
```

Create release artifacts locally:

```powershell
# Windows: GUI exe + installer + CLI zips for all runtimes (requires Inno Setup for the installer step)
.\scripts\build-release.ps1 --version 2.3.0
```

```bash
# macOS: self-contained .app bundle(s) + zip
bash scripts/build-macos-app.sh --version v2.3.0 --arch both
```

Releases are cut from the **Actions** tab by running the [`release.yml`](.github/workflows/release.yml) workflow manually (from `main`). It computes the next version, tags `main`, builds the macOS `.app`, Windows installer/GUI/CLI, and Linux bundles, then publishes them to a GitHub Release with checksums. Either pick a `bump` (`patch`/`minor`/`major`) or supply an exact `version` such as `v2.3.0`. With no existing tags, `bump` starts from `v0.0.0`, so for the first release supply the `version` input explicitly.

## License

Released under the [MIT License](LICENSE).
