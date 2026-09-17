using System;
using System.Collections.Generic;
using Il2CppScheduleOne.Employees;
using VehicleHandlers.Contracts;
using VehicleHandlers.Runtime;

namespace VehicleHandlers.Employees
{
    public sealed class HandlerEmployeeRuntime : IHandlerStatePublisher
    {
        internal HandlerEmployeeRuntime(HandlerEmployee marker, Packager employee)
        {
            Marker = marker ?? throw new ArgumentNullException(nameof(marker));
            Employee = employee ?? throw new ArgumentNullException(nameof(employee));
        }

        public event EventHandler<HandlerStateChangedEventArgs> StateChanged;

        public HandlerEmployee Marker { get; }

        public Packager Employee { get; }

        public HandlerAssignment Assignment { get; private set; }

        public HandlerConfiguration Configuration { get; private set; } = new HandlerConfiguration();

        public HandlerState State { get; private set; } = HandlerState.Unconfigured;

        public bool CanWorkNow { get; private set; }

        public string HandlerGuid => Employee.GUID.ToString().ToUpperInvariant();

        public void ApplyConfiguration(HandlerConfiguration configuration)
        {
            HandlerValidationResult result = TryApplyConfiguration(configuration);
            if (!result.IsValid)
            {
                throw new InvalidOperationException(result.Message);
            }
        }

        public HandlerValidationResult TryApplyConfiguration(HandlerConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (State == HandlerState.Moving || State == HandlerState.Completing)
            {
                return HandlerValidationResult.Failure(
                    HandlerValidationCode.MovementInProgress,
                    "The Handler assignment cannot be changed while its vehicle is moving.");
            }

            HandlerAssignment candidate = configuration.CreateAssignment(HandlerGuid);
            HandlerValidationResult validation = HandlerRuntimeServices.AssignmentResolver.ValidateForRegistration(candidate);
            if (!validation.IsValid)
            {
                return validation;
            }

            HandlerValidationResult registration = HandlerRuntimeServices.Assignments.TryAssign(candidate, out HandlerAssignment registered);
            if (!registration.IsValid)
            {
                return registration;
            }

            Configuration = configuration;
            Assignment = registered;
            TransitionTo(configuration.Enabled ? HandlerState.Idle : HandlerState.Unconfigured);
            return HandlerValidationResult.Success();
        }

        public HandlerValidationResult TryRestoreAssignment(HandlerAssignment savedAssignment)
        {
            if (savedAssignment == null)
            {
                return HandlerValidationResult.Failure(HandlerValidationCode.MissingHandler, "Saved assignment data is missing.");
            }

            HandlerConfiguration restoredConfiguration = new HandlerConfiguration
            {
                VehicleGuid = savedAssignment.VehicleGuid,
                DestinationPropertyCode = savedAssignment.DestinationPropertyCode,
                DestinationPropertyGuid = savedAssignment.DestinationPropertyGuid,
                DestinationDockIndex = savedAssignment.DestinationDockIndex,
                Enabled = savedAssignment.Enabled,
                ManualHidden = savedAssignment.ManualHidden
            };

            HandlerValidationResult result = TryApplyConfiguration(restoredConfiguration);
            if (!result.IsValid)
            {
                return result;
            }

            Assignment.LastSafeVehicleState = savedAssignment.LastSafeVehicleState?.Clone();
            Assignment.RemainingTripMinutes = 0;
            if (savedAssignment.ManualHidden && Assignment.Enabled && State == HandlerState.Idle)
            {
                TransitionTo(HandlerState.Hidden);
            }

            return HandlerValidationResult.Success();
        }

        public void ResetConfiguration()
        {
            HandlerRuntimeServices.MovementCoordinator.CancelAndRecover(this);
            ReleaseReservation();
            HandlerRuntimeServices.Assignments.RemoveByHandler(HandlerGuid);
            if (State != HandlerState.Unconfigured && State != HandlerState.Idle && State != HandlerState.Faulted)
            {
                TransitionTo(HandlerState.Faulted);
            }

            Assignment = null;
            Configuration = new HandlerConfiguration();
            TransitionTo(HandlerState.Unconfigured);
        }

        public HandlerValidationResult TryBeginWork(IHandlerAssignmentValidator validator)
        {
            if (Employee.Fired || !CanWorkNow)
            {
                return HandlerValidationResult.Failure(
                    HandlerValidationCode.HandlerUnavailable,
                    "The Handler is off duty, unpaid, fired, or otherwise unavailable.");
            }

            if (Assignment == null)
            {
                return HandlerValidationResult.Failure(
                    HandlerValidationCode.MissingHandler,
                    "The Handler has no vehicle delivery assignment.");
            }

            if (!Assignment.Enabled)
            {
                return HandlerValidationResult.Failure(
                    HandlerValidationCode.AssignmentDisabled,
                    "The Handler assignment is disabled.");
            }

            HandlerValidationResult validation = validator == null
                ? HandlerValidationResult.Success()
                : validator.Validate(Assignment);
            ApplyValidationState(validation);
            return validation;
        }

        public void TransitionTo(HandlerState next)
        {
            HandlerState previous = State;
            HandlerStateMachine.EnsureTransition(previous, next);
            State = next;
            if (Assignment != null)
            {
                Assignment.State = next;
            }

            if (previous != next)
            {
                StateChanged?.Invoke(this, new HandlerStateChangedEventArgs(HandlerGuid, previous, next));
            }
        }

