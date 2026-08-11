using System;
using System.Collections.Generic;
using UnityEngine;

#if DOUYINMINIGAME
using TTSDK;
using TTSDK.UNBridgeLib.LitJson;
namespace MGKit
{


public class DouyinPlatform : IPlatformSDK
{
    public MiniGamePlatform Platform => MiniGamePlatform.DouyinMiniGame;
    public string PlatformName => "抖音小游戏";
    public bool IsInitialized { get; private set; }

    public event Action OnShow;
    public event Action OnHide;
    public event Action<Dictionary<string, object>> OnShowWithOptions;

    private TTSDK.TTAppLifeCycle.OnShowEventWithDict _onShowDelegate;
    private TTSDK.TTAppLifeCycle.OnAppHideEvent _onHideDelegate;

    public void Initialize()
    {
        // 早启由 DouyinEarlyInit 负责；此处仅兜底（例如未走到 RuntimeInitialize）
        DouyinEarlyInit.EnsureInit();
        IsInitialized = true;
        Debug.Log("[DouyinPlatform] 抖音大一统SDK初始化完成");

#if !UNITY_EDITOR
        TT.ShowShareMenu();
#endif
        _onShowDelegate = (dict) =>
        {
            OnShowWithOptions?.Invoke(dict);
            OnShow?.Invoke();
        };
        _onHideDelegate = () => { OnHide?.Invoke(); };
        TT.GetAppLifeCycle().OnShow += _onShowDelegate;
        TT.GetAppLifeCycle().OnHide += _onHideDelegate;
    }

    public void Destroy()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (_onShowDelegate != null)
            TT.GetAppLifeCycle().OnShow -= _onShowDelegate;
        if (_onHideDelegate != null)
            TT.GetAppLifeCycle().OnHide -= _onHideDelegate;
        _onShowDelegate = null;
        _onHideDelegate = null;
        IsInitialized = false;
    }

    #region IMiniGamePlatform 实现

    public void GetBannerRect(int defaultLeft, int defaultTop, int defaultWidth, int defaultHeight, out int left, out int top, out int width, out int height)
    {
        left = defaultLeft;
        top = defaultTop;
        width = defaultWidth;
        height = defaultHeight;
    }

    public void ShareApp(string title, string query)
    {
        JsonData shareJson = new JsonData();
        shareJson["title"] = title;
        shareJson["query"] = query;

        TT.ShareAppMessage(shareJson, 
            (data) => { Debug.Log("[DouyinPlatform] 抖音分享成功"); },
            (errMsg) => { Debug.LogWarning($"[DouyinPlatform] 抖音分享失败: {errMsg}"); },
            () => { Debug.Log("[DouyinPlatform] 抖音分享取消"); });
    }

    public void OpenCustomerService()
    {
        // 抖音无此明确对应接口
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
        // 抖音无此明确对应接口
        fail?.Invoke("Not supported on Douyin");
    }

    public void VibrateShort()
    {
        TT.VibrateShort(new TTSDK.VibrateShortParam());
    }

    public void VibrateLong()
    {
        TT.VibrateLong(new TTSDK.VibrateLongParam());
    }

    public void ReportGameStart()
    {
        // 抖音无此明确对应接口
    }

    public void CheckSidebarSupported(
        Action<bool> onResult,
        Action onComplete = null,
        Action<int, string> onError = null)
    {
        TT.CheckScene(
            TTSideBar.SceneEnum.SideBar,
            supported => onResult?.Invoke(supported),
            () => onComplete?.Invoke(),
            (errCode, errMsg) => onError?.Invoke(errCode, errMsg));
    }

    public void NavigateToSidebar(
        Action onSuccess = null,
        Action onComplete = null,
        Action<int, string> onError = null)
    {
        var data = new JsonData { ["scene"] = "sidebar" };
        TT.NavigateToScene(
            data,
            () => onSuccess?.Invoke(),
            () => onComplete?.Invoke(),
            (errCode, errMsg) => onError?.Invoke(errCode, errMsg));
    }

