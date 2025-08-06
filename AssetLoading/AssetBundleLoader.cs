using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityModManagerNet;

namespace JunctionSwitchReplacer.AssetLoading
{
    public static class AssetBundleLoader
    {
        private static AssetBundle loadedAssetBundle = null;
        private static string currentBundlePath = null;
        
        public static void UnloadAssetBundle()
        {
            if (loadedAssetBundle != null)
            {
                loadedAssetBundle.Unload(false);
                loadedAssetBundle = null;
                currentBundlePath = null;
            }
        }
        
        // Force reload asset bundle (useful when materials become invalid)
        public static void ForceReloadAssetBundle()
        {
            UnloadAssetBundle();
        }
        
        // Check if the current asset bundle is still valid
        private static bool IsAssetBundleValid()
        {
            return loadedAssetBundle != null && !loadedAssetBundle.Equals(null);
        }
        
        // Ensure asset bundle is loaded and valid
        private static bool EnsureAssetBundleLoaded(string bundlePath, UnityModManager.ModEntry mod)
        {
            // If we have a valid bundle and it's the same path, keep using it
            if (IsAssetBundleValid() && currentBundlePath == bundlePath)
            {
                return true;
            }
            
            // If we have a different bundle loaded, unload it first
            if (loadedAssetBundle != null && currentBundlePath != bundlePath)
            {
                UnloadAssetBundle();
            }
            
            // Load the new bundle
            if (loadedAssetBundle == null)
            {
                loadedAssetBundle = AssetBundle.LoadFromFile(bundlePath);
                if (loadedAssetBundle != null)
                {
                    currentBundlePath = bundlePath;
                    return true;
                }
                else
                {
                    mod.Logger.Error($"Failed to load AssetBundle from: {bundlePath}");
                    return false;
                }
            }
            
            return true;
        }
        
        public static Mesh LoadMeshFromAssetBundle(string bundlePath, UnityModManager.ModEntry mod)
        {
            try
            {
                bool debugEnabled = Main.settings?.enableDebugLogging == true;
                
                if (debugEnabled)
                {
                    mod.Logger.Log($"=== AssetBundle Loading Debug ===");
                    mod.Logger.Log($"Loading asset bundle from: {bundlePath}");
                }
                
                // Ensure the AssetBundle is loaded (but don't unload it if it's already the right one)
                if (!EnsureAssetBundleLoaded(bundlePath, mod))
                {
                    return null;
                }
                
                if (debugEnabled)
                    mod.Logger.Log("AssetBundle loaded successfully");
                
                // Debug: List all assets in the bundle
                var allAssetNames = loadedAssetBundle.GetAllAssetNames();
                if (debugEnabled)
                {
                    mod.Logger.Log($"Assets in bundle ({allAssetNames.Length} total):");
                    foreach (string assetName in allAssetNames)
                    {
                        mod.Logger.Log($"  - {assetName}");
                    }
                }
                
                // First try to load OBJ file from the AssetBundle
                if (debugEnabled)
                    mod.Logger.Log("Trying to load OBJ file: assets/switch_sign.obj");
                var objAsset = loadedAssetBundle.LoadAsset<TextAsset>("assets/switch_sign.obj");
                if (objAsset != null)
                {
                    if (debugEnabled)
                        mod.Logger.Log("Found switch_sign.obj, attempting to parse...");
                    var mesh = OBJLoader.LoadMeshFromText(objAsset.text, mod);
                    if (mesh != null)
                    {
                        mesh.name = "AssetBundle_OBJ_switch_sign";
                        mod.Logger.Log("Successfully loaded mesh from OBJ file");
                        return mesh;
                    }
                    else
                    {
                        mod.Logger.Warning("OBJ parsing failed");
                    }
                }
                else
                {
                    if (debugEnabled)
                        mod.Logger.Log("No switch_sign.obj found at assets/switch_sign.obj");
                }
                
                // Try to find other OBJ files in the bundle
                if (debugEnabled)
                    mod.Logger.Log("Searching for other OBJ files...");
                foreach (string assetName in allAssetNames)
                {
                    if (assetName.EndsWith(".obj", StringComparison.OrdinalIgnoreCase))
                    {
                        if (debugEnabled)
                            mod.Logger.Log($"Found OBJ file: {assetName}");
                        var objTextAsset = loadedAssetBundle.LoadAsset<TextAsset>(assetName);
                        if (objTextAsset != null)
                        {
                            var mesh = OBJLoader.LoadMeshFromText(objTextAsset.text, mod);
                            if (mesh != null)
                            {
                                mesh.name = $"AssetBundle_OBJ_{Path.GetFileNameWithoutExtension(assetName)}";
                                mod.Logger.Log($"Successfully loaded mesh from OBJ: {assetName}");
                                return mesh;
                            }
                        }
                    }
                }
                
                // Fallback: Try to instantiate GameObjects and use their meshes directly
                if (debugEnabled)
                    mod.Logger.Log("Trying GameObject instantiation method...");
                var allGameObjects = loadedAssetBundle.LoadAllAssets<GameObject>();
                if (debugEnabled)
                    mod.Logger.Log($"Found {allGameObjects.Length} GameObjects in bundle");
                
                foreach (var prefab in allGameObjects)
                {
                    if (debugEnabled)
                        mod.Logger.Log($"Checking GameObject: {prefab.name}");
                    try
                    {
                        // Instantiate the prefab temporarily
                        var instance = UnityEngine.Object.Instantiate(prefab);
                        if (instance != null)
                        {
                            var meshFilter = instance.GetComponent<MeshFilter>();
                            if (meshFilter?.sharedMesh == null)
                            {
                                meshFilter = instance.GetComponentInChildren<MeshFilter>();
                            }
                            
                            if (meshFilter?.sharedMesh != null)
                            {
                                // Use the mesh directly from the instantiated object
                                var mesh = meshFilter.sharedMesh;
                                mesh.name = $"AssetBundle_GameObject_{prefab.name}";
                                mod.Logger.Log($"Successfully loaded mesh from GameObject: {prefab.name}");
                                
                                // Don't destroy the instance yet, let Unity handle cleanup
                                // The mesh will remain valid even after the instance is destroyed
                                UnityEngine.Object.DestroyImmediate(instance);
                                return mesh;
                            }
                            else
                            {
                                if (debugEnabled)
                                    mod.Logger.Log($"GameObject {prefab.name} has no usable MeshFilter");
                            }
                            
                            UnityEngine.Object.DestroyImmediate(instance);
                        }
                    }
                    catch (Exception ex)
                    {
                        mod.Logger.Error($"Failed to instantiate GameObject {prefab.name}: {ex.Message}");
                    }
                }
                
                // Last resort: try direct mesh loading (this will likely fail due to isReadable=false)
                mod.Logger.Log("Trying direct mesh loading...");
                Mesh loadedMesh = null;
                
                // Look for common mesh names first
                string[] commonMeshNames = { "switchmodel", "switch_sign", "switch_setter", "pole", "sign", "model" };
                
                foreach (string meshName in commonMeshNames)
                {
                    mod.Logger.Log($"Checking for mesh named: {meshName}");
                    loadedMesh = loadedAssetBundle.LoadAsset<Mesh>(meshName);
                    if (loadedMesh != null)
                    {
                        mod.Logger.Log($"Successfully loaded mesh: {meshName}");
                        return loadedMesh; // Return the mesh directly, don't try to copy
                    }
                }
                
                // If no mesh found with common names, get all meshes and use the first one
                mod.Logger.Log("Checking all meshes in bundle...");
                var allMeshes = loadedAssetBundle.LoadAllAssets<Mesh>();
                mod.Logger.Log($"Found {allMeshes.Length} meshes in bundle");
                
                if (allMeshes != null && allMeshes.Length > 0)
                {
                    for (int i = 0; i < allMeshes.Length; i++)
                    {
                        mod.Logger.Log($"  Mesh {i}: {allMeshes[i].name}");
                    }
                    loadedMesh = allMeshes[0];
                    mod.Logger.Log($"Using first mesh: {loadedMesh.name}");
                    return loadedMesh; // Return the mesh directly, don't try to copy
                }
                
                mod.Logger.Error("No usable mesh found in AssetBundle");
                return null;
            }
            catch (Exception ex)
            {
                mod.Logger.Error($"Failed to load AssetBundle mesh: {ex.Message}");
                return null;
            }
        }
        
