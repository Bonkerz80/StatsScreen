# Stats Screen v0.4.0 (experimental, local build)

This release gives the fixed 800 × 600 dashboard a neon telemetry-cluster
identity inspired by the supplied CPU/GPU reference while preserving the
existing sensor hierarchy and controls.

- Reworked the dashboard around luminous cyan/red rounded cards, layered accent
  strokes, technical background lines, CPU/GPU pictograms, pulse traces, and a
  stronger white numeric hierarchy.
- Kept CPU/GPU temperatures dominant with fixed-width numeric columns, clear
  units, source labels, and pill-style tracks. Decimal readings such as `9.8`,
  `40.3`, and `105.0` remain inside the layout without clipping.
- Added reference-style CPU/GPU power cards with lightning badges, package/board
  sublabels, coloured rules, and rising telemetry bars.
- Reworked the hardware strip and footer with split CPU/GPU accents, diagonal
  corner detailing, a live status divider, and the `SYSTEM MONITOR` treatment.
- Added a deterministic dark dual-channel telemetry icon in PNG and multi-size
  ICO formats. The icon is applied to the executable, both windows, Start menu,
  desktop shortcut, and installer.
- Added the live status label without changing the existing `StatusText` contract;
  healthy dashboards show `LIVE`, while errors remain explicit.

The experimental read-only Granite Ridge provider, per-core/average-core source
selection, colour settings, polling, logging, display mode, installer flow and
tests remain intact. Unknown PM versions and unsupported CPUs still fail closed;
transient provider failures use LibreHardwareMonitor and retry automatically.

The self-contained x64 installer is built locally for validation. The v0.4.0
GitHub release is intentionally not published yet.

## Previous v0.3.1 changes

Transient Granite Ridge sensor-access failures recover automatically. The
provider releases the failed reader, keeps LibreHardwareMonitor as the live
fallback, and retries after 30 seconds. Unsupported CPUs and unknown PM table
versions remain permanently disabled for the current run so no unverified
offsets are ever read.

## Retained 0.3.0 Granite Ridge support

Adds a read-only Granite Ridge temperature provider for the eight-core Ryzen 7
9800X3D and PM table 0x620105. Auto selects a calculated Max Core when all eight
core values are valid; otherwise LibreHardwareMonitor remains the fallback.
Right-click CPU Temperature Source offers Auto, Max Core, Average Core, Core 0–7,
and Tctl/Tdie with immediate persistent selection and disabled unavailable options.

Uses the existing PawnIO driver with the official signed 0.2.11 RyzenSMU module.
No CPU tuning operations are exposed. Unknown PM versions disable the
experimental provider until restart; transient invalid/incomplete tables and
access failures use the fallback and retry automatically. Diagnostics include
individual cores, offsets, max, average and actual selection.

## Retained 0.2.3 settings changes

This small patch improves Settings-window readability on the physical 800 × 600 display while preserving the existing dashboard and colour customisation work.

Highlights:

- Replaced the theme-dependent ComboBox appearance with an explicit dark StatsScreen template, including a visible arrow, selected text, border, focus state, and popup placement.
- Added an explicit high-contrast ComboBoxItem template for normal, highlighted, selected, and disabled dropdown items.
- Added a deliberate dark TextBox template with bright text, visible caret, readable selection colours, focus border, and larger input sizing.
- Added a StatsScreen CheckBox template with a visible unchecked border, a fixed centred 14 × 14 checked mark, hover state, and keyboard focus state so the tick cannot clip or distort at different DPI settings.
- Increased Settings-window contrast and typography, enlarged the window to 560 × 420, and made Save more prominent without changing the dashboard layout.
- Preserved monitor selection, polling interval, colour settings, cancellation behaviour, sensor selection, polling, logging, fullscreen/display handling, installer behaviour, and the 800 × 600 dashboard.

The update retains the v0.2.2 palette and colour menu unchanged.

## Previous release

The published 0.2.0 installer remains available from the [StatsScreen GitHub release page](https://github.com/Bonkerz80/StatsScreen/releases/tag/v0.2.0).
