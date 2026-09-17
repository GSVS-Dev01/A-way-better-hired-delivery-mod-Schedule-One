# Current Handover

## Active claim

- Backlog: `VH-M0-001`, `VH-M0-002`
- Owner: Codex
- Branch: `feat/vh-m0-001-project-scaffold`
- Status: `VERIFYING`

## Current outcome

The public-repository-safe foundation, canonical project documents, IL2CPP project scaffold, and exact API verification entry point are complete and ready for review.

## Compatibility target

- Schedule I: `0.4.6f13`
- Backend: IL2CPP
- MelonLoader: `0.7.0 Open-Beta` reference layout
- `Assembly-CSharp.dll` SHA-256: `0D2EB364F3E84120AF7CCC9FA6BAFD597D42D495EBACC3A260CB4CA0CF0513DA`

Game references remain outside this repository.

## Verification status

- Public repository cloned successfully and confirmed empty.
- Compatibility hash confirmed from the existing out-of-tree reference set.
- Static API verification passed for the initial Handler, employee, delivery, dock, property, vehicle, parking, NPC, and save signatures.
- Release build completed with zero warnings and zero errors.
- Initial scaffold DLL SHA-256: `BAB430DDDCD2E690A085838B42293662F0BB6E70FF1A8AC4E560E2DB49F35655`

Commands:

```powershell
.\scripts\verify-game-api.ps1 -MelonLoaderRoot "<out-of-tree-MelonLoader>"
.\scripts\build-il2cpp.ps1 -MelonLoaderRoot "<out-of-tree-MelonLoader>" -DotNet "<dotnet-8.0.425>"
```

## Next work

1. Review and merge the foundation branch.
2. Claim `VH-M1-001` on its own branch after the foundation is publicly visible.
3. Freeze the cross-worker contracts before parallel M2/M3/M4 implementation.
