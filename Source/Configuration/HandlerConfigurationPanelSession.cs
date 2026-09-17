using System;
using System.Collections.Generic;
using VehicleHandlers.Contracts;
using VehicleHandlers.Employees;
using VehicleHandlers.Runtime;

namespace VehicleHandlers.Configuration
{
    public sealed class HandlerConfigurationPanelSession
    {
        public HandlerConfigurationPanelSession(HandlerEmployeeRuntime runtime)
        {
            Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

            HandlerConfiguration existing = runtime.Configuration ?? new HandlerConfiguration();
            SelectedVehicleGuid = existing.VehicleGuid ?? string.Empty;
            SelectedPropertyCode = existing.DestinationPropertyCode ?? string.Empty;
            SelectedDockIndex = existing.DestinationDockIndex;
            Enabled = existing.Enabled;
            ManualHidden = existing.ManualHidden;
            RefreshOptions();
        }

        public HandlerEmployeeRuntime Runtime { get; }

        public IReadOnlyList<HandlerConfigurationOption> VehicleOptions { get; private set; }

        public IReadOnlyList<HandlerConfigurationOption> PropertyOptions { get; private set; }

        public IReadOnlyList<HandlerConfigurationOption> BayOptions { get; private set; }

        public string SelectedVehicleGuid { get; private set; }

        public string SelectedPropertyCode { get; private set; }

        public int SelectedDockIndex { get; private set; }

        public bool Enabled { get; private set; }

        public bool ManualHidden { get; private set; }

        public void RefreshOptions()
        {
            VehicleOptions = HandlerConfigurationCatalog.GetVehicles(Runtime.HandlerGuid, SelectedVehicleGuid);
            PropertyOptions = HandlerConfigurationCatalog.GetProperties();
            SelectedVehicleGuid = SelectExistingOrFirst(VehicleOptions, SelectedVehicleGuid);
            SelectedPropertyCode = SelectExistingOrFirst(PropertyOptions, SelectedPropertyCode);
            RefreshBays();
        }

        public void SelectVehicle(string vehicleGuid)
        {
            SelectedVehicleGuid = FindOption(VehicleOptions, vehicleGuid)?.Id ?? string.Empty;
        }

        public void SelectProperty(string propertyCode)
        {
            SelectedPropertyCode = FindOption(PropertyOptions, propertyCode)?.Id ?? string.Empty;
            SelectedDockIndex = -1;
            RefreshBays();
        }

        public void SelectBay(string dockIndex)
        {
            HandlerConfigurationOption option = FindOption(BayOptions, dockIndex);
            SelectedDockIndex = option != null && int.TryParse(option.Id, out int parsed) ? parsed : -1;
        }

        public void ToggleEnabled()
        {
            Enabled = !Enabled;
        }

        public HandlerValidationResult Apply()
        {
            HandlerConfiguration configuration = new HandlerConfiguration
            {
                VehicleGuid = SelectedVehicleGuid,
                DestinationPropertyCode = SelectedPropertyCode,
                DestinationDockIndex = SelectedDockIndex,
                Enabled = Enabled,
                ManualHidden = ManualHidden
            };

            return Runtime.TryApplyConfiguration(configuration);
        }

        public HandlerValidationResult LoadIntoAssignedBay()
        {
            return HandlerRuntimeServices.ManualBayController.TryLoadIntoAssignedBay(Runtime);
        }

        public HandlerValidationResult HideAndReleaseBay()
        {
            return HandlerRuntimeServices.ManualBayController.TryHideAndReleaseBay(Runtime);
        }

        private void RefreshBays()
        {
            string current = SelectedDockIndex >= 0 ? SelectedDockIndex.ToString() : string.Empty;
            BayOptions = HandlerConfigurationCatalog.GetLoadingBays(SelectedPropertyCode);
            HandlerConfigurationOption selected = FindOption(BayOptions, current);
            if (selected == null && BayOptions.Count > 0)
            {
                selected = BayOptions[0];
            }

            SelectedDockIndex = selected != null && int.TryParse(selected.Id, out int parsed) ? parsed : -1;
        }

        private static string SelectExistingOrFirst(
            IReadOnlyList<HandlerConfigurationOption> options,
            string selectedId)
        {
            HandlerConfigurationOption selected = FindOption(options, selectedId);
            return selected?.Id ?? (options.Count > 0 ? options[0].Id : string.Empty);
        }

        private static HandlerConfigurationOption FindOption(
            IReadOnlyList<HandlerConfigurationOption> options,
            string id)
        {
            if (options == null)
            {
                return null;
            }

            foreach (HandlerConfigurationOption option in options)
            {
                if (option != null && string.Equals(option.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    return option;
                }
            }

            return null;
        }
    }
}
