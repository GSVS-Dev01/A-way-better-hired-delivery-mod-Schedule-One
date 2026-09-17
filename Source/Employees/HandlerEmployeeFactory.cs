using System;
using Il2CppScheduleOne.Employees;
using VehicleHandlers.Contracts;
using UnityEngine;

namespace VehicleHandlers.Employees
{
    public static class HandlerEmployeeFactory
    {
        public static HandlerEmployee ConvertDonor(Packager donor)
        {
            if (donor == null)
            {
                throw new ArgumentNullException(nameof(donor));
            }

            if (donor.EmployeeType != Il2CppScheduleOne.Employees.EEmployeeType.Handler)
            {
                throw new ArgumentException("The Handler construction donor must use EEmployeeType.Handler.", nameof(donor));
            }

            HandlerEmployee existing = donor.GetComponent<HandlerEmployee>();
            if (existing != null)
            {
                HandlerEmployeeRegistry.Register(existing, donor);
                HandlerAppearance.Apply(donor);
                return existing;
            }

            ResetDonorConfiguration(donor);
            HandlerEmployee marker = donor.gameObject.AddComponent<HandlerEmployee>();
            HandlerEmployeeRegistry.Register(marker, donor);
            HandlerAppearance.Apply(donor);
            return marker;
        }

        public static bool IsConvertedHandler(Packager donor)
        {
            return donor != null && donor.GetComponent<HandlerEmployee>() != null;
        }

        private static void ResetDonorConfiguration(Packager donor)
        {
            try
            {
                donor.Configuration?.Reset();
            }
            catch (Exception exception)
            {
                MelonLoader.MelonLogger.Warning($"Could not clear donor Packager configuration: {exception.Message}");
            }
        }
    }
}
