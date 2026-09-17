using System;
using System.Collections.Generic;
using Il2CppScheduleOne.Employees;
using VehicleHandlers.Contracts;

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

        public string HandlerGuid => Employee.GUID.ToString().ToUpperInvariant();

        public void ApplyConfiguration(HandlerConfiguration configuration)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            Assignment = configuration.CreateAssignment(HandlerGuid);
            TransitionTo(configuration.Enabled ? HandlerState.Idle : HandlerState.Unconfigured);
        }

        public void ResetConfiguration()
        {
            if (State != HandlerState.Unconfigured && State != HandlerState.Idle && State != HandlerState.Faulted)
            {
                TransitionTo(HandlerState.Faulted);
            }

            Assignment = null;
            Configuration = new HandlerConfiguration();
            TransitionTo(HandlerState.Unconfigured);
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

        public void Tick()
        {
            HandlerEmployeeRegistry.SuppressDonorRole(Employee);
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
                RuntimeByEmployee.Remove(employeePointer);
            }
        }

        public static void Clear()
        {
            lock (Sync)
            {
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
