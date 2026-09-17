# Current Handover

## Active claim

- Backlog: `VH-M2-001`
- Owner: Codex
- Branch: `feat/vh-m2-001-handler-employee`
- Status: `IN_PROGRESS`

## Current outcome

Implement the genuine Handler runtime worker using the base employee prefab only as a construction donor. Preserve base networking and employee lifecycle components while replacing donor role behavior, applying Handler identity/appearance, and exposing the frozen Handler state/configuration boundary.

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

1. Verify the exact IL2CPP component registration and employee prefab construction path.
2. Implement Handler runtime identity, role-behaviour replacement, and recoloured appearance.
3. Build against the f13 references and publish the stacked VH-M2-001 draft PR.
