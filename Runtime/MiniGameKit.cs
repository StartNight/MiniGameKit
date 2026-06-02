/****************************************************
 * FileName:		MiniGameKit
 * CompanyName:		苏州微游科技有限公司
 * Author:			Felix/李康康
 * Email:			kangkang.li@outlook.com
 * CreateTime:		2025-01-04 14:12:28
 * Version:			2.1
 * UnityVersion:	2022.3.43f1c1
 * Description:		兼容微信和抖音的小游戏工具包，广告逻辑委托给AdManager
 *
 *****************************************************/

using System;
using UnityEngine;

/// <summary>
/// 兼容微信和抖音的小游戏工具包
/// </summary>
namespace MGKit
{
    public class MiniGameKit : Singleton<MiniGameKit>
    {
        private IPlatformSDK _currentPlatform => AdManager.Instance.PlatformSDK;
        private bool isDestroyed = false;
        private string _bannerAdUnitId;

        // 小游戏生命周期事件，方便各业务系统订阅
        public static event Action OnMiniGameShow;

        public static event Action OnMiniGameHide;

        public override void AwakeOf()
        {
            base.AwakeOf();
            AdManager.Instance.Initialize();

            if (_currentPlatform != null)
            {
                _currentPlatform.OnShow += () => OnMiniGameShow?.Invoke();
                _currentPlatform.OnHide += () => OnMiniGameHide?.Invoke();
            }
        }

        private void Start()
        {
            Debug.Log("[MGKit] Start");
            // 初始化移到了 AwakeOf 的 _currentPlatform.Initialize()
        }

        #region 广告接口 - 委托给 AdManager

        /// <summary>
        /// 当前平台是否支持广告（仅微信/抖音小游戏有实际广告）
        /// </summary>
        private static bool IsMiniGamePlatform()
        {
            if (!AdManager.Instance.IsInitialized) return false;
            var p = AdManager.Instance.CurrentPlatform;
            return p == MiniGamePlatform.WeChatMiniGame || p == MiniGamePlatform.DouyinMiniGame || p == MiniGamePlatform.CrazyGames;
        }

        /// <summary>
        /// 显示插屏广告
        /// </summary>
        public void ShowInterstitialAd(string adId)
        {
            if (!IsMiniGamePlatform()) return;
            AdManager.Instance.ShowAd(AdType.Interstitial, adId);
        }

        /// <summary>
        /// 统一的激励视频展示接口（不干涉 Time.timeScale）
        /// </summary>
        public void ShowRewardedVideo(string adId, Action<bool> onRewardResult)
        {
            if (!IsMiniGamePlatform())
            {
                onRewardResult?.Invoke(true);
                return;
            }
            AdManager.Instance.ShowRewardedVideo(adId, onRewardResult);
        }

        /// <summary>
        /// 创建并加载 Banner 广告
        /// </summary>
        public void CreateBannerAd(string adId, int left = 0, int top = 1620, int width = 1080, int height = 300)
        {
            if (!IsMiniGamePlatform()) return;

            _bannerAdUnitId = adId;
            if (AdManager.Instance.IsInitialized)
            {
                AdManager.Instance.Config.SetAdUnitId(AdType.Banner, AdManager.Instance.CurrentPlatform, adId);
            }

            int bannerLeft = 0, bannerTop = 0, bannerWidth = 0, bannerHeight = 0;
            _currentPlatform?.GetBannerRect(left, top, width, height, out bannerLeft, out bannerTop, out bannerWidth, out bannerHeight);

            var banner = AdManager.Instance.LoadAd(AdType.Banner, adId);
            if (banner is IBannerAdUnit bannerUnit)
            {
                bannerUnit.SetPosition(bannerLeft, bannerTop);
                bannerUnit.SetSize(bannerWidth, bannerHeight);
            }
        }

        /// <summary>
        /// 显示Banner广告
        /// </summary>
        public void BannerAdShow(string adId = null)
        {
            if (!IsMiniGamePlatform()) return;
            AdManager.Instance.ShowAd(AdType.Banner, adId ?? _bannerAdUnitId);
        }

        /// <summary>
        /// 隐藏Banner广告
        /// </summary>
        public void BannerAdHide(string adId = null)
        {
            if (!IsMiniGamePlatform()) return;
            AdManager.Instance.HideAd(AdType.Banner, adId ?? _bannerAdUnitId);
        }

        /// <summary>
        /// 显示自定义广告
        /// </summary>
        public void ShowCustomAd()
        {
            if (!IsMiniGamePlatform()) return;
            AdManager.Instance.ShowAd(AdType.Custom);
        }

        #endregion 广告接口 - 委托给 AdManager

        #region 分享与平台功能

        /// <summary>
        /// 分享 App，直接调出分享界面
        /// </summary>
        public void ShareApp(string title = "", string query = "key1=val1&key2=val2")
        {
            if (string.IsNullOrEmpty(title))
            {
                title = Application.productName;
            }
            _currentPlatform?.ShareApp(title, query);
        }

        /// <summary>
        /// 打开微信客服会话
        /// </summary>
        public void OpenCustomerService()
        {
            _currentPlatform?.OpenCustomerService();
        }

        /// <summary>
        /// 打开特定业务场景面板（如客服会话、游戏评星等）
        /// </summary>
        public void OpenBusinessView(string businessType = "servicecommentpage", Action<string> fail = null, Action<string> success = null)
        {
            _currentPlatform?.OpenBusinessView(businessType, fail, success);
        }

        #endregion 分享与平台功能

        #region 震动接口

        /// <summary>
        /// 短震动（轻微震动反馈）
        /// </summary>
        public void VibrateShort()
        {
            _currentPlatform?.VibrateShort();
        }

        /// <summary>
        /// 长震动（明显震动反馈）
        /// </summary>
        public void VibrateLong()
        {
            _currentPlatform?.VibrateLong();
        }

        #endregion 震动接口

        #region 平台兼容保留接口

        /// <summary>
        /// 上报游戏开始状态
        /// </summary>
        public void WXReportGameStart()
        {
            _currentPlatform?.ReportGameStart();
        }

        /// <summary>
        /// 小游戏 OnShow 事件注册 (已废弃，请直接订阅 MiniGameKit.OnMiniGameShow 事件)
        /// </summary>
        [Obsolete("Use MiniGameKit.OnMiniGameShow event instead")]
        public void WXOnShow()
        {
        }

        #endregion 平台兼容保留接口

        private void OnDestroy()
        {
            isDestroyed = true;
            // PlatformSDK is now disposed by AdManager
        }
    }
}