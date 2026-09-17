# Current Handover

## Active claim

- Backlog: `VH-M3-003`
- Owner: Codex
- Branch: `feat/vh-m3-003-handler-persistence`
- Status: `IN_PROGRESS`

## Current outcome

Persist versioned Handler assignments under `UserData/VehicleHandlers.json`, write through normal SaveManager operations, and rebind only to existing marked Handlers, vehicles, owned properties, and loading bays after load stabilization. Stale or invalid records must fail closed and release reservations without spawning replacements.

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
- In-game Fixer dialogue, payment, networking, conversion, and appearance verification remains part of the isolated M5 runtime matrix.

Commands:

```powershell
.\scripts\verify-game-api.ps1 -MelonLoaderRoot "<out-of-tree-MelonLoader>"
.\scripts\build-il2cpp.ps1 -MelonLoaderRoot "<out-of-tree-MelonLoader>" -DotNet "<dotnet-8.0.425>"
```

## Next work

1. Map the exact load-completion hook and game-save identity surface.
2. Add atomic JSON read/write and schema validation.
3. Queue records until marked Handler runtime objects exist, then validate and rebind safely.
