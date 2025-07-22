using UnityEngine;
using UnityModManagerNet;

namespace JunctionSwitchReplacer.Core
{
    // Settings class
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        [Draw("Enable Debug Logging")] public bool enableDebugLogging = false;
        [Draw("Use Custom Materials")] public bool useCustomMaterials = false;

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }

        public void OnChange()
        {
            // Called when settings change
        }
    }
}
