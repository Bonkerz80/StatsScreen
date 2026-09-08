# Stats Screen v0.2.3

This small patch improves Settings-window readability on the physical 800 × 600 display while preserving the existing dashboard and colour customisation work.

Highlights:

- Replaced the theme-dependent ComboBox appearance with an explicit dark StatsScreen template, including a visible arrow, selected text, border, focus state, and popup placement.
- Added an explicit high-contrast ComboBoxItem template for normal, highlighted, selected, and disabled dropdown items.
- Added a deliberate dark TextBox template with bright text, visible caret, readable selection colours, focus border, and larger input sizing.
- Added a StatsScreen CheckBox template with a visible unchecked border, clear checked mark, hover state, and keyboard focus state.
- Increased Settings-window contrast and typography, enlarged the window to 560 × 420, and made Save more prominent without changing the dashboard layout.
- Preserved monitor selection, polling interval, colour settings, cancellation behaviour, sensor selection, polling, logging, fullscreen/display handling, installer behaviour, and the 800 × 600 dashboard.

The update retains the v0.2.2 palette and colour menu unchanged. The self-contained x64 installer is built locally for validation; the v0.2.3 GitHub release is intentionally not published yet.

## Previous release

The published 0.2.0 installer remains available from the [StatsScreen GitHub release page](https://github.com/Bonkerz80/StatsScreen/releases/tag/v0.2.0).
