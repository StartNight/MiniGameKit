/****************************************************
 * FileName:		PlatformFeatureProfile
 * Description:		单平台大厅 / 设置页能力开关数据
 *
*****************************************************/

using System;
using System.Collections.Generic;

namespace MGKit
{
    [Serializable]
    public struct PlatformFeatureProfile
    {
        public bool showLobbyShareButton;
        public bool showLobbyFriendPkButton;
        public bool showLobbyWeChatCallbackButton;
        public bool showPrivacyPolicyInSettings;
        public bool reportGameplayStopOnLobbyReturn;
    }

    [Serializable]
    public class PlatformFeatureProfileEntry
    {
        public MiniGamePlatform platform;
        public PlatformFeatureProfile profile;
    }

    /// <summary>
    /// 各平台内置默认值；项目可在 <see cref="PlatformFeatureConfig"/> 中覆盖。
    /// </summary>
    public static class PlatformFeaturePresets
    {
        public static PlatformFeatureProfile GetDefault(MiniGamePlatform platform)
        {
            return platform switch
            {
                MiniGamePlatform.CrazyGames => CrazyGames,
                _ => MiniGameStyle
            };
        }

        /// <summary>微信 / 抖音 / Editor 等小游戏风格平台默认。</summary>
        public static PlatformFeatureProfile MiniGameStyle => new PlatformFeatureProfile
        {
            showLobbyShareButton = true,
            showLobbyFriendPkButton = false,
            showLobbyWeChatCallbackButton = true,
            showPrivacyPolicyInSettings = false,
            reportGameplayStopOnLobbyReturn = false
        };

        public static PlatformFeatureProfile CrazyGames => new PlatformFeatureProfile
        {
            showLobbyShareButton = false,
            showLobbyFriendPkButton = false,
            showLobbyWeChatCallbackButton = false,
            showPrivacyPolicyInSettings = true,
            reportGameplayStopOnLobbyReturn = true
        };

        public static List<PlatformFeatureProfileEntry> CreateAllPlatformEntries()
        {
            var list = new List<PlatformFeatureProfileEntry>();
            foreach (MiniGamePlatform platform in Enum.GetValues(typeof(MiniGamePlatform)))
            {
                list.Add(new PlatformFeatureProfileEntry
                {
                    platform = platform,
                    profile = GetDefault(platform)
                });
            }

            return list;
        }
    }
}
