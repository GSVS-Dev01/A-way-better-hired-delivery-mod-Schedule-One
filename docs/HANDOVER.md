# Current Handover

## Active claim

- Backlog: `VH-M2-003`
- Owner: Codex
- Branch: `feat/vh-m2-003-handler-lifecycle`
- Status: `IN_PROGRESS`

## Current outcome

Bind Handler state and work-start validation to base employee availability/payment behavior. Guarantee assignment and reservation cleanup on unassignment, firing, destruction, leave/despawn, and mod unload while retaining the base leave/despawn implementation.

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
- Release build completed with zero warnings and zero errors.
- VH-M2-002 DLL SHA-256: `AF5D1BC4ABDF839A4EBCB15AC3BDEFDC6F049C4CDC1D065AFED37CC8564050D7`
- Static source check confirms no vehicle spawn API is used.
- Managed marker tests pass for custom-role encode/decode and prove an unmarked employee ID remains unchanged.
- In-game Fixer dialogue, payment, networking, conversion, and appearance verification remains part of the isolated M5 runtime matrix.

Commands:

```powershell
.\scripts\verify-game-api.ps1 -MelonLoaderRoot "<out-of-tree-MelonLoader>"
.\scripts\build-il2cpp.ps1 -MelonLoaderRoot "<out-of-tree-MelonLoader>" -DotNet "<dotnet-8.0.425>"
```

## Next work

1. Bridge exact `Employee.CanWork` results into the Handler runtime.
2. Implement work-start validation-to-state mapping for vehicle and destination waits.
3. Release owner reservations and clear runtime state at every employee lifecycle terminal path.
