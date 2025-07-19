using System;
using HarmonyLib;
using UnityModManagerNet;
using JunctionSwitchReplacer.SwitchManagement;

namespace JunctionSwitchReplacer.Patches
{
    // Harmony patch to catch new switches being created
    [HarmonyPatch(typeof(VisualSwitch), "Start")]
    public static class VisualSwitchStartPatch
    {
        private static SwitchProcessor switchProcessor;
        private static bool enabled;
        private static UnityModManager.ModEntry mod;
        
        public static void Initialize(SwitchProcessor processor, UnityModManager.ModEntry modEntry, bool isEnabled)
        {
            switchProcessor = processor;
            mod = modEntry;
            enabled = isEnabled;
        }
        
        public static void SetEnabled(bool isEnabled)
        {
            enabled = isEnabled;
        }
        
        static void Postfix(VisualSwitch __instance)
        {
            if (!enabled || switchProcessor == null) return;
            
            try
            {
                // Apply modification to newly spawned switches
                switchProcessor.ApplyMeshModificationToSwitch(__instance);
            }
            catch (Exception ex)
            {
                mod?.Logger.Error($"Failed to modify newly spawned switch: {ex.Message}");
            }
        }
    }
}
