using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
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

            runtime.Tick();
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
                runtime.Tick();
            }
        }

        [HarmonyReversePatch(HarmonyReversePatchType.Original)]
        [HarmonyPatch(typeof(Employee), "UpdateBehaviour", new Type[] { })]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RunBaseEmployeeBehaviour(Employee instance)
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
        private static void Postfix(Packager __instance)
        {
            if (HandlerEmployeeRegistry.TryGet(__instance, out HandlerEmployeeRuntime runtime))
            {
                runtime.ResetConfiguration();
            }
        }
    }
}
