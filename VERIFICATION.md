# Stats Screen 0.2.2 verification — 8 September 2026

- Current HEAD was inspected before editing: `fd9bb71` (`Polish dashboard presentation for 0.2.1`), with a clean working tree and `main` synchronized with `origin/main`.
- The update adds four persisted accent selections, a central named palette, live right-click menu updates, reset behaviour, and temperature warning overrides without changing the sensor backend or 800 × 600 layout.
- Thirty-six automated tests passed, including existing sensor-selection, settings, and polling coverage plus colour persistence, invalid-settings fallback, reset, palette-default, accent binding, and warning-override tests.
- Release build succeeded, followed by self-contained x64 publish and Inno Setup 6.7.3 packaging as `StatsScreen-Setup-0.2.2.exe`.
- Installer size: 53,899,253 bytes.
- Installer SHA256: `023720B088F0A1CEB65BD7A9A4091F04F4239A28624049179A5F150E289C0F04`.
- The diagnostic dashboard rendered at 800 × 600 without clipping or scrollbars. It picked up the existing local Blue/Red accent selections, demonstrating persisted custom colours across the temperature cards, power headings/units, thin power accents, and related hardware names while the panel backgrounds remained dark.
- The non-elevated diagnostic rendered a partial-reading state with CPU temperature `N/A`, GPU temperature `56.0`, CPU power `0.0`, GPU power `53.0`, and centre status `SENSOR ERROR`.
- Default cyan/blue/amber/purple keys, old-settings fallback, invalid-settings fallback, and reset behaviour are covered by automated tests; the diagnostic did not overwrite the user’s existing settings.
- The warning override path is covered by focused tests: normal temperature returns the selected accent, 70 °C changes the value/bar to amber, 85 °C changes them to red, and a later normal reading restores the selected accent.

Evidence: `artifacts/sensor-check-0.2.2-final/`, including `dashboard.png` and the diagnostic log.

## Colour behaviour

The predefined palette is Cyan `#4DC6C7`, Blue `#5F9FEA`, Green `#6CCB8A`, Lime `#A8C95D`, Amber `#D5A85B`, Orange `#E38B57`, Red `#D96B6B`, Pink `#D38AB5`, Purple `#B98BE7`, White `#E6EDF3`, and Grey `#8C9AA8`.

Defaults are CPU temperature Cyan, GPU temperature Blue, CPU power Amber, and GPU power Purple. The right-click **Colours** submenu uses stable palette names, marks the active choice with the native WPF check state and a swatch, saves immediately, updates the ViewModel without restarting polling, and provides **Reset Colours**.

Settings loading tolerates missing colour properties from older files and invalid names; each section falls back to its own default. Only stable strings are persisted, not WPF brushes.

## CPU power zero-reading finding

The current application descriptor carries the Libre Hardware Monitor sensor value as `float?` and does not carry a per-sensor readable/permission flag. In the non-elevated log, CPU `Core (Tctl/Tdie)` and CPU `Package` both reported numeric zero while GPU readings remained available. There is therefore no reliable application-level distinction between a genuine readable zero and a permission-related zero in this data path.

The update does not blanket-convert zero watts to `N/A`, preserving genuine zero readings. A missing CPU temperature causes the dashboard to show `SENSOR ERROR`, while the CPU power value remains the value supplied by the library. The existing regression test confirms that a descriptor-provided zero is preserved.

## Remaining physical checks

- Run the v0.2.2 installer on the physical 800 × 600 HDMI LCD to confirm mixed-DPI positioning, fullscreen entry/exit, right-click menu placement, submenu usability, and hardware-strip readability.
- Run the installed executable interactively with administrator consent to validate the elevated CPU temperature and CPU package power path on the target PC.
- The v0.2.2 GitHub release was intentionally not published; v0.2.0 remains the latest published installer.

The build emitted `NU1900` restore warnings because the NuGet vulnerability feed was unavailable; there were no compilation errors or dashboard-related build warnings.
