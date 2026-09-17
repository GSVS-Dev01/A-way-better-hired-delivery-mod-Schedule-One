# Current Handover

## Active claim

- Backlog: `VH-M2-003`
- Owner: Codex
- Branch: `feat/vh-m2-003-handler-lifecycle`
- Status: `DONE`

## Current outcome

VH-M2-003 bridges exact base `Employee.CanWork` results into the Handler runtime, adds validation-driven work-start states, and centralizes the shared reservation service. Reset, unassignment, firing, marker destruction, leave/despawn, registry clear, and mod unload release only reservations owned by that Handler while allowing base lifecycle methods to continue.

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
- In-game Fixer dialogue, payment, networking, conversion, and appearance verification remains part of the isolated M5 runtime matrix.

Commands:

```powershell
.\scripts\verify-game-api.ps1 -MelonLoaderRoot "<out-of-tree-MelonLoader>"
.\scripts\build-il2cpp.ps1 -MelonLoaderRoot "<out-of-tree-MelonLoader>" -DotNet "<dotnet-8.0.425>"
```

## Next work

1. Review the stacked VH-M2-003 draft PR.
2. Begin VH-M3-001 on a separate claimed branch using the frozen assignment model.
3. Keep runtime in-game hiring/payment/fire/leave verification in the M5 isolated matrix.
