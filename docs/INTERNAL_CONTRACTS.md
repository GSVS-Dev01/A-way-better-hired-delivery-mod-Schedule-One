# Frozen Internal Contracts

These contracts are the shared boundary for M2 employee work, M3 configuration/persistence work, and M4 vehicle/reservation work. Changes require coordination across every affected claimed backlog item.

## Runtime authority

The server owns assignment mutation, validation, state transitions, vehicle movement, and loading-bay reservations. A client may request a change through the management UI, but it must not directly mutate an assignment, dock occupant, parking occupant, or vehicle transform.

## Stable identities

- Handler identity is the base employee GUID serialized as an uppercase invariant string.
- Vehicle identity is `LandVehicle.GUID` serialized as an uppercase invariant string.
- Destination property identity prefers the runtime property GUID when one is available. `PropertyCode` is the stable fallback and is persisted alongside it.
- Loading bays are zero-based indices into the resolved property's `LoadingDocks` array.
- Runtime reservations use `HandlerBayKey`, consisting of a normalized property GUID/code plus loading-bay index.
- Display names are never identity keys.

## Assignment ownership

`HandlerConfiguration` is the management/persistence input model. It creates the canonical `HandlerAssignment` only after server-side validation. Each Handler may own at most one assignment and each vehicle may be referenced by at most one active assignment.

An assignment records:

- Handler GUID.
- Exact existing vehicle GUID.
- Destination property GUID/code.
- Destination loading-bay index.
- Enabled state.
- Manual hidden state.
- Current Handler state.
- Remaining trip minutes.
- Last safe vehicle location snapshot for recovery.

An assignment never contains a prefab code that could be used to spawn a replacement vehicle.

## Supported destinations

Version 1 accepts owned instances of Barn, Bungalow, Storage Unit, Dock Warehouse, and Mansion. UI labels may be localized or styled, but identity and validation use resolved runtime property data.

## Validation result contract

Validation returns one `HandlerValidationResult`. `Valid` is the only success code. Failure codes distinguish missing or unavailable Handler, missing/non-owned/occupied/already-assigned vehicle, unsupported or non-owned property, invalid/occupied/reserved bay, disabled assignment, and in-progress movement.

Callers must surface the result message to the management UI when a player action fails. Background work may log the code and transition to a waiting state when the condition is recoverable.

## Handler state machine

The source-level states are:

| State | Meaning |
| --- | --- |
| `Unconfigured` | Required assignment fields are unresolved or absent. |
| `Idle` | Assignment is valid and no movement is requested. |
| `WaitingForVehicle` | The configured vehicle is temporarily unavailable or occupied. |
| `WaitingForDestinationBay` | The target bay cannot yet be reserved or physically occupied. |
| `Moving` | The exact vehicle is detached/hidden and its game-time trip is running. |
| `Completing` | The vehicle is being aligned and registered as the dock occupant. |
| `Hidden` | Manual release has put the vehicle in its safe hidden/additional-parking state. |
| `Faulted` | Recovery or explicit reconfiguration is required. |

All transitions pass through `HandlerStateMachine.EnsureTransition`. A worker must not assign arbitrary enum values during normal runtime flow. Runtime state owners implement `IHandlerStatePublisher` and publish `HandlerStateChangedEventArgs` only after a successful transition; subscribers treat the event as notification rather than authority.

The default trip duration is one in-game minute. M4 may make this configurable later without changing the state contract.

## Reservation contract

`HandlerReservationRegistry` is the canonical in-process registry. It enforces:

- One active destination reservation per Handler.
- One reservation owner per property/bay key.
- Idempotent acquisition only when Handler GUID and vehicle GUID both match the existing reservation.
- Owner-only release.
- Atomic registry operations.
- Read-only snapshots for diagnostics.

M4 must consult vanilla `DeliveryManager.IsLoadingBayFree(Property, int)` before acquiring a Handler reservation. Its Harmony patch then makes a Handler-reserved bay unavailable to standard deliveries. The patch must preserve a vanilla `false` result and may only change vanilla `true` to `false` when another Handler owns the reservation.

Physical `LoadingDock.DynamicOccupant` and `StaticOccupant` values remain authoritative. A reservation is coordination state, not permission to displace an occupant.

Reservations must be released when movement completes or fails, and when the Handler is fired, deleted, unassigned, leaves/despawns, or the mod unloads. Load reconciliation clears stale reservations before rebinding persisted assignments.

## Vehicle transition contract

M4 operates on the resolved existing `LandVehicle`; assignment execution must never call a vehicle spawn API.

Before detaching a vehicle, the server records its safe parking/dock state and validates that it is player-owned, unoccupied, not already moving, and uniquely assigned. Movement then follows this order:

1. Confirm vanilla bay availability and atomically reserve the destination.
2. Detach the exact vehicle from its current loading dock or parking spot.
3. Preserve its GUID, storage contents, ownership, colour, and save identity.
4. Hide/disable physical simulation while the game-time timer advances.
5. Continue waiting if a physical occupant or vanilla delivery conflict exists at completion time.
6. Align the same vehicle to the destination loading dock and call normal occupant/parking APIs.
7. Restore visibility/physics, enter `Idle`, and release the reservation.

Any failure after detachment must attempt safe recovery from `LastSafeVehicleState`. It must not create a substitute vehicle or clear an unrelated delivery occupant.

## Persistence lifecycle

Mod-only data is serialized as schema version 1 in `UserData/VehicleHandlers.json`. Base employee and vehicle save data remains owned by the game.

Load order is:

1. Deserialize the assignment document without spawning objects.
2. Wait for employees, properties, loading docks, and vehicles to load.
3. Rebind all stable identities to existing runtime objects.
4. Reject duplicate Handler or vehicle claims deterministically.
5. Validate property/bay indices and ownership.
6. Reconstruct safe runtime state; clear stale reservations.
7. Disable invalid records and remove orphan records only after load stabilization.

Save hooks snapshot server-authoritative state during normal `SaveManager.Save` operations. Interrupted `Moving` records are recovered against the same existing vehicle on load.

## Public implementation boundaries

- M2 owns Handler construction, hiring integration, employee identity/appearance, and employee lifecycle hooks.
- M3 owns assignment mutation APIs, management panel, serialization, and load rebinding.
- M4 owns reservation integration, physical vehicle transitions, movement timing, and manual load/hide operations.
- Cross-boundary communication uses these contracts and state events; workers do not duplicate registries or persistence models.
