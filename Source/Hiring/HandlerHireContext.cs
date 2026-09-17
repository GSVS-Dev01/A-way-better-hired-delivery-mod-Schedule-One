using System;

namespace VehicleHandlers.Hiring
{
    internal static class HandlerHireContext
    {
        public const string ChoiceLabel = "VEHICLE_HANDLER_DRIVER";
        public const string ChoiceText = "Vehicle Handler (Driver)";
        public const string VanillaDonorChoiceLabel = "Packager";
        public const string ChoiceGuid = "9cfecb47-96d7-4b66-a1c3-c62c64d6973d";

        private const string NetworkMarker = "vehiclehandlers.driver.v1|";
        private static bool pendingLocalHire;

        [ThreadStatic]
        private static int serverConversionDepth;

        public static bool IsPendingLocalHire => pendingLocalHire;

        public static bool IsServerConversionActive => serverConversionDepth > 0;

        public static void RequestLocalHire()
        {
            pendingLocalHire = true;
        }

        public static void CancelLocalHire()
        {
            pendingLocalHire = false;
        }

        public static bool TryMarkOutboundEmployeeId(ref string employeeId)
        {
            if (!pendingLocalHire)
            {
                return false;
            }

            pendingLocalHire = false;
            employeeId = NetworkMarker + (employeeId ?? string.Empty);
            return true;
        }

        public static bool TryDecodeInboundEmployeeId(ref string employeeId)
        {
            if (string.IsNullOrEmpty(employeeId) || !employeeId.StartsWith(NetworkMarker, StringComparison.Ordinal))
            {
                return false;
            }

            employeeId = employeeId.Substring(NetworkMarker.Length);
            return true;
        }

        public static void EnterServerConversion()
        {
            serverConversionDepth++;
        }

        public static void ExitServerConversion()
        {
            if (serverConversionDepth > 0)
            {
                serverConversionDepth--;
            }
        }

        public static void Clear()
        {
            pendingLocalHire = false;
            serverConversionDepth = 0;
        }

        public static bool IsVanillaRoleChoice(string choiceLabel)
        {
            return string.Equals(choiceLabel, "Botanist", StringComparison.Ordinal) ||
                   string.Equals(choiceLabel, VanillaDonorChoiceLabel, StringComparison.Ordinal) ||
                   string.Equals(choiceLabel, "Chemist", StringComparison.Ordinal) ||
                   string.Equals(choiceLabel, "Cleaner", StringComparison.Ordinal);
        }
    }
}