        public static Material[] LoadMaterialsFromAssetBundle(string bundlePath, UnityModManager.ModEntry mod)
        {
            try
            {
                // Ensure the AssetBundle is loaded and valid
                if (!EnsureAssetBundleLoaded(bundlePath, mod))
                {
                    return null;
                }
                
                // First try to get materials from GameObjects
                var allGameObjects = loadedAssetBundle.LoadAllAssets<GameObject>();
                
                foreach (var prefab in allGameObjects)
                {
                    try
                    {
                        // Instantiate the prefab temporarily to get its materials
                        var instance = UnityEngine.Object.Instantiate(prefab);
                        if (instance != null)
                        {
                            var renderer = instance.GetComponent<Renderer>();
                            if (renderer == null)
                            {
                                renderer = instance.GetComponentInChildren<Renderer>();
                            }
                            
                            if (renderer?.materials != null && renderer.materials.Length > 0)
                            {
                                // Copy the materials array
                                var materials = new Material[renderer.materials.Length];
                                for (int i = 0; i < renderer.materials.Length; i++)
                                {
                                    materials[i] = renderer.materials[i];
                                }
                                
                                UnityEngine.Object.DestroyImmediate(instance);
                                return materials;
                            }
                            
                            UnityEngine.Object.DestroyImmediate(instance);
                        }
                    }
                    catch (Exception ex)
                    {
                        mod.Logger.Error($"Failed to get materials from GameObject {prefab.name}: {ex.Message}");
                    }
                }
                
                // Fallback: Try direct material loading
                var allMaterials = loadedAssetBundle.LoadAllAssets<Material>();
                if (allMaterials != null && allMaterials.Length > 0)
                {
                    return allMaterials;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                mod.Logger.Error($"Failed to load AssetBundle materials: {ex.Message}");
                return null;
            }
        }
    }
}
