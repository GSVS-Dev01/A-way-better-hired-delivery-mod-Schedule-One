using System.Collections.Generic;
using VehicleHandlers.Contracts;

namespace VehicleHandlers.Persistence
{
    public sealed class HandlerPersistenceDocument
    {
        public int SchemaVersion { get; set; } = HandlerDefaults.PersistenceSchemaVersion;

        public List<HandlerPersistenceProfile> Profiles { get; set; } = new List<HandlerPersistenceProfile>();
    }

    public sealed class HandlerPersistenceProfile
    {
        public string SaveKey { get; set; } = string.Empty;

        public List<HandlerAssignment> Assignments { get; set; } = new List<HandlerAssignment>();
    }
}
