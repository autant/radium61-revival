# Radium61 Revival v5.0 🎹

[Français](README.md) | **EN**

Independent community project providing a **WinUSB → Windows MIDI bridge** for the **M-Audio Radium 61** keyboard on Windows 11 x64. The application reads the operational USB device `VID_0763&PID_1015` and forwards decoded MIDI messages to a Windows MIDI output such as loopMIDI or Microsoft GS Wavetable Synth.

## Validation status

**Confirmed on one Windows 11 x64 machine:** keyboard notes, velocity, note release, knobs and faders, graphical UI, and the **multi-file self-contained** Windows build extracted from a ZIP. Automatic reconnection and cross-machine compatibility require further testing.

## Requirements and use

1. Radium 61 firmware must **already be loaded**, and the keyboard must appear as `VID_0763&PID_1015` with WinUSB attached to that device.
2. To send data to a DAW, create a virtual port using [loopMIDI](https://www.tobias-erichsen.de/software/loopmidi.html), keep it running, and select the port as MIDI **output** in Revival and MIDI **input** in your DAW. For immediate audio testing, select `Microsoft GS Wavetable Synth`.
3. Extract the **entire** `Radium61Revival-v5.0-win-x64.zip` archive, run `Radium61Revival.exe`, choose a MIDI output and click Start. Stop the bridge before closing.

**Firmware loading and driver installation are not automated or bundled.** After power loss, the keyboard may revert to `PID_1014`; restoring its firmware is a separate process and requires legally obtained firmware files. See [English installation guide](docs/INSTALLATION_EN.md).

## Build from source

Requires .NET 8 SDK and Windows x64:

```powershell
dotnet build .\Radium61Revival\Radium61Revival.csproj
dotnet publish .\Radium61Revival\Radium61Revival.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o .\dist\Radium61Revival-win-x64
Compress-Archive -Path '.\dist\Radium61Revival-win-x64\*' -DestinationPath '.\dist\Radium61Revival-v5.0-win-x64.zip'
```

**Multi-file publishing is intentional**: the single-file Windows build compiled but failed to start on the reference machine. Distribute every published file, not just the `.exe`.

User settings and USB captures may be stored in `%LOCALAPPDATA%\Radium61Inspector`. SysEx is not supported; pitch bend and broader compatibility still need tests. This project is unaffiliated with M-Audio; original code is MIT-licensed. **No proprietary firmware or M-Audio drivers are distributed.** Do not disable driver signature verification or Windows security protections.

See [USB protocol research](docs/PROTOCOLE_USB.md) and [release instructions](docs/PUBLICATION_GITHUB.md).
