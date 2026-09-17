using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppScheduleOne.Employees;
using Il2CppScheduleOne.Persistence;
using MelonLoader;
using MelonLoader.Utils;
using VehicleHandlers.Contracts;
using VehicleHandlers.Employees;
using VehicleHandlers.Runtime;

namespace VehicleHandlers.Persistence
{
    public static class HandlerAssignmentPersistence
    {
        public const string FileName = "VehicleHandlers.json";
        private const int MaximumRebindFrames = 600;

        private static readonly object Sync = new object();
        private static readonly List<HandlerAssignment> PendingAssignments = new List<HandlerAssignment>();
        private static readonly HandlerPersistenceStore Store = new HandlerPersistenceStore(
            System.IO.Path.Combine(MelonEnvironment.UserDataDirectory, FileName));

        private static string currentSaveKey = string.Empty;
        private static int rebindFrames;

        public static void BeginLoad(SaveInfo saveInfo)
        {
            HandlerEmployeeRegistry.Clear();
            HandlerRuntimeServices.Assignments.Clear();
            HandlerRuntimeServices.Reservations.Clear();

            lock (Sync)
            {
                currentSaveKey = GetSaveKey(saveInfo, null);
                PendingAssignments.Clear();
                rebindFrames = 0;

                HandlerPersistenceDocument document = LoadDocumentSafely();
                HandlerPersistenceProfile profile = FindProfile(document, currentSaveKey);
                if (profile?.Assignments == null)
                {
                    return;
                }

                foreach (HandlerAssignment assignment in profile.Assignments)
                {
                    if (assignment == null || string.IsNullOrWhiteSpace(assignment.HandlerGuid))
                    {
                        continue;
                    }

                    HandlerAssignment pending = assignment.Clone();
                    pending.State = pending.ManualHidden ? HandlerState.Hidden : HandlerState.Idle;
                    pending.RemainingTripMinutes = 0;
                    PendingAssignments.Add(pending);
                }
            }
        }

        public static void TickRebind(LoadManager loadManager)
        {
            if (loadManager == null || loadManager.IsLoading || !loadManager.IsGameLoaded)
            {
                return;
            }

            lock (Sync)
            {
                if (PendingAssignments.Count == 0)
                {
                    return;
                }

                rebindFrames++;
                for (int index = PendingAssignments.Count - 1; index >= 0; index--)
                {
                    HandlerAssignment saved = PendingAssignments[index];
                    Packager donor = FindPackager(saved.HandlerGuid);
                    if (donor == null)
                    {
                        continue;
                    }

                    if (donor.EmployeeType != EEmployeeType.Handler)
                    {
                        PendingAssignments.RemoveAt(index);
                        continue;
                    }

                    HandlerEmployeeFactory.ConvertDonor(donor, false);
                    if (!HandlerEmployeeRegistry.TryGet(donor, out HandlerEmployeeRuntime runtime))
                    {
                        continue;
                    }

                    HandlerValidationResult result = runtime.TryRestoreAssignment(saved);
                    if (result.IsValid)
                    {
                        PendingAssignments.RemoveAt(index);
                        continue;
                    }

                    if (!IsLoadOrderingFailure(result.Code))
                    {
                        runtime.ReleaseReservation();
                        PendingAssignments.RemoveAt(index);
                        MelonLogger.Warning(
                            $"Discarded invalid Vehicle Handler assignment for {ShortGuid(saved.HandlerGuid)}: {result.Message}");
                    }
                }

                if (rebindFrames < MaximumRebindFrames || PendingAssignments.Count == 0)
                {
                    return;
                }

                foreach (HandlerAssignment stale in PendingAssignments)
                {
                    HandlerRuntimeServices.Reservations.ReleaseByHandler(stale.HandlerGuid);
                }

                MelonLogger.Warning(
                    $"Discarded {PendingAssignments.Count} stale Vehicle Handler assignment(s) after load stabilization.");
                PendingAssignments.Clear();
            }
        }

