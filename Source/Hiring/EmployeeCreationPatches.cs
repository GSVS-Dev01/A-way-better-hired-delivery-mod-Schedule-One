using System;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppScheduleOne.Employees;
using Il2CppScheduleOne.Property;
using MelonLoader;
using UnityEngine;
using VehicleHandlers.Employees;

namespace VehicleHandlers.Hiring
{
    [HarmonyPatch(typeof(EmployeeManager), "CreateEmployee", new[]
    {
        typeof(Property),
        typeof(EEmployeeType),
        typeof(string),
        typeof(string),
        typeof(string),
        typeof(bool),
        typeof(int),
        typeof(Vector3),
        typeof(Quaternion),
        typeof(string)
    })]
    internal static class HandlerOutboundEmployeeCreationPatch
    {
        private static void Prefix(EEmployeeType type, ref string id)
        {
            if (type == EEmployeeType.Handler)
            {
                HandlerHireContext.TryMarkOutboundEmployeeId(ref id);
            }
        }
    }

    [HarmonyPatch(typeof(EmployeeManager), "RpcLogic___CreateEmployee_311954683", new[]
    {
        typeof(Property),
        typeof(EEmployeeType),
        typeof(string),
        typeof(string),
        typeof(string),
        typeof(bool),
        typeof(int),
        typeof(Vector3),
        typeof(Quaternion),
        typeof(string)
    })]
    internal static class HandlerInboundEmployeeCreationPatch
    {
        private static void Prefix(EEmployeeType type, ref string id, out bool __state)
        {
            __state = type == EEmployeeType.Handler && HandlerHireContext.TryDecodeInboundEmployeeId(ref id);
            if (__state)
            {
                HandlerHireContext.EnterServerConversion();
            }
        }

        private static void Finalizer(bool __state)
        {
            if (__state)
            {
                HandlerHireContext.ExitServerConversion();
            }
        }
    }

    [HarmonyPatch(typeof(EmployeeManager), "CreateEmployee_Server", new[]
    {
        typeof(Property),
        typeof(EEmployeeType),
        typeof(string),
        typeof(string),
        typeof(string),
        typeof(bool),
        typeof(int),
        typeof(Vector3),
        typeof(Quaternion),
        typeof(string)
    })]
    internal static class HandlerServerEmployeeConversionPatch
    {
        private static void Postfix(EEmployeeType type, Employee __result)
        {
            if (!HandlerHireContext.IsServerConversionActive || type != EEmployeeType.Handler || __result == null)
            {
                return;
            }

            Packager donor = (__result as Il2CppObjectBase)?.TryCast<Packager>();
            if (donor == null)
            {
                MelonLogger.Error("The marked Handler hire did not produce a Packager donor; conversion was refused.");
                return;
            }

            HandlerEmployeeFactory.ConvertDonor(donor);
            MelonLogger.Msg($"Converted hired employee {donor.GUID} into a Vehicle Handler.");
        }
    }
}
