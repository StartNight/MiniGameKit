using System;
using UnityEngine;

#if MGKIT_WECHAT
using WeChatWASM;
namespace MGKit
{


public class WeChatPlatform : IPlatformSDK
{
    public MiniGamePlatform Platform => MiniGamePlatform.WeChatMiniGame;
    public string PlatformName => "微信小游戏";
    public bool IsInitialized { get; private set; }

    public event Action OnShow;
    public event Action OnHide;

    private Action<WeChatWASM.OnShowListenerResult> _onShowDelegate;
    private Action<WeChatWASM.GeneralCallbackResult> _onHideDelegate;
    private readonly WeChatRecommendPageService _recommendPage = new WeChatRecommendPageService();

    public void Initialize()
    {
        IsInitialized = true;
        Debug.Log("[WeChatPlatform] 微信小游戏大一统SDK初始化完成");

#if !UNITY_EDITOR
        WX.ShowShareMenu(new ShowShareMenuOption() { });
#endif
        _onShowDelegate = (res) => { OnShow?.Invoke(); };
        _onHideDelegate = (res) => { OnHide?.Invoke(); };
        WX.OnShow(_onShowDelegate);
        WX.OnHide(_onHideDelegate);
    }

    public void Destroy()
    {
        Dispose();
    }

    public void Dispose()
    {
        IsInitialized = false;
        if (_onShowDelegate != null) WX.OffShow(_onShowDelegate);
        if (_onHideDelegate != null) WX.OffHide(_onHideDelegate);
    }

    #region IMiniGamePlatform 实现

    public void GetBannerRect(int defaultLeft, int defaultTop, int defaultWidth, int defaultHeight, out int left, out int top, out int width, out int height)
    {
        left = defaultLeft;
        top = defaultTop;
        width = defaultWidth;
        height = defaultHeight;

        var sysInfo = WX.GetSystemInfoSync();
        if (sysInfo != null)
        {
            width = (int)sysInfo.windowWidth;
            height = width * 300 / 1080;
            left = (int)((sysInfo.windowWidth - width) / 2);
            top = (int)sysInfo.windowHeight - height;
        }
    }

    public void ShareApp(string title, string query)
    {
        WX.ShareAppMessage(new ShareAppMessageOption()
        {
            title = title,
            query = query
        });
    }

    public void OpenCustomerService()
    {
        WX.OpenCustomerServiceConversation(new OpenCustomerServiceConversationOption()
        {
            success = (s) => { Debug.Log("[WeChatPlatform] 打开微信客服会话成功"); },
            fail = (res) => { Debug.LogError($"[WeChatPlatform] 打开微信客服会话失败: {res.errMsg}"); }
        });
    }

    public void PreloadRecommendPage(Action onComplete = null)
    {
        _recommendPage.Load(onComplete);
    }

    public void ShowRecommendPage(Action onSuccess = null, Action<RecommendPageError> onFail = null)
    {
        _recommendPage.Show(onSuccess, onFail);
    }

    public void ShowRecommendPageWithReward(
        Action onRecommended,
        Action onSuccess = null,
        Action<RecommendPageError> onFail = null)
    {
        _recommendPage.ShowWithReward(onRecommended, onSuccess, onFail);
    }

    public void OpenBusinessView(string businessType, Action<string> fail, Action<string> success)
    {
        WX.OpenBusinessView(new OpenBusinessViewOption()
        {
            businessType = businessType,
            fail = (s) =>
            {
                Debug.LogWarning($"[WeChatPlatform] OpenBusinessView 失败: {s.errMsg}");
                fail?.Invoke(s.errMsg);
            },
            success = (s) =>
            {
                Debug.Log("[WeChatPlatform] OpenBusinessView 成功");
                success?.Invoke(s.ToString());
            }
        });
    }

    public void VibrateShort()
    {
        WX.VibrateShort(new VibrateShortOption()
        {
            type = "heavy"
        });
    }

    public void VibrateLong()
    {
        WX.VibrateLong(new VibrateLongOption());
    }

    public void ReportGameStart()
    {
        WX.ReportGameStart();
    }

    internal static void PrepareForFullscreenAdOverlay()
    {
        WeChatAdInputCleanup.PrepareBeforeFullscreenAd();
    }

    internal static void CleanupAfterFullscreenAdOverlay()
    {
        WeChatAdInputCleanup.CleanupAfterFullscreenAd();
    }

    #endregion

    #region IAdAdapter 实现

