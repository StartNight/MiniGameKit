namespace MGKit
{
    /// <summary>
    /// 各平台大厅 / 设置页能力开关，收敛项目内分散的 #if 分支。
    /// </summary>
    public static class PlatformFeatureFlags
    {
        public static bool ShowLobbyShareButton
        {
            get
            {
#if CRAZYGAMES
                return false;
#else
                return true;
#endif
            }
        }

        public static bool ShowLobbyFriendPkButton
        {
            get
            {
#if CRAZYGAMES
                return false;
#else
                return false;
#endif
            }
        }

        public static bool ShowLobbyWeChatCallbackButton
        {
            get
            {
#if CRAZYGAMES
                return false;
#else
                return true;
#endif
            }
        }

        public static bool ShowPrivacyPolicyInSettings
        {
            get
            {
#if CRAZYGAMES
                return true;
#else
                return false;
#endif
            }
        }

        public static bool ReportGameplayStopOnLobbyReturn
        {
            get
            {
#if CRAZYGAMES && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }
    }
}