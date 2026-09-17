using System;
using Il2CppScheduleOne.Employees;
using UnityEngine;

namespace VehicleHandlers.Employees
{
    internal static class HandlerAppearance
    {
        private static readonly Color HandlerAccent = new Color(0.12f, 0.55f, 0.62f, 1f);

        public static void Apply(Packager donor)
        {
            if (donor == null)
            {
                return;
            }

            Renderer[] renderers = donor.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                Material[] materials = renderer.materials;
                bool changed = false;
                for (int index = 0; index < materials.Length; index++)
                {
                    Material material = materials[index];
                    if (material == null || IsBodyMaterial(material.name) || !material.HasProperty("_Color"))
                    {
                        continue;
                    }

                    material.color = Color.Lerp(material.color, HandlerAccent, 0.42f);
                    changed = true;
                }

                if (changed)
                {
                    renderer.materials = materials;
                }
            }
        }

        private static bool IsBodyMaterial(string materialName)
        {
            string normalized = (materialName ?? string.Empty).ToLowerInvariant();
            return normalized.Contains("skin") ||
                   normalized.Contains("eye") ||
                   normalized.Contains("hair") ||
                   normalized.Contains("teeth") ||
                   normalized.Contains("mouth");
        }
    }
}
