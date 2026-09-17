using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppScheduleOne.Management;
using Il2CppScheduleOne.UI.Management;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VehicleHandlers.Contracts;
using VehicleHandlers.Employees;

namespace VehicleHandlers.Configuration
{
    [HarmonyPatch(typeof(PackagerConfigPanel), "BindInternal", new[]
    {
        typeof(Il2CppSystem.Collections.Generic.List<EntityConfiguration>)
    })]
    internal static class HandlerConfigPanelBindPatch
    {
        private const string RootName = "VehicleHandlerConfiguration";

        private static void Postfix(
            PackagerConfigPanel __instance,
            Il2CppSystem.Collections.Generic.List<EntityConfiguration> configurations)
        {
            if (__instance == null)
            {
                return;
            }

            DestroyExisting(__instance);
            List<HandlerEmployeeRuntime> handlers = ResolveHandlers(configurations, out bool containsVanillaPackager);
            if (containsVanillaPackager || handlers.Count == 0)
            {
                SetVanillaFieldsVisible(__instance, true);
                return;
            }

            SetVanillaFieldsVisible(__instance, false);
            if (handlers.Count != 1)
            {
                HandlerConfigPanelView.BuildMessage(
                    __instance,
                    RootName,
                    "Vehicle Handler",
                    "Select one Handler at a time to configure its vehicle route.");
                return;
            }

            HandlerConfigPanelView.Build(__instance, RootName, new HandlerConfigurationPanelSession(handlers[0]));
        }

        private static List<HandlerEmployeeRuntime> ResolveHandlers(
            Il2CppSystem.Collections.Generic.List<EntityConfiguration> configurations,
            out bool containsVanillaPackager)
        {
            List<HandlerEmployeeRuntime> result = new List<HandlerEmployeeRuntime>();
            containsVanillaPackager = false;
            if (configurations == null)
            {
                return result;
            }

            foreach (EntityConfiguration configuration in configurations)
            {
                PackagerConfiguration packagerConfiguration =
                    (configuration as Il2CppObjectBase)?.TryCast<PackagerConfiguration>();
                if (packagerConfiguration?.packager == null ||
                    !HandlerEmployeeRegistry.TryGet(packagerConfiguration.packager, out HandlerEmployeeRuntime runtime))
                {
                    containsVanillaPackager = true;
                    continue;
                }

                result.Add(runtime);
            }

            return result;
        }

        private static void SetVanillaFieldsVisible(PackagerConfigPanel panel, bool visible)
        {
            if (panel.StationsUI != null)
            {
                panel.StationsUI.gameObject.SetActive(visible);
            }

            if (panel.RoutesUI != null)
            {
                panel.RoutesUI.gameObject.SetActive(visible);
            }
        }

        private static void DestroyExisting(PackagerConfigPanel panel)
        {
            Transform existing = panel.transform.Find(RootName);
            if (existing != null)
            {
                UnityEngine.Object.Destroy(existing.gameObject);
            }
        }
    }

    internal static class HandlerConfigPanelView
    {
        private static readonly Color PanelColor = new Color(0.055f, 0.075f, 0.085f, 0.97f);
        private static readonly Color FieldColor = new Color(0.10f, 0.14f, 0.15f, 1f);
        private static readonly Color AccentColor = new Color(0.10f, 0.70f, 0.65f, 1f);
        private static readonly Color MutedColor = new Color(0.58f, 0.66f, 0.67f, 1f);
        private static readonly Color ErrorColor = new Color(1f, 0.45f, 0.36f, 1f);

