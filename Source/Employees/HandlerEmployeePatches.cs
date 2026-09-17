using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppScheduleOne.Employees;
using MelonLoader;
using VehicleHandlers.Contracts;

namespace VehicleHandlers.Employees
{
    [HarmonyPatch(typeof(Packager), "UpdateBehaviour", new System.Type[] { })]
    internal static class HandlerUpdateBehaviourPatch
    {
        private static bool reversePatchWarningLogged;

        private static bool Prefix(Packager __instance)
        {
            if (!HandlerEmployeeRegistry.TryGet(__instance, out HandlerEmployeeRuntime runtime))
            {
                return true;
            }

            runtime.Tick(GetCanWork(__instance));
            try
            {
                RunBaseEmployeeBehaviour(__instance);
                return false;
            }
            catch (Exception exception)
            {
                if (!reversePatchWarningLogged)
                {
                    reversePatchWarningLogged = true;
                    MelonLogger.Warning($"Handler base-behaviour bridge failed; using guarded donor fallback: {exception.Message}");
                }

                return true;
            }
        }

        private static void Postfix(Packager __instance)
        {
            if (HandlerEmployeeRegistry.TryGet(__instance, out HandlerEmployeeRuntime runtime))
            {
                runtime.Tick(GetCanWork(__instance));
            }
        }

        private static bool GetCanWork(Employee instance)
        {
            try
            {
                return RunBaseCanWork(instance);
            }
            catch
            {
                return !instance.Fired && instance.PaidForToday && instance.AssignedProperty != null && instance.GetHome() != null;
            }
        }

        [HarmonyReversePatch(HarmonyReversePatchType.Original)]
        [HarmonyPatch(typeof(Employee), "UpdateBehaviour", new Type[] { })]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RunBaseEmployeeBehaviour(Employee instance)
        {
            throw new NotSupportedException("Harmony reverse patch was not applied.");
        }

        [HarmonyReversePatch(HarmonyReversePatchType.Original)]
        [HarmonyPatch(typeof(Employee), "CanWork", new Type[] { })]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool RunBaseCanWork(Employee instance)
        {
            throw new NotSupportedException("Harmony reverse patch was not applied.");
        }
    }

    [HarmonyPatch(typeof(Packager), "IsAnyWorkInProgress", new System.Type[] { })]
    internal static class HandlerWorkProgressPatch
    {
        private static bool Prefix(Packager __instance, ref bool __result)
        {
            if (!HandlerEmployeeRegistry.TryGet(__instance, out HandlerEmployeeRuntime runtime))
            {
                return true;
            }

            __result = runtime.State == HandlerState.Moving || runtime.State == HandlerState.Completing;
            return false;
        }
    }

    [HarmonyPatch(typeof(Packager), "ShouldIdle", new System.Type[] { })]
    internal static class HandlerShouldIdlePatch
    {
        private static bool Prefix(Packager __instance, ref bool __result)
        {
            if (!HandlerEmployeeRegistry.TryGet(__instance, out HandlerEmployeeRuntime runtime))
            {
                return true;
            }

            __result = runtime.State != HandlerState.Moving && runtime.State != HandlerState.Completing;
            return false;
        }
    }

    [HarmonyPatch(typeof(Packager), "ResetConfiguration", new System.Type[] { })]
    internal static class HandlerResetConfigurationPatch
    {
        private static void Postfix(Packager __instance)
        {
            if (HandlerEmployeeRegistry.TryGet(__instance, out HandlerEmployeeRuntime runtime))
            {
                runtime.ResetConfiguration();
            }
        }
    }

    [HarmonyPatch(typeof(Packager), "UnassignProperty", new System.Type[] { })]
    internal static class HandlerUnassignPropertyPatch
    {
        private static void Prefix(Packager __instance)
        {
            if (HandlerEmployeeRegistry.TryGet(__instance, out HandlerEmployeeRuntime runtime))
            {
                runtime.ReleaseReservation();
            }
        }

        private static void Postfix(Packager __instance)
        {
            if (HandlerEmployeeRegistry.TryGet(__instance, out HandlerEmployeeRuntime runtime))
            {
                runtime.ResetConfiguration();
            }
        }
    }

    [HarmonyPatch(typeof(Packager), "Fire", new System.Type[] { })]
    internal static class HandlerFirePatch
    {
        private static void Prefix(Packager __instance)
        {
            if (HandlerEmployeeRegistry.TryGet(__instance, out HandlerEmployeeRuntime runtime))
            {
                runtime.Shutdown();
            }
        }
    }

    [HarmonyPatch(typeof(Employee), "LeavePropertyAndDespawn", new System.Type[] { })]
    internal static class HandlerLeaveAndDespawnPatch
    {
        private static void Prefix(Employee __instance)
        {
            Packager donor = (__instance as Il2CppObjectBase)?.TryCast<Packager>();
            if (donor != null && HandlerEmployeeRegistry.TryGet(donor, out HandlerEmployeeRuntime runtime))
            {
                runtime.Shutdown();
            }
        }
    }
}
