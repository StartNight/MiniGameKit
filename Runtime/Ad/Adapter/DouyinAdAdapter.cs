/****************************************************
 * FileName:		DouyinAdAdapter
 * CompanyName:		苏州微游科技有限公司
 * Author:			Felix/李康康
 * Email:			kangkang.li@outlook.com
 * CreateTime:		2026-05-18 10:00:00
 * Version:			1.0
 * UnityVersion:	2022.3.43f1c1
 * Description:		抖音小游戏平台广告适配器
 *
*****************************************************/

using System;
using UnityEngine;

#if DOUYINMINIGAME
using TTSDK;
#endif

public class DouyinAdAdapter : IAdAdapter
{
    public AdPlatform Platform => AdPlatform.DouyinMiniGame;
    public string PlatformName => "抖音小游戏";
    public bool IsInitialized { get; private set; }

    public void Initialize()
    {
#if DOUYINMINIGAME
        IsInitialized = true;
        Debug.Log("[Ad] 抖音小游戏广告适配器初始化完成");
#else
        Debug.LogWarning("[Ad] 当前未定义DOUYINMINIGAME宏，抖音广告适配器不可用");
#endif
    }

    public void Dispose()
    {
        IsInitialized = false;
    }

    public IAdUnit CreateAd(AdType type, string adUnitId)
    {
#if DOUYINMINIGAME
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
                Debug.LogWarning($"[Ad-Douyin] 不支持的广告类型: {type}");
                return null;
        }
#else
        return null;
#endif
    }

    public bool IsAdSupported(AdType type)
    {
#if DOUYINMINIGAME
        return type == AdType.Banner || type == AdType.Interstitial
            || type == AdType.RewardedVideo || type == AdType.Custom;
#else
        return false;
#endif
    }

#if DOUYINMINIGAME

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

            var param = new CreateBannerAdParam { AdUnitId = AdUnitId };
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

        public void SetPosition(int left, int top)
        {
            // TODO: TT SDK does not expose banner position change after creation.
            // If CreateBannerAdParam supports style, set it during Load().
        }

        public void SetSize(int width, int height)
        {
            // TODO: TT SDK does not expose banner resize after creation.
        }

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

            var param = new CreateInterstitialAdParam { AdUnitId = AdUnitId };
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

        public void Show()
        {
            if (_isDestroyed || _interstitialAd == null) return;
            _interstitialAd.Show();
            State = AdState.Showing;
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
                Debug.Log("[Ad-Douyin] 激励视频加载成功");
                OnLoaded?.Invoke(this);
            };

            _rewardedVideoAd.OnError += (code, msg) =>
            {
                State = AdState.Error;
                Debug.LogError($"[Ad-Douyin] 激励视频错误: code={code}");
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

        public void Show()
        {
            if (_isDestroyed || _rewardedVideoAd == null) return;
            _rewardedVideoAd.Show();
            State = AdState.Showing;
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
            // TT SDK has no "Custom Ad" type. This is a stub for interface compatibility.
            State = AdState.Loaded;
            OnLoaded?.Invoke(this);
        }

        public void Show() { State = AdState.Showing; }
        public void Hide() { }
        public void SetPosition(int left, int top) { }
        public void SetSize(int width, int height) { }

        public void Dispose()
        {
            _isDestroyed = true;
            State = AdState.None;
        }
    }

#endif
}