        public static void Build(
            PackagerConfigPanel panel,
            string rootName,
            HandlerConfigurationPanelSession session)
        {
            TextMeshProUGUI textTemplate = panel.BedUI?.FieldLabel;
            GameObject root = CreatePanelRoot(panel, rootName);
            Transform parent = root.transform;

            CreateText(parent, textTemplate, "Vehicle Handler / Driver", 23f, FontStyles.Bold, Color.white, 32f);
            CreateText(
                parent,
                textTemplate,
                "Move one existing owned vehicle to a reserved property loading bay.",
                14f,
                FontStyles.Normal,
                MutedColor,
                36f);

            HandlerDropdownControl vehicle = CreateDropdown(parent, textTemplate, "Owned vehicle");
            HandlerDropdownControl property = CreateDropdown(parent, textTemplate, "Destination property");
            HandlerDropdownControl bay = CreateDropdown(parent, textTemplate, "Loading bay");
            TextMeshProUGUI status = CreateText(
                parent,
                textTemplate,
                "Choose a vehicle, destination, and bay, then apply the assignment.",
                13f,
                FontStyles.Normal,
                MutedColor,
                38f);

            Button enabledButton = CreateButton(parent, textTemplate, string.Empty, 34f, FieldColor);
            Button applyButton = CreateButton(parent, textTemplate, "Apply / reconfigure", 38f, AccentColor);
            GameObject manualRow = CreateHorizontalRow(parent, 40f);
            Button loadButton = CreateButton(manualRow.transform, textTemplate, "Load into bay", 38f, FieldColor);
            Button hideButton = CreateButton(manualRow.transform, textTemplate, "Hide / release bay", 38f, FieldColor);
            Button refreshButton = CreateButton(parent, textTemplate, "Refresh owned vehicles and bays", 32f, FieldColor);

            Action updateEnabledText = () => SetButtonText(
                enabledButton,
                session.Enabled ? "Assignment: enabled" : "Assignment: disabled");

            Action refreshBays = () =>
            {
                bay.SetOptions(session.BayOptions, session.SelectedDockIndex.ToString(), session.SelectBay);
            };

            Action refreshAll = () =>
            {
                session.RefreshOptions();
                vehicle.SetOptions(session.VehicleOptions, session.SelectedVehicleGuid, session.SelectVehicle);
                property.SetOptions(session.PropertyOptions, session.SelectedPropertyCode, selected =>
                {
                    session.SelectProperty(selected);
                    refreshBays();
                });
                refreshBays();
                SetStatus(status, "Choices refreshed from existing owned objects.", false);
            };

            vehicle.SetOptions(session.VehicleOptions, session.SelectedVehicleGuid, session.SelectVehicle);
            property.SetOptions(session.PropertyOptions, session.SelectedPropertyCode, selected =>
            {
                session.SelectProperty(selected);
                refreshBays();
            });
            refreshBays();
            updateEnabledText();

            enabledButton.onClick.AddListener((UnityAction)(() =>
            {
                session.ToggleEnabled();
                updateEnabledText();
            }));
            applyButton.onClick.AddListener((UnityAction)(() =>
            {
                HandlerValidationResult result = session.Apply();
                SetStatus(status, result.IsValid ? "Handler assignment applied." : result.Message, !result.IsValid);
            }));
            loadButton.onClick.AddListener((UnityAction)(() =>
            {
                HandlerValidationResult result = session.LoadIntoAssignedBay();
                SetStatus(status, result.IsValid ? "Vehicle placed in the assigned bay." : result.Message, !result.IsValid);
            }));
            hideButton.onClick.AddListener((UnityAction)(() =>
            {
                HandlerValidationResult result = session.HideAndReleaseBay();
                SetStatus(status, result.IsValid ? "Vehicle hidden and bay released." : result.Message, !result.IsValid);
            }));
            refreshButton.onClick.AddListener((UnityAction)(() => refreshAll()));

            if (session.VehicleOptions.Count == 0)
            {
                SetStatus(status, "No unassigned player-owned vehicle is available.", true);
            }
            else if (session.PropertyOptions.Count == 0)
            {
                SetStatus(status, "No supported owned destination property is available.", true);
            }
            else if (session.BayOptions.Count == 0)
            {
                SetStatus(status, "The selected property has no loading bay.", true);
            }
        }

        public static void BuildMessage(
            PackagerConfigPanel panel,
            string rootName,
            string title,
            string message)
        {
            TextMeshProUGUI template = panel.BedUI?.FieldLabel;
            Transform parent = CreatePanelRoot(panel, rootName).transform;
            CreateText(parent, template, title, 23f, FontStyles.Bold, Color.white, 34f);
            CreateText(parent, template, message, 15f, FontStyles.Normal, ErrorColor, 70f);
        }

        private static GameObject CreatePanelRoot(PackagerConfigPanel panel, string rootName)
        {
            GameObject root = CreateRectObject(rootName, panel.transform);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0.79f);
            rect.offsetMin = new Vector2(14f, 12f);
            rect.offsetMax = new Vector2(-14f, -4f);

            Image background = root.AddComponent<Image>();
            background.color = PanelColor;
            VerticalLayoutGroup layout = root.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;
            return root;
        }

