# Stats Screen 0.4.0 verification — 9 September 2026

- Inspected clean local HEAD `241c1a2` (`Add automatic Granite Ridge sensor
  retry`) before the visual update. Existing telemetry, settings, menu,
  polling, logging, installer, and test files were preserved.
- Reworked only the dashboard presentation layer: layered dark surfaces,
  restrained accent edges, fixed-width values, slim temperature tracks,
  integrated power band, and compact hardware/status strip.
- Rendered the new dashboard at exactly 800 × 600. Local visual inspection
  confirmed aligned cards, readable units and source labels, no clipping or
  overlap, and visible accent consistency. The rendered evidence is in
  `artifacts/visual-check-0.4.0/dashboard.png`.
- Generated and inspected the original icon at 1024 × 1024 and 256 × 256.
  The ICO contains 16, 24, 32, 48, 64, 128, and 256 pixel images.

- Added automatic recovery for transient Granite Ridge reader, access, and
  table-validation failures. The provider retries with a fresh reader after 30
  seconds while unsupported CPUs and unknown PM versions remain permanently
  disabled for the current run. The new cooldown/recovery behavior is covered
  by the Granite Ridge test suite.
- The visual change was applied to clean HEAD `241c1a2` without discarding
  local work.

- Retained the experimental read-only Granite Ridge provider and immediate,
  persistent CPU Temperature Source submenu. Existing settings control styles,
  accents, warnings, and selection behaviour remain intact.
- 74 tests passed: the original 36 plus 38 cases covering offsets, complete core
  extraction, max/index/average, invalid floats and sizes, version gates, identity
  gates, failure disposal and automatic retry, stale data, Auto/explicit selection
  and persistence.
- Release build, self-contained win-x64 publish and Inno Setup 6.7.3 packaging
  succeeded. The published executable reports file version `0.4.0.0`. Local
  installer: `artifacts/installer/StatsScreen-Setup-0.4.0.exe`; size:
  55,040,593 bytes; SHA256:
  `88EC2DA2D7DC8B8399482A44DFE811C3BD6838AA382DDE3522497EE89F8957CE`.
- The ICO is embedded through the project ApplicationIcon setting, both WPF
  windows reference the same resource, and the installer compiled with
  `SetupIconFile` plus explicit Start menu/desktop shortcut icon settings.
- The official bundled PawnIO installer signature remains valid.
- NU1900 warnings: vulnerability-feed metadata unavailable. No compilation errors.
- A framework-hosted, non-elevated diagnostic first received Windows error 5 from
  PawnIO; this explains why that diagnostic could not verify the hardware. The
  packaged executable carries `requireAdministrator` in its manifest.
- A run of the packaged executable succeeded with `Elevated: True` and PawnIO
  2.2.0.0. CPU detected: AuthenticAMD Ryzen 7 9800X3D, family 0x1A, model 0x44,
  eight cores, one package. The signed module loaded successfully.
- The detected PM table was `0x00620105`, matching the explicitly allowed
  `0x620105` layout. Core 0–7 read 36.1, 33.3, 35.0, 33.4, 33.1, 32.5, 32.5,
  and 32.1 °C. Max Core was 36.1 °C (Core 0); Average Core was 33.5 °C.
- The same sample logged LHM Tctl/Tdie at 54.6 °C. This is a comparison only;
  the two readings represent different sensor concepts and are not forced to
  match. The difference was below the diagnostic warning threshold.
- The application recorded a subsequent `AverageCore` source selection, and the
  settings file persisted `CpuTemperatureSource: AverageCore`. The visual
  refresh keeps the existing blue/red colour selections and temperature warning
  overrides.
- Evidence is in the local StatsScreen log at
  `%LOCALAPPDATA%\\StatsScreen\\logs\\stats-screen-20260909.log`; the earlier
  non-elevated telemetry render remains in `artifacts/sensor-check-0.3.0-final/`.
- Interactive source-menu and physical LCD checks remain unverified here.
- Research, licences, exact module hash/source and read-only boundary are in
  docs/GRANITE-RIDGE.md. No private reflection or additional driver was introduced.
- No GitHub release was performed. If a different machine reports another PM
  version, STOP without reading offsets.

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
