using System;
using System.Collections.Generic;
using Il2CppScheduleOne.Property;
using Il2CppScheduleOne.Vehicles;
using VehicleHandlers.Contracts;
using VehicleHandlers.Runtime;

namespace VehicleHandlers.Configuration
{
    public sealed class HandlerConfigurationOption
    {
        public HandlerConfigurationOption(string id, string label)
        {
            Id = id ?? string.Empty;
            Label = label ?? string.Empty;
        }

        public string Id { get; }

        public string Label { get; }
    }

    public static class HandlerConfigurationCatalog
    {
        public static IReadOnlyList<HandlerConfigurationOption> GetVehicles(string handlerGuid, string currentVehicleGuid)
        {
            List<HandlerConfigurationOption> result = new List<HandlerConfigurationOption>();
            if (VehicleManager.Instance == null)
            {
                return result;
            }

            string current = Normalize(currentVehicleGuid);
            foreach (LandVehicle vehicle in VehicleManager.Instance.PlayerOwnedVehicles)
            {
                if (vehicle == null || !vehicle.IsPlayerOwned)
                {
                    continue;
                }

                string guid = vehicle.GUID.ToString().ToUpperInvariant();
                if (HandlerRuntimeServices.Assignments.IsVehicleAssignedToOther(guid, handlerGuid) &&
                    !StringComparer.Ordinal.Equals(guid, current))
                {
                    continue;
                }

                string name = string.IsNullOrWhiteSpace(vehicle.VehicleName) ? vehicle.VehicleCode : vehicle.VehicleName;
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = "Owned vehicle";
                }

                result.Add(new HandlerConfigurationOption(guid, $"{name}  [{ShortGuid(guid)}]"));
            }

            return result;
        }

        public static IReadOnlyList<HandlerConfigurationOption> GetProperties()
        {
            List<HandlerConfigurationOption> result = new List<HandlerConfigurationOption>();
            foreach (Property property in Property.OwnedProperties)
            {
                if (property == null || !property.IsOwned ||
                    !HandlerDefaults.IsSupportedProperty(property.PropertyName, property.PropertyCode))
                {
                    continue;
                }

                result.Add(new HandlerConfigurationOption(property.PropertyCode, property.PropertyName));
            }

            return result;
        }

        public static IReadOnlyList<HandlerConfigurationOption> GetLoadingBays(string propertyCode)
        {
            List<HandlerConfigurationOption> result = new List<HandlerConfigurationOption>();
            Property property = FindOwnedProperty(propertyCode);
            if (property == null || property.LoadingDocks == null)
            {
                return result;
            }

            for (int index = 0; index < property.LoadingDocks.Length; index++)
            {
                Il2CppScheduleOne.Delivery.LoadingDock dock = property.LoadingDocks[index];
                if (dock == null)
                {
                    continue;
                }

                string name = string.IsNullOrWhiteSpace(dock.Name) ? $"Loading bay {index + 1}" : dock.Name;
                result.Add(new HandlerConfigurationOption(index.ToString(), name));
            }

            return result;
        }

        private static Property FindOwnedProperty(string propertyCode)
        {
            string expected = Normalize(propertyCode);
            foreach (Property property in Property.OwnedProperties)
            {
                if (property != null && property.IsOwned && Normalize(property.PropertyCode) == expected)
                {
                    return property;
                }
            }

            return null;
        }

        private static string ShortGuid(string guid)
        {
            return string.IsNullOrEmpty(guid) || guid.Length <= 8 ? guid : guid.Substring(0, 8);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();
        }
    }
}
