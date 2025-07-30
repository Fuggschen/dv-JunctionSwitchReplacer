using UnityEngine;
using UnityModManagerNet;
using System.Collections.Generic;
using System.IO;

namespace JunctionSwitchReplacer.Core
{
    // Settings class
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        public bool enableDebugLogging = false;
        
        // Asset bundle selection - handled in OnGUI
        public string selectedAssetBundlePath = "";
        public int selectedAssetBundleIndex = 0;
        
        // Cache for available asset bundles
        private List<string> availableAssetBundles = new List<string>();
        private string[] assetBundleDisplayNames = new string[0];
        private bool assetBundlesScanned = false;

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }

        public void OnChange()
        {
            // Called when settings change
        }
        
        public void OnGUI(UnityModManager.ModEntry modEntry)
        {
            GUILayout.Space(10);
            // Render the debug logging checkbox manually
            enableDebugLogging = GUILayout.Toggle(enableDebugLogging, "Enable Debug Logging");

            GUILayout.Label("Asset Bundle Selection:", GUILayout.Width(200));

            // Scan for asset bundles if not done yet
            if (!assetBundlesScanned)
            {
                ScanForAssetBundles(modEntry);
            }

            if (availableAssetBundles.Count == 0)
            {
                GUILayout.Label("No asset bundles found. Place .assetbundle files in mod directories.");
                if (GUILayout.Button("Refresh Asset Bundle List", GUILayout.Width(180)))
                {
                    ScanForAssetBundles(modEntry);
                }
                return;
            }

            // Ensure index is valid
            if (selectedAssetBundleIndex >= assetBundleDisplayNames.Length)
            {
                selectedAssetBundleIndex = 0;
            }

            int newIndex = GUILayout.SelectionGrid(selectedAssetBundleIndex, assetBundleDisplayNames, 1, GUILayout.Width(400));
            if (newIndex != selectedAssetBundleIndex)
            {
                selectedAssetBundleIndex = newIndex;
                if (newIndex < availableAssetBundles.Count)
                {
                    selectedAssetBundlePath = availableAssetBundles[newIndex];
                    if (enableDebugLogging)
                        modEntry.Logger.Log($"Asset bundle selection changed to: {selectedAssetBundlePath}");
                }
            }

            // Refresh button
            if (GUILayout.Button("Refresh Asset Bundle List", GUILayout.Width(180)))
            {
                ScanForAssetBundles(modEntry);
            }

            GUILayout.Space(10);

            // Action buttons
            GUILayout.Label("Actions:", GUILayout.Width(200));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply Custom Model", GUILayout.Width(150)))
            {
                Main.OnApplyCustomModel();
            }

            if (GUILayout.Button("Restore Original", GUILayout.Width(120)))
            {
                Main.OnRestoreOriginal();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reload Custom Model", GUILayout.Width(150)))
            {
                Main.OnReloadCustomModel();
            }
            if (GUILayout.Button("Refresh Materials", GUILayout.Width(120)))
            {
                Main.OnRefreshMaterials();
            }
            GUILayout.EndHorizontal();
        }
        
        private void ScanForAssetBundles(UnityModManager.ModEntry modEntry)
        {
            availableAssetBundles.Clear();
            availableAssetBundles = CustomModelManager.ScanForAssetBundles(modEntry.Path);
            
            // Create display names (show relative path from mods directory)
            assetBundleDisplayNames = new string[availableAssetBundles.Count];
            string modsDirectory = Directory.GetParent(modEntry.Path)?.FullName;
            
            for (int i = 0; i < availableAssetBundles.Count; i++)
            {
                string path = availableAssetBundles[i];
                
                if (!string.IsNullOrEmpty(modsDirectory) && path.StartsWith(modsDirectory))
                {
                    // Show relative path from mods directory
                    string relativePath = path.Substring(modsDirectory.Length + 1);
                    assetBundleDisplayNames[i] = relativePath;
                }
                else
                {
                    // Fallback to filename with parent directory
                    string fileName = Path.GetFileName(path);
                    string parentDir = Path.GetFileName(Path.GetDirectoryName(path));
                    assetBundleDisplayNames[i] = $"{parentDir}/{fileName}";
                }
            }
            
            assetBundlesScanned = true;
            
            // Auto-select the first asset bundle if none is selected
            if (availableAssetBundles.Count > 0 && string.IsNullOrEmpty(selectedAssetBundlePath))
            {
                selectedAssetBundleIndex = 0;
                selectedAssetBundlePath = availableAssetBundles[0];
            }
        }
    }
}
