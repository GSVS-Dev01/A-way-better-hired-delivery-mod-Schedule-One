# Current Handover

## Active claim

- Backlog: `VH-M2-002`
- Owner: Codex
- Branch: `feat/vh-m2-002-handler-hiring`
- Status: `DONE`

## Current outcome

VH-M2-002 adds a distinct `Vehicle Handler (Driver)` Fixer choice without replacing the vanilla Packager. It reuses normal choice validation, property capacity, Packager signing fee/daily wage, random employee creation, and FishNet RPC flow. A transient namespaced employee-ID marker is removed server-side before initialization/save and converts only the corresponding returned donor.

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

1. Review the stacked VH-M2-002 draft PR.
2. Claim VH-M2-003 and bind Handler work states, payment/work checks, firing, unassignment, leave/despawn, and reservation cleanup to the base employee lifecycle.
3. Preserve the current rule that ordinary Packager hires and loaded Packagers remain unmarked.
