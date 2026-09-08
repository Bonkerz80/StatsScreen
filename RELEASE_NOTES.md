# Stats Screen v0.2.2

This update adds restrained, user-selectable colour accents to the existing 800 × 600 instrumentation dashboard without changing the hardware-monitoring backend or layout dimensions.

Highlights:

- Added a central predefined palette: cyan, blue, green, lime, amber, orange, red, pink, purple, white, and grey.
- Added **Colours** to the existing right-click menu with CPU Temperature, GPU Temperature, CPU Power, GPU Power, checked selections, colour swatches, and **Reset Colours**.
- Applies colour choices immediately and persists stable palette names in the existing settings file.
- Uses the selected CPU/GPU temperature accents for the related hardware names in the bottom strip.
- Keeps temperature warnings authoritative: amber from 70 °C, red from 85 °C, and the selected normal accent restored below 70 °C.
- Keeps temperature values readable, with colour concentrated in headings, bars, borders, units, thin power accents, and hardware names.
- Preserves sensor selection, polling, logging, settings, fullscreen/display handling, right-click controls, installer behaviour, and the 800 × 600 layout.

The update includes focused persistence, invalid-settings, reset, dashboard-accent, and warning-override tests. The self-contained x64 installer is built locally for validation; the v0.2.2 GitHub release is intentionally not published yet.

## Previous release

The published 0.2.0 installer remains available from the [StatsScreen GitHub release page](https://github.com/Bonkerz80/StatsScreen/releases/tag/v0.2.0).
