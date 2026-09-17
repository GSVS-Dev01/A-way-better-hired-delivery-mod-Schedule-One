using System;
using System.Collections.Generic;
using Il2CppScheduleOne.Delivery;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Map;
using Il2CppScheduleOne.Property;
using Il2CppScheduleOne.Vehicles;
using VehicleHandlers.Assignments;
using VehicleHandlers.Contracts;
using VehicleHandlers.Employees;
using VehicleHandlers.Reservations;

namespace VehicleHandlers.Movement
{
    public sealed class HandlerMovementCoordinator
    {
        private readonly HandlerAssignmentResolver resolver;
        private readonly HandlerReservationCoordinator reservations;
        private readonly Dictionary<string, MovementSession> sessions =
            new Dictionary<string, MovementSession>(StringComparer.Ordinal);

        public HandlerMovementCoordinator(
            HandlerAssignmentResolver resolver,
            HandlerReservationCoordinator reservations)
        {
            this.resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            this.reservations = reservations ?? throw new ArgumentNullException(nameof(reservations));
        }

        public void Tick(HandlerEmployeeRuntime runtime)
        {
            if (runtime == null || runtime.Assignment == null)
            {
                return;
            }

            if (sessions.TryGetValue(runtime.HandlerGuid, out MovementSession active))
            {
                TickActive(runtime, active);
                return;
            }

            if (!runtime.CanWorkNow || !runtime.Assignment.Enabled ||
                runtime.State == HandlerState.Hidden || runtime.State == HandlerState.Faulted ||
                runtime.State == HandlerState.Unconfigured)
            {
                return;
            }

            HandlerValidationResult structural = resolver.TryResolveForRegistration(
                runtime.Assignment,
                out ResolvedHandlerAssignment resolved);
            if (!structural.IsValid)
            {
                runtime.ApplyWorkValidation(structural);
                return;
            }

            if (IsVehicleAtDestination(resolved))
            {
                ReturnToIdle(runtime);
                return;
            }

            HandlerValidationResult validation = runtime.TryBeginWork(resolver);
            if (!validation.IsValid)
            {
                return;
            }

            int now = GetGameMinute();
            HandlerValidationResult reservation = reservations.TryReserve(runtime.Assignment, now);
            if (!reservation.IsValid)
            {
                runtime.ApplyWorkValidation(reservation);
                return;
            }

            MovementSession session = CaptureSession(resolved, now);
            try
            {
                DetachAndHide(session);
                runtime.Assignment.LastSafeVehicleState = session.Snapshot.Clone();
                runtime.Assignment.RemainingTripMinutes = HandlerDefaults.DefaultTripMinutes;
                runtime.TransitionTo(HandlerState.Moving);
                sessions.Add(runtime.HandlerGuid, session);
            }
            catch
            {
                reservations.Release(runtime.Assignment);
                RecoverVehicle(session);
                runtime.ApplyWorkValidation(HandlerValidationResult.Failure(
                    HandlerValidationCode.HandlerUnavailable,
                    "The vehicle could not be detached safely."));
            }
        }

        public void CancelAndRecover(HandlerEmployeeRuntime runtime)
        {
            if (runtime == null)
            {
                return;
            }

            if (sessions.TryGetValue(runtime.HandlerGuid, out MovementSession session))
            {
                sessions.Remove(runtime.HandlerGuid);
                RecoverVehicle(session);
            }

            if (runtime.Assignment != null)
            {
                runtime.Assignment.RemainingTripMinutes = 0;
                reservations.Release(runtime.Assignment);
            }
        }

        public void ClearAndRecover()
        {
            foreach (MovementSession session in sessions.Values)
            {
                RecoverVehicle(session);
            }

            sessions.Clear();
        }