        public static void SaveCurrent(string saveNameHint = null)
        {
            lock (Sync)
            {
                string saveKey = ResolveCurrentSaveKey(saveNameHint);
                if (string.IsNullOrEmpty(saveKey))
                {
                    MelonLogger.Warning("Vehicle Handler assignments were not saved because no active save identity was available.");
                    return;
                }

                currentSaveKey = saveKey;
                HandlerPersistenceDocument document = LoadDocumentSafely();
                HandlerPersistenceProfile profile = FindProfile(document, saveKey);
                if (profile == null)
                {
                    profile = new HandlerPersistenceProfile { SaveKey = saveKey };
                    document.Profiles.Add(profile);
                }

                profile.Assignments = new List<HandlerAssignment>();
                foreach (HandlerAssignment assignment in HandlerRuntimeServices.Assignments.Snapshot())
                {
                    profile.Assignments.Add(assignment.Clone());
                }

                try
                {
                    Store.Save(document);
                }
                catch (Exception exception)
                {
                    MelonLogger.Error($"Could not save {FileName}: {exception.Message}");
                }
            }
        }

        public static void Reset()
        {
            lock (Sync)
            {
                PendingAssignments.Clear();
                currentSaveKey = string.Empty;
                rebindFrames = 0;
            }
        }

        private static HandlerPersistenceDocument LoadDocumentSafely()
        {
            try
            {
                return Store.Load();
            }
            catch (Exception exception)
            {
                BackupUnreadableFile();
                MelonLogger.Warning($"Could not read {FileName}; starting with empty assignment data: {exception.Message}");
                return new HandlerPersistenceDocument();
            }
        }

        private static void BackupUnreadableFile()
        {
            try
            {
                if (!File.Exists(Store.Path))
                {
                    return;
                }

                string backup = Store.Path + ".unreadable-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ".bak";
                File.Copy(Store.Path, backup, false);
            }
            catch (Exception exception)
            {
                MelonLogger.Warning($"Could not back up unreadable {FileName}: {exception.Message}");
            }
        }

        private static HandlerPersistenceProfile FindProfile(HandlerPersistenceDocument document, string saveKey)
        {
            if (document?.Profiles == null || string.IsNullOrEmpty(saveKey))
            {
                return null;
            }

            foreach (HandlerPersistenceProfile profile in document.Profiles)
            {
                if (profile != null && string.Equals(profile.SaveKey, saveKey, StringComparison.Ordinal))
                {
                    return profile;
                }
            }

            return null;
        }

        private static Packager FindPackager(string handlerGuid)
        {
            if (EmployeeManager.Instance == null || EmployeeManager.Instance.AllEmployees == null)
            {
                return null;
            }

            string expected = Normalize(handlerGuid);
            foreach (Employee employee in EmployeeManager.Instance.AllEmployees)
            {
                if (employee == null || Normalize(employee.GUID.ToString()) != expected)
                {
                    continue;
                }

                return (employee as Il2CppObjectBase)?.TryCast<Packager>();
            }

            return null;
        }

        private static bool IsLoadOrderingFailure(HandlerValidationCode code)
        {
            return code == HandlerValidationCode.MissingVehicle ||
                   code == HandlerValidationCode.MissingDestinationProperty ||
                   code == HandlerValidationCode.InvalidDestinationBay;
        }

        private static string ResolveCurrentSaveKey(string saveNameHint)
        {
            SaveInfo active = LoadManager.Instance?.ActiveSaveInfo;
            string resolved = GetSaveKey(active, saveNameHint);
            return string.IsNullOrEmpty(resolved) ? currentSaveKey : resolved;
        }

        private static string GetSaveKey(SaveInfo saveInfo, string saveNameHint)
        {
            if (saveInfo != null && saveInfo.SaveSlotNumber >= 0)
            {
                return "slot:" + saveInfo.SaveSlotNumber;
            }

            if (string.IsNullOrWhiteSpace(saveNameHint))
            {
                return string.Empty;
            }

            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(saveNameHint.Trim()));
            return "name:" + Convert.ToHexString(hash, 0, 8);
        }

        private static string ShortGuid(string guid)
        {
            string normalized = Normalize(guid);
            return normalized.Length <= 8 ? normalized : normalized.Substring(0, 8);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();
        }
    }
}
