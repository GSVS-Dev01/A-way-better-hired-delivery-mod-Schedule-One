using System;
using Il2CppScheduleOne.Delivery;
using Il2CppScheduleOne.Property;
using Il2CppScheduleOne.Vehicles;
using VehicleHandlers.Contracts;

namespace VehicleHandlers.Assignments
{
    public sealed class ResolvedHandlerAssignment
    {
        public ResolvedHandlerAssignment(
            HandlerAssignment assignment,
            LandVehicle vehicle,
            Property destinationProperty,
            LoadingDock destinationDock)
        {
            Assignment = assignment;
            Vehicle = vehicle;
            DestinationProperty = destinationProperty;
            DestinationDock = destinationDock;
        }

        public HandlerAssignment Assignment { get; }

        public LandVehicle Vehicle { get; }

        public Property DestinationProperty { get; }

        public LoadingDock DestinationDock { get; }
    }

    public sealed class HandlerAssignmentResolver : IHandlerAssignmentValidator
    {
        private readonly HandlerAssignmentRegistry assignments;
        private readonly HandlerReservationRegistry reservations;

        public HandlerAssignmentResolver(
            HandlerAssignmentRegistry assignments,
            HandlerReservationRegistry reservations)
        {
            this.assignments = assignments ?? throw new ArgumentNullException(nameof(assignments));
            this.reservations = reservations ?? throw new ArgumentNullException(nameof(reservations));
        }

        public HandlerValidationResult Validate(HandlerAssignment assignment)
        {
            return TryResolve(assignment, out _);
        }

        public HandlerValidationResult TryResolve(HandlerAssignment assignment, out ResolvedHandlerAssignment resolved)
        {
            resolved = null;
            if (assignment == null)
            {
                return HandlerValidationResult.Failure(HandlerValidationCode.MissingHandler, "Assignment data is missing.");
            }

            LandVehicle vehicle = ResolveOwnedVehicle(assignment.VehicleGuid);
            if (vehicle == null)
            {
                return HandlerValidationResult.Failure(HandlerValidationCode.MissingVehicle, "The assigned owned vehicle was not found.");
            }

            if (!vehicle.IsPlayerOwned)
            {
                return HandlerValidationResult.Failure(HandlerValidationCode.VehicleNotOwned, "The assigned vehicle is not player-owned.");
            }

            if (vehicle.IsOccupied)
            {
                return HandlerValidationResult.Failure(HandlerValidationCode.VehicleOccupied, "The assigned vehicle is occupied.");
            }

            if (assignments.IsVehicleAssignedToOther(assignment.VehicleGuid, assignment.HandlerGuid))
            {
                return HandlerValidationResult.Failure(
                    HandlerValidationCode.VehicleAlreadyAssigned,
                    "The assigned vehicle belongs to another Handler assignment.");
            }

            Property property = ResolveOwnedProperty(assignment.DestinationPropertyCode);
            if (property == null)
            {
                return HandlerValidationResult.Failure(
                    HandlerValidationCode.MissingDestinationProperty,
                    "The destination property was not found among owned properties.");
            }

            if (!property.IsOwned)
            {
                return HandlerValidationResult.Failure(
                    HandlerValidationCode.DestinationPropertyNotOwned,
                    "The destination property is not owned.");
            }

            if (!HandlerDefaults.IsSupportedProperty(property.PropertyName, property.PropertyCode))
            {
                return HandlerValidationResult.Failure(
                    HandlerValidationCode.UnsupportedDestinationProperty,
                    "The destination property is not supported by Vehicle Handlers.");
            }

            if (assignment.DestinationDockIndex < 0 ||
                assignment.DestinationDockIndex >= property.LoadingDockCount ||
                assignment.DestinationDockIndex >= property.LoadingDocks.Length)
            {
                return HandlerValidationResult.Failure(
                    HandlerValidationCode.InvalidDestinationBay,
                    "The selected loading bay does not exist at the destination property.");
            }

            LoadingDock dock = property.LoadingDocks[assignment.DestinationDockIndex];
            if (dock == null)
            {
                return HandlerValidationResult.Failure(HandlerValidationCode.InvalidDestinationBay, "The selected loading bay is unavailable.");
            }

            if (IsOccupiedByOther(dock, vehicle))
            {
                return HandlerValidationResult.Failure(
                    HandlerValidationCode.DestinationBayOccupied,
                    "The selected loading bay is occupied.");
            }

            HandlerBayKey bay = new HandlerBayKey(property.PropertyCode, assignment.DestinationDockIndex);
            if (reservations.IsReservedByOther(bay, assignment.HandlerGuid))
            {
                return HandlerValidationResult.Failure(
                    HandlerValidationCode.DestinationBayReserved,
                    "The selected loading bay is reserved by another Handler.");
            }

            resolved = new ResolvedHandlerAssignment(assignment, vehicle, property, dock);
            return HandlerValidationResult.Success();
        }

        private static LandVehicle ResolveOwnedVehicle(string vehicleGuid)
        {
            string expected = Normalize(vehicleGuid);
            if (expected.Length == 0 || VehicleManager.Instance == null)
            {
                return null;
            }

            foreach (LandVehicle vehicle in VehicleManager.Instance.PlayerOwnedVehicles)
            {
                if (vehicle != null && Normalize(vehicle.GUID.ToString()) == expected)
                {
                    return vehicle;
                }
            }

            return null;
        }

        private static Property ResolveOwnedProperty(string propertyCode)
        {
            string expected = Normalize(propertyCode);
            if (expected.Length == 0)
            {
                return null;
            }

            foreach (Property property in Property.OwnedProperties)
            {
                if (property != null && Normalize(property.PropertyCode) == expected)
                {
                    return property;
                }
            }

            return null;
        }

        private static bool IsOccupiedByOther(LoadingDock dock, LandVehicle assignedVehicle)
        {
            if (dock.DynamicOccupant != null && dock.DynamicOccupant.Pointer != assignedVehicle.Pointer)
            {
                return true;
            }

            if (dock.StaticOccupant != null && dock.StaticOccupant.Pointer != assignedVehicle.Pointer)
            {
                return true;
            }

            return dock.IsInUse && dock.DynamicOccupant == null && dock.StaticOccupant == null;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();
        }
    }
}
