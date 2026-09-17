using System;
using Il2CppScheduleOne.Delivery;
using VehicleHandlers.Assignments;
using VehicleHandlers.Contracts;

namespace VehicleHandlers.Reservations
{
    public sealed class HandlerReservationCoordinator
    {
        private readonly HandlerAssignmentResolver resolver;
        private readonly HandlerReservationRegistry reservations;

        public HandlerReservationCoordinator(
            HandlerAssignmentResolver resolver,
            HandlerReservationRegistry reservations)
        {
            this.resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            this.reservations = reservations ?? throw new ArgumentNullException(nameof(reservations));
        }

        public HandlerValidationResult TryReserve(HandlerAssignment assignment, int acquiredAtGameMinute)
        {
            if (DeliveryManager.Instance == null || !DeliveryManager.Instance.IsServerInitialized)
            {
                return HandlerValidationResult.Failure(
                    HandlerValidationCode.HandlerUnavailable,
                    "Only the active server may reserve a loading bay.");
            }

            HandlerValidationResult validation = resolver.TryResolveForRegistration(assignment, out ResolvedHandlerAssignment resolved);
            if (!validation.IsValid)
            {
                return validation;
            }

            HandlerBayKey bay = new HandlerBayKey(
                resolved.DestinationProperty.PropertyCode,
                assignment.DestinationDockIndex);

            bool vanillaAvailable;
            using (HandlerReservationQueryScope.Enter(assignment.HandlerGuid))
            {
                vanillaAvailable = DeliveryManager.Instance.IsLoadingBayFree(
                    resolved.DestinationProperty,
                    assignment.DestinationDockIndex);
            }

            if (!vanillaAvailable)
            {
                return HandlerValidationResult.Failure(
                    reservations.IsReservedByOther(bay, assignment.HandlerGuid)
                        ? HandlerValidationCode.DestinationBayReserved
                        : HandlerValidationCode.DestinationBayOccupied,
                    reservations.IsReservedByOther(bay, assignment.HandlerGuid)
                        ? "The destination loading bay is reserved by another Handler."
                        : "The destination loading bay is occupied or reserved by a standard delivery.");
            }

            HandlerReservation requested = new HandlerReservation(
                bay,
                assignment.HandlerGuid,
                assignment.VehicleGuid,
                acquiredAtGameMinute);
            if (!reservations.TryReserve(requested, out HandlerReservation conflict))
            {
                return HandlerValidationResult.Failure(
                    HandlerValidationCode.DestinationBayReserved,
                    conflict == null
                        ? "This Handler already owns another loading-bay reservation."
                        : "The destination loading bay is reserved by another Handler.");
            }

            return HandlerValidationResult.Success();
        }

        public bool IsAvailableToOwner(HandlerAssignment assignment)
        {
            if (assignment == null || DeliveryManager.Instance == null || !DeliveryManager.Instance.IsServerInitialized)
            {
                return false;
            }

            HandlerValidationResult validation = resolver.TryResolveForRegistration(assignment, out ResolvedHandlerAssignment resolved);
            if (!validation.IsValid)
            {
                return false;
            }

            using (HandlerReservationQueryScope.Enter(assignment.HandlerGuid))
            {
                return DeliveryManager.Instance.IsLoadingBayFree(
                    resolved.DestinationProperty,
                    assignment.DestinationDockIndex);
            }
        }

        public bool Release(HandlerAssignment assignment)
        {
            return assignment != null && reservations.ReleaseByHandler(assignment.HandlerGuid);
        }
    }
}
