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
        // WX-WASM-SDK-V2 插件在 WebGL 构建下始终编译 WeChatWASM.WX。
        // 检测到 WX SDK 存在时优先使用微信适配器（兼容 WX 插件导出路径）。
        if (IsWXRuntimePresent())
            return MiniGamePlatform.WeChatMiniGame;

#if CRAZYGAMES
        // 检测 CrazySDK 是否可用
        if (CrazyGames.CrazySDK.IsAvailable)
            return MiniGamePlatform.CrazyGames;
#endif

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

        /// <summary>
        /// 检测当前运行时是否包含微信小游戏环境。
        /// 在 WebGL 构建中，WX SDK 类型始终被静态链接，不能靠 Type.GetType 判断。
        /// 通过 GetSystemInfoSync() 检查 WeChat JS 运行时是否存在来区分纯 WebGL 和微信小游戏。
        /// </summary>
        private static bool IsWXRuntimePresent()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var info = WeChatWASM.WX.GetSystemInfoSync();
            return !string.IsNullOrEmpty(info.platform);
#else
            return System.Type.GetType("WeChatWASM.WX, Wx") != null;
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