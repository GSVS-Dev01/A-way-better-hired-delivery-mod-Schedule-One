using System;
using System.Collections.Generic;

namespace VehicleHandlers.Contracts
{
    [Serializable]
    public sealed class HandlerAssignment
    {
        public string HandlerGuid { get; set; } = string.Empty;

        public string VehicleGuid { get; set; } = string.Empty;

        public string DestinationPropertyCode { get; set; } = string.Empty;

        public string DestinationPropertyGuid { get; set; } = string.Empty;

        public int DestinationDockIndex { get; set; } = -1;

        public bool Enabled { get; set; }

        public bool ManualHidden { get; set; }

        public HandlerState State { get; set; } = HandlerState.Unconfigured;

        public int RemainingTripMinutes { get; set; }

        public HandlerVehicleSnapshot LastSafeVehicleState { get; set; }

        public HandlerBayKey GetDestinationKey()
        {
            string propertyKey = string.IsNullOrWhiteSpace(DestinationPropertyGuid)
                ? DestinationPropertyCode
                : DestinationPropertyGuid;
            return new HandlerBayKey(propertyKey, DestinationDockIndex);
        }

        public HandlerAssignment Clone()
        {
            return new HandlerAssignment
            {
                HandlerGuid = HandlerGuid,
                VehicleGuid = VehicleGuid,
                DestinationPropertyCode = DestinationPropertyCode,
                DestinationPropertyGuid = DestinationPropertyGuid,
                DestinationDockIndex = DestinationDockIndex,
                Enabled = Enabled,
                ManualHidden = ManualHidden,
                State = State,
                RemainingTripMinutes = RemainingTripMinutes,
                LastSafeVehicleState = LastSafeVehicleState?.Clone()
            };
        }
    }

    [Serializable]
    public sealed class HandlerVehicleSnapshot
    {
        public string PropertyCode { get; set; } = string.Empty;

        public string ParkingLotGuid { get; set; } = string.Empty;

        public int ParkingSpotIndex { get; set; } = -1;

        public int LoadingDockIndex { get; set; } = -1;

        public bool WasVisible { get; set; } = true;

        public HandlerVehicleSnapshot Clone()
        {
            return new HandlerVehicleSnapshot
            {
                PropertyCode = PropertyCode,
                ParkingLotGuid = ParkingLotGuid,
                ParkingSpotIndex = ParkingSpotIndex,
                LoadingDockIndex = LoadingDockIndex,
                WasVisible = WasVisible
            };
        }
    }

    [Serializable]
    public sealed class HandlerAssignmentDocument
    {
        public int SchemaVersion { get; set; } = HandlerDefaults.PersistenceSchemaVersion;

        public List<HandlerAssignment> Assignments { get; set; } = new List<HandlerAssignment>();
    }
}
