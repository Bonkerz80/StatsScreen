# Stats Screen v0.1.1

The first public Windows release of Stats Screen.

Highlights:

- Self-contained x64 installer for Windows 10 and later.
- Live CPU temperature, GPU temperature, CPU package power, and GPU board/package power tiles.
- Deterministic selection of the discrete Radeon RX 9070 XT instead of the integrated Radeon graphics device on the tested PC.
- Visible `Tctl/Tdie · CPU FALLBACK` label when the processor does not expose numbered core temperatures.
- Optional desktop shortcut and optional elevated sign-in task.
- Signed PawnIO 2.2.0 installer bundled for hardware access when needed.

Validation covered the release build, eight automated sensor/presentation tests, real elevated readings, installation, startup-task creation/removal, and uninstall. See [VERIFICATION.md](VERIFICATION.md) for the detailed record and known limits.

[Download StatsScreen-Setup-0.1.1.exe](https://github.com/Bonkerz80/StatsScreen/releases/download/v0.1.1/StatsScreen-Setup-0.1.1.exe)
