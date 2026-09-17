# Current Handover

## Active claim

- Backlog: `VH-M1-001`, `VH-M1-002`
- Owner: Codex
- Branch: `feat/vh-m1-001-api-verification`
- Status: `DONE`

## Current outcome

The exact f13 compatibility verifier now covers the hiring, employee lifecycle, management clipboard, property, vehicle, parking, loading-dock, delivery reservation, game-time, save/load, and NPC vehicle surfaces required by the planned implementation. Shared assignment, state, validation, reservation, recovery, and persistence contracts are frozen for M2, M3, and M4.

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
- Release build completed with zero warnings and zero errors.
- M1 verification DLL SHA-256: `0E599969A51E1620615EDA19D003ABA3B213617987A42CE704B356C55E83C93B`
- Runtime gameplay verification remains deferred until the implementation milestones.

Commands:

```powershell
.\scripts\verify-game-api.ps1 -MelonLoaderRoot "<out-of-tree-MelonLoader>"
.\scripts\build-il2cpp.ps1 -MelonLoaderRoot "<out-of-tree-MelonLoader>" -DotNet "<dotnet-8.0.425>"
```

## Next work

1. Review and merge the repository-foundation draft PR.
2. Review the stacked M1 contracts/API-verification draft PR.
3. Begin M2, M3, and M4 on separate claimed branches using `docs/INTERNAL_CONTRACTS.md` as their shared boundary.
