/****************************************************
 * FileName:		MobilePlatform
 * CompanyName:		苏州微游科技有限公司
 * Author:			Felix/李康康
 * Email:			kangkang.li@outlook.com
 * CreateTime:		2026-06-01 10:00:00
 * Version:			1.0
 * UnityVersion:	2022.3.43f1c1
 * Description:		移动端(安卓/iOS)大一统SDK，原生插件调用
 *
 *****************************************************/

#if UNITY_ANDROID || UNITY_IOS
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
namespace MGKit
{


public class MobilePlatform : IPlatformSDK
{
    public MiniGamePlatform Platform => GetCurrentPlatform();
    public string PlatformName => IsAndroid() ? "Android" : "iOS";
    public bool IsInitialized { get; private set; }

    public event Action OnShow;
    public event Action OnHide;
    public event Action<Dictionary<string, object>> OnShowWithOptions;

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

    private static MiniGamePlatform GetCurrentPlatform()
    {
#if UNITY_ANDROID
        return MiniGamePlatform.Android;
#elif UNITY_IOS
        return MiniGamePlatform.iOS;
#else
        return MiniGamePlatform.Editor;
#endif
    }

    public void Initialize()
    {
        IsInitialized = true;
        Debug.Log($"[MobilePlatform] {PlatformName}大一统SDK初始化完成");
        NativeInit();
    }

    public void Destroy()
    {
        Dispose();
    }

    public void Dispose()
    {
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
        Debug.Log($"[MobilePlatform] 模拟原生分享 App: title={title}, query={query}");
    }

    public void OpenCustomerService()
    {
        Debug.LogWarning("[MobilePlatform] 移动端暂未对接客服接口");
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
        fail?.Invoke("Not supported on Mobile");
    }

    public void VibrateShort()
    {
        Handheld.Vibrate();
    }

    public void VibrateLong()
    {
        Handheld.Vibrate();
    }

    public void ReportGameStart()
    {
        Debug.Log("[MobilePlatform] 模拟上报游戏开始");
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

    #endregion

    #region IAdAdapter 实现

    public IAdUnit CreateAd(AdType type, string adUnitId)
    {
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
    }

    public bool IsAdSupported(AdType type)
    {
        return type == AdType.Banner || type == AdType.Interstitial
            || type == AdType.RewardedVideo;
    }

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
        try { AndroidAd_Init(); } catch { }
#elif UNITY_IOS
        try { IOSAd_Init(); } catch { }
#endif
    }

    private static void NativeLoad(string adType, string adUnitId)
    {
#if UNITY_ANDROID
        try { AndroidAd_Load(adType, adUnitId); } catch { }
#elif UNITY_IOS
        try { IOSAd_Load(adType, adUnitId); } catch { }
#endif
    }

    private static void NativeShow(string adType, string adUnitId)
    {
#if UNITY_ANDROID
        try { AndroidAd_Show(adType, adUnitId); } catch { }
#elif UNITY_IOS
        try { IOSAd_Show(adType, adUnitId); } catch { }
#endif
    }

    private static void NativeHide(string adType, string adUnitId)
    {
#if UNITY_ANDROID
        try { AndroidAd_Hide(adType, adUnitId); } catch { }
#elif UNITY_IOS
        try { IOSAd_Hide(adType, adUnitId); } catch { }
#endif
    }

    private static void NativeDestroy(string adType, string adUnitId)
    {
#if UNITY_ANDROID
        try { AndroidAd_Destroy(adType, adUnitId); } catch { }
#elif UNITY_IOS
        try { IOSAd_Destroy(adType, adUnitId); } catch { }
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

        public void Show(Action onDisplayed = null)
        {
            if (State != AdState.Loaded && State != AdState.Loading) return;
            NativeShow(ToNativeAdType(Type), AdUnitId);
            State = AdState.Showing;
            onDisplayed?.Invoke();
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

    #endregion
}
#endif
