# Experimental 9800X3D temperature telemetry

This feature supplements LibreHardwareMonitor 0.9.6. Only an AuthenticAMD Ryzen 7
9800X3D, family 0x1A/model 0x44, one CPU package and eight enabled physical cores is
accepted. Core numbering is always Core 0–7. This is not general Ryzen support.

## Research and provenance (8 September 2026)

- https://github.com/Kyworn/gnr-smu/blob/master/PM_TABLE_MAP.md and README:
  MIT; map identifies table 0x620105, 1828 bytes (457 little-endian float32 values).
  Core 0–7 are indices 317–324, offsets 0x4F4, 0x4F8, 0x4FC, 0x500,
  0x504, 0x508, 0x50C, 0x510. The current map agrees with the requested mapping.
  This is community research on one 9800X3D, not an AMD specification or proof on
  this PC. No implementation code from gnr-smu was incorporated.
- https://github.com/HorizonUnix/ZenMaster/tree/812488e03965cf84c453901bc5aa1f6a0b28d729:
  GPL-3.0 licence inspected. Windows backend examined as technical reference only;
  no source or binary from that project is redistributed or adapted.
- https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/tree/v0.9.6:
  MPL-2.0. Existing NuGet dependency remains unmodified. Hardware/RyzenSMU.cs does
  not define layout 0x620105. Its public RyzenSmu wrapper loads the embedded module;
  the general module loader is internal. No private-member reflection is used.
- https://github.com/namazso/PawnIO.Modules/blob/0.1.6/RyzenSMU.p:
  the module bundled by LHM 0.9.6 rejects family 0x1A in main(), despite a Granite
  Ridge codename entry. It cannot supply this provider's access path.
- https://github.com/namazso/PawnIO.Modules/blob/0.2.11/RyzenSMU.p:
  LGPL-2.1-or-later. The current official signed module accepts family 0x1A.
  Its unmodified RyzenSMU.bin is embedded separately, SHA256
  301D9CA397108E09F31BFBD5AC4C9BB4F352A5DE68532C32DB3BA7DDCDE93450.
  Exact module source archive and COPYING accompany the installer under
  licenses/PawnIO-Modules-0.2.11. StatsScreen source is available to rebuild with a
  modified module; update the integrity hash when doing so. Driver signature
  enforcement remains in place.

## Read-only access boundary

PawnIoSmuReader is an original, isolated Windows interop wrapper. It opens the
existing PawnIO device, loads the signed module (the driver verifies it), and
allows only three telemetry entry points: resolve PM table, refresh telemetry,
read PM table. No arbitrary register address, SMU opcode, input arguments or
configuration-write operation is exposed. No additional kernel driver is installed.

Telemetry is read-only with respect to CPU configuration: the official module
uses RSMU mailbox queries 0x05 (version), 0x04 (address), and 0x03 (telemetry
transfer). Like normal hardware monitoring, mailbox transport entails register
writes and firmware copying telemetry to DRAM. StatsScreen never edits PM table
contents or tuning settings. These are RSMU commands; do not confuse them with
MP1 command numbers used by tuning tools.

All operations use Global\\Access_PCI, the same mutex as LHM. The reader checks
operation success and exact byte counts, closes handles on failure, and exposes
no write helpers. PM version is rechecked before each transfer. A known table is
read twice without a second refresh; disagreement rejects the snapshot. PawnIO
uses 64-bit cells, so 1832 transport bytes contain 1828 table bytes plus four
unused padding bytes. The firmware descriptor does not report a separate table
length: expected length comes from the explicit version layout; returned length
checks catch short transfers, but cannot independently prove firmware layout.

## Selection, validation and failure

All eight floats must be finite and strictly between 0 and 125 °C. Invalid or
incomplete data is rejected, never clamped. Max, hottest index (lowest index on a
tie), and arithmetic average are computed from the same complete snapshot.
Unknown versions, access failures or invalid readings disable this provider until
app restart, leaving LHM available. No nearby offsets are probed.

Right-click CPU Temperature Source selects Auto, Max Core, Average Core, Core 0–7,
or Tctl/Tdie. Options without genuine readings are disabled. Auto prefers Max Core;
explicit unavailable sources also use the existing LHM reading with its actual
label, preserving the preference for a future successful run. Changing the source
applies to the current snapshot and saves immediately, without restarting polling.
Core samples older than 15 seconds are not offered for selection.

The temperature accent and warm/hot overrides operate on the selected value.
Normal logs record identity, version, availability and the first good sample.
--diagnose <output-directory> records each core, offsets, max, average, LHM
comparison and actual dashboard source. It does not dump the entire table.

## Hardware verification boundary

The local CPU was identified as AMD Ryzen 7 9800X3D 8-Core Processor, 8 cores/16
threads, CPUID signature 00B40F40 (family 0x1A/model 0x44). A framework-hosted,
non-elevated probe received Windows error 5 (access denied) from PawnIO. The
packaged executable requests `requireAdministrator`; when run that way, it loaded
the signed module and detected PM version `0x00620105`.

The first verified sample was Core 0–7 = 36.1, 33.3, 35.0, 33.4, 33.1, 32.5,
32.5 and 32.1 °C; Max Core = 36.1 °C (Core 0); Average Core = 33.5 °C; LHM
Tctl/Tdie comparison = 54.6 °C. This confirms the allowed 0x620105 layout on
this machine. The sample was idle and is evidence of telemetry access, not a
load or accuracy characterization.

Run the installed app with administrator consent and `--diagnose` when repeating
the check. If another machine reports a different PM version, stop and research
that exact layout; do not expand the allowlist or change offsets speculatively.
