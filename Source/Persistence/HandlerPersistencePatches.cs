using HarmonyLib;
using Il2CppScheduleOne.Persistence;

namespace VehicleHandlers.Persistence
{
    [HarmonyPatch(typeof(LoadManager), "StartGame", new[]
    {
        typeof(SaveInfo),
        typeof(bool),
        typeof(bool)
    })]
    internal static class HandlerLoadStartPatch
    {
        private static void Prefix(SaveInfo saveInfo)
        {
            HandlerAssignmentPersistence.BeginLoad(saveInfo);
        }
    }

    [HarmonyPatch(typeof(LoadManager), "Update", new System.Type[] { })]
    internal static class HandlerLoadRebindPatch
    {
        private static void Postfix(LoadManager __instance)
        {
            HandlerAssignmentPersistence.TickRebind(__instance);
        }
    }

    [HarmonyPatch(typeof(LoadManager), "CleanUp", new System.Type[] { })]
    internal static class HandlerLoadCleanupPatch
    {
        private static void Prefix()
        {
            HandlerAssignmentPersistence.Reset();
        }
    }

    [HarmonyPatch(typeof(SaveManager), "Save", new System.Type[] { })]
    internal static class HandlerSavePatch
    {
        private static void Postfix()
        {
            HandlerAssignmentPersistence.SaveCurrent();
        }
    }

    [HarmonyPatch(typeof(SaveManager), "Save", new[] { typeof(string) })]
    internal static class HandlerNamedSavePatch
    {
        private static void Postfix(string saveName)
        {
            HandlerAssignmentPersistence.SaveCurrent(saveName);
        }
    }
}
