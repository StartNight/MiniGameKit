/****************************************************
 * FileName:		WebPlatform
 * CompanyName:		苏州微游科技有限公司
 * Author:			Felix/李康康
 * Email:			kangkang.li@outlook.com
 * CreateTime:		2026-06-01 10:00:00
 * Version:			1.0
 * UnityVersion:	2022.3.43f1c1
 * Description:		Web平台SDK(大一统接口实现)，包含JS插件调用
 *
 *****************************************************/

#if UNITY_WEBGL && !WEIXINMINIGAME && !DOUYINMINIGAME && !CRAZYGAMES
using System;
using System.Runtime.InteropServices;
using UnityEngine;
namespace MGKit
{


public class WebPlatform : IPlatformSDK
{
    public MiniGamePlatform Platform => MiniGamePlatform.WebGL;
    public string PlatformName => "Web(H5)";
    public bool IsInitialized { get; private set; }

    public event Action OnShow;
    public event Action OnHide;

    public void Initialize()
    {
        IsInitialized = true;
        Debug.Log("[WebPlatform] Web大一统SDK初始化完成");
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
        Debug.Log($"[WebPlatform] 模拟分享 App: title={title}, query={query}");
    }

    public void OpenCustomerService()
    {
        Debug.LogWarning("[WebPlatform] 平台暂不支持打开客服");
    }

    public void OpenBusinessView(string businessType, Action<string> fail, Action<string> success)
    {
        fail?.Invoke("Not supported on Web");
    }

    public void VibrateShort()
    {
        Debug.Log("[WebPlatform] 模拟短震动");
    }

    public void VibrateLong()
    {
        Debug.Log("[WebPlatform] 模拟长震动");
    }

    public void ReportGameStart()
    {
        Debug.Log("[WebPlatform] 模拟上报游戏开始");
    }

    #endregion

    #region IAdAdapter 实现

    public IAdUnit CreateAd(AdType type, string adUnitId)
    {
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
    }

    public bool IsAdSupported(AdType type)
    {
        return true;
    }

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

    #endregion
}
#endif
