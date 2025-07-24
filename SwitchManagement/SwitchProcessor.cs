using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityModManagerNet;
using JunctionSwitchReplacer.Components;
using JunctionSwitchReplacer.Core;

namespace JunctionSwitchReplacer.SwitchManagement
{
    public class SwitchProcessor
    {
        private readonly UnityModManager.ModEntry mod;
        private readonly CustomModelManager modelManager;
        private readonly HashSet<int> modifiedSwitches;
        
        public SwitchProcessor(UnityModManager.ModEntry modEntry, CustomModelManager customModelManager, HashSet<int> modifiedSwitchesSet)
        {
            mod = modEntry;
            modelManager = customModelManager;
            modifiedSwitches = modifiedSwitchesSet;
        }
        
        private bool IsDebugLoggingEnabled => Main.settings?.enableDebugLogging ?? false;
        
        public void ApplyModificationToAllSwitches()
        {
            if (!modelManager.UseCustomModel)
            {
                mod.Logger.Warning("No custom model available. Cannot apply modifications.");
                return;
            }

            var switches = UnityEngine.Object.FindObjectsOfType<VisualSwitch>();
            int modifiedCount = 0;
            int failedCount = 0;
            int foundRenderers = 0;

            foreach (var visualSwitch in switches)
            {
                try
                {
                    var meshRenderers = visualSwitch.GetComponentsInChildren<MeshRenderer>();
                    foundRenderers += meshRenderers.Length;
                    
                    if (ApplyMeshModificationToSwitch(visualSwitch))
                    {
                        modifiedCount++;
                    }
                    else
                    {
                        failedCount++;
                    }
                }
                catch (Exception ex)
                {
                    mod.Logger.Error($"Failed to modify switch {visualSwitch?.name}: {ex.Message}");
                    failedCount++;
                }
            }

            // Summary logging only
            mod.Logger.Log($"Switch replacement completed:");
            mod.Logger.Log($"  - VisualSwitch objects: {switches.Length}");
            mod.Logger.Log($"  - Total MeshRenderers found: {foundRenderers}");
            mod.Logger.Log($"  - Successfully modified: {modifiedCount}");
            mod.Logger.Log($"  - Failed: {failedCount}");
        }
        
        public bool ApplyMeshModificationToSwitch(VisualSwitch visualSwitch)
        {
            if (visualSwitch?.gameObject == null) 
            {
                return false;
            }

            int switchId = visualSwitch.GetInstanceID();
            if (modifiedSwitches.Contains(switchId))
            {
                return true; // Already modified
            }

            if (!modelManager.UseCustomModel)
            {
                return false; // Don't log warning here - it's already logged in ApplyModificationToAllSwitches
            }

            try
            {
                // Debug first few switches to understand structure
                if (modifiedSwitches.Count < 3 && IsDebugLoggingEnabled)
                {
                    DebugVisualSwitchStructure(visualSwitch);
                }

                // The VisualSwitch is just a trigger - find the actual switch models nearby
                var nearbyRenderers = FindSwitchRenderersNearPosition(visualSwitch.transform.position);
                
                bool anyModified = false;

                foreach (var meshRenderer in nearbyRenderers)
                {
                    if (ApplyCustomMeshReplacement(meshRenderer.transform))
                    {
                        anyModified = true;
                    }
                }

                if (anyModified)
                {
                    modifiedSwitches.Add(switchId);
                    return true;
                }
                else
                {
                    mod.Logger.Warning($"No suitable renderers found near switch: {visualSwitch.name}");
                }
            }
            catch (Exception ex)
            {
                mod.Logger.Error($"Failed to apply mesh modification: {ex.Message}");
            }

            return false;
        }
        