    public bool IsFromSidebar(IReadOnlyDictionary<string, object> options = null)
    {
        if (options != null)
            return PlatformSidebarSupport.IsFromSidebarOptions(options);

        var env = DouyinEarlyInit.Env;
        if (env == null)
            return false;

        return env.GetLaunchFrom() == "homepage" && env.GetLocation() == "sidebar_card";
    }

    #endregion

    #region IAdAdapter 实现

    public IAdUnit CreateAd(AdType type, string adUnitId)
    {
        switch (type)
        {
            case AdType.Banner:
                return new DouyinBannerAdUnit(adUnitId);
            case AdType.Interstitial:
                return new DouyinInterstitialAdUnit(adUnitId);
            case AdType.RewardedVideo:
                return new DouyinRewardedVideoAdUnit(adUnitId);
            case AdType.Custom:
                return new DouyinCustomAdUnit(adUnitId);
            default:
                Debug.LogWarning($"[DouyinPlatform] 不支持的广告类型: {type}");
                return null;
        }
    }

    public bool IsAdSupported(AdType type)
    {
        return type == AdType.Banner || type == AdType.Interstitial
            || type == AdType.RewardedVideo || type == AdType.Custom;
    }

    private class DouyinBannerAdUnit : IBannerAdUnit
    {
        public string AdUnitId { get; }
        public AdType Type => AdType.Banner;
        public AdState State { get; private set; }

        private TTBannerAd _bannerAd;
        private bool _isDestroyed;

        public event Action<IAdUnit> OnLoaded;
        public event Action<IAdUnit, string> OnError;
        public event Action<IAdUnit> OnClosed;
        public event Action<IAdUnit> OnClicked;

        public DouyinBannerAdUnit(string adUnitId)
        {
            AdUnitId = adUnitId;
            State = AdState.None;
        }

        public void Load()
        {
            if (_isDestroyed) return;
            State = AdState.Loading;

            var param = new CreateBannerAdParam { BannerAdId = AdUnitId };
            _bannerAd = TT.CreateBannerAd(param);

            _bannerAd.OnLoad += () =>
            {
                State = AdState.Loaded;
                OnLoaded?.Invoke(this);
            };

            _bannerAd.OnError += (code, msg) =>
            {
                State = AdState.Error;
                OnError?.Invoke(this, $"code:{code} msg:{msg}");
            };

            _bannerAd.OnClose += () =>
            {
                State = AdState.Closed;
                OnClosed?.Invoke(this);
            };
        }

        public void Show(Action onDisplayed = null)
        {
            if (_isDestroyed || _bannerAd == null) return;
            _bannerAd.Show();
            State = AdState.Showing;
            onDisplayed?.Invoke();
        }

        public void Hide()
        {
            if (_isDestroyed || _bannerAd == null) return;
            _bannerAd.Hide();
            State = AdState.Loaded;
        }

        public void SetPosition(int left, int top) { }
        public void SetSize(int width, int height) { }

        public void Dispose()
        {
            _bannerAd = null;
            _isDestroyed = true;
            State = AdState.None;
        }
    }

    private class DouyinInterstitialAdUnit : IInterstitialAdUnit
    {
        public string AdUnitId { get; }
        public AdType Type => AdType.Interstitial;
        public AdState State { get; private set; }

        private TTInterstitialAd _interstitialAd;
        private bool _isDestroyed;

        public event Action<IAdUnit> OnLoaded;
        public event Action<IAdUnit, string> OnError;
        public event Action<IAdUnit> OnClosed;
        public event Action<IAdUnit> OnClicked;

        public DouyinInterstitialAdUnit(string adUnitId)
        {
            AdUnitId = adUnitId;
            State = AdState.None;
        }

