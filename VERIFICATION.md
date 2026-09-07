# Stats Screen 0.1.1 verification — 7 September 2026

- Release build succeeded. Eight sensor/presentation regression tests passed.
- Live elevated dashboard run: eight real hardware snapshots, UI values updated, clean exit, no warning/error entries in diagnostic log.
- CPU: AMD Ryzen 7 9800X3D, `Core (Tctl/Tdie)` and `Package`. Individual core temperatures were not exposed in the inventory. The UI visibly labels the fallback.
- GPU: AMD Radeon RX 9070 XT, `GPU Core` and `GPU Package`. Integrated Radeon SoC readings are no longer selected.
- Self-contained x64 publish packaged with Inno Setup 6.7.3.
- Setup installed successfully, with desktop shortcut and optional elevated interactive sign-in task. The task's executable and Highest run level were inspected.
- Installed executable ran eight additional samples and exited successfully. Final displayed values: CPU 60.9 °C, GPU 56.0 °C, CPU 65.7 W, GPU 52.0 W. These are observed samples, not fixed/example readings in the app.
- Reinstallation with startup unchecked succeeded and removed the startup task.
- Uninstall succeeded, removed the installed executable and desktop shortcut, and left shared PawnIO running. The test installation has been removed; the setup EXE remains ready for the user to install.

Evidence: `artifacts/sensor-check/`, `artifacts/installed-check/`, `artifacts/install-test.log`. The diagnostic previews are rendered from the actual WPF window.

Limits: no Windows reboot/sign-out performed; actual sign-in triggering remains untested. PawnIO 2.2.0 was already installed, so its missing-driver/reboot path was not exercised. Physical secondary-monitor fullscreen and mixed-DPI transitions still require checking on the dedicated screen. No claim of calibration against an independent instrument. Stats Screen and its setup are unsigned; the bundled PawnIO installer signature was verified as valid.
