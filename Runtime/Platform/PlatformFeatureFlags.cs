/****************************************************
 * FileName:		PlatformFeatureFlags
 * Description:		各平台大厅 / 设置页能力开关，从项目 PlatformFeatureConfig 读取
 *
*****************************************************/

namespace MGKit
{
    /// <summary>
    /// 各平台大厅 / 设置页能力开关，收敛项目内分散的 #if 分支。
    /// 配置见 Assets/Resources/MGKit/PlatformFeatureConfig.asset。
    /// </summary>
    public static class PlatformFeatureFlags
    {
        private static PlatformFeatureConfig _config;
        private static PlatformFeatureProfile _cachedProfile;
        private static MiniGamePlatform _cachedPlatform = (MiniGamePlatform)(-1);
        private static bool _hasCachedProfile;

        private static PlatformFeatureConfig _testConfig;
        private static MiniGamePlatform? _testPlatform;

        public static void Reload()
        {
            _config = null;
            _hasCachedProfile = false;
            _cachedPlatform = (MiniGamePlatform)(-1);
        }

#if UNITY_INCLUDE_TESTS
        public static void SetTestContext(PlatformFeatureConfig config, MiniGamePlatform platform)
        {
            _testConfig = config;
            _testPlatform = platform;
            Reload();
        }

        public static void ClearTestContext()
        {
            _testConfig = null;
            _testPlatform = null;
            Reload();
        }
#endif

        public static bool ShowLobbyShareButton => ActiveProfile.showLobbyShareButton;

        public static bool ShowLobbyFriendPkButton => ActiveProfile.showLobbyFriendPkButton;

        public static bool ShowLobbyWeChatCallbackButton => ActiveProfile.showLobbyWeChatCallbackButton;

        public static bool ShowPrivacyPolicyInSettings => ActiveProfile.showPrivacyPolicyInSettings;

        public static bool ReportGameplayStopOnLobbyReturn => ActiveProfile.reportGameplayStopOnLobbyReturn;

        public static bool ShowSidebarRevisitEntry => ActiveProfile.showSidebarRevisitEntry;

        private static PlatformFeatureProfile ActiveProfile
        {
            get
            {
                var platform = ResolvePlatform();
                if (_hasCachedProfile && _cachedPlatform == platform)
                    return _cachedProfile;

                var config = _testConfig ?? (_config ??= PlatformFeatureConfig.Load());
                _cachedPlatform = platform;
                _cachedProfile = config != null
                    ? config.GetProfile(platform)
                    : PlatformFeaturePresets.GetDefault(platform);
                _hasCachedProfile = true;
                return _cachedProfile;
            }
        }

        private static MiniGamePlatform ResolvePlatform()
        {
#if UNITY_INCLUDE_TESTS
            if (_testPlatform.HasValue)
                return _testPlatform.Value;
#endif

            var config = _testConfig ?? (_config ??= PlatformFeatureConfig.Load());
            if (config != null && config.platformOverride != default)
                return config.platformOverride;

            if (AdManager.Instance != null
                && AdManager.Instance.IsInitialized
                && AdManager.Instance.Config != null
                && AdManager.Instance.Config.CurrentPlatform != default)
            {
                return AdManager.Instance.Config.CurrentPlatform;
            }

            return AdPlatformDetector.Detect();
        }
    }
}
