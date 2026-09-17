using VehicleHandlers.Contracts;

namespace VehicleHandlers.Runtime
{
    public static class HandlerRuntimeServices
    {
        public static HandlerReservationRegistry Reservations { get; } = new HandlerReservationRegistry();

        public static void Clear()
        {
            Reservations.Clear();
        }
    }
}
