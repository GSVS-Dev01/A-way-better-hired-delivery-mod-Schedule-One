# Architecture

## Runtime ownership

The server is authoritative for assignments, work state, bay reservations, and physical vehicle transitions. Clients render replicated employee, vehicle, and dock state and may request configuration changes through validated server paths.

## Components

Source-level identities, validation results, state transitions, reservation ownership, movement ordering, and persistence lifecycle are frozen in [INTERNAL_CONTRACTS.md](INTERNAL_CONTRACTS.md). M2, M3, and M4 implementations must use those contracts rather than creating parallel models.

### Handler employee

`HandlerEmployee` supplies the missing runtime worker for the existing `EEmployeeType.Handler` value. An existing employee prefab may be used as a construction donor, but role-specific behavior is replaced and the resulting worker is identified and displayed as Handler.

In 0.4.6f13, `EmployeeManager.GetEmployeePrefab(EEmployeeType.Handler)` resolves to the game's `PackagerPrefab`. Vehicle Handlers therefore does not treat the enum value alone as proof that an employee is a driver. `HandlerEmployeeFactory` explicitly converts only the newly requested donor instance, attaches the injected `HandlerEmployee` marker, and registers its GUID/pointer with `HandlerEmployeeRegistry`. Unmarked base Handlers/Packagers retain vanilla behavior.

The conversion keeps the original `Packager`/`Employee`/`NPC`/FishNet object as the networked and saved employee. Packaging, brick-press, and move-item role behaviours are disabled for marked instances. A Harmony reverse patch invokes the base `Employee.UpdateBehaviour` implementation so general employee lifecycle logic continues while `Packager.UpdateBehaviour` is bypassed. Handler-only configuration and state live in the managed runtime controller defined by the frozen contracts.

### Hiring discriminator

The Fixer choice list receives a distinct `Vehicle Handler (Driver)` entry only when the vanilla Packager choice is available. The dialogue continues through the vanilla Packager branch so property capacity, signing fee, wage source, random appearance, and employee creation remain base-game behavior.

Because FishNet serializes only the existing `EEmployeeType`, the selected custom role is carried in a namespaced marker prepended to the outbound employee ID. The server RPC strips that marker before calling `CreateEmployee_Server`; it is never assigned to the NPC or persisted. A thread-scoped conversion flag then converts only that returned donor. Unmarked Handler/Packager requests and save-loader calls cannot consume the custom conversion path.

### Employee lifecycle bridge

The Handler runtime samples the exact base `Employee.CanWork()` result through a Harmony reverse patch. Work-hour, bed, payment, firing, and other vanilla availability rules therefore gate Handler work without duplicating their balance logic. `TryBeginWork` applies the frozen validation codes to `WaitingForVehicle`, `WaitingForDestinationBay`, `Idle`, or `Faulted`; M4 owns the later `Moving` and `Completing` transitions.

`HandlerRuntimeServices.Reservations` is the single shared reservation service. Handler reset/unassignment, `Packager.Fire`, `Employee.LeavePropertyAndDespawn`, marker destruction, registry clear, and mod unload all release by Handler owner GUID. The original base fire and leave/despawn methods still execute.

### Assignment registry

`HandlerAssignmentRegistry` owns versioned assignments keyed by normalized Handler GUID and maintains a second atomic index by normalized vehicle GUID. Each assignment records vehicle GUID, destination property code/GUID, dock index, enabled state, manual hidden state, and recoverable trip state. Registry reads return defensive clones, a Handler reassignment releases its old vehicle index, and another Handler cannot claim the same vehicle.

`HandlerAssignmentResolver` binds configuration only to an existing entry in `VehicleManager.PlayerOwnedVehicles` and an existing entry in `Property.OwnedProperties`. It validates player ownership, vehicle occupancy, the supported property allow-list, exact dock bounds, physical dock occupants, and Handler reservation conflicts. It never creates a replacement vehicle. The Handler runtime validates and registers configuration as one operation and rejects changes while a trip is moving or completing.

