/****************************************************
 * FileName:		PlatformFeatureConfig
 * Description:		项目级平台能力开关配置（Resources 加载）
 *
*****************************************************/

using System.Collections.Generic;
using UnityEngine;

namespace MGKit
{
    [CreateAssetMenu(fileName = "PlatformFeatureConfig", menuName = "MGKit/Platform Feature Config")]
    public class PlatformFeatureConfig : ScriptableObject
    {
        public const string DefaultResourcesPath = "MGKit/PlatformFeatureConfig";

        [Tooltip("可选。非 Default 时强制使用该平台的配置（便于 Editor 内预览某平台 UI）。")]
        public MiniGamePlatform platformOverride;

        public List<PlatformFeatureProfileEntry> entries = new List<PlatformFeatureProfileEntry>();

        public PlatformFeatureProfile GetProfile(MiniGamePlatform platform)
        {
            if (entries != null)
            {
                for (var i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    if (entry != null && entry.platform == platform)
                        return entry.profile;
                }
            }

            return PlatformFeaturePresets.GetDefault(platform);
        }

        public void EnsureAllPlatforms()
        {
            if (entries == null)
                entries = new List<PlatformFeatureProfileEntry>();

            var existing = new HashSet<MiniGamePlatform>();
            for (var i = entries.Count - 1; i >= 0; i--)
            {
                var entry = entries[i];
                if (entry == null)
                {
                    entries.RemoveAt(i);
                    continue;
                }

                if (existing.Contains(entry.platform))
                {
                    entries.RemoveAt(i);
                    continue;
                }

                existing.Add(entry.platform);
            }

            foreach (var preset in PlatformFeaturePresets.CreateAllPlatformEntries())
            {
                if (existing.Contains(preset.platform))
                    continue;

                entries.Add(preset);
            }

            entries.Sort((a, b) => a.platform.CompareTo(b.platform));
        }

        public static PlatformFeatureConfig Load()
        {
            return Resources.Load<PlatformFeatureConfig>(DefaultResourcesPath);
        }
    }
}
