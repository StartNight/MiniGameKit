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

namespace MGKit
{
    public static class AdPlatformDetector
    {
        /// <summary>
        /// 可选平台覆盖：在 MGKit 初始化前设置，可强制指定广告平台。
        /// 当自动检测无法正确识别平台时使用（例如通过 WX 插件导出微信小游戏）。
        /// </summary>
        public static MiniGamePlatform? PlatformOverride { get; set; }

        public static MiniGamePlatform Detect()
        {
            if (PlatformOverride.HasValue)
                return PlatformOverride.Value;

#if UNITY_EDITOR
            return MiniGamePlatform.Editor;
#elif CRAZYGAMES
            return MiniGamePlatform.CrazyGames;
#elif WEIXINMINIGAME
        return MiniGamePlatform.WeChatMiniGame;
#elif DOUYINMINIGAME
        return MiniGamePlatform.DouyinMiniGame;
#elif UNITY_WEBGL
        return MiniGamePlatform.WebGL;
#elif UNITY_ANDROID
        return MiniGamePlatform.Android;
#elif UNITY_IOS
        return MiniGamePlatform.iOS;
#else
        Debug.LogWarning("[Ad] 未识别的平台，降级为Editor模式");
        return MiniGamePlatform.Editor;
#endif
        }

        public static string GetPlatformName(MiniGamePlatform platform)
        {
            return platform switch
            {
                MiniGamePlatform.Editor => "Editor(模拟)",
                MiniGamePlatform.WeChatMiniGame => "微信小游戏",
                MiniGamePlatform.DouyinMiniGame => "抖音小游戏",
                MiniGamePlatform.WebGL => "Web(H5)",
                MiniGamePlatform.Android => "Android",
                MiniGamePlatform.iOS => "iOS",
                MiniGamePlatform.CrazyGames => "CrazyGames",
                _ => "未知"
            };
        }
    }
}