# Radium61 Revival v5.0 installation (Windows 11 x64)

The self-contained application **does not install WinUSB or load firmware**. It requires the already operational device `VID_0763&PID_1015` using WinUSB. The `VID_0763&PID_1014` bootloader state is **not supported by the player**; restoring firmware is a separate, experimental step requiring legally obtained files.

1. Download the multi-file Windows x64 ZIP from GitHub Releases once available.
2. Extract **all** files to one directory. Keep the DLLs next to the executable.
3. Start `Radium61Revival.exe` and select a Windows MIDI output.
4. For instant audio output select `Microsoft GS Wavetable Synth`. For a DAW, run [loopMIDI](https://www.tobias-erichsen.de/software/loopmidi.html), create `Radium61 Virtual`, use this port as output in Revival and input in your DAW, and enable an instrument and MIDI monitoring.
5. Click Start, then Stop before closing.

Troubleshooting: no audio through loopMIDI alone is normal; loopMIDI routes MIDI but does not synthesize audio. If the keyboard is not detected check the USB PID and WinUSB binding. Never disable Windows driver-signature enforcement or security protections. SysEx is unsupported; pitch bend and reconnect behavior are not fully validated.
