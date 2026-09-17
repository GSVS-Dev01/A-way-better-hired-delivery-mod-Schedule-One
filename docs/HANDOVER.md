# Current Handover

## Completed claim

- Backlog: `VH-M4-002`
- Owner: Codex
- Branch: `feat/vh-m4-002-vehicle-movement`
- Status: `DONE`

## Current outcome

The exact-vehicle movement coordinator is complete. On work ticks it resolves the assigned existing vehicle, ignores a vehicle already in its target bay, acquires the destination reservation, snapshots its source dock/parking state, releases the source, hides/disables the same object, advances a one-game-minute trip, waits on completion conflicts, then aligns and registers it through destination parking and dock occupant APIs.

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
- Exact verification now includes the Handler clipboard Harmony target, Packager panel fields, Packager configuration owner, and vehicle labels used by the live option catalog.
- VH-M3-002 builds with zero warnings and zero errors; DLL SHA-256: `7BA1A42ED384FCA028DE47CB2BBF8983BDE648C4D7E463614317E5B54A294345`
- Static checks confirm the UI catalog reads existing owned vehicles/properties only and includes no vehicle creation API.
- Land vehicles do not implement the base `IConfigurable` management contract in f13, so the specified safe fallback keeps configuration on the Handler panel rather than injecting a competing vehicle-side configurable.
- `Load into bay` and `Hide / release bay` dispatch through `IHandlerManualBayController`; M4 supplies the physical movement implementation.
- A valid sidecar Handler GUID is the reload discriminator: only that existing Packager donor is reconverted, while unlisted vanilla Packagers remain untouched. Reload conversion preserves the donor's base bed/home configuration.
- Registration validation is structural, so transient vehicle occupancy or bay occupancy does not erase a valid saved assignment; operational validation still moves the Handler into waiting states.
- Persistence round-trip tests pass for schema version, save-profile scope, exact assignment fields, and atomic temporary-file replacement.
- Interrupted movement state is normalized to idle/hidden with zero remaining timer and no restored reservation. Missing runtime identities retry for 600 post-load frames, then fail closed and release ownership.
- Exact verification now covers all persistence Harmony targets, active save identity, save-slot identity, and the employee registry used for GUID rebinding.
- VH-M3-003 builds with zero warnings and zero errors; DLL SHA-256: `E3F9A9B95A348E00AAA502712C044A06A3861E32D4E2BCC9AD45CB88AFB78E33`
- Handler reservation queries carry a thread-local owner scope, allowing an owning Handler to recheck its own bay while standard deliveries and other Handlers see it as unavailable. Nested scopes restore correctly and do not leak identity.
- Physical dock occupancy remains controlled by the original `IsLoadingBayFree` result; the postfix never turns `false` into `true` and never clears an occupant.
- Reservation invariant tests pass for idempotent acquisition, conflicting-owner rejection, lookup, owner-only release, and nested query-scope isolation.
- Existing reset, unassignment, fire, destroy, leave/despawn, load-start, and mod-unload cleanup paths all release by Handler owner or clear the registry.
- VH-M4-001 builds with zero warnings and zero errors; DLL SHA-256: `4760FC5BBD857162A350A890991F33E32399CC9D9A3B7C7F047FE2687E961147`
- Movement preserves the same `LandVehicle` pointer/GUID and never invokes a spawn, instantiate-vehicle, or replacement path. Storage, ownership, colour, and base save identity are untouched.
- Source `ParkingSpot` and `LoadingDock` occupants are cleared before hiding, leaving the input bay available while the game-time timer runs.
- Completion uses owner-scoped availability, waits in `WaitingForDestinationBay` when blocked, and calls `AlignTo`, `ParkingSpot.SetOccupant`, `LoadingDock.SetOccupant`, and `RefreshOccupant` on success.
- Firing, reset, unassignment, leave/despawn, registry clear, and mod unload cancel active sessions, restore the source spot/dock when still safe, restore visibility/physics, and release the reservation.
- Exact verification now covers the parking-spot collection used by safe-state indexing.
- VH-M4-002 builds with zero warnings and zero errors; DLL SHA-256: `3929D8763C7D52F2DEF8137BB45305624DE6397FF7AA92BD6FC4689D260E71D8`
- In-game Fixer dialogue, payment, networking, conversion, and appearance verification remains part of the isolated M5 runtime matrix.

Commands:

```powershell
.\scripts\verify-game-api.ps1 -MelonLoaderRoot "<out-of-tree-MelonLoader>"
.\scripts\build-il2cpp.ps1 -MelonLoaderRoot "<out-of-tree-MelonLoader>" -DotNet "<dotnet-8.0.425>"
```

## Next work

1. Claim `VH-M4-003` on a task-specific branch.
2. Connect the existing clipboard action gateway to safe immediate load and hide operations.
3. Refuse occupied, moving, conflicting, or non-authoritative requests without clearing unrelated occupants.
