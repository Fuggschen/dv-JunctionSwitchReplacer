using UnityEngine;

namespace JunctionSwitchReplacer.Core
{
    public class CacheManager
    {
        private int cachedSwitchCount = 0;
        private float lastCacheUpdate = 0f;
        private readonly float CACHE_UPDATE_INTERVAL = 1.0f; // Update cache every 1 second
        
        public int CachedSwitchCount => cachedSwitchCount;
        
        public void UpdateSwitchCountCacheIfNeeded()
        {
            if (Time.time - lastCacheUpdate > CACHE_UPDATE_INTERVAL)
            {
                UpdateSwitchCountCache();
            }
        }
        
        public void UpdateSwitchCountCache()
        {
            try
            {
                var switches = UnityEngine.Object.FindObjectsOfType<VisualSwitch>();
                cachedSwitchCount = switches?.Length ?? 0;
                lastCacheUpdate = Time.time;
            }
            catch
            {
                // If FindObjectsOfType fails, keep the last known count
                cachedSwitchCount = 0;
            }
        }
    }
}
