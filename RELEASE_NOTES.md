# Stats Screen v0.2.1

This small visual update polishes the existing 800 × 600 PC instrumentation display without changing the hardware-monitoring backend.

Highlights:

- Removed the redundant TEMPERATURE heading and the heavy outer frame around the CPU/GPU temperature cards.
- Kept the distance-readable temperature values and widened the temperature bars within the available card space.
- Removed LIVE SENSOR, CPU FALLBACK, and the normal LIVE/updated timestamp text from the dashboard.
- Shows the selected CPU temperature source as a short label such as Tctl/Tdie, Tdie, Core #3, or CPU Package.
- Keeps GPU source text quiet during normal operation while showing SENSOR UNAVAILABLE when a temperature cannot be read.
- Leaves the centre of the hardware strip blank during healthy operation and reserves it for important statuses.
- Preserved the power layout, temperature thresholds, hardware names, right-click menu, display handling, polling, logging, and sensor-selection rules.

The update is validated by the existing sensor-selection tests plus focused presentation, settings, polling, status, and zero-reading tests. The self-contained x64 installer is built locally for validation; the v0.2.1 GitHub release is intentionally not published yet.

## Previous release

The published 0.2.0 installer remains available from the [StatsScreen GitHub release page](https://github.com/Bonkerz80/StatsScreen/releases/tag/v0.2.0).