        public void RestoreAllSwitches()
        {
            try
            {
                // Find all objects with OriginalMeshReference components
                var originalMeshComponents = UnityEngine.Object.FindObjectsOfType<OriginalMeshReference>();
                int restoredCount = 0;

                foreach (var originalMeshComponent in originalMeshComponents)
                {
                    if (originalMeshComponent?.originalMesh != null)
                    {
                        var meshFilter = originalMeshComponent.GetComponent<MeshFilter>();
                        var renderer = originalMeshComponent.GetComponent<Renderer>();
                        
                        if (meshFilter != null)
                        {
                            // Restore original mesh
                            meshFilter.mesh = originalMeshComponent.originalMesh;
                            
                            // Restore original materials if available
                            if (renderer != null && originalMeshComponent.originalMaterials != null)
                            {
                                renderer.materials = originalMeshComponent.originalMaterials;
                            }
                            
                            restoredCount++;
                        }
                        
                        // Clean up the component
                        UnityEngine.Object.DestroyImmediate(originalMeshComponent);
                    }
                }

                modifiedSwitches.Clear();
                modelManager.ClearCache();
                
                mod.Logger.Log($"Restored {restoredCount} switches to original state");
            }
            catch (Exception ex)
            {
                mod.Logger.Error($"Failed to restore switches: {ex.Message}");
            }
        }
        
        private void DebugVisualSwitchStructure(VisualSwitch visualSwitch)
        {
            if (visualSwitch?.gameObject == null || !IsDebugLoggingEnabled) return;
            
            mod.Logger.Log($"=== DEBUG: VisualSwitch '{visualSwitch.name}' Structure ===");
            
            // Get all components on the main object
            var components = visualSwitch.GetComponents<Component>();
            mod.Logger.Log($"Main object components: {string.Join(", ", components.Select(c => c.GetType().Name))}");
            
            // Recursively log all children with their components
            LogTransformHierarchy(visualSwitch.transform, 0, 3); // Max depth 3
        }
        
        private void LogTransformHierarchy(Transform transform, int depth, int maxDepth)
        {
            if (depth > maxDepth || !IsDebugLoggingEnabled) return;
            
            string indent = new string(' ', depth * 2);
            var components = transform.GetComponents<Component>();
            var renderers = transform.GetComponents<Renderer>();
            
            mod.Logger.Log($"{indent}{transform.name} ({components.Length} components, {renderers.Length} renderers)");
            
            if (renderers.Length > 0)
            {
                foreach (var renderer in renderers)
                {
                    mod.Logger.Log($"{indent}  -> {renderer.GetType().Name}: {renderer.name}");
                }
            }
            
            for (int i = 0; i < transform.childCount && i < 10; i++) // Limit to first 10 children
            {
                LogTransformHierarchy(transform.GetChild(i), depth + 1, maxDepth);
            }
        }
        
        private List<MeshRenderer> FindSwitchRenderersNearPosition(Vector3 position)
        {
            var results = new List<MeshRenderer>();
            var allRenderers = UnityEngine.Object.FindObjectsOfType<MeshRenderer>();
            
            foreach (var renderer in allRenderers)
            {
                if (renderer?.transform == null) continue;
                
                // Check if this renderer is close to the switch trigger (within 5 units - reduced range)
                float distance = Vector3.Distance(position, renderer.transform.position);
                if (distance > 5f) continue;
                
                // Check if this looks like a switch component (be more specific)
                if (IsSwitchPoleRenderer(renderer))
                {
                    results.Add(renderer);
                }
            }
            
            return results;
        }
        
