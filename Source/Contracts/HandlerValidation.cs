namespace VehicleHandlers.Contracts
{
    public enum HandlerValidationCode
    {
        Valid = 0,
        MissingHandler = 1,
        MissingVehicle = 2,
        VehicleNotOwned = 3,
        VehicleOccupied = 4,
        VehicleAlreadyAssigned = 5,
        MissingDestinationProperty = 6,
        DestinationPropertyNotOwned = 7,
        UnsupportedDestinationProperty = 8,
        InvalidDestinationBay = 9,
        DestinationBayOccupied = 10,
        DestinationBayReserved = 11,
        HandlerUnavailable = 12,
        AssignmentDisabled = 13,
        MovementInProgress = 14
    }

    public readonly struct HandlerValidationResult
    {
        private HandlerValidationResult(HandlerValidationCode code, string message)
        {
            Code = code;
            Message = message ?? string.Empty;
        }

        public HandlerValidationCode Code { get; }

        public string Message { get; }

        public bool IsValid => Code == HandlerValidationCode.Valid;

        public static HandlerValidationResult Success()
        {
            return new HandlerValidationResult(HandlerValidationCode.Valid, string.Empty);
        }

        public static HandlerValidationResult Failure(HandlerValidationCode code, string message)
        {
            return new HandlerValidationResult(code, message);
        }
    }

    public interface IHandlerAssignmentValidator
    {
        HandlerValidationResult Validate(HandlerAssignment assignment);
    }
}
