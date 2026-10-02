# vatSys Persistent Pan and Wheel Zoom Plugin

A vatSys plugin that keeps the final position after middle-button panning and adds mouse-wheel zooming to the ASD.

## Features

- Keeps the map at the position reached with middle-button dragging.
- Uses vatSys's native pan calculation and rendering state.
- Zooms the ASD with the mouse wheel.
- Wheel up zooms in; wheel down zooms out.
- Writes a diagnostic log next to the plugin DLL.
- Targets the 32-bit .NET Framework 4.7.2 vatSys application.

## Installation

1. Exit every running vatSys process.
2. Open the `Plugins` directory of the Profile you want to use.

   Example:

   ```text
   C:\Users\<username>\Documents\vatSys Files\Profiles\ZBPE FIR Beijing\Plugins\
   ```

3. Copy `vatSys.PersistentPan.dll` from the Release package into that directory.
4. Start vatSys and load the Profile.

The plugin must be installed in the active Profile's `Plugins` directory. Installing it only next to `vatSys.exe` does not load it.

## Usage

### Persistent panning

1. Move the mouse over the ASD.
2. Hold the middle mouse button.
3. Drag the map.
4. Release the middle mouse button.

The ASD remains at the final position instead of returning to the original centre.

### Wheel zoom

- Scroll up to zoom in.
- Scroll down to zoom out.

The plugin uses vatSys's ASD zoom range and limits.

## Diagnostics

After the plugin loads, it creates:

```text
PersistentPan.log
```

The log is stored in the same `Plugins` directory as the DLL. Typical entries include:

```text
Plugin loaded
Middle down 1000,500
Middle up 1200,650; replay final move
Wheel 120
Range 1500 -> 1200 requested 1200
```

If the log is not created, vatSys did not load the plugin. Check that the DLL is in the active Profile's `Plugins` directory and restart vatSys completely.

## Building from source

Requirements:

- Windows
- .NET Framework 4.7.2 Developer Pack
- An MSBuild installation
- A local vatSys installation at `I:\vatSys`, or an updated `HintPath` in the project file

Build the x86 Release configuration:

```powershell
dotnet msbuild .\PersistentPan\PersistentPan.csproj /p:Configuration=Release /p:Platform=x86
```

The output is created at:

```text
PersistentPan\bin\Release\vatSys.PersistentPan.dll
```

## Compatibility

This plugin is built against the vatSys SDK exposed by `vatSys.exe`. It may need to be rebuilt when the vatSys executable changes its internal ASD implementation.