        private bool IsSwitchPoleRenderer(MeshRenderer renderer)
        {
            if (renderer?.transform == null) return false;
            
            // Check object names in the hierarchy - be more specific
            Transform current = renderer.transform;
            while (current != null)
            {
                string name = current.name.ToLower();
                
                // Exclude levers and other moving parts
                if (name.Contains("lever") || name.Contains("handle") || name.Contains("arm") || 
                    name.Contains("actuator") || name.Contains("moving"))
                {
                    return false;
                }
                
                // Look specifically for switch signs/poles
                if (name.Contains("switch_sign"))
                {
                    return true;
                }
                
                current = current.parent;
            }
            
            // Check mesh names - be more specific
            var meshFilter = renderer.GetComponent<MeshFilter>();
            if (meshFilter?.mesh != null)
            {
                string meshName = meshFilter.mesh.name.ToLower();
                
                // Exclude moving parts
                if (meshName.Contains("lever") || meshName.Contains("handle") || meshName.Contains("arm"))
                {
                    return false;
                }
                
                // Look for switch signs/poles specifically
                if (meshName.Contains("switch_sign") || meshName.Contains("switchsign") ||
                    (meshName.Contains("switch") && meshName.Contains("sign")) ||
                    (meshName.Contains("switch") && meshName.Contains("pole")))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        private bool ApplyCustomMeshReplacement(Transform meshTransform)
        {
            try
            {
                var meshFilter = meshTransform.GetComponent<MeshFilter>();
                var renderer = meshTransform.GetComponent<Renderer>();
                if (meshFilter?.mesh == null || renderer == null) 
                {
                    return false;
                }
                
                // Store original mesh and materials for restoration
                var originalMeshComponent = meshTransform.gameObject.GetComponent<OriginalMeshReference>();
                if (originalMeshComponent == null)
                {
                    originalMeshComponent = meshTransform.gameObject.AddComponent<OriginalMeshReference>();
                    originalMeshComponent.originalMesh = meshFilter.mesh;
                    originalMeshComponent.originalMaterials = renderer.materials; // Store materials too
                }
                
                // Load custom mesh
                Mesh customMesh = modelManager.LoadCustomMesh();
                if (customMesh == null)
                {
                    return false;
                }
                
                // Try to preserve the original UV mapping by copying it to the custom mesh
                if (meshFilter.mesh.uv != null && meshFilter.mesh.uv.Length > 0 && 
                    customMesh.vertexCount <= meshFilter.mesh.vertexCount)
                {
                    if (IsDebugLoggingEnabled)
                    {
                        mod.Logger.Log("Attempting to preserve original UV mapping...");
                    }
                    var originalUVs = new Vector2[customMesh.vertexCount];
                    Array.Copy(meshFilter.mesh.uv, originalUVs, Math.Min(customMesh.vertexCount, meshFilter.mesh.uv.Length));
                    customMesh.uv = originalUVs;
                    {
                    }
                }
                
                // Replace the mesh with our custom version
                meshFilter.mesh = customMesh;
                
                // Apply custom materials if setting is enabled and materials are available
                if (Main.settings?.useCustomMaterials == true)
                {
                    var customMaterials = modelManager.LoadCustomMaterials();
                    if (customMaterials != null && customMaterials.Length > 0)
                    {
                        renderer.materials = customMaterials;
                        if (IsDebugLoggingEnabled)
                        {
                            mod.Logger.Log($"Applied {customMaterials.Length} custom materials from AssetBundle");
                            for (int i = 0; i < customMaterials.Length; i++)
                            {
                                var mat = customMaterials[i];
                                mod.Logger.Log($"  Custom Material {i}: {mat?.name ?? "null"}");
                                if (mat?.shader != null)
                                {
                                    mod.Logger.Log($"    Shader: {mat.shader.name}");
                                }
                                if (mat?.mainTexture != null)
                                {
                                    mod.Logger.Log($"    Main texture: {mat.mainTexture.name}");
                                    mod.Logger.Log($"    Texture size: {mat.mainTexture.width}x{mat.mainTexture.height}");
                                }
                            }
                        }
                    }
                    else
                    {
                        if (IsDebugLoggingEnabled)
                        {
                            mod.Logger.Log("No custom materials found in AssetBundle, keeping original materials");
                        }
                    }
                }
                else
                {
                    if (IsDebugLoggingEnabled)
                    {
                        mod.Logger.Log("Custom materials disabled in settings, keeping original materials");
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                mod.Logger.Error($"Failed to apply custom mesh replacement: {ex.Message}");
                return false;
            }
        }
        
        private void SaveTextureFromMaterial(Material material, int materialIndex)
        {
            try
            {
                if (material?.mainTexture == null || !IsDebugLoggingEnabled) return;
                
                var texture = material.mainTexture as Texture2D;
                if (texture == null) return;
                
                mod.Logger.Log($"Texture details for extraction:");
                mod.Logger.Log($"  Name: {texture.name}");
                mod.Logger.Log($"  Format: {texture.format}");
                mod.Logger.Log($"  Size: {texture.width}x{texture.height}");
                mod.Logger.Log($"  Is Readable: {texture.isReadable}");
                mod.Logger.Log($"  Material: {material.name}");
                mod.Logger.Log($"  Shader: {material.shader?.name}");
                
                // Note: For full texture extraction, use external tools like:
                // - AssetStudio (https://github.com/Perfare/AssetStudio)
                // - UABE (Unity Assets Bundle Extractor)
                // - DevX UnityUnpacker
                mod.Logger.Log("Use AssetStudio or UABE to extract textures from Derail Valley's data files");
            }
            catch (Exception ex)
            {
                mod.Logger.Error($"Failed to analyze texture: {ex.Message}");
            }
        }
    }
}
