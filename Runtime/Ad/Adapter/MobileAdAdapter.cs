/****************************************************
 * FileName:		MobileAdAdapter
 * CompanyName:		苏州微游科技有限公司
 * Author:			Felix/李康康
 * Email:			kangkang.li@outlook.com
 * CreateTime:		2026-05-18 10:00:00
 * Version:			1.0
 * UnityVersion:	2022.3.43f1c1
 * Description:		移动端(安卓/iOS)广告适配器，通过原生插件调用
 *
*****************************************************/

using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class MobileAdAdapter : IAdAdapter
{
    public AdPlatform Platform => GetCurrentPlatform();
    public string PlatformName => IsAndroid() ? "Android" : "iOS";
    public bool IsInitialized { get; private set; }

    private static bool IsAndroid()
    {
#if UNITY_ANDROID
        return true;
#else
        return false;
#endif
    }

    private static bool IsIOS()
    {
#if UNITY_IOS
        return true;
#else
        return false;
#endif
    }

    private static AdPlatform GetCurrentPlatform()
    {
#if UNITY_ANDROID
        return AdPlatform.Android;
#elif UNITY_IOS
        return AdPlatform.iOS;
#else
        return AdPlatform.Editor;
#endif
    }

    public void Initialize()
    {
#if UNITY_ANDROID || UNITY_IOS
        IsInitialized = true;
        Debug.Log($"[Ad] {PlatformName}广告适配器初始化完成");
#else
        Debug.LogWarning("[Ad] 当前非移动端平台，移动端广告适配器不可用");
#endif
    }

    public void Dispose()
    {
        IsInitialized = false;
    }

    public IAdUnit CreateAd(AdType type, string adUnitId)
    {
#if UNITY_ANDROID || UNITY_IOS
        switch (type)
        {
            case AdType.Banner:
                return new MobileBannerAdUnit(adUnitId);
            case AdType.Interstitial:
                return new MobileInterstitialAdUnit(adUnitId);
            case AdType.RewardedVideo:
                return new MobileRewardedVideoAdUnit(adUnitId);
            case AdType.Custom:
                return new MobileCustomAdUnit(adUnitId);
            default:
                return null;
        }
#else
        return null;
#endif
    }

    public bool IsAdSupported(AdType type)
    {
#if UNITY_ANDROID || UNITY_IOS
        return type == AdType.Banner || type == AdType.Interstitial
            || type == AdType.RewardedVideo;
#else
        return false;
#endif
    }

#if UNITY_ANDROID || UNITY_IOS

#if UNITY_ANDROID
    private const string LIB_NAME = "adplugin";
    [DllImport(LIB_NAME)]
    private static extern void AndroidAd_Init();
    [DllImport(LIB_NAME)]
    private static extern void AndroidAd_Load(string adType, string adUnitId);
    [DllImport(LIB_NAME)]
    private static extern void AndroidAd_Show(string adType, string adUnitId);
    [DllImport(LIB_NAME)]
    private static extern void AndroidAd_Hide(string adType, string adUnitId);
    [DllImport(LIB_NAME)]
    private static extern void AndroidAd_Destroy(string adType, string adUnitId);
#endif

#if UNITY_IOS
    [DllImport("__Internal")]
    private static extern void IOSAd_Init();
    [DllImport("__Internal")]
    private static extern void IOSAd_Load(string adType, string adUnitId);
    [DllImport("__Internal")]
    private static extern void IOSAd_Show(string adType, string adUnitId);
    [DllImport("__Internal")]
    private static extern void IOSAd_Hide(string adType, string adUnitId);
    [DllImport("__Internal")]
    private static extern void IOSAd_Destroy(string adType, string adUnitId);
#endif

    private static string ToNativeAdType(AdType type)
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

    private static void NativeInit()
    {
#if UNITY_ANDROID
        AndroidAd_Init();
#elif UNITY_IOS
        IOSAd_Init();
#endif
    }

    private static void NativeLoad(string adType, string adUnitId)
    {
#if UNITY_ANDROID
        AndroidAd_Load(adType, adUnitId);
#elif UNITY_IOS
        IOSAd_Load(adType, adUnitId);
#endif
    }

    private static void NativeShow(string adType, string adUnitId)
    {
#if UNITY_ANDROID
        AndroidAd_Show(adType, adUnitId);
#elif UNITY_IOS
        IOSAd_Show(adType, adUnitId);
#endif
    }

    private static void NativeHide(string adType, string adUnitId)
    {
#if UNITY_ANDROID
        AndroidAd_Hide(adType, adUnitId);
#elif UNITY_IOS
        IOSAd_Hide(adType, adUnitId);
#endif
    }

    private static void NativeDestroy(string adType, string adUnitId)
    {
#if UNITY_ANDROID
        AndroidAd_Destroy(adType, adUnitId);
#elif UNITY_IOS
        IOSAd_Destroy(adType, adUnitId);
#endif
    }

    private abstract class MobileAdUnitBase : IAdUnit
    {
        public string AdUnitId { get; }
        public abstract AdType Type { get; }
        public AdState State { get; protected set; }

        public event Action<IAdUnit> OnLoaded;
        public event Action<IAdUnit, string> OnError;
        public event Action<IAdUnit> OnClosed;
        public event Action<IAdUnit> OnClicked;

        protected MobileAdUnitBase(string adUnitId)
        {
            AdUnitId = adUnitId;
            State = AdState.None;
        }

        public void Load()
        {
            State = AdState.Loading;
            NativeLoad(ToNativeAdType(Type), AdUnitId);
            State = AdState.Loaded;
            OnLoaded?.Invoke(this);
        }

        public void Show()
        {
            if (State != AdState.Loaded) return;
            NativeShow(ToNativeAdType(Type), AdUnitId);
            State = AdState.Showing;
        }

        public void Hide()
        {
            NativeHide(ToNativeAdType(Type), AdUnitId);
        }

        public virtual void Dispose()
        {
            NativeDestroy(ToNativeAdType(Type), AdUnitId);
            State = AdState.None;
        }
    }

    private class MobileBannerAdUnit : MobileAdUnitBase, IBannerAdUnit
    {
        public override AdType Type => AdType.Banner;
        public MobileBannerAdUnit(string adUnitId) : base(adUnitId) { }
        public void SetPosition(int left, int top) { }
        public void SetSize(int width, int height) { }
    }

    private class MobileInterstitialAdUnit : MobileAdUnitBase, IInterstitialAdUnit
    {
        public override AdType Type => AdType.Interstitial;
        public MobileInterstitialAdUnit(string adUnitId) : base(adUnitId) { }
    }

    private class MobileRewardedVideoAdUnit : MobileAdUnitBase, IRewardedVideoAdUnit
    {
        public override AdType Type => AdType.RewardedVideo;
        public event Action<IRewardedVideoAdUnit, bool> OnRewarded;
        public MobileRewardedVideoAdUnit(string adUnitId) : base(adUnitId) { }

        public override void Dispose()
        {
            OnRewarded = null;
            base.Dispose();
        }
    }

    private class MobileCustomAdUnit : MobileAdUnitBase, ICustomAdUnit
    {
        public override AdType Type => AdType.Custom;
        public MobileCustomAdUnit(string adUnitId) : base(adUnitId) { }
        public void SetPosition(int left, int top) { }
        public void SetSize(int width, int height) { }
    }

#endif
}
