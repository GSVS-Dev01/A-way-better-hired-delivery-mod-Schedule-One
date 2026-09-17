# Current Handover

## Active claim

- Backlog: `VH-M2-001`
- Owner: Codex
- Branch: `feat/vh-m2-001-handler-employee`
- Status: `DONE`

## Current outcome

VH-M2-001 now provides an injected `HandlerEmployee` marker, explicit donor conversion factory, managed Handler runtime/state controller, guarded role-behaviour replacement, and per-instance teal appearance recolouring. Conversion preserves the original networked/saved employee object and does not alter unmarked base Handlers/Packagers.

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

1. Review the stacked VH-M2-001 draft PR.
2. Claim VH-M2-002 and wire the explicit Handler/Driver selection into the base Fixer hiring flow.
3. Ensure only that new selection invokes `HandlerEmployeeFactory.ConvertDonor`; ordinary base Handler/Packager hires must remain unmarked.
