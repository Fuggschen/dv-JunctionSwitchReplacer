using System;
using System.Collections.Generic;
using System.Reflection;
using UnityModManagerNet;
using HarmonyLib;
using JunctionSwitchReplacer.Core;
using JunctionSwitchReplacer.UI;
using JunctionSwitchReplacer.SwitchManagement;
using JunctionSwitchReplacer.Patches;
using JunctionSwitchReplacer.AssetLoading;

namespace JunctionSwitchReplacer
{
    [EnableReloading]
    public class Main
    {
        public static UnityModManager.ModEntry mod;
        public static Settings settings;
        public static bool enabled;
        
        // Core components
        private static CustomModelManager modelManager;
        private static SwitchProcessor switchProcessor;
        private static CacheManager cacheManager;
        private static ModGUI modGUI;
        
        // Track modified switches to avoid double-processing
        private static HashSet<int> modifiedSwitches = new HashSet<int>();

        // Called when the mod is loaded
        static bool Load(UnityModManager.ModEntry modEntry)
        {
            mod = modEntry;
            settings = Settings.Load<Settings>(modEntry);
            
            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;
            modEntry.OnUnload = Unload;

            // Initialize core components
            InitializeComponents();

            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            // Initialize patches
            VisualSwitchStartPatch.Initialize(switchProcessor, mod, enabled);

            mod.Logger.Log("Junction Switch Replacer loaded successfully!");
            return true;
        }

        private static void InitializeComponents()
        {
            modelManager = new CustomModelManager(mod);
            cacheManager = new CacheManager();
            switchProcessor = new SwitchProcessor(mod, modelManager, modifiedSwitches);
            
            modGUI = new ModGUI(
                mod, 
                modelManager, 
                cacheManager,
                OnApplyCustomModel,
                OnRestoreOriginal,
                OnReloadCustomModel,
                () => modifiedSwitches.Count
            );
            
            // Initialize model manager and cache
            modelManager.Initialize();
            cacheManager.UpdateSwitchCountCache();
        }

        static bool Unload(UnityModManager.ModEntry modEntry)
        {
            try
            {
                mod.Logger.Log("Unloading Junction Switch Replacer...");
                
                // Restore all modified switches
                switchProcessor?.RestoreAllSwitches();
                
                // Clear all tracking data
                modifiedSwitches.Clear();
                
                // Clean up components
                modelManager?.ClearCache();
                AssetBundleLoader.UnloadAssetBundle();
                
                // Unpatch all Harmony patches
                var harmony = new Harmony(modEntry.Info.Id);
                harmony.UnpatchSelf();

                mod.Logger.Log("Junction Switch Replacer unloaded successfully!");
                return true;
            }
            catch (Exception ex)
            {
                mod.Logger.Error($"Failed to unload mod: {ex.Message}");
                return false;
            }
        }

        // Called when the mod is enabled/disabled
        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            enabled = value;
            VisualSwitchStartPatch.SetEnabled(enabled);
            return true;
        }

        // GUI for mod settings
        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            modGUI?.DrawGUI();
        }

        // Save settings
        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }

        // Action handlers for GUI buttons
        private static void OnApplyCustomModel()
        {
            modifiedSwitches.Clear();
            switchProcessor.ApplyModificationToAllSwitches();
            cacheManager.UpdateSwitchCountCache();
        }

        private static void OnRestoreOriginal()
        {
            switchProcessor.RestoreAllSwitches();
            modifiedSwitches.Clear();
            cacheManager.UpdateSwitchCountCache();
        }

        private static void OnReloadCustomModel()
        {
            modelManager.Initialize();
            if (modelManager.UseCustomModel)
            {
                switchProcessor.RestoreAllSwitches();
                modifiedSwitches.Clear();
                switchProcessor.ApplyModificationToAllSwitches();
            }
        }
    }
}
