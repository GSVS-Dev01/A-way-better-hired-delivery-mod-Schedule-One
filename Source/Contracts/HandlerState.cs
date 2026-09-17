using System;

namespace VehicleHandlers.Contracts
{
    public enum HandlerState
    {
        Unconfigured = 0,
        Idle = 1,
        WaitingForVehicle = 2,
        WaitingForDestinationBay = 3,
        Moving = 4,
        Completing = 5,
        Hidden = 6,
        Faulted = 7
    }

    public sealed class HandlerStateChangedEventArgs : EventArgs
    {
        public HandlerStateChangedEventArgs(string handlerGuid, HandlerState previous, HandlerState current)
        {
            HandlerGuid = handlerGuid ?? string.Empty;
            Previous = previous;
            Current = current;
        }

        public string HandlerGuid { get; }

        public HandlerState Previous { get; }

        public HandlerState Current { get; }
    }

    public interface IHandlerStatePublisher
    {
        event EventHandler<HandlerStateChangedEventArgs> StateChanged;
    }

    public static class HandlerStateMachine
    {
        public static bool CanTransition(HandlerState current, HandlerState next)
        {
            if (current == next)
            {
                return true;
            }

            switch (current)
            {
                case HandlerState.Unconfigured:
                    return next == HandlerState.Idle || next == HandlerState.Faulted;
                case HandlerState.Idle:
                    return next == HandlerState.Unconfigured ||
                           next == HandlerState.WaitingForVehicle ||
                           next == HandlerState.WaitingForDestinationBay ||
                           next == HandlerState.Hidden ||
                           next == HandlerState.Faulted;
                case HandlerState.WaitingForVehicle:
                    return next == HandlerState.Idle ||
                           next == HandlerState.WaitingForDestinationBay ||
                           next == HandlerState.Faulted;
                case HandlerState.WaitingForDestinationBay:
                    return next == HandlerState.Idle ||
                           next == HandlerState.Moving ||
                           next == HandlerState.Hidden ||
                           next == HandlerState.Faulted;
                case HandlerState.Moving:
                    return next == HandlerState.WaitingForDestinationBay ||
                           next == HandlerState.Completing ||
                           next == HandlerState.Faulted;
                case HandlerState.Completing:
                    return next == HandlerState.Idle ||
                           next == HandlerState.Hidden ||
                           next == HandlerState.Faulted;
                case HandlerState.Hidden:
                    return next == HandlerState.Idle ||
                           next == HandlerState.WaitingForDestinationBay ||
                           next == HandlerState.Faulted;
                case HandlerState.Faulted:
                    return next == HandlerState.Unconfigured ||
                           next == HandlerState.Idle ||
                           next == HandlerState.Hidden;
                default:
                    return false;
            }
        }

        public static void EnsureTransition(HandlerState current, HandlerState next)
        {
            if (!CanTransition(current, next))
            {
                throw new InvalidOperationException($"Invalid Handler state transition: {current} -> {next}.");
            }
        }
    }
}