        public void Tick(bool canWorkNow)
        {
            CanWorkNow = canWorkNow && !Employee.Fired;
            HandlerEmployeeRegistry.SuppressDonorRole(Employee);

            if (Employee.Fired)
            {
                Shutdown();
                return;
            }

            HandlerRuntimeServices.MovementCoordinator.Tick(this);

            if (!CanWorkNow && (State == HandlerState.WaitingForVehicle || State == HandlerState.WaitingForDestinationBay))
            {
                TransitionTo(HandlerState.Idle);
            }
        }

        public void ReleaseReservation()
        {
            HandlerRuntimeServices.Reservations.ReleaseByHandler(HandlerGuid);
        }

        public void Shutdown()
        {
            CanWorkNow = false;
            HandlerRuntimeServices.MovementCoordinator.CancelAndRecover(this);
            ReleaseReservation();
            HandlerRuntimeServices.Assignments.RemoveByHandler(HandlerGuid);
            if (State != HandlerState.Faulted && State != HandlerState.Unconfigured)
            {
                if (HandlerStateMachine.CanTransition(State, HandlerState.Faulted))
                {
                    TransitionTo(HandlerState.Faulted);
                }
            }
        }

        private void ApplyValidationState(HandlerValidationResult validation)
        {
            if (validation.IsValid)
            {
                TransitionTo(HandlerState.WaitingForDestinationBay);
                return;
            }

            switch (validation.Code)
            {
                case HandlerValidationCode.MissingVehicle:
                case HandlerValidationCode.VehicleNotOwned:
                case HandlerValidationCode.VehicleOccupied:
                case HandlerValidationCode.VehicleAlreadyAssigned:
                    TransitionTo(HandlerState.WaitingForVehicle);
                    break;
                case HandlerValidationCode.DestinationBayOccupied:
                case HandlerValidationCode.DestinationBayReserved:
                    TransitionTo(HandlerState.WaitingForDestinationBay);
                    break;
                case HandlerValidationCode.HandlerUnavailable:
                case HandlerValidationCode.AssignmentDisabled:
                    TransitionTo(HandlerState.Idle);
                    break;
                default:
                    ReleaseReservation();
                    TransitionTo(HandlerState.Faulted);
                    break;
            }
        }

        public void ApplyWorkValidation(HandlerValidationResult validation)
        {
            ApplyValidationState(validation);
        }
    }

    public static class HandlerEmployeeRegistry
    {
        private static readonly object Sync = new object();
        private static readonly Dictionary<IntPtr, HandlerEmployeeRuntime> RuntimeByEmployee =
            new Dictionary<IntPtr, HandlerEmployeeRuntime>();
        private static readonly Dictionary<IntPtr, IntPtr> EmployeeByMarker =
            new Dictionary<IntPtr, IntPtr>();

        public static HandlerEmployeeRuntime Register(HandlerEmployee marker, Packager donor)
        {
            if (marker == null)
            {
                throw new ArgumentNullException(nameof(marker));
            }

            if (donor == null)
            {
                throw new ArgumentNullException(nameof(donor));
            }

            IntPtr employeePointer = donor.Pointer;
            IntPtr markerPointer = marker.Pointer;
            lock (Sync)
            {
                if (!RuntimeByEmployee.TryGetValue(employeePointer, out HandlerEmployeeRuntime runtime))
                {
                    runtime = new HandlerEmployeeRuntime(marker, donor);
                    RuntimeByEmployee.Add(employeePointer, runtime);
                }

                EmployeeByMarker[markerPointer] = employeePointer;
                SuppressDonorRole(donor);
                return runtime;
            }
        }

        public static bool TryGet(Packager donor, out HandlerEmployeeRuntime runtime)
        {
            if (donor == null)
            {
                runtime = null;
                return false;
            }

            lock (Sync)
            {
                return RuntimeByEmployee.TryGetValue(donor.Pointer, out runtime);
            }
        }

        public static bool TryGet(HandlerEmployee marker, out HandlerEmployeeRuntime runtime)
        {
            runtime = null;
            if (marker == null)
            {
                return false;
            }

            lock (Sync)
            {
                return EmployeeByMarker.TryGetValue(marker.Pointer, out IntPtr employeePointer) &&
                       RuntimeByEmployee.TryGetValue(employeePointer, out runtime);
            }
        }

        public static void Detach(HandlerEmployee marker)
        {
            if (marker == null)
            {
                return;
            }

            lock (Sync)
            {
                if (!EmployeeByMarker.TryGetValue(marker.Pointer, out IntPtr employeePointer))
                {
                    return;
                }

                EmployeeByMarker.Remove(marker.Pointer);
                if (RuntimeByEmployee.TryGetValue(employeePointer, out HandlerEmployeeRuntime runtime))
                {
                    runtime.Shutdown();
                    RuntimeByEmployee.Remove(employeePointer);
                }
            }
        }

        public static void Clear()
        {
            lock (Sync)
            {
                foreach (HandlerEmployeeRuntime runtime in RuntimeByEmployee.Values)
                {
                    runtime.Shutdown();
                }

                EmployeeByMarker.Clear();
                RuntimeByEmployee.Clear();
            }
        }

        internal static void SuppressDonorRole(Packager donor)
        {
            Disable(donor?.PackagingBehaviour);
            Disable(donor?.BrickPressBehaviour);
            Disable(donor?.MoveItemBehaviour);
        }

        private static void Disable(Il2CppScheduleOne.NPCs.Behaviour.Behaviour behaviour)
        {
            if (behaviour == null)
            {
                return;
            }

            behaviour.EnabledOnAwake = false;
            if (behaviour.Enabled)
            {
                behaviour.Disable();
            }

            behaviour.enabled = false;
        }
    }
}
