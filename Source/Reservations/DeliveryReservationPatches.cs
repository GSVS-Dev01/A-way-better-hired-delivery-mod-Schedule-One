using System;
using HarmonyLib;
using Il2CppScheduleOne.Delivery;
using Il2CppScheduleOne.Property;
using VehicleHandlers.Contracts;
using VehicleHandlers.Runtime;

namespace VehicleHandlers.Reservations
{
    [HarmonyPatch(typeof(DeliveryManager), "IsLoadingBayFree", new[]
    {
        typeof(Property),
        typeof(int)
    })]
    internal static class DeliveryLoadingBayAvailabilityPatch
    {
        private static void Postfix(Property property, int loadingDockIndex, ref bool __result)
        {
            if (!__result || property == null)
            {
                return;
            }

            HandlerBayKey bay = new HandlerBayKey(property.PropertyCode, loadingDockIndex);
            if (!HandlerRuntimeServices.Reservations.TryGet(bay, out HandlerReservation reservation))
            {
                return;
            }

            string queryingHandler = HandlerReservationQueryScope.CurrentHandlerGuid;
            if (queryingHandler.Length == 0 ||
                !string.Equals(queryingHandler, reservation.HandlerGuid, StringComparison.Ordinal))
            {
                __result = false;
            }
        }
    }
}
