/****************************************************
 * FileName:		WebAdAdapter
 * CompanyName:		苏州微游科技有限公司
 * Author:			Felix/李康康
 * Email:			kangkang.li@outlook.com
 * CreateTime:		2026-05-18 10:00:00
 * Version:			1.0
 * UnityVersion:	2022.3.43f1c1
 * Description:		Web平台广告适配器，通过JS插件调用Web广告SDK
 *
*****************************************************/

using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class WebAdAdapter : IAdAdapter
{
    public AdPlatform Platform => AdPlatform.Web;
    public string PlatformName => "Web(H5)";
    public bool IsInitialized { get; private set; }

    public void Initialize()
    {
#if UNITY_WEBGL && !WEIXINMINIGAME && !DOUYINMINIGAME
        IsInitialized = true;
        Debug.Log("[Ad] Web广告适配器初始化完成");
#else
        Debug.LogWarning("[Ad] 当前非WebGL平台，Web广告适配器不可用");
#endif
    }

    public void Dispose()
    {
        IsInitialized = false;
    }

    public IAdUnit CreateAd(AdType type, string adUnitId)
    {
#if UNITY_WEBGL && !WEIXINMINIGAME && !DOUYINMINIGAME
        switch (type)
        {
            case AdType.Banner:
                return new WebBannerAdUnit(adUnitId);
            case AdType.Interstitial:
                return new WebInterstitialAdUnit(adUnitId);
            case AdType.RewardedVideo:
                return new WebRewardedVideoAdUnit(adUnitId);
            case AdType.Custom:
                return new WebCustomAdUnit(adUnitId);
            default:
                return null;
        }
#else
        return null;
#endif
    }

    public bool IsAdSupported(AdType type)
    {
#if UNITY_WEBGL && !WEIXINMINIGAME && !DOUYINMINIGAME
        return true;
#else
        return false;
#endif
    }

#if UNITY_WEBGL && !WEIXINMINIGAME && !DOUYINMINIGAME

    [DllImport("__Internal")]
    private static extern void WebAd_Init();

    [DllImport("__Internal")]
    private static extern void WebAd_Load(string adType, string adUnitId);

    [DllImport("__Internal")]
    private static extern void WebAd_Show(string adType, string adUnitId);

    [DllImport("__Internal")]
    private static extern void WebAd_Hide(string adType, string adUnitId);

    private static string ToJsAdType(AdType type)
    {
        return type switch
        {
            AdType.Banner => "banner",
            AdType.Interstitial => "interstitial",
            AdType.RewardedVideo => "rewardedVideo",
            AdType.Custom => "custom",
            _ => "unknown"
        };
    }

    private abstract class WebAdUnitBase : IAdUnit
    {
        public string AdUnitId { get; }
        public abstract AdType Type { get; }
        public AdState State { get; protected set; }

        public event Action<IAdUnit> OnLoaded;
        public event Action<IAdUnit, string> OnError;
        public event Action<IAdUnit> OnClosed;
        public event Action<IAdUnit> OnClicked;

        protected WebAdUnitBase(string adUnitId)
        {
            AdUnitId = adUnitId;
            State = AdState.None;
        }

        public void Load()
        {
            State = AdState.Loading;
            WebAd_Load(ToJsAdType(Type), AdUnitId);
            // Note: The JS plugin (__Internal) does not report load completion back to C#.
            // We optimistically transition to Loaded so the AdManager flow continues.
            State = AdState.Loaded;
            OnLoaded?.Invoke(this);
        }

        public void Show()
        {
            if (State != AdState.Loaded && State != AdState.Loading) return;
            WebAd_Show(ToJsAdType(Type), AdUnitId);
            State = AdState.Showing;
        }

        public void Hide()
        {
            WebAd_Hide(ToJsAdType(Type), AdUnitId);
        }

        public virtual void Dispose()
        {
            State = AdState.None;
        }

        protected void NotifyError(string msg) => OnError?.Invoke(this, msg);
        protected void NotifyClosed() => OnClosed?.Invoke(this);
        protected void NotifyClicked() => OnClicked?.Invoke(this);
    }

    private class WebBannerAdUnit : WebAdUnitBase, IBannerAdUnit
    {
        public override AdType Type => AdType.Banner;
        public WebBannerAdUnit(string adUnitId) : base(adUnitId) { }
        public void SetPosition(int left, int top) { }
        public void SetSize(int width, int height) { }
    }

    private class WebInterstitialAdUnit : WebAdUnitBase, IInterstitialAdUnit
    {
        public override AdType Type => AdType.Interstitial;
        public WebInterstitialAdUnit(string adUnitId) : base(adUnitId) { }
    }

    private class WebRewardedVideoAdUnit : WebAdUnitBase, IRewardedVideoAdUnit
    {
        public override AdType Type => AdType.RewardedVideo;
        public event Action<IRewardedVideoAdUnit, bool> OnRewarded;
        public WebRewardedVideoAdUnit(string adUnitId) : base(adUnitId) { }

        public override void Dispose()
        {
            OnRewarded = null;
            base.Dispose();
        }
    }

    private class WebCustomAdUnit : WebAdUnitBase, ICustomAdUnit
    {
        public override AdType Type => AdType.Custom;
        public WebCustomAdUnit(string adUnitId) : base(adUnitId) { }
        public void SetPosition(int left, int top) { }
        public void SetSize(int width, int height) { }
    }

#endif
}