        private void TickActive(HandlerEmployeeRuntime runtime, MovementSession session)
        {
            if (session.Vehicle == null || runtime.Assignment == null)
            {
                FailAndRecover(runtime, session, "The moving vehicle no longer exists.");
                return;
            }

            int elapsed = Math.Max(0, GetGameMinute() - session.StartGameMinute);
            int remaining = Math.Max(0, HandlerDefaults.DefaultTripMinutes - elapsed);
            runtime.Assignment.RemainingTripMinutes = remaining;
            if (remaining > 0)
            {
                return;
            }

            if (!reservations.IsAvailableToOwner(runtime.Assignment))
            {
                if (runtime.State == HandlerState.Moving)
                {
                    runtime.TransitionTo(HandlerState.WaitingForDestinationBay);
                }

                return;
            }

            HandlerValidationResult resolvedResult = resolver.TryResolveForRegistration(
                runtime.Assignment,
                out ResolvedHandlerAssignment resolved);
            if (!resolvedResult.IsValid)
            {
                FailAndRecover(runtime, session, resolvedResult.Message);
                return;
            }

            if (runtime.State == HandlerState.WaitingForDestinationBay)
            {
                runtime.TransitionTo(HandlerState.Moving);
            }

            runtime.TransitionTo(HandlerState.Completing);
            try
            {
                if (!PlaceAtDestination(session.Vehicle, resolved.DestinationDock))
                {
                    runtime.TransitionTo(HandlerState.Faulted);
                    FailAndRecover(runtime, session, "The destination dock has no free parking alignment.");
                    return;
                }

                sessions.Remove(runtime.HandlerGuid);
                runtime.Assignment.RemainingTripMinutes = 0;
                reservations.Release(runtime.Assignment);
                runtime.TransitionTo(HandlerState.Idle);
            }
            catch (Exception exception)
            {
                FailAndRecover(runtime, session, exception.Message);
            }
        }

        private void FailAndRecover(HandlerEmployeeRuntime runtime, MovementSession session, string message)
        {
            sessions.Remove(runtime.HandlerGuid);
            RecoverVehicle(session);
            if (runtime.Assignment != null)
            {
                runtime.Assignment.RemainingTripMinutes = 0;
                reservations.Release(runtime.Assignment);
            }

            runtime.ApplyWorkValidation(HandlerValidationResult.Failure(
                HandlerValidationCode.InvalidDestinationBay,
                string.IsNullOrWhiteSpace(message) ? "Vehicle movement failed and was recovered." : message));
        }

        private static MovementSession CaptureSession(ResolvedHandlerAssignment resolved, int now)
        {
            LandVehicle vehicle = resolved.Vehicle;
            ParkingLot sourceLot = vehicle.CurrentParkingLot;
            ParkingSpot sourceSpot = vehicle.CurrentParkingSpot;
            LoadingDock sourceDock = FindCurrentDock(vehicle, out string sourcePropertyCode, out int sourceDockIndex);
            int sourceSpotIndex = -1;
            if (sourceLot?.ParkingSpots != null && sourceSpot != null)
            {
                sourceSpotIndex = sourceLot.ParkingSpots.IndexOf(sourceSpot);
            }

            HandlerVehicleSnapshot snapshot = new HandlerVehicleSnapshot
            {
                PropertyCode = sourcePropertyCode,
                ParkingLotGuid = sourceLot == null ? string.Empty : sourceLot.GUID.ToString().ToUpperInvariant(),
                ParkingSpotIndex = sourceSpotIndex,
                LoadingDockIndex = sourceDockIndex,
                WasVisible = vehicle.IsVisible
            };

            return new MovementSession(vehicle, sourceDock, sourceSpot, snapshot, now);
        }

        private static void DetachAndHide(MovementSession session)
        {
            LandVehicle vehicle = session.Vehicle;
            if (vehicle.IsOccupied)
            {
                throw new InvalidOperationException("The assigned vehicle became occupied.");
            }

            if (vehicle.CurrentParkingLot != null || vehicle.CurrentParkingSpot != null)
            {
                vehicle.ExitPark(false);
            }

            if (session.SourceSpot != null && SameVehicle(session.SourceSpot.OccupantVehicle, vehicle))
            {
                session.SourceSpot.SetOccupant(null);
            }

            if (session.SourceDock != null)
            {
                if (SameVehicle(session.SourceDock.DynamicOccupant, vehicle))
                {
                    session.SourceDock.SetOccupant(null);
                }

                if (SameVehicle(session.SourceDock.StaticOccupant, vehicle))
                {
                    session.SourceDock.SetStaticOccupant(null);
                }

                session.SourceDock.RefreshOccupant();
            }

            vehicle.SetObstaclesActive(false);
            vehicle.UpdatePhysicallySimulated(false);
            vehicle.SetVisible(false);
        }