    public IAdUnit CreateAd(AdType type, string adUnitId)
    {
        switch (type)
        {
            case AdType.Banner:
                return new WeChatBannerAdUnit(adUnitId);
            case AdType.Interstitial:
                return new WeChatInterstitialAdUnit(adUnitId);
            case AdType.RewardedVideo:
                return new WeChatRewardedVideoAdUnit(adUnitId);
            case AdType.Custom:
                return new WeChatCustomAdUnit(adUnitId);
            default:
                Debug.LogWarning($"[WeChatPlatform] 不支持的广告类型: {type}");
                return null;
        }
    }

    public bool IsAdSupported(AdType type)
    {
        return type == AdType.Banner || type == AdType.Interstitial
            || type == AdType.RewardedVideo || type == AdType.Custom;
    }

    private class WeChatBannerAdUnit : IBannerAdUnit
    {
        public string AdUnitId { get; }
        public AdType Type => AdType.Banner;
        public AdState State { get; private set; }

        private WXBannerAd _bannerAd;
        private bool _isDestroyed;

        public event Action<IAdUnit> OnLoaded;
        public event Action<IAdUnit, string> OnError;
        public event Action<IAdUnit> OnClosed;
        public event Action<IAdUnit> OnClicked;

        private int _left, _top, _width, _height;

        public WeChatBannerAdUnit(string adUnitId)
        {
            AdUnitId = adUnitId;
            State = AdState.None;
            _left = 0;
            _top = 1620;
            _width = 1080;
            _height = 300;
        }

        public void Load()
        {
            if (_isDestroyed) return;
            if (_bannerAd != null) return;
            State = AdState.Loading;

            var windowInfo = WX.GetWindowInfo();
            _left = 0;
            _top = (int)windowInfo.windowHeight - _height;
            _width = (int)windowInfo.windowWidth;

            _bannerAd = WX.CreateBannerAd(new WXCreateBannerAdParam()
            {
                adUnitId = AdUnitId,
                adIntervals = 30,
                style = new Style()
                {
                    left = _left,
                    top = _top,
                    width = _width,
                    height = _height
                }
            });

            _bannerAd.OnLoad(res =>
            {
                if (_isDestroyed) return;
                State = AdState.Loaded;
                OnLoaded?.Invoke(this);
            });

            _bannerAd.OnError(err =>
            {
                if (_isDestroyed) return;
                State = AdState.Error;
                OnError?.Invoke(this, err.errMsg);
                _bannerAd = null;
            });

            _bannerAd.OnResize(res =>
            {
                if (_isDestroyed || _bannerAd == null) return;
                var winfo = WX.GetWindowInfo();
                _top = (int)winfo.windowHeight - (int)res.height;
                _width = (int)res.width;
                _height = (int)res.height;
                _bannerAd.style.top = _top;
                _bannerAd.style.left = 0;
                _bannerAd.style.width = _width;
                _bannerAd.style.height = _height;
            });
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

        public void SetPosition(int left, int top) { _left = left; _top = top; }
        public void SetSize(int width, int height) { _width = width; _height = height; }

        public void Dispose()
        {
            if (_bannerAd != null)
            {
                _bannerAd.Destroy();
                _bannerAd = null;
            }
            _isDestroyed = true;
            State = AdState.None;
        }
    }

    private class WeChatInterstitialAdUnit : IInterstitialAdUnit
    {
        public string AdUnitId { get; }
        public AdType Type => AdType.Interstitial;
        public AdState State { get; private set; }

        private WXInterstitialAd _interstitialAd;
        private bool _isDestroyed;

        public event Action<IAdUnit> OnLoaded;
        public event Action<IAdUnit, string> OnError;
        public event Action<IAdUnit> OnClosed;
        public event Action<IAdUnit> OnClicked;

        public WeChatInterstitialAdUnit(string adUnitId)
        {
            AdUnitId = adUnitId;
            State = AdState.None;
        }

        public void Load()
        {
            if (_isDestroyed) return;
            State = AdState.Loading;

            _interstitialAd = WX.CreateInterstitialAd(new WXCreateInterstitialAdParam()
            {
                adUnitId = AdUnitId
            });

            _interstitialAd.OnLoad(res =>
            {
                State = AdState.Loaded;
                OnLoaded?.Invoke(this);
            });

            _interstitialAd.OnError(res =>
            {
                State = AdState.Error;
                OnError?.Invoke(this, res.errMsg);
            });

            _interstitialAd.OnClose(() =>
            {
                WeChatPlatform.CleanupAfterFullscreenAdOverlay();
                State = AdState.Closed;
                OnClosed?.Invoke(this);
            });
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
            if (_interstitialAd != null)
            {
                _interstitialAd.Destroy();
                _interstitialAd = null;
            }
            _isDestroyed = true;
            State = AdState.None;
        }
    }

