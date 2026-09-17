namespace VehicleHandlers.Contracts
{
    public sealed class HandlerConfiguration
    {
        public string VehicleGuid { get; set; } = string.Empty;

        public string DestinationPropertyCode { get; set; } = string.Empty;

        public string DestinationPropertyGuid { get; set; } = string.Empty;

        public int DestinationDockIndex { get; set; } = -1;

        public bool Enabled { get; set; }

        public bool ManualHidden { get; set; }

        public HandlerAssignment CreateAssignment(string handlerGuid)
        {
            return new HandlerAssignment
            {
                HandlerGuid = handlerGuid ?? string.Empty,
                VehicleGuid = VehicleGuid ?? string.Empty,
                DestinationPropertyCode = DestinationPropertyCode ?? string.Empty,
                DestinationPropertyGuid = DestinationPropertyGuid ?? string.Empty,
                DestinationDockIndex = DestinationDockIndex,
                Enabled = Enabled,
                ManualHidden = ManualHidden,
                State = HandlerState.Unconfigured,
                RemainingTripMinutes = 0
            };
        }
    }
}
