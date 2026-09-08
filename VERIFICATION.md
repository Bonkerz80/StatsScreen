# Stats Screen 0.3.0 verification — 8 September 2026

- Inspected local HEAD f0902d765b04649e6595e4f8e179358bf70ab5a7 (version 0.2.3),
  clean working tree, three commits ahead of origin/main. Inspected all three
  recent colour/settings commits. No local changes were discarded.
- Implemented experimental read-only Granite Ridge provider and immediate,
  persistent CPU Temperature Source submenu. Existing dashboard XAML, settings
  control styles, accents and warnings remain intact.
- 74 tests passed: the original 36 plus 38 cases covering offsets, complete core
  extraction, max/index/average, invalid floats and sizes, version gates, identity
  gates, failure disposal, stale data, Auto/explicit selection and persistence.
- Release build, self-contained win-x64 publish and Inno Setup 6.7.3 packaging
  succeeded. Local installer: artifacts/installer/StatsScreen-Setup-0.3.0.exe.
  Size: 54,969,824 bytes. SHA256:
  16BA8230F1B77F2F35D4196C94BA39C3CFC8FC296D82E7C7AC4F905A94265CFF.
- NU1900 warnings: vulnerability-feed metadata unavailable. No compilation errors.
- Real diagnostic exited successfully and rendered the dashboard. CPU detected:
  AuthenticAMD Ryzen 7 9800X3D, family 0x1A, model 0x44, eight cores, one package.
  PawnIO installed version 2.2.0.0; execution was non-elevated. Driver access was
  denied. The signed module could not be loaded, so PM version is unknown.
- 0x620105 and indices 317–324 are confirmed against the reference map, NOT
  confirmed by local hardware. Core 0–7, max/core number and average are unavailable.
  LHM Tctl/Tdie returned zero in inventory (rejected as unavailable), so no valid
  aggregate comparison was possible. Auto used existing LHM fallback selection.
- Last UI values: CPU temperature N/A; GPU 57.0 °C; CPU power 0.0 W; GPU power
  54.0 W; SENSOR ERROR. This preserves the existing non-elevated CPU access
  limitation; it is not a successful per-core hardware verification.
- Evidence: artifacts/sensor-check-0.3.0-final/dashboard.png and
  stats-screen-20260908.log. Screenshot inspected: no dashboard clipping; saved
  blue/red colours preserved. Interactive source-menu and physical LCD checks
  remain unverified in this environment.
- Research, licences, exact module hash/source and read-only boundary are in
  docs/GRANITE-RIDGE.md. No private reflection or additional driver was introduced.
- No push or GitHub release was performed. Administrator-run diagnostics are
  still required. If the actual PM version differs, STOP without reading offsets.

## Historical 0.2.3 verification

- Current local HEAD was inspected before editing: `c409353` (`Add configurable dashboard colours for 0.2.2`), with a clean working tree and `main` one commit ahead of `origin/main`.
- No uncommitted local work existed before this focused Settings-window change.
- The existing dashboard colour customisation, sensor backend, settings persistence, monitor selection, polling, logging, fullscreen handling, right-click menu, and 800 × 600 layout were preserved.
- Thirty-six automated tests passed, including the existing sensor-selection, settings, polling, and colour-customisation coverage.
- Release build, self-contained x64 publish, and Inno Setup 6.7.3 packaging succeeded as `StatsScreen-Setup-0.2.3.exe`.
- Installer size: 53,916,580 bytes.
- Installer SHA256: `919B0CC4577D12CFF3B76F7F37C533A57EAB27B365DC1B830DF8C01F7A97CC22`.

## Settings-window fix

The ComboBox problem came from relying on the generic WPF ComboBox style: changing `Foreground`, `Background`, and `BorderBrush` did not replace the standard control template, its theme-dependent toggle button, or its popup item containers. Those template parts could still use Windows system-theme colours, producing a light input area and unreadable selected text on the LCD.

The fix adds named shared resources and explicit templates for `StatsComboBoxStyle`, `StatsComboBoxItemStyle`, `StatsTextBoxStyle`, and `StatsCheckBoxStyle`. The ComboBox now controls its closed surface, arrow, focus state, popup background, item text, hover, selected, and disabled states. The TextBox explicitly controls its content host, caret, selection colours, focus border, and disabled state. The CheckBox explicitly controls its unchecked border, fixed centred 14 × 14 checked mark, hover, pressed, focus, and disabled states; the fixed geometry prevents DPI-dependent clipping or distortion.

Settings text was raised to a stronger hierarchy: white 22 px title, bright 13–14 px labels, `#AEBCC9` explanatory text, 15 px input/control text, 13 px shortcut help, and a 560 × 420 non-fullscreen window. Save uses a slightly stronger accent while Cancel retains the normal dark button treatment.

## UI verification status

- The WPF templates compile successfully and the Settings window remains within the intended 800 × 600 display.
- Closed-state styling is explicit in the XAML resources: dark `#151D26` surfaces, bright text, visible `#526779` borders, 36–38 px controls, a custom dropdown arrow, and focus outlines.
- Dropdown item styling is explicit for normal, hover/highlight, selected, and disabled states; it no longer relies on Windows light/dark theme colours.
- TextBox selection/caret, CheckBox checked/unchecked, and button hover/pressed/focus states are explicitly styled.
- Native interactive popup inspection was not available in this environment, so the opened ComboBox popup and physical off-axis LCD readability still require confirmation on the target machine. The implementation specifically covers the popup template and item containers rather than relying on a closed-state screenshot.

## Existing dashboard verification

The dashboard colour work remains covered by the existing 800 × 600 diagnostic render and 36-test suite. The CPU/GPU colour settings and temperature warning override behaviour were not changed in this patch. The final non-elevated diagnostic logged CPU temperature `N/A`, GPU temperature `49.0`, CPU power `0.0`, GPU power `51.0`, and centre status `SENSOR ERROR` without clipping or layout changes.

Evidence: `artifacts/sensor-check-0.2.3-final/`, including `dashboard.png` and the diagnostic log.

## Remaining physical checks

- Run the v0.2.3 installer on the physical 800 × 600 HDMI LCD to confirm closed/open ComboBox readability, selected/hovered item contrast, TextBox selection, CheckBox states, mixed-DPI positioning, and button focus states.
- Run the installed executable interactively with administrator consent to validate the elevated CPU temperature and CPU package power path on the target PC.
- The v0.2.3 GitHub release was intentionally not published; v0.2.0 remains the latest published installer.

The build emitted `NU1900` restore warnings because the NuGet vulnerability feed was unavailable; there were no compilation errors or dashboard-related build warnings.
