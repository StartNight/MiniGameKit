namespace MGKit
{
    /// <summary>
    /// MiniGameKit 平台 Scripting Define 名称。勿使用 WEIXINMINIGAME（与微信 WASM 插件导出路径检测冲突）。
    /// </summary>
    public static class MGKitScriptingDefines
    {
        public const string WeChat = "MGKIT_WECHAT";
        public const string Douyin = "DOUYINMINIGAME";
        public const string CrazyGames = "CRAZYGAMES";

        /// <summary>已废弃：微信官方插件用此名判断 Bee 目录，与 Unity 2022.3 WebGL 导出冲突。</summary>
        public const string LegacyWeChatPluginMacro = "WEIXINMINIGAME";
    }
}