        private static bool PlaceAtDestination(LandVehicle vehicle, LoadingDock dock)
        {
            if (vehicle == null || dock == null || dock.Parking == null || vehicle.IsOccupied)
            {
                return false;
            }

            ParkingSpot spot = dock.Parking.GetRandomFreeSpot();
            if (spot == null || spot.AlignmentPoint == null)
            {
                return false;
            }

            vehicle.AlignTo(spot.AlignmentPoint, spot.Alignment, true);
            spot.SetOccupant(vehicle);
            dock.SetOccupant(vehicle);
            dock.RefreshOccupant();
            vehicle.SetVisible(true);
            vehicle.SetObstaclesActive(true);
            vehicle.UpdatePhysicallySimulated(false);
            return true;
        }

        private static void RecoverVehicle(MovementSession session)
        {
            if (session?.Vehicle == null)
            {
                return;
            }

            LandVehicle vehicle = session.Vehicle;
            bool restored = false;
            if (session.SourceSpot != null &&
                (session.SourceSpot.OccupantVehicle == null || SameVehicle(session.SourceSpot.OccupantVehicle, vehicle)) &&
                session.SourceSpot.AlignmentPoint != null)
            {
                vehicle.AlignTo(session.SourceSpot.AlignmentPoint, session.SourceSpot.Alignment, true);
                session.SourceSpot.SetOccupant(vehicle);
                restored = true;
            }

            if (session.SourceDock != null &&
                (session.SourceDock.DynamicOccupant == null || SameVehicle(session.SourceDock.DynamicOccupant, vehicle)) &&
                session.SourceDock.StaticOccupant == null)
            {
                session.SourceDock.SetOccupant(vehicle);
                session.SourceDock.RefreshOccupant();
                restored = true;
            }

            vehicle.SetVisible(session.Snapshot.WasVisible || !restored);
            vehicle.SetObstaclesActive(true);
            vehicle.UpdatePhysicallySimulated(!restored);
        }

        private static LoadingDock FindCurrentDock(
            LandVehicle vehicle,
            out string propertyCode,
            out int dockIndex)
        {
            propertyCode = string.Empty;
            dockIndex = -1;
            foreach (Property property in Property.OwnedProperties)
            {
                if (property?.LoadingDocks == null)
                {
                    continue;
                }

                for (int index = 0; index < property.LoadingDocks.Length; index++)
                {
                    LoadingDock dock = property.LoadingDocks[index];
                    if (dock != null &&
                        (SameVehicle(dock.DynamicOccupant, vehicle) || SameVehicle(dock.StaticOccupant, vehicle)))
                    {
                        propertyCode = property.PropertyCode;
                        dockIndex = index;
                        return dock;
                    }
                }
            }

            return null;
        }

        private static bool IsVehicleAtDestination(ResolvedHandlerAssignment resolved)
        {
            return SameVehicle(resolved.DestinationDock.DynamicOccupant, resolved.Vehicle) ||
                   SameVehicle(resolved.DestinationDock.StaticOccupant, resolved.Vehicle);
        }

        private static bool SameVehicle(LandVehicle left, LandVehicle right)
        {
            return left != null && right != null && left.Pointer == right.Pointer;
        }

        private static int GetGameMinute()
        {
            return TimeManager.Instance == null ? 0 : TimeManager.Instance.GetTotalMinSum();
        }

        private static void ReturnToIdle(HandlerEmployeeRuntime runtime)
        {
            if (runtime.State != HandlerState.Idle && HandlerStateMachine.CanTransition(runtime.State, HandlerState.Idle))
            {
                runtime.TransitionTo(HandlerState.Idle);
            }
        }

        private sealed class MovementSession
        {
            public MovementSession(
                LandVehicle vehicle,
                LoadingDock sourceDock,
                ParkingSpot sourceSpot,
                HandlerVehicleSnapshot snapshot,
                int startGameMinute)
            {
                Vehicle = vehicle;
                SourceDock = sourceDock;
                SourceSpot = sourceSpot;
                Snapshot = snapshot;
                StartGameMinute = startGameMinute;
            }

            public LandVehicle Vehicle { get; }
            public LoadingDock SourceDock { get; }
            public ParkingSpot SourceSpot { get; }
            public HandlerVehicleSnapshot Snapshot { get; }
            public int StartGameMinute { get; }
        }
    }
}
