/****************************************************
 * FileName:		WeChatAdAdapter
 * CompanyName:		苏州微游科技有限公司
 * Author:			Felix/李康康
 * Email:			kangkang.li@outlook.com
 * CreateTime:		2026-05-18 10:00:00
 * Version:			1.0
 * UnityVersion:	2022.3.43f1c1
 * Description:		微信小游戏平台广告适配器
 *
*****************************************************/

using System;
using UnityEngine;

#if UNITY_WEBGL || WEIXINMINIGAME
using WeChatWASM;
#endif

public class WeChatAdAdapter : IAdAdapter
{
    public AdPlatform Platform => AdPlatform.WeChatMiniGame;
    public string PlatformName => "微信小游戏";
    public bool IsInitialized { get; private set; }

    public void Initialize()
    {
#if UNITY_WEBGL || WEIXINMINIGAME
        IsInitialized = true;
        Debug.Log("[Ad] 微信小游戏广告适配器初始化完成");
#else
        Debug.LogWarning("[Ad] 当前未定义WEIXINMINIGAME宏，微信广告适配器不可用");
#endif
    }

    public void Dispose()
    {
        IsInitialized = false;
    }

    public IAdUnit CreateAd(AdType type, string adUnitId)
    {
#if UNITY_WEBGL || WEIXINMINIGAME
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
                Debug.LogWarning($"[Ad-WeChat] 不支持的广告类型: {type}");
                return null;
        }
#else
        return null;
#endif
    }

    public bool IsAdSupported(AdType type)
    {
#if UNITY_WEBGL || WEIXINMINIGAME
        return type == AdType.Banner || type == AdType.Interstitial
            || type == AdType.RewardedVideo || type == AdType.Custom;
#else
        return false;
#endif
    }

#if UNITY_WEBGL || WEIXINMINIGAME

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
            if (_bannerAd != null) return; // 复用已创建的实例
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
                Debug.Log("[Ad-WeChat] Banner广告加载成功");
                OnLoaded?.Invoke(this);
            });

            _bannerAd.OnError(err =>
            {
                if (_isDestroyed) return;
                State = AdState.Error;
                Debug.LogError($"[Ad-WeChat] Banner广告加载失败: {err.errMsg}");
                OnError?.Invoke(this, err.errMsg);
                _bannerAd = null;
            });

            _bannerAd.OnResize(res =>
            {
                if (_isDestroyed || _bannerAd == null) return;
                // 拉取的广告可能跟设置的不一样，需要动态调整位置
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

        public void Show()
        {
            if (_isDestroyed || _bannerAd == null) return;
            _bannerAd.Show();
            State = AdState.Showing;
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
                Debug.Log("[Ad-WeChat] 插屏广告加载成功");
                OnLoaded?.Invoke(this);
            });

            _interstitialAd.OnError(res =>
            {
                State = AdState.Error;
                Debug.LogError($"[Ad-WeChat] 插屏广告错误: {res.errMsg}");
                OnError?.Invoke(this, res.errMsg);
            });

            _interstitialAd.OnClose(() =>
            {
                State = AdState.Closed;
                OnClosed?.Invoke(this);
            });
        }

        public void Show()
        {
            if (_isDestroyed || _interstitialAd == null) return;
            _interstitialAd.Show();
            State = AdState.Showing;
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

            if (_rewardedVideoAd == null)
            {
                _rewardedVideoAd = WX.CreateRewardedVideoAd(new WXCreateRewardedVideoAdParam()
                {
                    adUnitId = AdUnitId
                });

                _rewardedVideoAd.OnClose(s =>
                {
                    State = AdState.Closed;
                    OnRewarded?.Invoke(this, s.isEnded);
                    OnClosed?.Invoke(this);
                    // 预加载下一次广告（复用同一实例，关闭后重新加载）
                    _rewardedVideoAd.Load(loadRes =>
                    {
                        State = AdState.Loaded;
                        Debug.Log("[Ad-WeChat] 激励视频预加载成功");
                        OnLoaded?.Invoke(this);
                    }, loadRes =>
                    {
                        State = AdState.Error;
                        Debug.LogWarning("[Ad-WeChat] 激励视频预加载失败，下次调用时将重试");
                    });
                });
            }

            _rewardedVideoAd.Load(s =>
            {
                State = AdState.Loaded;
                Debug.Log("[Ad-WeChat] 激励视频加载成功");
                OnLoaded?.Invoke(this);
            }, s =>
            {
                State = AdState.Error;
                Debug.LogError("[Ad-WeChat] 激励视频加载失败");
                OnError?.Invoke(this, "激励视频加载失败");
            });
        }

        public void Show()
        {
            if (_isDestroyed || _rewardedVideoAd == null) return;
            _rewardedVideoAd.Show();
            State = AdState.Showing;
        }

        public void Hide() { }

        public void Dispose()
        {
            if (_rewardedVideoAd != null)
            {
                _rewardedVideoAd.Destroy();
                _rewardedVideoAd = null;
            }
            _isDestroyed = true;
            State = AdState.None;
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

        public void Show()
        {
            if (_isDestroyed || _customAd == null) return;
            _customAd.Show();
            State = AdState.Showing;
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

#endif
}
