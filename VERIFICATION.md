# Stats Screen 0.2.1 verification — 8 September 2026

- Current HEAD was inspected before editing: `8b49d5a` (`Document installer version update`), with a clean working tree.
- The working tree was then updated from v0.2.0 to v0.2.1 for this local validation pass.
- Thirty automated tests passed, including the existing sensor-selection, settings, and polling coverage plus focused temperature-source, status, and CPU-zero presentation tests.
- Self-contained x64 publish and Inno Setup 6.7.3 packaging succeeded as `StatsScreen-Setup-0.2.1.exe`.
- Installer size: 53,908,967 bytes.
- Installer SHA256: `D304C89CF6D1D8E4D82614C8D2D749F81F81E8E08BD847EB7DA9FA8561C7E39D`.
- The rendered diagnostic dashboard completed without clipping or scrollbars. The temperature cards are now the main visual structure, the bars are wider, normal GPU source text is quiet, the CPU source is presentation-ready, and the normal centre status is blank when all four readings are available.
- The non-elevated diagnostic rendered a partial-reading state with CPU temperature `N/A`, GPU temperature `51.0`, CPU power `0.0`, GPU power `55.0`, and centre status `SENSOR ERROR`.

Evidence: `artifacts/sensor-check-0.2.1-final/`, including `dashboard.png` and the diagnostic log.

## CPU power zero-reading finding

The current application descriptor carries the Libre Hardware Monitor sensor value as `float?` and does not carry a per-sensor readable/permission flag. In the non-elevated log, CPU `Core (Tctl/Tdie)` and CPU `Package` both reported numeric zero while GPU readings remained available. There is therefore no reliable application-level distinction between a genuine readable zero and a permission-related zero in this data path.

The update does not blanket-convert zero watts to `N/A`, preserving genuine zero readings. A missing CPU temperature now causes the dashboard to show `SENSOR ERROR`, while the CPU power value remains the value supplied by the library. The focused regression test confirms that a descriptor-provided zero is preserved.

## Remaining physical checks

- Run the v0.2.1 installer on the physical 800 × 600 HDMI LCD to confirm mixed-DPI positioning, fullscreen entry/exit, right-click menu placement, wider bar balance, and hardware-strip readability.
- Run the installed executable interactively with administrator consent to validate the elevated CPU temperature and CPU package power path on the target PC.
- The v0.2.1 GitHub release was intentionally not published; v0.2.0 remains the latest published installer.

The build emitted `NU1900` restore warnings because the NuGet vulnerability feed was unavailable; there were no compilation errors or dashboard-related build warnings.
