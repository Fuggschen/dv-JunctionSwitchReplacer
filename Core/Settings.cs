using UnityModManagerNet;

namespace JunctionSwitchReplacer.Core
{
    // Settings class
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
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
