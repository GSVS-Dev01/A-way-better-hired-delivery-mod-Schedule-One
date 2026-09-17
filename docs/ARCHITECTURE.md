# Architecture

## Runtime ownership

The server is authoritative for assignments, work state, bay reservations, and physical vehicle transitions. Clients render replicated employee, vehicle, and dock state and may request configuration changes through validated server paths.

## Components

Source-level identities, validation results, state transitions, reservation ownership, movement ordering, and persistence lifecycle are frozen in [INTERNAL_CONTRACTS.md](INTERNAL_CONTRACTS.md). M2, M3, and M4 implementations must use those contracts rather than creating parallel models.

### Handler employee

`HandlerEmployee` supplies the missing runtime worker for the existing `EEmployeeType.Handler` value. An existing employee prefab may be used as a construction donor, but role-specific behavior is replaced and the resulting worker is identified and displayed as Handler.

In 0.4.6f13, `EmployeeManager.GetEmployeePrefab(EEmployeeType.Handler)` resolves to the game's `PackagerPrefab`. Vehicle Handlers therefore does not treat the enum value alone as proof that an employee is a driver. `HandlerEmployeeFactory` explicitly converts only the newly requested donor instance, attaches the injected `HandlerEmployee` marker, and registers its GUID/pointer with `HandlerEmployeeRegistry`. Unmarked base Handlers/Packagers retain vanilla behavior.

The conversion keeps the original `Packager`/`Employee`/`NPC`/FishNet object as the networked and saved employee. Packaging, brick-press, and move-item role behaviours are disabled for marked instances. A Harmony reverse patch invokes the base `Employee.UpdateBehaviour` implementation so general employee lifecycle logic continues while `Packager.UpdateBehaviour` is bypassed. Handler-only configuration and state live in the managed runtime controller defined by the frozen contracts.

### Assignment registry

`HandlerAssignmentRegistry` owns versioned assignments keyed by Handler GUID. Each assignment records vehicle GUID, destination property code/GUID, dock index, enabled state, manual hidden state, and recoverable trip state.

### Reservation registry

`HandlerReservationRegistry` owns destination reservations keyed by stable property and dock identity. Reservation acquisition and release are atomic and owner-specific. A Harmony postfix on `DeliveryManager.IsLoadingBayFree(Property, int)` combines vanilla availability with Handler reservations.

### Movement coordinator

`HandlerMovementCoordinator` validates an assignment, detaches the exact vehicle from its current dock or parking spot, advances a timed trip, waits for destination availability, aligns the vehicle to the dock, and restores normal visibility/physics.

### Management UI

The Handler configuration panel follows the delivery application's location/bay selection pattern. Vehicle-side management is added only if a safe `IConfigurable` adapter can be attached without replacing base vehicle interaction; otherwise all controls remain on the Handler panel.

## Persistence

Base employee and vehicle data remains owned by the game. Mod-only assignment and in-progress state is stored in `UserData/VehicleHandlers.json`. Loading resolves GUIDs against existing runtime objects and never spawns a missing vehicle.

## Failure handling

- Missing Handler: discard its orphan assignment after load stabilization.
- Missing vehicle: disable assignment and release reservation.
- Missing property/bay: disable assignment and release reservation.
- Interrupted trip: restore the known physical vehicle to a safe visible state or preserve its valid base-game saved position.
- Occupied target: remain waiting; do not displace the occupant.

