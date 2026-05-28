#if UNITY_EDITOR

namespace MiniGameKit.Editor
{
    /// <summary>WebGL 构建时小游戏原生插件（jslib 等）启用策略。</summary>
    public enum BuildPluginProfile
    {
        /// <summary>启用微信 Runtime Plugins，禁用抖音 WebGL jslib。</summary>
        WeChatMiniGame,

        /// <summary>启用抖音 WebGL jslib，禁用微信 Runtime Plugins。</summary>
        DouyinMiniGame,

        /// <summary>纯 WebGL H5：禁用双端小游戏 jslib。</summary>
        WebGL,
    }
}

#endif
