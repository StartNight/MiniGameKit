/****************************************************
 * FileName:		CrazyGamesPlatform
 * CompanyName:		苏州微游科技有限公司
 * Author:			Felix/李康康
 * CreateTime:		2026-06-01 10:00:00
 * Version:			1.0
 * UnityVersion:	2022.3.43f1c1
 * Description:		CrazyGames平台SDK(大一统接口实现)
 *
 *****************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

#if CRAZYGAMES

using CrazyGames;

namespace MGKit
{
    public class CrazyGamesPlatform : IPlatformSDK
    {
        /// <summary>SDK locale detected after init (e.g. en-US, zh-CN). Subscribe from game code for I2.</summary>
        public static event Action<string> OnLocaleReady;

        public MiniGamePlatform Platform => MiniGamePlatform.CrazyGames;
        public string PlatformName => "CrazyGames";
        public bool IsInitialized { get; private set; }

        public event Action OnShow;

        public event Action OnHide;

        public event Action<Dictionary<string, object>> OnShowWithOptions;

        public void Initialize()
        {
            CrazySDK.Init(() =>
            {
                IsInitialized = true;
                ApplyCrazyGamesLocale();
                Debug.Log("[CrazyGamesPlatform] 大一统SDK初始化完成");
            });
        }

        private static void ApplyCrazyGamesLocale()
        {
            try
            {
                var systemInfo = CrazySDK.User.SystemInfo;
                var locale = systemInfo != null && !string.IsNullOrEmpty(systemInfo.locale)
                    ? systemInfo.locale
                    : "en-US";
                OnLocaleReady?.Invoke(locale);
                Debug.Log($"[CrazyGamesPlatform] SDK locale: {locale}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CrazyGamesPlatform] ApplyCrazyGamesLocale failed: {ex.Message}");
            }
        }

        public void Destroy()
        {
            Dispose();
        }

        public void Dispose()
        {
            IsInitialized = false;
        }

        #region IMiniGamePlatform 实现 (Fallbacks)

        public void GetBannerRect(int defaultLeft, int defaultTop, int defaultWidth, int defaultHeight, out int left, out int top, out int width, out int height)
        {
            left = defaultLeft;
            top = defaultTop;
            width = defaultWidth;
            height = defaultHeight;
        }

        public void ShareApp(string title, string query)
        {
            Debug.Log($"[CrazyGamesPlatform] 模拟分享 App: title={title}");
            // CrazyGames 也有自己的分享接口，如果有需要这里可以接 CrazySDK.Game.InviteLink()
        }

        public void OpenCustomerService()
        {
            Debug.LogWarning("[CrazyGamesPlatform] 不支持打开客服");
        }

        public void PreloadRecommendPage(Action onComplete = null)
        {
            PlatformRecommendSupport.PreloadNoOp(onComplete);
        }

        public void ShowRecommendPage(Action onSuccess = null, Action<RecommendPageError> onFail = null)
        {
            PlatformRecommendSupport.ShowUnsupported(onSuccess, onFail);
        }

        public void ShowRecommendPageWithReward(
            Action onRecommended,
            Action onSuccess = null,
            Action<RecommendPageError> onFail = null)
        {
            PlatformRecommendSupport.ShowWithRewardUnsupported(onRecommended, onSuccess, onFail);
        }

        public void OpenBusinessView(string businessType, Action<string> fail, Action<string> success)
        {
            fail?.Invoke("Not supported on CrazyGames");
        }

        public void VibrateShort()
        {
#if UNITY_IOS || UNITY_ANDROID
        Handheld.Vibrate();
#else
            Debug.Log("[CrazyGamesPlatform] 模拟短震动");
#endif
        }

        public void VibrateLong()
        {
#if UNITY_IOS || UNITY_ANDROID
        Handheld.Vibrate();
#else
            Debug.Log("[CrazyGamesPlatform] 模拟长震动");
#endif
        }

        public void ReportGameStart()
        {
            CrazySDK.Game.GameplayStart();
        }

        public void ReportGameStop()
        {
            CrazySDK.Game.GameplayStop();
        }

        public void CheckSidebarSupported(
            Action<bool> onResult,
            Action onComplete = null,
            Action<int, string> onError = null)
        {
            PlatformSidebarSupport.CheckUnsupported(onResult, onComplete, onError);
        }

        public void NavigateToSidebar(
            Action onSuccess = null,
            Action onComplete = null,
            Action<int, string> onError = null)
        {
            PlatformSidebarSupport.NavigateUnsupported(onSuccess, onComplete, onError);
        }

        public bool IsFromSidebar(IReadOnlyDictionary<string, object> options = null)
        {
            return false;
        }

        #endregion IMiniGamePlatform 实现 (Fallbacks)

        #region IAdAdapter 实现

        public IAdUnit CreateAd(AdType type, string adUnitId)
        {
            switch (type)
            {
                case AdType.Banner:
                    return new CrazyGamesBannerAdUnit(adUnitId);

                case AdType.Interstitial:
                    return new CrazyGamesInterstitialAdUnit(adUnitId);

                case AdType.RewardedVideo:
                    return new CrazyGamesRewardedVideoAdUnit(adUnitId);

                default:
                    Debug.LogWarning($"[CrazyGamesPlatform] 不支持的广告类型: {type}");
                    return null;
            }
        }

        public bool IsAdSupported(AdType type)
        {
            return type == AdType.Banner || type == AdType.Interstitial || type == AdType.RewardedVideo;
        }

        /// <summary>
        /// Basic Launch 阶段 CrazyGames 会禁用所有广告（错误码 adsDisabledBasicLaunch）。
        /// 激励视频在此阶段应直接发奖，避免按钮可点但无效果导致 QA 拒审。
        /// </summary>
        private static bool IsAdsDisabledBasicLaunch(SdkError error)
        {
            return error != null && error.code == "adsDisabledBasicLaunch";
        }

        private class CrazyGamesBannerAdUnit : IBannerAdUnit
        {
            public string AdUnitId { get; }
            public AdType Type => AdType.Banner;
            public AdState State { get; private set; }

            public event Action<IAdUnit> OnLoaded;

            public event Action<IAdUnit, string> OnError;

            public event Action<IAdUnit> OnClosed;

            public event Action<IAdUnit> OnClicked;

            private CrazyBanner _banner;
            private GameObject _bannerGo;
            private int _left, _top, _width, _height;

            public CrazyGamesBannerAdUnit(string adUnitId)
            {
                AdUnitId = adUnitId;
                State = AdState.None;
            }

            public void Load()
            {
                if (State == AdState.Loading || State == AdState.Loaded) return;
                State = AdState.Loading;

                var prefab = Resources.Load<GameObject>("CrazyBanner");
                if (prefab == null)
                {
                    State = AdState.Error;
                    OnError?.Invoke(this, "CrazyBanner prefab not found in Resources");
                    return;
                }

                _bannerGo = UnityEngine.Object.Instantiate(prefab);
                _banner = _bannerGo.GetComponent<CrazyBanner>();

                _bannerGo.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(_bannerGo);

                State = AdState.Loaded;
                OnLoaded?.Invoke(this);
            }

            public void Show(Action onDisplayed = null)
            {
                if (_bannerGo == null) return;
                _bannerGo.SetActive(true);
                State = AdState.Showing;
                CrazySDK.Banner.RefreshBanners();
                onDisplayed?.Invoke();
            }

            public void Hide()
            {
                if (_bannerGo == null) return;
                _bannerGo.SetActive(false);
                State = AdState.Loaded;
            }

            public void SetPosition(int left, int top)
            {
                _left = left;
                _top = top;
                if (_banner != null)
                {
                    _banner.Position = new Vector2(left, -top);
                }
            }

            public void SetSize(int width, int height)
            {
                _width = width;
                _height = height;
                if (_banner != null)
                {
                    if (width >= 728) _banner.Size = CrazyBanner.BannerSize.Leaderboard_728x90;
                    else if (width >= 468) _banner.Size = CrazyBanner.BannerSize.Main_Banner_468x60;
                    else if (width >= 320 && height >= 100) _banner.Size = CrazyBanner.BannerSize.Large_Mobile_320x100;
                    else if (width >= 320) _banner.Size = CrazyBanner.BannerSize.Mobile_320x50;
                    else _banner.Size = CrazyBanner.BannerSize.Medium_300x250;
                }
            }

            public void Dispose()
            {
                if (_bannerGo != null)
                {
                    UnityEngine.Object.Destroy(_bannerGo);
                    _bannerGo = null;
                    _banner = null;
                }
                State = AdState.None;
            }
        }

        private class CrazyGamesInterstitialAdUnit : IInterstitialAdUnit
        {
            public string AdUnitId { get; }
            public AdType Type => AdType.Interstitial;
            public AdState State { get; private set; }

            public event Action<IAdUnit> OnLoaded;

            public event Action<IAdUnit, string> OnError;

            public event Action<IAdUnit> OnClosed;

            public event Action<IAdUnit> OnClicked;

            public CrazyGamesInterstitialAdUnit(string adUnitId)
            {
                AdUnitId = adUnitId;
                State = AdState.None;
            }

            public void Load()
            {
                if (State == AdState.Loading || State == AdState.Loaded) return;
                State = AdState.Loading;
                CrazySDK.Ad.PrefetchAd(CrazyAdType.Midgame);
                State = AdState.Loaded;
                OnLoaded?.Invoke(this);
            }

            public void Show(Action onDisplayed = null)
            {
                if (State != AdState.Loaded) return;

                State = AdState.Showing;
                onDisplayed?.Invoke();
                CrazySDK.Ad.RequestAd(CrazyAdType.Midgame,
                    () => { },
                    (error) =>
                    {
                        State = AdState.Error;
                        OnError?.Invoke(this, error.message);
                    },
                    () =>
                    {
                        State = AdState.Closed;
                        OnClosed?.Invoke(this);
                    }
                );
            }

            public void Hide()
            { }

            public void Dispose()
            {
                State = AdState.None;
            }
        }

        private class CrazyGamesRewardedVideoAdUnit : IRewardedVideoAdUnit
        {
            public string AdUnitId { get; }
            public AdType Type => AdType.RewardedVideo;
            public AdState State { get; private set; }

            public event Action<IAdUnit> OnLoaded;

            public event Action<IAdUnit, string> OnError;

            public event Action<IAdUnit> OnClosed;

            public event Action<IAdUnit> OnClicked;

            public event Action<IRewardedVideoAdUnit, bool> OnRewarded;

            public CrazyGamesRewardedVideoAdUnit(string adUnitId)
            {
                AdUnitId = adUnitId;
                State = AdState.None;
            }

            public void Load()
            {
                if (State == AdState.Loading || State == AdState.Loaded) return;
                State = AdState.Loading;
                CrazySDK.Ad.PrefetchAd(CrazyAdType.Rewarded);
                State = AdState.Loaded;
                OnLoaded?.Invoke(this);
            }

            public void Show(Action onDisplayed = null)
            {
                if (State != AdState.Loaded) return;

                State = AdState.Showing;
                onDisplayed?.Invoke();
                CrazySDK.Ad.RequestAd(CrazyAdType.Rewarded,
                    () => { },
                    (error) =>
                    {
                        if (IsAdsDisabledBasicLaunch(error))
                        {
                            Debug.Log("[CrazyGamesPlatform] Basic Launch 阶段广告不可用，直接发放激励奖励");
                            State = AdState.Closed;
                            OnRewarded?.Invoke(this, true);
                            OnClosed?.Invoke(this);
                            return;
                        }

                        State = AdState.Error;
                        OnError?.Invoke(this, error.message);
                    },
                    () =>
                    {
                        State = AdState.Closed;
                        OnRewarded?.Invoke(this, true);
                        OnClosed?.Invoke(this);
                    }
                );
            }

            public void Hide()
            { }

            public void Dispose()
            {
                State = AdState.None;
            }
        }

        #endregion IAdAdapter 实现
    }
}

#endif