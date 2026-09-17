using System;
using System.IO;
using System.Text.Json;
using VehicleHandlers.Contracts;

namespace VehicleHandlers.Persistence
{
    public sealed class HandlerPersistenceStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        public HandlerPersistenceStore(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("A persistence path is required.", nameof(path));
            }

            Path = path;
        }

        public string Path { get; }

        public HandlerPersistenceDocument Load()
        {
            if (!File.Exists(Path))
            {
                return new HandlerPersistenceDocument();
            }

            string json = File.ReadAllText(Path);
            HandlerPersistenceDocument document = JsonSerializer.Deserialize<HandlerPersistenceDocument>(json, JsonOptions);
            if (document == null)
            {
                throw new InvalidDataException("Vehicle Handler persistence data is empty.");
            }

            if (document.SchemaVersion != HandlerDefaults.PersistenceSchemaVersion)
            {
                throw new InvalidDataException(
                    $"Unsupported Vehicle Handler persistence schema {document.SchemaVersion}.");
            }

            document.Profiles ??= new System.Collections.Generic.List<HandlerPersistenceProfile>();
            return document;
        }

        public void Save(HandlerPersistenceDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            document.SchemaVersion = HandlerDefaults.PersistenceSchemaVersion;
            document.Profiles ??= new System.Collections.Generic.List<HandlerPersistenceProfile>();
            string directory = System.IO.Path.GetDirectoryName(Path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string temporaryPath = Path + ".tmp";
            string json = JsonSerializer.Serialize(document, JsonOptions);
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, Path, true);
        }
    }
}
