using System;
using System.Collections.Generic;
using VehicleHandlers.Contracts;

namespace VehicleHandlers.Assignments
{
    public sealed class HandlerAssignmentRegistry
    {
        private readonly object sync = new object();
        private readonly Dictionary<string, HandlerAssignment> assignmentsByHandler =
            new Dictionary<string, HandlerAssignment>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> handlerByVehicle =
            new Dictionary<string, string>(StringComparer.Ordinal);

        public HandlerValidationResult TryAssign(HandlerAssignment assignment, out HandlerAssignment registered)
        {
            registered = null;
            if (assignment == null)
            {
                return HandlerValidationResult.Failure(HandlerValidationCode.MissingHandler, "Assignment data is missing.");
            }

            HandlerAssignment candidate = assignment.Clone();
            candidate.HandlerGuid = Normalize(candidate.HandlerGuid);
            candidate.VehicleGuid = Normalize(candidate.VehicleGuid);
            candidate.DestinationPropertyCode = NormalizeCode(candidate.DestinationPropertyCode);
            candidate.DestinationPropertyGuid = Normalize(candidate.DestinationPropertyGuid);

            if (candidate.HandlerGuid.Length == 0)
            {
                return HandlerValidationResult.Failure(HandlerValidationCode.MissingHandler, "Handler identity is missing.");
            }

            if (candidate.VehicleGuid.Length == 0)
            {
                return HandlerValidationResult.Failure(HandlerValidationCode.MissingVehicle, "Vehicle identity is missing.");
            }

            if (candidate.DestinationPropertyCode.Length == 0 && candidate.DestinationPropertyGuid.Length == 0)
            {
                return HandlerValidationResult.Failure(
                    HandlerValidationCode.MissingDestinationProperty,
                    "Destination property identity is missing.");
            }

            if (candidate.DestinationDockIndex < 0)
            {
                return HandlerValidationResult.Failure(HandlerValidationCode.InvalidDestinationBay, "Destination bay index is invalid.");
            }

            lock (sync)
            {
                if (handlerByVehicle.TryGetValue(candidate.VehicleGuid, out string currentOwner) &&
                    !StringComparer.Ordinal.Equals(currentOwner, candidate.HandlerGuid))
                {
                    return HandlerValidationResult.Failure(
                        HandlerValidationCode.VehicleAlreadyAssigned,
                        "That vehicle is already assigned to another Handler.");
                }

                if (assignmentsByHandler.TryGetValue(candidate.HandlerGuid, out HandlerAssignment previous) &&
                    !StringComparer.Ordinal.Equals(previous.VehicleGuid, candidate.VehicleGuid))
                {
                    handlerByVehicle.Remove(previous.VehicleGuid);
                }

                assignmentsByHandler[candidate.HandlerGuid] = candidate;
                handlerByVehicle[candidate.VehicleGuid] = candidate.HandlerGuid;
                registered = candidate;
                return HandlerValidationResult.Success();
            }
        }

        public bool TryGetByHandler(string handlerGuid, out HandlerAssignment assignment)
        {
            lock (sync)
            {
                if (assignmentsByHandler.TryGetValue(Normalize(handlerGuid), out HandlerAssignment stored))
                {
                    assignment = stored.Clone();
                    return true;
                }
            }

            assignment = null;
            return false;
        }

        public bool TryGetByVehicle(string vehicleGuid, out HandlerAssignment assignment)
        {
            lock (sync)
            {
                if (handlerByVehicle.TryGetValue(Normalize(vehicleGuid), out string handlerGuid) &&
                    assignmentsByHandler.TryGetValue(handlerGuid, out HandlerAssignment stored))
                {
                    assignment = stored.Clone();
                    return true;
                }
            }

            assignment = null;
            return false;
        }

        public bool IsVehicleAssignedToOther(string vehicleGuid, string handlerGuid)
        {
            lock (sync)
            {
                return handlerByVehicle.TryGetValue(Normalize(vehicleGuid), out string currentOwner) &&
                       !StringComparer.Ordinal.Equals(currentOwner, Normalize(handlerGuid));
            }
        }

        public bool RemoveByHandler(string handlerGuid)
        {
            string normalizedHandler = Normalize(handlerGuid);
            lock (sync)
            {
                if (!assignmentsByHandler.TryGetValue(normalizedHandler, out HandlerAssignment assignment))
                {
                    return false;
                }

                assignmentsByHandler.Remove(normalizedHandler);
                handlerByVehicle.Remove(assignment.VehicleGuid);
                return true;
            }
        }

        public IReadOnlyCollection<HandlerAssignment> Snapshot()
        {
            lock (sync)
            {
                List<HandlerAssignment> snapshot = new List<HandlerAssignment>(assignmentsByHandler.Count);
                foreach (HandlerAssignment assignment in assignmentsByHandler.Values)
                {
                    snapshot.Add(assignment.Clone());
                }

                return snapshot.AsReadOnly();
            }
        }

        public void Clear()
        {
            lock (sync)
            {
                assignmentsByHandler.Clear();
                handlerByVehicle.Clear();
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();
        }

        private static string NormalizeCode(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
