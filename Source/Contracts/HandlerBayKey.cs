using System;

namespace VehicleHandlers.Contracts
{
    public readonly struct HandlerBayKey : IEquatable<HandlerBayKey>
    {
        public HandlerBayKey(string propertyKey, int dockIndex)
        {
            PropertyKey = Normalize(propertyKey);
            DockIndex = dockIndex;
        }

        public string PropertyKey { get; }

        public int DockIndex { get; }

        public bool IsValid => !string.IsNullOrEmpty(PropertyKey) && DockIndex >= 0;

        public bool Equals(HandlerBayKey other)
        {
            return DockIndex == other.DockIndex &&
                   StringComparer.Ordinal.Equals(PropertyKey, other.PropertyKey);
        }

        public override bool Equals(object obj)
        {
            return obj is HandlerBayKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (StringComparer.Ordinal.GetHashCode(PropertyKey) * 397) ^ DockIndex;
            }
        }

        public override string ToString()
        {
            return $"{PropertyKey}:{DockIndex}";
        }

        public static bool operator ==(HandlerBayKey left, HandlerBayKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(HandlerBayKey left, HandlerBayKey right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();
        }
    }
}
