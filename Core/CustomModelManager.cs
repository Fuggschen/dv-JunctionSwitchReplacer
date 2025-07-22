using System;
using System.IO;
using UnityEngine;
using UnityModManagerNet;
using JunctionSwitchReplacer.AssetLoading;

namespace JunctionSwitchReplacer.Core
{
    public class CustomModelManager
    {
        private static Mesh customSwitchMesh = null;
        private static Material[] customSwitchMaterials = null;
        private static string customModelPath = "";
        private static bool useCustomModel = false;
        
        private readonly UnityModManager.ModEntry mod;
        
        public CustomModelManager(UnityModManager.ModEntry modEntry)
        {
            mod = modEntry;
        }
        
        public bool UseCustomModel => useCustomModel;
        public string CustomModelPath => customModelPath;
        
        public void Initialize()
        {
            try
            {
                // Clear previous state
                customSwitchMesh = null;
                customSwitchMaterials = null;
                AssetBundleLoader.UnloadAssetBundle();
                customModelPath = "";
                useCustomModel = false;
                
                // Look for AssetBundle files only
                string modPath = mod.Path;
                string[] assetBundleFiles = { "switchmodel", "switch_sign", "custom_switch" };
                
                // Check for AssetBundle files
                foreach (string fileName in assetBundleFiles)
                {
                    string fullPath = Path.Combine(modPath, fileName);
                    if (File.Exists(fullPath))
                    {
                        customModelPath = fullPath;
                        useCustomModel = true;
                        mod.Logger.Log($"Found AssetBundle: {fileName}");
                        return;
                    }
                }
                
                mod.Logger.Log("No AssetBundle found. Place 'switchmodel' AssetBundle in mod folder.");
                useCustomModel = false;
            }
            catch (Exception ex)
            {
                mod.Logger.Error($"Failed to initialize custom model: {ex.Message}");
                useCustomModel = false;
            }
        }
        
        public Mesh LoadCustomMesh()
        {
            if (customSwitchMesh != null)
            {
                return customSwitchMesh;
            }

            try
            {
                // Only load from AssetBundle
                if (!string.IsNullOrEmpty(customModelPath) && File.Exists(customModelPath))
                {
                    string fileName = Path.GetFileName(customModelPath).ToLower();
                    
                    // Check if this is an AssetBundle (no extension or specific names)
                    if (fileName == "switchmodel" || fileName == "switch_sign" || fileName == "custom_switch" || 
                        (!customModelPath.Contains(".") && File.Exists(customModelPath)))
                    {
                        mod.Logger.Log($"Loading AssetBundle: {customModelPath}");
                        customSwitchMesh = AssetBundleLoader.LoadMeshFromAssetBundle(customModelPath, mod);
                        if (customSwitchMesh != null)
                        {
                            mod.Logger.Log("Successfully loaded mesh from AssetBundle!");
                            return customSwitchMesh;
                        }
                        else
                        {
                            mod.Logger.Warning("AssetBundle mesh loading returned null");
                        }
                    }
                }
                
                mod.Logger.Warning("No valid AssetBundle found. Place 'switchmodel' AssetBundle in mod folder.");
                useCustomModel = false;
                return null;
            }
            catch (Exception ex)
            {
                mod.Logger.Error($"Failed to load custom mesh: {ex.Message}");
                useCustomModel = false;
                return null;
            }
        }
        
        public void ClearCache()
        {
            customSwitchMesh = null;
            customSwitchMaterials = null;
            AssetBundleLoader.UnloadAssetBundle();
        }
        
        public Material[] LoadCustomMaterials()
        {
            if (customSwitchMaterials != null)
            {
                return customSwitchMaterials;
            }

            try
            {
                // Only load from AssetBundle
                if (!string.IsNullOrEmpty(customModelPath) && File.Exists(customModelPath))
                {
                    mod.Logger.Log($"Loading materials from AssetBundle: {customModelPath}");
                    customSwitchMaterials = AssetBundleLoader.LoadMaterialsFromAssetBundle(customModelPath, mod);
                    if (customSwitchMaterials != null && customSwitchMaterials.Length > 0)
                    {
                        mod.Logger.Log($"Successfully loaded {customSwitchMaterials.Length} materials from AssetBundle!");
                        return customSwitchMaterials;
                    }
                    else
                    {
                        mod.Logger.Warning("AssetBundle material loading returned null or empty");
                    }
                }
                
                mod.Logger.Warning("No materials found in AssetBundle");
                return null;
            }
            catch (Exception ex)
            {
                mod.Logger.Error($"Failed to load custom materials: {ex.Message}");
                return null;
            }
        }
    }
}
