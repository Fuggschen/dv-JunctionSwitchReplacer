using System;
using System.Collections.Generic;
using System.Reflection;
using UnityModManagerNet;
using HarmonyLib;
using JunctionSwitchReplacer.Core;
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
        
        // Track modified switches to avoid double-processing
        private static HashSet<int> modifiedSwitches = new HashSet<int>();

        // Called when the mod is loaded
        static bool Load(UnityModManager.ModEntry modEntry)
        {
            mod = modEntry;
            settings = Settings.Load<Settings>(modEntry);
            
            modEntry.OnToggle = OnToggle;
            modEntry.OnSaveGUI = OnSaveGUI;
            modEntry.OnGUI = OnGUI;
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
            
            // Initialize model manager and cache
            modelManager.Initialize();
            cacheManager.UpdateSwitchCountCache();
        }

        static bool Unload(UnityModManager.ModEntry modEntry)
        {
            try
            {
                var harmony = new Harmony(modEntry.Info.Id);
                mod.Logger.Log("Unloading Junction Switch Replacer...");
                
                // Restore all modified switches
                switchProcessor?.RestoreAllSwitches();
                
                // Clear all tracking data
                modifiedSwitches.Clear();
                
                // Clean up components
                modelManager?.ClearCache();
                AssetBundleLoader.UnloadAssetBundle();
                
                // Unpatch Harmony
                harmony.UnpatchAll();

                mod.Logger.Log("Junction Switch Replacer unloaded successfully!");
                
                // Clear static references for proper reload
                modelManager = null;
                switchProcessor = null;
                cacheManager = null;
                mod = null;
                settings = null;
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

        // Save settings
        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }

        // Draw settings GUI
        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            settings.OnGUI(modEntry);
        }

        // Action handlers for GUI buttons
        public static void OnApplyCustomModel()
        {
            mod.Logger.Log("Applying custom model...");
            
            // First reinitialize the model manager to use the currently selected asset bundle
            modelManager.Initialize();
            
            if (modelManager.UseCustomModel)
            {
                modifiedSwitches.Clear();
                switchProcessor.ApplyModificationToAllSwitches();
                cacheManager.UpdateSwitchCountCache();
                mod.Logger.Log("Apply custom model completed.");
            }
            else
            {
                mod.Logger.Warning("No custom model available. Check asset bundle selection.");
            }
        }

        public static void OnRestoreOriginal()
        {
            mod.Logger.Log("Restoring original switches...");
            switchProcessor.RestoreAllSwitches();
            modifiedSwitches.Clear();
            cacheManager.UpdateSwitchCountCache();
            mod.Logger.Log("Restore original completed.");
        }

        public static void OnReloadCustomModel()
        {
            mod.Logger.Log("Reloading custom model...");
            
            try
            {
                modelManager.Initialize();
                mod.Logger.Log($"Model manager initialized. UseCustomModel: {modelManager.UseCustomModel}");
                
                if (modelManager.UseCustomModel)
                {
                    mod.Logger.Log("Restoring original switches before applying new model...");
                    switchProcessor.RestoreAllSwitches();
                    modifiedSwitches.Clear();
                    
                    mod.Logger.Log("Applying custom model to all switches...");
                    switchProcessor.ApplyModificationToAllSwitches();
                    cacheManager.UpdateSwitchCountCache();
                    
                    mod.Logger.Log("Custom model reload completed successfully.");
                }
                else
                {
                    mod.Logger.Warning("No custom model available after reload. Check asset bundle selection and file paths.");
                }
            }
            catch (System.Exception ex)
            {
                mod.Logger.Error($"Failed to reload custom model: {ex.Message}");
                mod.Logger.Error($"Stack trace: {ex.StackTrace}");
            }
        }
        
        public static void TriggerMaterialRefresh()
        {
            // Called by CustomModelManager when materials need to be refreshed on existing switches
            if (switchProcessor != null)
            {
                switchProcessor.RefreshMaterialsIfNeeded();
            }
        }
        
        public static void OnRefreshMaterials()
        {
            mod.Logger.Log("Manual material refresh triggered...");
            
            try
            {
                // Force clear the material cache and reload asset bundle
                modelManager?.ClearCache();
                AssetBundleLoader.ForceReloadAssetBundle();
                
                // Refresh materials on all switches
                if (switchProcessor != null)
                {
                    switchProcessor.RefreshMaterialsIfNeeded();
                    mod.Logger.Log("Manual material refresh completed successfully.");
                }
                else
                {
                    mod.Logger.Warning("Switch processor not initialized.");
                }
            }
            catch (System.Exception ex)
            {
                mod.Logger.Error($"Failed to refresh materials manually: {ex.Message}");
            }
        }
    }
}