        private static HandlerDropdownControl CreateDropdown(
            Transform parent,
            TextMeshProUGUI template,
            string label)
        {
            GameObject block = CreateRectObject(label + " selector", parent);
            VerticalLayoutGroup layout = block.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 3f;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = block.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateText(block.transform, template, label, 13f, FontStyles.Bold, MutedColor, 18f);
            Button button = CreateButton(block.transform, template, "None available", 32f, FieldColor);
            GameObject options = CreateRectObject(label + " options", block.transform);
            VerticalLayoutGroup optionsLayout = options.AddComponent<VerticalLayoutGroup>();
            optionsLayout.spacing = 2f;
            optionsLayout.childControlWidth = true;
            optionsLayout.childForceExpandWidth = true;
            optionsLayout.childControlHeight = false;
            optionsLayout.childForceExpandHeight = false;
            ContentSizeFitter optionsFitter = options.AddComponent<ContentSizeFitter>();
            optionsFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            options.SetActive(false);
            return new HandlerDropdownControl(button, options, template);
        }

        private static GameObject CreateHorizontalRow(Transform parent, float height)
        {
            GameObject row = CreateRectObject("Manual bay controls", parent);
            LayoutElement element = row.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = true;
            return row;
        }

        private static Button CreateButton(
            Transform parent,
            TextMeshProUGUI template,
            string label,
            float height,
            Color color)
        {
            GameObject gameObject = CreateRectObject(label + " button", parent);
            LayoutElement element = gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            Image image = gameObject.AddComponent<Image>();
            image.color = color;
            Button button = gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            TextMeshProUGUI text = CreateText(
                gameObject.transform,
                template,
                label,
                14f,
                FontStyles.Normal,
                Color.white,
                height);
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 1f);
            textRect.offsetMax = new Vector2(-8f, -1f);
            text.alignment = TextAlignmentOptions.Center;
            return button;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent,
            TextMeshProUGUI template,
            string value,
            float size,
            FontStyles style,
            Color color,
            float height)
        {
            GameObject gameObject;
            TextMeshProUGUI text;
            if (template != null)
            {
                gameObject = UnityEngine.Object.Instantiate(template.gameObject);
                gameObject.transform.SetParent(parent, false);
                text = gameObject.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                gameObject = CreateRectObject("Text", parent);
                text = gameObject.AddComponent<TextMeshProUGUI>();
            }

            gameObject.name = "Handler " + value;
            LayoutElement existingLayout = gameObject.GetComponent<LayoutElement>();
            LayoutElement layout = existingLayout != null ? existingLayout : gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = height;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreateRectObject(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, new[] { Il2CppType.Of<RectTransform>() });
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static void SetButtonText(Button button, string value)
        {
            TextMeshProUGUI text = button?.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null)
            {
                text.text = value;
            }
        }

        private static void SetStatus(TextMeshProUGUI status, string message, bool isError)
        {
            if (status == null)
            {
                return;
            }

            status.text = string.IsNullOrWhiteSpace(message) ? "Ready." : message;
            status.color = isError ? ErrorColor : MutedColor;
        }

        private sealed class HandlerDropdownControl
        {
            private readonly Button button;
            private readonly GameObject optionsRoot;
            private readonly TextMeshProUGUI template;
            private readonly List<GameObject> optionObjects = new List<GameObject>();

            public HandlerDropdownControl(Button button, GameObject optionsRoot, TextMeshProUGUI template)
            {
                this.button = button;
                this.optionsRoot = optionsRoot;
                this.template = template;
                button.onClick.AddListener((UnityAction)(() => optionsRoot.SetActive(!optionsRoot.activeSelf)));
            }

            public void SetOptions(
                IReadOnlyList<HandlerConfigurationOption> options,
                string selectedId,
                Action<string> selected)
            {
                foreach (GameObject optionObject in optionObjects)
                {
                    if (optionObject != null)
                    {
                        UnityEngine.Object.Destroy(optionObject);
                    }
                }

                optionObjects.Clear();
                optionsRoot.SetActive(false);
                HandlerConfigurationOption current = null;
                foreach (HandlerConfigurationOption option in options)
                {
                    if (string.Equals(option.Id, selectedId, StringComparison.OrdinalIgnoreCase))
                    {
                        current = option;
                    }

                    HandlerConfigurationOption captured = option;
                    Button optionButton = CreateButton(optionsRoot.transform, template, option.Label, 29f, FieldColor);
                    optionButton.onClick.AddListener((UnityAction)(() =>
                    {
                        selected(captured.Id);
                        SetButtonText(button, captured.Label + "  ▼");
                        optionsRoot.SetActive(false);
                    }));
                    optionObjects.Add(optionButton.gameObject);
                }

                SetButtonText(button, current != null ? current.Label + "  ▼" : "None available");
                button.interactable = options.Count > 0;
            }
        }
    }
}
