# vatSys Persistent Pan and Wheel Zoom Plugin Guide

## Purpose

This plugin changes the ASD interaction in vatSys:

- Middle-button panning remains at the final dragged position.
- Mouse-wheel zooming is available directly over the ASD.

## Package contents

The release package contains:

```text
vatSys.PersistentPan.dll
```

Only this DLL is required for installation.

## Installation procedure

1. Close vatSys.
2. Open the Profile directory used by vatSys.
3. Open its `Plugins` subdirectory.
4. Copy `vatSys.PersistentPan.dll` into that directory.
5. Start vatSys again.

Example installation path:

```text
C:\Users\<username>\Documents\vatSys Files\Profiles\ZBPE FIR Beijing\Plugins\vatSys.PersistentPan.dll
```

The Profile name may be different on your computer.

## How to use the plugin

### Pan the ASD

1. Place the cursor over the ASD map.
2. Press and hold the middle mouse button.
3. Move the mouse to pan the map.
4. Release the middle mouse button.

The map stays at the new position.

### Zoom the ASD

1. Place the cursor over the ASD map.
2. Rotate the mouse wheel upward to zoom in.
3. Rotate the mouse wheel downward to zoom out.

Each wheel step changes the ASD range while respecting vatSys's configured minimum and maximum limits.

## Log file

The plugin creates `PersistentPan.log` beside the DLL:

```text
<Profile>\Plugins\PersistentPan.log
```

The log can be used to verify the following:

- `Plugin loaded`: the plugin was discovered and instantiated.
- `Middle down` and `Middle up`: middle-button input was received.
- `replay final move`: the final pan position was reapplied.
- `Wheel`: a wheel message was received.
- `Range ... -> ...`: the ASD range changed.

## Troubleshooting

### No log file

Confirm that:

- The DLL is in the active Profile's `Plugins` directory.
- The DLL is named exactly `vatSys.PersistentPan.dll`.
- All vatSys processes were closed before replacing the DLL.
- vatSys was restarted after installation.

### Panning does not remain in place

Close all vatSys processes, replace the DLL with the latest release asset, and restart vatSys. Do not replace the DLL while vatSys is running.

### Wheel input is logged but the map does not zoom

Check whether the log contains a range update such as:

```text
Range 1500 -> 1200 requested 1200
```

If range updates are present, restart vatSys completely and test again with the cursor directly over the ASD.

## Building the plugin

The project targets x86 .NET Framework 4.7.2 because the installed vatSys application is a 32-bit .NET Framework application.

```powershell
dotnet msbuild .\PersistentPan\PersistentPan.csproj /p:Configuration=Release /p:Platform=x86
```

Build output:

```text
PersistentPan\bin\Release\vatSys.PersistentPan.dll
```
