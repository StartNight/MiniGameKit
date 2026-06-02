/****************************************************
 * FileName:		AdConfig
 * CompanyName:		苏州微游科技有限公司
 * Author:			Felix/李康康
 * Email:			kangkang.li@outlook.com
 * CreateTime:		2026-05-18 10:00:00
 * Version:			1.0
 * UnityVersion:	2022.3.43f1c1
 * Description:		广告配置数据，管理各平台广告位ID映射
 *
*****************************************************/

using System.Collections.Generic;
using UnityEngine;

namespace MGKit
{
    [System.Serializable]
    public class AdConfig
    {
        public MiniGamePlatform CurrentPlatform;
        public bool EnableAd = true;
        public float BannerRefreshInterval = 30f;

        [System.Serializable]
        public class AdUnitIdEntry
        {
            public AdType adType;
            public MiniGamePlatform platform;
            public string adUnitId;
        }

        // Unity cannot serialize nested dictionaries; use a list for inspector editing instead.
        public List<AdUnitIdEntry> adUnitIdEntries = new List<AdUnitIdEntry>();

        private Dictionary<AdType, Dictionary<MiniGamePlatform, string>> _adUnitIdMap;

        private Dictionary<AdType, Dictionary<MiniGamePlatform, string>> GetOrBuildMap()
        {
            if (_adUnitIdMap != null)
                return _adUnitIdMap;

            _adUnitIdMap = new Dictionary<AdType, Dictionary<MiniGamePlatform, string>>();
            foreach (var entry in adUnitIdEntries)
            {
                if (string.IsNullOrEmpty(entry.adUnitId))
                    continue;

                if (!_adUnitIdMap.TryGetValue(entry.adType, out var platformMap))
                {
                    platformMap = new Dictionary<MiniGamePlatform, string>();
                    _adUnitIdMap[entry.adType] = platformMap;
                }
                platformMap[entry.platform] = entry.adUnitId;
            }
            return _adUnitIdMap;
        }

        public string GetAdUnitId(AdType adType, MiniGamePlatform platform)
        {
            var map = GetOrBuildMap();
            if (map.TryGetValue(adType, out var platformMap))
            {
                if (platformMap.TryGetValue(platform, out var adUnitId))
                {
                    return adUnitId;
                }
            }
            Debug.LogWarning($"[AdConfig] 未找到广告位ID: AdType={adType}, Platform={platform}");
            return string.Empty;
        }

        public void SetAdUnitId(AdType adType, MiniGamePlatform platform, string adUnitId)
        {
            // Update lookup cache
            var map = GetOrBuildMap();
            if (!map.TryGetValue(adType, out var platformMap))
            {
                platformMap = new Dictionary<MiniGamePlatform, string>();
                map[adType] = platformMap;
            }
            platformMap[platform] = adUnitId;

            // Update serializable list
            adUnitIdEntries.RemoveAll(e => e.adType == adType && e.platform == platform);
            adUnitIdEntries.Add(new AdUnitIdEntry
            {
                adType = adType,
                platform = platform,
                adUnitId = adUnitId
            });
        }
    }
}