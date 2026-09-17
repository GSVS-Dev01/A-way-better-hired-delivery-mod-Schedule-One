using System;
using HarmonyLib;
using Il2CppScheduleOne.Dialogue;

namespace VehicleHandlers.Hiring
{
    [HarmonyPatch(typeof(DialogueController_Fixer), "ModifyChoiceList", new[]
    {
        typeof(string),
        typeof(Il2CppSystem.Collections.Generic.List<DialogueChoiceData>)
    }, new[]
    {
        ArgumentType.Normal,
        ArgumentType.Ref
    })]
    internal static class FixerHandlerChoiceListPatch
    {
        private static void Postfix(ref Il2CppSystem.Collections.Generic.List<DialogueChoiceData> existingChoices)
        {
            if (existingChoices == null)
            {
                return;
            }

            DialogueChoiceData donorChoice = null;
            foreach (DialogueChoiceData choice in existingChoices)
            {
                if (choice == null)
                {
                    continue;
                }

                if (string.Equals(choice.ChoiceLabel, HandlerHireContext.ChoiceLabel, StringComparison.Ordinal))
                {
                    return;
                }

                if (string.Equals(choice.ChoiceLabel, HandlerHireContext.VanillaDonorChoiceLabel, StringComparison.Ordinal))
                {
                    donorChoice = choice;
                }
            }

            if (donorChoice == null)
            {
                return;
            }

            existingChoices.Add(new DialogueChoiceData
            {
                Guid = HandlerHireContext.ChoiceGuid,
                ChoiceText = HandlerHireContext.ChoiceText,
                ChoiceLabel = HandlerHireContext.ChoiceLabel,
                ShowWorldspaceDialogue = donorChoice.ShowWorldspaceDialogue
            });
        }
    }

    [HarmonyPatch(typeof(DialogueController_Fixer), "CheckChoice", new[]
    {
        typeof(string),
        typeof(string)
    }, new[]
    {
        ArgumentType.Normal,
        ArgumentType.Out
    })]
    internal static class FixerHandlerChoiceValidationPatch
    {
        private static void Prefix(ref string choiceLabel)
        {
            if (string.Equals(choiceLabel, HandlerHireContext.ChoiceLabel, StringComparison.Ordinal))
            {
                choiceLabel = HandlerHireContext.VanillaDonorChoiceLabel;
            }
        }
    }

    [HarmonyPatch(typeof(DialogueController_Fixer), "ChoiceCallback", new[] { typeof(string) })]
    internal static class FixerHandlerChoiceCallbackPatch
    {
        private static void Prefix(ref string choiceLabel)
        {
            if (string.Equals(choiceLabel, HandlerHireContext.ChoiceLabel, StringComparison.Ordinal))
            {
                HandlerHireContext.RequestLocalHire();
                choiceLabel = HandlerHireContext.VanillaDonorChoiceLabel;
                return;
            }

            if (HandlerHireContext.IsVanillaRoleChoice(choiceLabel))
            {
                HandlerHireContext.CancelLocalHire();
            }
        }
    }

    [HarmonyPatch(typeof(DialogueController_Fixer), "ModifyDialogueText", new[] { typeof(string), typeof(string) })]
    internal static class FixerHandlerDialogueTextPatch
    {
        private static void Postfix(ref string __result)
        {
            if (!HandlerHireContext.IsPendingLocalHire || string.IsNullOrEmpty(__result))
            {
                return;
            }

            __result = __result
                .Replace("Packager", "Vehicle Handler")
                .Replace("packager", "vehicle handler");
        }
    }
}
