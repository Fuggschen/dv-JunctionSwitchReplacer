using System;
using System.Collections.Generic;
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
                if (Main.settings?.enableDebugLogging == true)
                    mod.Logger.Log("Initializing CustomModelManager...");
                
                // Clear previous state
                customSwitchMesh = null;
                customSwitchMaterials = null;
                AssetBundleLoader.UnloadAssetBundle();
                customModelPath = "";
                useCustomModel = false;
                
                if (Main.settings?.enableDebugLogging == true)
                    mod.Logger.Log($"Settings check - selectedAssetBundlePath: '{Main.settings?.selectedAssetBundlePath ?? "null"}'");
                
                // Check if settings has a valid selected asset bundle path
                if (!string.IsNullOrEmpty(Main.settings?.selectedAssetBundlePath))
                {
                    bool fileExists = File.Exists(Main.settings.selectedAssetBundlePath);
                    if (Main.settings?.enableDebugLogging == true)
                        mod.Logger.Log($"Checking saved path exists: {fileExists} for {Main.settings.selectedAssetBundlePath}");
                    
                    if (fileExists)
                    {
                        customModelPath = Main.settings.selectedAssetBundlePath;
                        useCustomModel = true;
                        if (Main.settings?.enableDebugLogging == true)
                            mod.Logger.Log($"Using selected asset bundle: {customModelPath}");
                        return;
                    }
                }
                
                // If we have a saved path but it doesn't exist, or no saved path, scan for asset bundles
                if (!string.IsNullOrEmpty(Main.settings?.selectedAssetBundlePath))
                {
                    if (Main.settings?.enableDebugLogging == true)
                        mod.Logger.Warning($"Saved asset bundle path doesn't exist: {Main.settings.selectedAssetBundlePath}");
                }
                if (Main.settings?.enableDebugLogging == true)
                    mod.Logger.Log("Scanning for available asset bundles...");
                
                // Try to find asset bundles and update settings
                var availableBundles = ScanForAssetBundles(mod.Path);
                if (availableBundles.Count > 0)
                {
                    customModelPath = availableBundles[0];
                    useCustomModel = true;
                    Main.settings.selectedAssetBundlePath = customModelPath;
                    Main.settings.selectedAssetBundleIndex = 0;
                    if (Main.settings?.enableDebugLogging == true)
                        mod.Logger.Log($"Auto-updated to first available asset bundle: {customModelPath}");
                    return;
                }
                
                if (Main.settings?.enableDebugLogging == true)
                    mod.Logger.Log($"Fallback: Scanning mod folder '{mod.Path}' for asset bundles...");
                
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
                        if (Main.settings?.enableDebugLogging == true)
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
            // Check if we have a cached mesh and it's still valid
            if (IsMeshValid())
            {
                return customSwitchMesh;
            }
            
            // Clear invalid cache
            customSwitchMesh = null;

            try
            {
                // Only load from AssetBundle
                if (!string.IsNullOrEmpty(customModelPath) && File.Exists(customModelPath))
                {
                    string fileName = Path.GetFileName(customModelPath).ToLower();
                    
                    // Check if this is an AssetBundle (.assetbundle extension or specific names without extension)
                    if (fileName.EndsWith(".assetbundle") || 
                        fileName == "switchmodel" || fileName == "switch_sign" || fileName == "custom_switch")
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
        
        // Check if cached materials are still valid
        private bool AreMaterialsValid()
        {
            if (customSwitchMaterials == null || customSwitchMaterials.Length == 0)
                return false;
                
            // Check if any material is null or destroyed
            foreach (var material in customSwitchMaterials)
            {
                if (material == null || material.Equals(null))
                    return false;
                    
                // Check if the material's shader is missing (common sign of asset bundle unload)
                if (material.shader == null || material.shader.Equals(null))
                    return false;
            }
            
            return true;
        }
        
        // Check if cached mesh is still valid
        private bool IsMeshValid()
        {
            return customSwitchMesh != null && !customSwitchMesh.Equals(null);
        }
        
        public Material[] LoadCustomMaterials()
        {
            // Check if we have cached materials and they're still valid
            if (AreMaterialsValid())
            {
                return customSwitchMaterials;
            }
            
            // Clear invalid cache
            customSwitchMaterials = null;

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
        
        public static List<string> ScanForAssetBundles(string modPath)
        {
            var assetBundles = new List<string>();
            
            try
            {
                // Get the parent directory of the mod (where other mods might be)
                string modsDirectory = Directory.GetParent(modPath)?.FullName;
                if (string.IsNullOrEmpty(modsDirectory))
                    return assetBundles;
                
                // Search in the mod directory itself
                ScanDirectoryForAssetBundles(modPath, assetBundles);
                
                // Search in parent mods directory (other mod folders)
                var subdirectories = Directory.GetDirectories(modsDirectory);
                foreach (string subdir in subdirectories)
                {
                    ScanDirectoryForAssetBundles(subdir, assetBundles);
                }
                
                // Also search in a common "AssetBundles" folder if it exists
                string assetBundlesDir = Path.Combine(modsDirectory, "AssetBundles");
                if (Directory.Exists(assetBundlesDir))
                {
                    ScanDirectoryForAssetBundles(assetBundlesDir, assetBundles);
                }
                
                // Debug output
                if (Main.settings?.enableDebugLogging == true && Main.mod != null)
                {
                    Main.mod.Logger.Log($"Scanned for asset bundles, found {assetBundles.Count} files:");
                    foreach (string bundle in assetBundles)
                    {
                        Main.mod.Logger.Log($"  - {bundle} (exists: {File.Exists(bundle)})");
                    }
                }
            }
            catch (Exception ex)
            {
                if (Main.mod != null)
                {
                    Main.mod.Logger.Error($"Error scanning for asset bundles: {ex.Message}");
                }
            }
            
            return assetBundles;
        }
        
        private static void ScanDirectoryForAssetBundles(string directory, List<string> results)
        {
            try
            {
                if (!Directory.Exists(directory))
                    return;
                
                // Define patterns to search for - only .assetbundle files as requested
                string[] patterns = { "*.assetbundle" };
                
                foreach (string pattern in patterns)
                {
                    string[] files = Directory.GetFiles(directory, pattern, SearchOption.AllDirectories);
                    foreach (string file in files)
                    {
                        if (!results.Contains(file))
                        {
                            results.Add(file);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (Main.mod != null)
                {
                    Main.mod.Logger.Error($"Error scanning directory '{directory}': {ex.Message}");
                }
            }
        }
    }
}