### Reservation registry

`HandlerReservationRegistry` owns destination reservations keyed by stable property and dock identity. Reservation acquisition and release are atomic and owner-specific. `HandlerReservationCoordinator` requires an initialized FishNet server, resolves an existing assignment, asks vanilla `DeliveryManager.IsLoadingBayFree(Property, int)`, and only then acquires the registry entry.

A Harmony postfix combines vanilla availability with Handler reservations. It never changes a vanilla `false`, so physical occupants and standard-delivery conflicts remain authoritative. A thread-local query scope lets the owning Handler recheck its already-reserved bay; standard delivery calls carry no owner and other Handler calls carry a different owner, so both receive `false`. Scope nesting restores the previous identity and cannot leak a Handler into later delivery checks.

### Movement coordinator

`HandlerMovementCoordinator` runs from the marked Handler's normal work tick. It structurally resolves the assignment, leaves an exact vehicle already occupying its target alone, obtains the destination reservation, and captures both a persisted `HandlerVehicleSnapshot` and live source dock/parking references. It then clears only occupant references that point to that same vehicle, hides it, disables obstacles/physics, and advances the trip using `TimeManager.GetTotalMinSum`.

After one game minute, owner-scoped bay checks continue until vanilla availability is true. Placement uses the destination dock's free `ParkingSpot`, `LandVehicle.AlignTo`, `ParkingSpot.SetOccupant`, `LoadingDock.SetOccupant`, and `RefreshOccupant`, then restores visibility/obstacles while keeping the docked vehicle non-simulated. No vehicle constructor, prefab, spawn, or replacement API is used. Failure and lifecycle cancellation first try the original live parking spot/dock, then make the same vehicle visible and physical if a safe source occupant cannot be restored.

### Management UI

The Handler configuration panel reuses the base clipboard's `PackagerConfigPanel` only for configurations whose Packager owner is present in `HandlerEmployeeRegistry`. The vanilla bed control remains bound so normal home/locker setup is unchanged. Vanilla Packager station and route controls are hidden for that panel instance and replaced with runtime-built, base-styled selection controls for one existing owned vehicle, one supported owned property, and one exact loading-bay index. Property changes rebuild the bay choices, and assignment validation is shown in the panel.

Land vehicles do not implement the base `IConfigurable` contract in 0.4.6f13. Adding a vehicle-side adapter would compete with base vehicle interaction and management selection, so the safe fallback required by the product specification keeps the same controls on the Handler panel. Manual load and hide buttons dispatch through `IHandlerManualBayController`; the default reports unavailable until M4 installs the authoritative movement implementation.

## Persistence

Base employee and vehicle data remains owned by the game. Mod-only assignment and in-progress state is stored in the schema-versioned `UserData/VehicleHandlers.json`. One document contains profiles keyed by save-slot identity; fallback save names are hashed so local paths or player-entered names are not persisted as keys. Writes use a same-directory temporary file followed by replacement, and an unreadable source file is copied to a timestamped backup before fresh data replaces it.

`LoadManager.StartGame` clears transient assignment/reservation state and queues the selected profile. After the base game reports loaded, a bounded rebinder searches `EmployeeManager.AllEmployees`, `VehicleManager.PlayerOwnedVehicles`, and owned properties by the saved identities. The sidecar Handler GUID is the reload discriminator: only the matching Packager donor is reconverted, with donor role reset disabled so normal bed/home data survives. Interrupted movement timers and reservations are not resumed blindly; state returns to idle or explicitly hidden, then M4 can recover the same physical vehicle from its saved safe snapshot. Missing or invalid identities are discarded without spawning replacements or retaining bay ownership.

## Failure handling

- Missing Handler: discard its orphan assignment after load stabilization.
- Missing vehicle: disable assignment and release reservation.
- Missing property/bay: disable assignment and release reservation.
- Interrupted trip: restore the known physical vehicle to a safe visible state or preserve its valid base-game saved position.
- Occupied target: remain waiting; do not displace the occupant.

