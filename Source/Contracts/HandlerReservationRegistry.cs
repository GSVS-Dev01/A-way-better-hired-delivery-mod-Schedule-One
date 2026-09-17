using System;
using System.Collections.Generic;

namespace VehicleHandlers.Contracts
{
    public sealed class HandlerReservation
    {
        public HandlerReservation(HandlerBayKey bay, string handlerGuid, string vehicleGuid, int acquiredAtGameMinute)
        {
            Bay = bay;
            HandlerGuid = NormalizeGuid(handlerGuid);
            VehicleGuid = NormalizeGuid(vehicleGuid);
            AcquiredAtGameMinute = acquiredAtGameMinute;
        }

        public HandlerBayKey Bay { get; }

        public string HandlerGuid { get; }

        public string VehicleGuid { get; }

        public int AcquiredAtGameMinute { get; }

        private static string NormalizeGuid(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();
        }
    }

    public sealed class HandlerReservationRegistry
    {
        private readonly object sync = new object();
        private readonly Dictionary<HandlerBayKey, HandlerReservation> reservationsByBay =
            new Dictionary<HandlerBayKey, HandlerReservation>();
        private readonly Dictionary<string, HandlerBayKey> bayByHandler =
            new Dictionary<string, HandlerBayKey>(StringComparer.Ordinal);

        public bool TryReserve(HandlerReservation reservation, out HandlerReservation conflict)
        {
            if (reservation == null)
            {
                throw new ArgumentNullException(nameof(reservation));
            }

            if (!reservation.Bay.IsValid || reservation.HandlerGuid.Length == 0 || reservation.VehicleGuid.Length == 0)
            {
                throw new ArgumentException("A reservation requires a valid bay, Handler GUID, and vehicle GUID.", nameof(reservation));
            }

            lock (sync)
            {
                if (reservationsByBay.TryGetValue(reservation.Bay, out conflict))
                {
                    return StringComparer.Ordinal.Equals(conflict.HandlerGuid, reservation.HandlerGuid) &&
                           StringComparer.Ordinal.Equals(conflict.VehicleGuid, reservation.VehicleGuid);
                }

                if (bayByHandler.TryGetValue(reservation.HandlerGuid, out HandlerBayKey existingBay))
                {
                    conflict = reservationsByBay[existingBay];
                    return false;
                }

                reservationsByBay.Add(reservation.Bay, reservation);
                bayByHandler.Add(reservation.HandlerGuid, reservation.Bay);
                conflict = null;
                return true;
            }
        }

        public bool IsReserved(HandlerBayKey bay)
        {
            lock (sync)
            {
                return reservationsByBay.ContainsKey(bay);
            }
        }

        public bool IsReservedByOther(HandlerBayKey bay, string handlerGuid)
        {
            string normalizedHandlerGuid = NormalizeGuid(handlerGuid);
            lock (sync)
            {
                return reservationsByBay.TryGetValue(bay, out HandlerReservation reservation) &&
                       !StringComparer.Ordinal.Equals(reservation.HandlerGuid, normalizedHandlerGuid);
            }
        }

        public bool Release(HandlerBayKey bay, string handlerGuid)
        {
            string normalizedHandlerGuid = NormalizeGuid(handlerGuid);
            lock (sync)
            {
                if (!reservationsByBay.TryGetValue(bay, out HandlerReservation reservation) ||
                    !StringComparer.Ordinal.Equals(reservation.HandlerGuid, normalizedHandlerGuid))
                {
                    return false;
                }

                reservationsByBay.Remove(bay);
                bayByHandler.Remove(normalizedHandlerGuid);
                return true;
            }
        }

        public bool ReleaseByHandler(string handlerGuid)
        {
            string normalizedHandlerGuid = NormalizeGuid(handlerGuid);
            lock (sync)
            {
                if (!bayByHandler.TryGetValue(normalizedHandlerGuid, out HandlerBayKey bay))
                {
                    return false;
                }

                bayByHandler.Remove(normalizedHandlerGuid);
                reservationsByBay.Remove(bay);
                return true;
            }
        }

        public IReadOnlyCollection<HandlerReservation> Snapshot()
        {
            lock (sync)
            {
                return new List<HandlerReservation>(reservationsByBay.Values).AsReadOnly();
            }
        }

        public void Clear()
        {
            lock (sync)
            {
                reservationsByBay.Clear();
                bayByHandler.Clear();
            }
        }

        private static string NormalizeGuid(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();
        }
    }
}
