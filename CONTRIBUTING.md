# Contributing

## Source of truth

Read, in order:

1. `docs/PRODUCT_SPEC.md`
2. `docs/ARCHITECTURE.md`
3. `docs/IMPLEMENTATION_PLAN.md`
4. `docs/BACKLOG.md`
5. `docs/HANDOVER.md`

Repository Markdown is authoritative. Chat messages are supporting context only.

## Claiming work

Backlog IDs use `VH-M<milestone>-<number>`. Valid states are `READY`, `IN_PROGRESS`, `BLOCKED`, `VERIFYING`, and `DONE`.

1. Fetch `origin` and inspect active branches and pull requests.
2. Create a branch named `<type>/vh-mN-NNN-short-description`.
3. Mark one backlog item `IN_PROGRESS`, name the owner, and update the handover.
4. Push that status checkpoint and open a draft pull request.
5. Implement only after the public claim is visible.

## Pull requests

Every pull request states:

- Backlog ID and user-visible outcome.
- Target game version and backend.
- Exact API/build/runtime verification performed.
- Save, multiplayer, and compatibility impact.
- Documentation and handover impact.

Do not merge red checks. AI contributors stop after verification and request owner confirmation before merging.

## Definition of done

A task is `DONE` only when its acceptance criteria pass, material behavior is tested, and the handover describes the resulting repository state.

