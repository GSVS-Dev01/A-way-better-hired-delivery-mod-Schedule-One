using MelonLoader;
using VehicleHandlers.Employees;
using VehicleHandlers.Hiring;
using VehicleHandlers.Persistence;
using VehicleHandlers.Runtime;

namespace VehicleHandlers
{
    public sealed class VehicleHandlersMod : MelonMod
    {
        public const string ModName = "Vehicle Handlers";
        public const string Version = "0.1.0";
        public const string ModDescription = "Adds a hireable Handler who moves owned vehicles while respecting loading-bay reservations.";

        public override void OnInitializeMelon()
        {
            HandlerEmployee.RegisterIl2CppType();
            MelonLogger.Msg($"{ModName} {Version} initialized. Handler runtime type registered.");
        }

        public override void OnDeinitializeMelon()
        {
            HandlerHireContext.Clear();
            HandlerAssignmentPersistence.Reset();
            HandlerEmployeeRegistry.Clear();
            HandlerRuntimeServices.Clear();
        }
    }
}

