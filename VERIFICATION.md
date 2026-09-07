# Stats Screen 0.2.0 verification — 7 September 2026

- Current HEAD was inspected before editing: `551cee4` (`0.1.1`), with a clean working tree.
- Release solution build succeeded after the dashboard changes.
- Nineteen automated tests passed, including the existing sensor-selection coverage and new temperature, hardware-name, settings-persistence, missing-value, and polling-restart tests.
- Self-contained x64 publish and Inno Setup 6.7.3 packaging succeeded as `StatsScreen-Setup-0.2.0.exe`.
- The hosted diagnostic run exited with code 0 and rendered a complete 800 × 600 dashboard preview without clipping or scrollbars.
- The diagnostic run detected and displayed `Radeon RX 9070 XT`, GPU core temperature, GPU package power, and shortened hardware names. It was intentionally non-elevated in this environment, so CPU temperature was `N/A` and CPU power was unreadable for that run.
- Existing CPU `Tctl/Tdie` fallback selection and the deterministic discrete-GPU sensor selection were left in the hardware backend; the earlier elevated target-PC validation remains the evidence for those readings.
- The final build emitted `NU1900` restore warnings because the NuGet vulnerability feed was unavailable; there were no compilation errors or warnings caused by the dashboard changes.

Evidence: `artifacts/sensor-check-0.2.0-final/`, including the rendered `dashboard.png` and diagnostic log. The final diagnostic UI values were `N/A`, `55.0`, `0.0`, and `53.0` for CPU temperature, GPU temperature, CPU power, and GPU power respectively.

Remaining checks: use the installer on the physical 800 × 600 HDMI LCD to confirm mixed-DPI positioning, fullscreen entry/exit, and right-click menu placement on that monitor. A Windows elevation consent prompt was not interactable in this development session, so the new installer’s elevated live-sensor path should be checked during that physical-device run.
