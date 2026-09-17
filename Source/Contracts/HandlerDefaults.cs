using System;
using System.Collections.Generic;

namespace VehicleHandlers.Contracts
{
    public static class HandlerDefaults
    {
        public const int PersistenceSchemaVersion = 1;
        public const int DefaultTripMinutes = 1;

        private static readonly HashSet<string> SupportedPropertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Barn",
            "Bungalow",
            "Storage Unit",
            "Dock Warehouse",
            "Mansion"
        };

        public static bool IsSupportedPropertyName(string propertyName)
        {
            return !string.IsNullOrWhiteSpace(propertyName) && SupportedPropertyNames.Contains(propertyName.Trim());
        }
    }
}
