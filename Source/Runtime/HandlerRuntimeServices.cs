using VehicleHandlers.Assignments;
using VehicleHandlers.Configuration;
using VehicleHandlers.Contracts;
using VehicleHandlers.Reservations;

namespace VehicleHandlers.Runtime
{
    public static class HandlerRuntimeServices
    {
        public static HandlerReservationRegistry Reservations { get; } = new HandlerReservationRegistry();

        public static HandlerAssignmentRegistry Assignments { get; } = new HandlerAssignmentRegistry();

        public static HandlerAssignmentResolver AssignmentResolver { get; } =
            new HandlerAssignmentResolver(Assignments, Reservations);

        public static IHandlerManualBayController ManualBayController { get; set; } =
            new UnavailableHandlerManualBayController();

        public static HandlerReservationCoordinator ReservationCoordinator { get; } =
            new HandlerReservationCoordinator(AssignmentResolver, Reservations);

        public static void Clear()
        {
            Reservations.Clear();
            Assignments.Clear();
            ManualBayController = new UnavailableHandlerManualBayController();
        }
    }
}
