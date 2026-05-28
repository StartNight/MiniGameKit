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

[System.Serializable]
public class AdConfig
{
    public AdPlatform CurrentPlatform;
    public bool EnableAd = true;
    public float BannerRefreshInterval = 30f;
    public Dictionary<AdType, Dictionary<AdPlatform, string>> AdUnitIdMap = new Dictionary<AdType, Dictionary<AdPlatform, string>>();

    public string GetAdUnitId(AdType adType, AdPlatform platform)
    {
        if (AdUnitIdMap.TryGetValue(adType, out var platformMap))
        {
            if (platformMap.TryGetValue(platform, out var adUnitId))
            {
                return adUnitId;
            }
        }
        Debug.LogWarning($"[AdConfig] 未找到广告位ID: AdType={adType}, Platform={platform}");
        return string.Empty;
    }

    public void SetAdUnitId(AdType adType, AdPlatform platform, string adUnitId)
    {
        if (!AdUnitIdMap.ContainsKey(adType))
        {
            AdUnitIdMap[adType] = new Dictionary<AdPlatform, string>();
        }
        AdUnitIdMap[adType][platform] = adUnitId;
    }
}