    private class WeChatRewardedVideoAdUnit : IRewardedVideoAdUnit
    {
        public string AdUnitId { get; }
        public AdType Type => AdType.RewardedVideo;
        public AdState State { get; private set; }

        private WXRewardedVideoAd _rewardedVideoAd;
        private bool _isDestroyed;
        private bool _isShowing;
        private int _showRetryCount;
        private int _nativeRecreateRetryCount;
        private Action _pendingDisplayedCallback;

        public event Action<IAdUnit> OnLoaded;
        public event Action<IAdUnit, string> OnError;
        public event Action<IAdUnit> OnClosed;
        public event Action<IAdUnit> OnClicked;
        public event Action<IRewardedVideoAdUnit, bool> OnRewarded;

        public WeChatRewardedVideoAdUnit(string adUnitId)
        {
            AdUnitId = adUnitId;
            State = AdState.None;
        }

        public void Load()
        {
            if (_isDestroyed) return;
            State = AdState.Loading;
            EnsureNativeAdCreated();
            RequestNativeLoad(isPostClosePreload: false);
        }

        public void Show(Action onDisplayed = null)
        {
            if (_isDestroyed) return;

            if (_rewardedVideoAd == null)
            {
                Debug.LogWarning($"[WeChatRewardedVideoAd] 实例未创建，先加载: adUnitId={AdUnitId}");
                _pendingDisplayedCallback = onDisplayed;
                Load();
                return;
            }

            if (State == AdState.Loading)
            {
                Debug.Log($"[WeChatRewardedVideoAd] 加载中，排队 Show: adUnitId={AdUnitId}");
                _pendingDisplayedCallback = onDisplayed;
                return;
            }

            if (_isShowing)
            {
                Debug.LogWarning($"[WeChatRewardedVideoAd] 广告正在播放中，忽略重复 Show: adUnitId={AdUnitId}");
                return;
            }

            if (State != AdState.Loaded)
            {
                Debug.LogWarning($"[WeChatRewardedVideoAd] 状态不可展示，重新加载: adUnitId={AdUnitId}, state={State}");
                _pendingDisplayedCallback = onDisplayed;
                Load();
                return;
            }

            WeChatPlatform.PrepareForFullscreenAdOverlay();

            _rewardedVideoAd.Show(
                _ =>
                {
                    if (_isDestroyed) return;
                    _showRetryCount = 0;
                    _nativeRecreateRetryCount = 0;
                    _isShowing = true;
                    State = AdState.Showing;
                    Debug.Log($"[WeChatRewardedVideoAd] Show成功: adUnitId={AdUnitId}");
                    onDisplayed?.Invoke();
                },
                err =>
                {
                    if (_isDestroyed) return;
                    _isShowing = false;
                    WeChatPlatform.CleanupAfterFullscreenAdOverlay();

                    if (_showRetryCount < 1)
                    {
                        _showRetryCount++;
                        Debug.LogWarning($"[WeChatRewardedVideoAd] Show失败，重新加载后重试: adUnitId={AdUnitId}, err={err?.errMsg}");
                        State = AdState.Loading;
                        _pendingDisplayedCallback = onDisplayed;
                        RequestNativeLoad(isPostClosePreload: false);
                        return;
                    }

                    State = AdState.Error;
                    OnError?.Invoke(this, err?.errMsg ?? "激励视频展示失败");
                });
        }

        public void Hide() { }

        public void Dispose()
        {
            _isDestroyed = true;
            ResetNativeAd();
            State = AdState.None;
        }

        private void EnsureNativeAdCreated()
        {
            if (_rewardedVideoAd != null) return;

            _rewardedVideoAd = WX.CreateRewardedVideoAd(new WXCreateRewardedVideoAdParam()
            {
                adUnitId = AdUnitId
            });

            _rewardedVideoAd.OnError(err =>
            {
                if (_isDestroyed) return;
                _isShowing = false;
                HandleNativeFailure(err?.errMsg ?? "激励视频错误", isPostClosePreload: false);
            });

            _rewardedVideoAd.OnClose(s =>
            {
                if (_isDestroyed) return;
                _isShowing = false;
                bool isEnded = s == null || s.isEnded;
                Debug.Log($"[WeChatRewardedVideoAd] OnClose adUnitId={AdUnitId}, isEnded={isEnded}, closeResultNull={s == null}");
                State = AdState.Closed;
                OnRewarded?.Invoke(this, isEnded);
                OnClosed?.Invoke(this);
                WeChatPlatform.CleanupAfterFullscreenAdOverlay();
                State = AdState.Loading;
                RequestNativeLoad(isPostClosePreload: true);
            });
        }

