using UnityEngine;
using UnityModManagerNet;
using JunctionSwitchReplacer.Core;

namespace JunctionSwitchReplacer.UI
{
    public class ModGUI
    {
        private readonly UnityModManager.ModEntry mod;
        private readonly CustomModelManager modelManager;
        private readonly CacheManager cacheManager;
        private readonly System.Action onApplyCustomModel;
        private readonly System.Action onRestoreOriginal;
        private readonly System.Action onReloadCustomModel;
        private readonly System.Func<int> getModifiedSwitchesCount;
        
        public ModGUI(UnityModManager.ModEntry modEntry, CustomModelManager customModelManager, 
                     CacheManager cacheManager, System.Action applyCustomModel, System.Action restoreOriginal, 
                     System.Action reloadCustomModel, System.Func<int> modifiedSwitchesCount)
        {
            mod = modEntry;
            modelManager = customModelManager;
            this.cacheManager = cacheManager;
            onApplyCustomModel = applyCustomModel;
            onRestoreOriginal = restoreOriginal;
            onReloadCustomModel = reloadCustomModel;
            getModifiedSwitchesCount = modifiedSwitchesCount;
        }
        
        public void DrawGUI()
        {
            GUILayout.Label("Junction Switch Replacer", GUILayout.Width(350));
            GUILayout.Space(10);
            
            // Action buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply Custom Model", GUILayout.Width(150)))
            {
                onApplyCustomModel?.Invoke();
            }
            
            if (GUILayout.Button("Restore Original", GUILayout.Width(120)))
            {
                onRestoreOriginal?.Invoke();
            }
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reload Custom Model", GUILayout.Width(150)))
            {
                onReloadCustomModel?.Invoke();
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            // Status information - use cached values for performance
            cacheManager.UpdateSwitchCountCacheIfNeeded();
            int modifiedCount = getModifiedSwitchesCount();
            
            GUILayout.Label($"Junction Switch found: {cacheManager.CachedSwitchCount}");
            GUILayout.Label($"Currently modified: {modifiedCount}");
        }
    }
}
