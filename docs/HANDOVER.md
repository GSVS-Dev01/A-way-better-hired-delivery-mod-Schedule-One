# Current Handover

## Active claim

- Backlog: `VH-M2-002`
- Owner: Codex
- Branch: `feat/vh-m2-002-handler-hiring`
- Status: `IN_PROGRESS`

## Current outcome

Add an explicit Vehicle Handler/Driver option to the Fixer employee flow while preserving every vanilla role. Route that selection through the normal Handler/Packager fee, property-capacity, random appearance, employee creation, and networking path, then convert only the resulting marked donor instance.

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
- Release build completed with zero warnings and zero errors.
- VH-M2-001 DLL SHA-256: `E2FBA5FC837FADB1C4FCC35D5823DF6D9B49523BA480B0F9CBB1160A9720FDDC`
- Static source check confirms no vehicle spawn API is used.
- In-game conversion/appearance verification remains part of the isolated M5 runtime matrix after hiring integration can create the worker.

Commands:

```powershell
.\scripts\verify-game-api.ps1 -MelonLoaderRoot "<out-of-tree-MelonLoader>"
.\scripts\build-il2cpp.ps1 -MelonLoaderRoot "<out-of-tree-MelonLoader>" -DotNet "<dotnet-8.0.425>"
```

## Next work

1. Add a distinct dynamic Fixer choice without replacing the vanilla Packager choice.
2. Carry an internal hire marker through the existing FishNet employee RPC without persisting it as the employee ID.
3. Convert only the marked `CreateEmployee_Server` result and verify normal Packager creation remains unchanged.
