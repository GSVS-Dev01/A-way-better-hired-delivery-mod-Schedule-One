using VehicleHandlers.Contracts;
using VehicleHandlers.Employees;

namespace VehicleHandlers.Configuration
{
    public interface IHandlerManualBayController
    {
        HandlerValidationResult TryLoadIntoAssignedBay(HandlerEmployeeRuntime runtime);

        HandlerValidationResult TryHideAndReleaseBay(HandlerEmployeeRuntime runtime);
    }

    public sealed class UnavailableHandlerManualBayController : IHandlerManualBayController
    {
        public HandlerValidationResult TryLoadIntoAssignedBay(HandlerEmployeeRuntime runtime)
        {
            return Unavailable();
        }

        public HandlerValidationResult TryHideAndReleaseBay(HandlerEmployeeRuntime runtime)
        {
            return Unavailable();
        }

        private static HandlerValidationResult Unavailable()
        {
            return HandlerValidationResult.Failure(
                HandlerValidationCode.HandlerUnavailable,
                "Vehicle movement controls are not available until the movement service is ready.");
        }
    }
}
