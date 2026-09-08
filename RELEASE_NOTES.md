# Stats Screen v0.3.0 (experimental, local build)

Adds a read-only Granite Ridge temperature provider for the eight-core Ryzen 7
9800X3D and PM table 0x620105. Auto selects a calculated Max Core when all eight
core values are valid; otherwise LibreHardwareMonitor remains the fallback.
Right-click CPU Temperature Source offers Auto, Max Core, Average Core, Core 0–7,
and Tctl/Tdie with immediate persistent selection and disabled unavailable options.

Uses the existing PawnIO driver with the official signed 0.2.11 RyzenSMU module.
No CPU tuning operations are exposed. Unknown PM versions, invalid/incomplete
tables and access failures disable the experimental provider until restart.
Diagnostics include individual cores, offsets, max, average and actual selection.
Real per-core telemetry has not been verified: Windows denied driver access in
the available test environment. This is not a verified hardware release.

## Retained 0.2.3 settings changes

This small patch improves Settings-window readability on the physical 800 × 600 display while preserving the existing dashboard and colour customisation work.

Highlights:

- Replaced the theme-dependent ComboBox appearance with an explicit dark StatsScreen template, including a visible arrow, selected text, border, focus state, and popup placement.
- Added an explicit high-contrast ComboBoxItem template for normal, highlighted, selected, and disabled dropdown items.
- Added a deliberate dark TextBox template with bright text, visible caret, readable selection colours, focus border, and larger input sizing.
- Added a StatsScreen CheckBox template with a visible unchecked border, a fixed centred 14 × 14 checked mark, hover state, and keyboard focus state so the tick cannot clip or distort at different DPI settings.
- Increased Settings-window contrast and typography, enlarged the window to 560 × 420, and made Save more prominent without changing the dashboard layout.
- Preserved monitor selection, polling interval, colour settings, cancellation behaviour, sensor selection, polling, logging, fullscreen/display handling, installer behaviour, and the 800 × 600 dashboard.

The update retains the v0.2.2 palette and colour menu unchanged. The self-contained x64 installer is built locally for validation; the v0.2.3 GitHub release is intentionally not published yet.

## Previous release

The published 0.2.0 installer remains available from the [StatsScreen GitHub release page](https://github.com/Bonkerz80/StatsScreen/releases/tag/v0.2.0).
