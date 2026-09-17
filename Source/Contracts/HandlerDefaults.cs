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
            "Docks Warehouse",
            "Mansion"
        };

        public static bool IsSupportedPropertyName(string propertyName)
        {
            return !string.IsNullOrWhiteSpace(propertyName) && SupportedPropertyNames.Contains(propertyName.Trim());
        }

        public static bool IsSupportedProperty(string propertyName, string propertyCode)
        {
            if (IsSupportedPropertyName(propertyName))
            {
                return true;
            }

            string key = NormalizePropertyKey(propertyCode);
            return key == "barn" ||
                   key == "bungalow" ||
                   key == "storageunit" ||
                   key == "dockwarehouse" ||
                   key == "dockswarehouse" ||
                   key == "mansion";
        }

        private static string NormalizePropertyKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            char[] buffer = new char[value.Length];
            int length = 0;
            foreach (char character in value)
            {
                if (char.IsLetterOrDigit(character))
                {
                    buffer[length++] = char.ToLowerInvariant(character);
                }
            }

            return new string(buffer, 0, length);
        }
    }
}
