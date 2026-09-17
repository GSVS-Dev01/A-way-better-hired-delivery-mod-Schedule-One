using System;

namespace VehicleHandlers.Reservations
{
    internal sealed class HandlerReservationQueryScope : IDisposable
    {
        [ThreadStatic]
        private static string currentHandlerGuid;

        private readonly string previousHandlerGuid;
        private bool disposed;

        private HandlerReservationQueryScope(string handlerGuid)
        {
            previousHandlerGuid = currentHandlerGuid;
            currentHandlerGuid = Normalize(handlerGuid);
        }

        public static string CurrentHandlerGuid => currentHandlerGuid ?? string.Empty;

        public static HandlerReservationQueryScope Enter(string handlerGuid)
        {
            return new HandlerReservationQueryScope(handlerGuid);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            currentHandlerGuid = previousHandlerGuid;
            disposed = true;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();
        }
    }
}
