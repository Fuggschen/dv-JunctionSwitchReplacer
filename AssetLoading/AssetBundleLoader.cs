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
        
        public static void UnloadAssetBundle()
        {
            if (loadedAssetBundle != null)
            {
                loadedAssetBundle.Unload(true);
                loadedAssetBundle = null;
            }
        }
        
        public static Mesh LoadMeshFromAssetBundle(string bundlePath, UnityModManager.ModEntry mod)
        {
            try
            {
                mod.Logger.Log($"Attempting to load AssetBundle from: {bundlePath}");
                
                // Unload previous AssetBundle if exists
                UnloadAssetBundle();
                
                // Load the AssetBundle
                loadedAssetBundle = AssetBundle.LoadFromFile(bundlePath);
                if (loadedAssetBundle == null)
                {
                    mod.Logger.Error("Failed to load AssetBundle - bundle is null");
                    return null;
                }
                
                mod.Logger.Log($"AssetBundle loaded successfully. Asset names: {string.Join(", ", loadedAssetBundle.GetAllAssetNames())}");
                
                // First try to load OBJ file from the AssetBundle
                var objAsset = loadedAssetBundle.LoadAsset<TextAsset>("assets/switch_sign.obj");
                if (objAsset != null)
                {
                    mod.Logger.Log("Found switch_sign.obj in AssetBundle, parsing...");
                    var mesh = OBJLoader.LoadMeshFromText(objAsset.text, mod);
                    if (mesh != null)
                    {
                        mesh.name = "AssetBundle_OBJ_switch_sign";
                        mod.Logger.Log($"Successfully loaded OBJ from AssetBundle: {mesh.vertexCount} vertices, {mesh.triangles.Length/3} triangles");
                        return mesh;
                    }
                }
                
                // Try to find other OBJ files in the bundle
                foreach (string assetName in loadedAssetBundle.GetAllAssetNames())
                {
                    if (assetName.EndsWith(".obj", StringComparison.OrdinalIgnoreCase))
                    {
                        var objTextAsset = loadedAssetBundle.LoadAsset<TextAsset>(assetName);
                        if (objTextAsset != null)
                        {
                            mod.Logger.Log($"Found OBJ file in AssetBundle: {assetName}, parsing...");
                            var mesh = OBJLoader.LoadMeshFromText(objTextAsset.text, mod);
                            if (mesh != null)
                            {
                                mesh.name = $"AssetBundle_OBJ_{Path.GetFileNameWithoutExtension(assetName)}";
                                mod.Logger.Log($"Successfully loaded OBJ from AssetBundle: {mesh.vertexCount} vertices");
                                return mesh;
                            }
                        }
                    }
                }
                
                // Fallback: Try to instantiate GameObjects and use their meshes directly
                mod.Logger.Log("No OBJ files found, trying to instantiate GameObjects...");
                var allGameObjects = loadedAssetBundle.LoadAllAssets<GameObject>();
                
                foreach (var prefab in allGameObjects)
                {
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
                                mod.Logger.Log($"Successfully got mesh from instantiated GameObject: {mesh.vertexCount} vertices");
                                
                                // Don't destroy the instance yet, let Unity handle cleanup
                                // The mesh will remain valid even after the instance is destroyed
                                UnityEngine.Object.DestroyImmediate(instance);
                                return mesh;
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
                mod.Logger.Log("Trying direct mesh loading as last resort...");
                Mesh loadedMesh = null;
                
                // Look for common mesh names first
                string[] commonMeshNames = { "switchmodel", "switch_sign", "switch_setter", "pole", "sign", "model" };
                
                foreach (string meshName in commonMeshNames)
                {
                    loadedMesh = loadedAssetBundle.LoadAsset<Mesh>(meshName);
                    if (loadedMesh != null)
                    {
                        mod.Logger.Log($"Found mesh with name: {meshName} (but may not be readable)");
                        return loadedMesh; // Return the mesh directly, don't try to copy
                    }
                }
                
                // If no mesh found with common names, get all meshes and use the first one
                var allMeshes = loadedAssetBundle.LoadAllAssets<Mesh>();
                if (allMeshes != null && allMeshes.Length > 0)
                {
                    loadedMesh = allMeshes[0];
                    mod.Logger.Log($"Using first mesh found: {loadedMesh.name} (but may not be readable)");
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
    }
}