        public void Load()
        {
            if (_isDestroyed) return;
            State = AdState.Loading;

            var param = new CreateInterstitialAdParam { InterstitialAdId = AdUnitId };
            _interstitialAd = TT.CreateInterstitialAd(param);

            _interstitialAd.OnLoad += () =>
            {
                State = AdState.Loaded;
                OnLoaded?.Invoke(this);
            };

            _interstitialAd.OnError += (code, msg) =>
            {
                State = AdState.Error;
                OnError?.Invoke(this, $"code:{code} msg:{msg}");
            };

            _interstitialAd.OnClose += () =>
            {
                State = AdState.Closed;
                OnClosed?.Invoke(this);
            };
        }

        public void Show(Action onDisplayed = null)
        {
            if (_isDestroyed || _interstitialAd == null) return;
            _interstitialAd.Show();
            State = AdState.Showing;
            onDisplayed?.Invoke();
        }

        public void Hide() { }

        public void Dispose()
        {
            _interstitialAd = null;
            _isDestroyed = true;
            State = AdState.None;
        }
    }

    private class DouyinRewardedVideoAdUnit : IRewardedVideoAdUnit
    {
        public string AdUnitId { get; }
        public AdType Type => AdType.RewardedVideo;
        public AdState State { get; private set; }

        private TTRewardedVideoAd _rewardedVideoAd;
        private bool _isDestroyed;

        public event Action<IAdUnit> OnLoaded;
        public event Action<IAdUnit, string> OnError;
        public event Action<IAdUnit> OnClosed;
        public event Action<IAdUnit> OnClicked;
        public event Action<IRewardedVideoAdUnit, bool> OnRewarded;

        public DouyinRewardedVideoAdUnit(string adUnitId)
        {
            AdUnitId = adUnitId;
            State = AdState.None;
        }

        public void Load()
        {
            if (_isDestroyed) return;
            State = AdState.Loading;

            var param = new CreateRewardedVideoAdParam { AdUnitId = AdUnitId, Multiton = false };
            _rewardedVideoAd = TT.CreateRewardedVideoAd(param);

            _rewardedVideoAd.OnLoad += () =>
            {
                State = AdState.Loaded;
                OnLoaded?.Invoke(this);
            };

            _rewardedVideoAd.OnError += (code, msg) =>
            {
                State = AdState.Error;
                OnError?.Invoke(this, $"code:{code} msg:{msg}");
            };

            _rewardedVideoAd.OnClose += (ended, count) =>
            {
                State = AdState.Closed;
                OnRewarded?.Invoke(this, ended);
                OnClosed?.Invoke(this);
            };

            _rewardedVideoAd.Load();
        }

        public void Show(Action onDisplayed = null)
        {
            if (_isDestroyed || _rewardedVideoAd == null) return;
            _rewardedVideoAd.Show();
            State = AdState.Showing;
            onDisplayed?.Invoke();
        }

        public void Hide() { }

        public void Dispose()
        {
            _rewardedVideoAd = null;
            _isDestroyed = true;
            State = AdState.None;
        }
    }

    private class DouyinCustomAdUnit : ICustomAdUnit
    {
        public string AdUnitId { get; }
        public AdType Type => AdType.Custom;
        public AdState State { get; private set; }
        private bool _isDestroyed;

        public event Action<IAdUnit> OnLoaded;
        public event Action<IAdUnit, string> OnError;
        public event Action<IAdUnit> OnClosed;
        public event Action<IAdUnit> OnClicked;

        public DouyinCustomAdUnit(string adUnitId)
        {
            AdUnitId = adUnitId;
            State = AdState.None;
        }

        public void Load()
        {
            State = AdState.Loaded;
            OnLoaded?.Invoke(this);
        }

        public void Show(Action onDisplayed = null)
        {
            State = AdState.Showing;
            onDisplayed?.Invoke();
        }
        public void Hide() { }
        public void SetPosition(int left, int top) { }
        public void SetSize(int width, int height) { }

        public void Dispose()
        {
            _isDestroyed = true;
            State = AdState.None;
        }
    }

    #endregion
}
}
#endif
