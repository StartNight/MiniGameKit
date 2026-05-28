/****************************************************
 * FileName:		AdPlatformDetector
 * CompanyName:		苏州微游科技有限公司
 * Author:			Felix/李康康
 * Email:			kangkang.li@outlook.com
 * CreateTime:		2026-05-18 10:00:00
 * Version:			1.0
 * UnityVersion:	2022.3.43f1c1
 * Description:		运行时广告平台自动检测
 *
*****************************************************/

using UnityEngine;

public static class AdPlatformDetector
{
    public static AdPlatform Detect()
    {
#if UNITY_EDITOR
        return AdPlatform.Editor;
#elif WEIXINMINIGAME
        return AdPlatform.WeChatMiniGame;
#elif DOUYINMINIGAME
        return AdPlatform.DouyinMiniGame;
#elif UNITY_WEBGL
        return AdPlatform.Web;
#elif UNITY_ANDROID
        return AdPlatform.Android;
#elif UNITY_IOS
        return AdPlatform.iOS;
#else
        Debug.LogWarning("[Ad] 未识别的平台，降级为Editor模式");
        return AdPlatform.Editor;
#endif
    }

    public static string GetPlatformName(AdPlatform platform)
    {
        return platform switch
        {
            AdPlatform.Editor => "Editor(模拟)",
            AdPlatform.WeChatMiniGame => "微信小游戏",
            AdPlatform.DouyinMiniGame => "抖音小游戏",
            AdPlatform.Web => "Web(H5)",
            AdPlatform.Android => "Android",
            AdPlatform.iOS => "iOS",
            _ => "未知"
        };
    }
}
