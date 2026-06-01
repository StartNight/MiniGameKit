/****************************************************
 * FileName:		PlatformSDKFactory
 * CompanyName:		苏州微游科技有限公司
 * Author:			Felix/李康康
 * Email:			kangkang.li@outlook.com
 * CreateTime:		2026-06-01 10:00:00
 * Version:			1.0
 * UnityVersion:	2022.3.43f1c1
 * Description:		大一统平台SDK工厂，根据平台创建对应SDK实例
 *
 *****************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

public static class PlatformSDKFactory
{
    private static readonly Dictionary<AdPlatform, Func<IPlatformSDK>> _creators = new Dictionary<AdPlatform, Func<IPlatformSDK>>()
    {
        { AdPlatform.Editor, () => new EditorMockPlatform() },
#if WEIXINMINIGAME
        { AdPlatform.WeChatMiniGame, () => new WeChatPlatform() },
#endif
#if DOUYINMINIGAME
        { AdPlatform.DouyinMiniGame, () => new DouyinPlatform() },
#endif
#if UNITY_WEBGL && !WEIXINMINIGAME && !DOUYINMINIGAME && !CRAZYGAMES
        { AdPlatform.Web, () => new WebPlatform() },
#endif
#if UNITY_ANDROID || UNITY_IOS
        { AdPlatform.Android, () => new MobilePlatform() },
        { AdPlatform.iOS, () => new MobilePlatform() },
#endif
#if CRAZYGAMES
        { AdPlatform.CrazyGames, () => new CrazyGamesPlatform() }
#endif
    };

    public static IPlatformSDK Create(AdPlatform platform)
    {
        if (_creators.TryGetValue(platform, out var creator))
        {
            var sdk = creator();
            Debug.Log($"[PlatformSDKFactory] 创建平台SDK: {sdk.PlatformName}");
            return sdk;
        }

        Debug.LogError($"[PlatformSDKFactory] 不支持的平台: {platform}，降级为Editor模式");
        return new EditorMockPlatform();
    }

    public static void RegisterCreator(AdPlatform platform, Func<IPlatformSDK> creator)
    {
        _creators[platform] = creator;
    }
}