        private void RequestNativeLoad(bool isPostClosePreload)
        {
            if (_isDestroyed || _rewardedVideoAd == null) return;

            _rewardedVideoAd.Load(
                _ =>
                {
                    if (_isDestroyed) return;
                    _nativeRecreateRetryCount = 0;
                    State = AdState.Loaded;
                    OnLoaded?.Invoke(this);

                    if (_pendingDisplayedCallback != null)
                    {
                        var pending = _pendingDisplayedCallback;
                        _pendingDisplayedCallback = null;
                        Show(pending);
                    }
                },
                err =>
                {
                    if (_isDestroyed) return;
                    HandleNativeFailure(err?.errMsg ?? (isPostClosePreload ? "激励视频预加载失败" : "激励视频加载失败"), isPostClosePreload);
                });
        }

        private void HandleNativeFailure(string error, bool isPostClosePreload)
        {
            if (_isDestroyed) return;

            if (_nativeRecreateRetryCount < 1 && IsDestroyedNativeError(error))
            {
                _nativeRecreateRetryCount++;
                Debug.LogWarning($"[WeChatRewardedVideoAd] native 已销毁，重建后重试: adUnitId={AdUnitId}, err={error}");
                ResetNativeAd();
                State = AdState.Loading;
                EnsureNativeAdCreated();
                RequestNativeLoad(isPostClosePreload);
                return;
            }

            State = AdState.Error;
            _pendingDisplayedCallback = null;
            OnError?.Invoke(this, error);
        }

        private void ResetNativeAd()
        {
            if (_rewardedVideoAd != null)
            {
                _rewardedVideoAd.Destroy();
                _rewardedVideoAd = null;
            }

            _isShowing = false;
            _showRetryCount = 0;
            _nativeRecreateRetryCount = 0;
            _pendingDisplayedCallback = null;
        }

        private static bool IsDestroyedNativeError(string error)
        {
            return !string.IsNullOrEmpty(error)
                && error.IndexOf("destroyed", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    private class WeChatCustomAdUnit : ICustomAdUnit
    {
        public string AdUnitId { get; }
        public AdType Type => AdType.Custom;
        public AdState State { get; private set; }

        private WXCustomAd _customAd;
        private bool _isDestroyed;

        public event Action<IAdUnit> OnLoaded;
        public event Action<IAdUnit, string> OnError;
        public event Action<IAdUnit> OnClosed;
        public event Action<IAdUnit> OnClicked;

        private int _left, _top, _width;

        public WeChatCustomAdUnit(string adUnitId)
        {
            AdUnitId = adUnitId;
            State = AdState.None;
        }

        public void Load()
        {
            if (_isDestroyed) return;
            State = AdState.Loading;

            _customAd = WX.CreateCustomAd(new WXCreateCustomAdParam()
            {
                adUnitId = AdUnitId,
                style = new CustomStyle()
                {
                    left = _left,
                    top = _top,
                    width = _width > 0 ? _width : 350
                }
            });

            _customAd.OnLoad((res) =>
            {
                State = AdState.Loaded;
                OnLoaded?.Invoke(this);
            });

            _customAd.OnError(err =>
            {
                State = AdState.Error;
                OnError?.Invoke(this, err.errMsg);
            });

            _customAd.OnClose(() =>
            {
                State = AdState.Closed;
                OnClosed?.Invoke(this);
            });
        }

        public void Show(Action onDisplayed = null)
        {
            if (_isDestroyed || _customAd == null) return;
            _customAd.Show();
            State = AdState.Showing;
            onDisplayed?.Invoke();
        }

        public void Hide() { }

        public void SetPosition(int left, int top) { _left = left; _top = top; }
        public void SetSize(int width, int height) { _width = width; }

        public void Dispose()
        {
            if (_customAd != null)
            {
                _customAd.Destroy();
                _customAd = null;
            }
            _isDestroyed = true;
            State = AdState.None;
        }
    }

    #endregion
}
}

#endif
