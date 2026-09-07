# Stats Screen v0.2.0

This feature release turns the working four-value monitor into a purpose-built 800 × 600 PC instrumentation display.

Highlights:

- Dominant CPU and GPU temperature panels with large, distance-readable values.
- Restrained temperature strips with normal, warm, and hot colour bands.
- Smaller but prominent CPU POWER and GPU POWER panels.
- Compact detected hardware identification strip with shortened friendly names.
- Right-click anywhere opens the dark StatsScreen menu at the pointer.
- Mouse controls for fullscreen, monitor selection, startup mode, polling interval, settings, diagnostics, and exit.
- Monitor changes move an active fullscreen window immediately and save the selection.
- Polling interval changes restart the existing single polling loop without overlap.
- Existing LibreHardwareMonitor sensor selection, CPU fallback labeling, display handling, and keyboard shortcuts are preserved.

The release is validated by the existing sensor-selection tests plus focused presentation, settings, and polling tests. The self-contained x64 installer is attached to the GitHub v0.2.0 release.

## Previous release

The published 0.1.1 installer remains available from the [StatsScreen GitHub release page](https://github.com/Bonkerz80/StatsScreen/releases/tag/v0.1.1).
