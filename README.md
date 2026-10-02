# vatSys Persistent Pan

An open-source plugin for vatSys that improves ASD map navigation.

## Features

- Keeps the ASD at the position reached after middle-button panning.
- Prevents the native temporary-pan behaviour from restoring the previous map centre.
- Adds mouse-wheel zooming for the ASD.
- Uses vatSys's native map projection, range limits, and rendering APIs.
- Provides diagnostic logging for input and map state changes.

## Installation

Download the latest `vatSys.PersistentPan.dll` from the [Releases](https://github.com/lpxlpx7/Drag-and-zom/releases) page.

Copy the DLL into the `Plugins` directory of the active vatSys Profile:

```text
C:\Users\<username>\Documents\vatSys Files\Profiles\<Profile Name>\Plugins\vatSys.PersistentPan.dll
```

Restart vatSys after installing or replacing the DLL.

The plugin must be installed in the active Profile's `Plugins` directory. The vatSys installation directory is not used for Profile plugins.

## Usage

### Persistent pan

1. Move the cursor over the ASD.
2. Press and hold the middle mouse button.
3. Drag the map.
4. Release the middle mouse button.

The map remains at the final position.

### Wheel zoom

- Scroll up to zoom in.
- Scroll down to zoom out.

The zoom range follows vatSys's configured limits.

## Diagnostics

The plugin writes `PersistentPan.log` beside the DLL:

```text
<Profile>\Plugins\PersistentPan.log
```

The log records plugin loading, middle-button input, wheel input, and range changes. For example:

```text
Plugin loaded
Middle down 1000,500
Middle up 1200,650; replay final move
Wheel 120
Range 1500 -> 1200 requested 1200
```

If no log is created, confirm that the DLL is in the active Profile's `Plugins` directory and restart every vatSys process.

## Building

The project targets the 32-bit .NET Framework 4.7.2 environment used by vatSys.

Requirements:

- Windows
- .NET Framework 4.7.2 Developer Pack
- MSBuild
- A vatSys installation, or an updated reference path in `PersistentPan.csproj`

Build the Release configuration:

```powershell
dotnet msbuild .\PersistentPan\PersistentPan.csproj /p:Configuration=Release /p:Platform=x86
```

The compiled plugin is written to:

```text
PersistentPan\bin\Release\vatSys.PersistentPan.dll
```

## Compatibility

The plugin uses the vatSys SDK and the ASD methods exposed by the installed vatSys executable. Changes to vatSys's internal ASD implementation may require a rebuild or compatibility update.

## License

This project is licensed under the GNU General Public License v3.0. See [LICENSE](LICENSE).

## Acknowledgements

Developed for the vatSys community and intended for use with the vatSys air traffic control simulation environment.
