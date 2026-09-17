# Current Handover

## Completed claim

- Backlog: `VH-M3-001`
- Owner: Codex
- Branch: `feat/vh-m3-001-handler-assignment`
- Status: `DONE`

## Current outcome

The authoritative assignment layer is complete. It maintains atomic indexes by Handler GUID and owned-vehicle GUID, rejects duplicate vehicle claims, resolves only existing player-owned vehicles, resolves only owned supported properties, validates exact loading-bay indexes and occupancy, and returns defensive assignment snapshots. Reconfiguration is refused during `Moving` or `Completing` states.

## Compatibility target

- Schedule I: `0.4.6f13`
- Backend: IL2CPP
- MelonLoader: `0.7.0 Open-Beta` reference layout
- `Assembly-CSharp.dll` SHA-256: `0D2EB364F3E84120AF7CCC9FA6BAFD597D42D495EBACC3A260CB4CA0CF0513DA`

Game references remain outside this repository.

## Verification status

- Public repository cloned successfully and confirmed empty.
- Compatibility hash confirmed from the existing out-of-tree reference set.
- Exact API verification passed against the target assembly hash.
- Return types are checked in addition to exact type, method, property, and parameter identities.
- Exact verification now includes every VH-M2-001 Harmony target, donor role behaviour, and donor configuration reset surface.
- Exact verification now includes Fixer choice/callback/text methods, dynamic choice fields, and the employee RPC logic method used by VH-M2-002.
- Exact verification includes the Packager fire override and base CanWork/leave targets used by VH-M2-003.
- Release build completed with zero warnings and zero errors.
- VH-M2-003 DLL SHA-256: `9734EAAEE56D990970DADD90ED06CFD455C678F0D0AB401CF9E2D4E696F3F8E2`
- Static source check confirms no vehicle spawn API is used.
- Managed marker tests pass for custom-role encode/decode and prove an unmarked employee ID remains unchanged.
- Reservation invariant tests pass for idempotent owner acquisition, conflict rejection, and owner-only release.
- Assignment registry tests pass for duplicate vehicle rejection, atomic reassignment, old-index release, and defensive lookup copies.
- Assignment resolution uses existing `VehicleManager.PlayerOwnedVehicles` and `Property.OwnedProperties` collections only; no vehicle spawn or instantiation path exists in source.
- VH-M3-001 DLL SHA-256: `9E33B8DD72548FE898059B9491D9DE1F08595D27D35CF3C2258A0D535FF5ACDF`
- In-game Fixer dialogue, payment, networking, conversion, and appearance verification remains part of the isolated M5 runtime matrix.

Commands:

```powershell
.\scripts\verify-game-api.ps1 -MelonLoaderRoot "<out-of-tree-MelonLoader>"
.\scripts\build-il2cpp.ps1 -MelonLoaderRoot "<out-of-tree-MelonLoader>" -DotNet "<dotnet-8.0.425>"
```

## Next work

1. Claim `VH-M3-002` on a task-specific branch.
2. Build the Handler clipboard panel against the frozen assignment contracts.
3. Populate vehicle, supported property, and exact loading-bay selections from live owned objects.
