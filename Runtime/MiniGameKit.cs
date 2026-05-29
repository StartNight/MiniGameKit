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
using System.Runtime.InteropServices;
using UnityEngine;

#if UNITY_WEBGL || WEIXINMINIGAME
using WeChatWASM;
#endif

#if DOUYINMINIGAME
using TTSDK;
using TTSDK.UNBridgeLib.LitJson;
#endif

/// <summary>
/// 兼容微信和抖音的小游戏工具包
/// </summary>
public class MiniGameKit : Singleton<MiniGameKit>
{
    private bool isDestroyed = false;
    private string _bannerAdUnitId;

    // 小游戏生命周期事件，方便各业务系统订阅
    public static event Action OnMiniGameShow;
    public static event Action OnMiniGameHide;

    public override void AwakeOf()
    {
        base.AwakeOf();
        AdManager.Instance.Initialize();
#if DOUYINMINIGAME
        TT.InitSDK();
#endif
    }

    private void Start()
    {
        Debug.Log("[MiniGameKit] Start");
#if UNITY_WEBGL || WEIXINMINIGAME
        WX.ShowShareMenu(new ShowShareMenuOption() { });
        WX.OnShow((res) => { OnMiniGameShow?.Invoke(); });
        WX.OnHide((res) => { OnMiniGameHide?.Invoke(); });
#endif
#if DOUYINMINIGAME
        TT.ShowShareMenu();
        TT.OnShow((res) => { OnMiniGameShow?.Invoke(); });
        TT.OnHide((res) => { OnMiniGameHide?.Invoke(); });
#endif
    }

    #region 广告接口 - 委托给 AdManager

    /// <summary>
    /// 当前平台是否支持广告（仅微信/抖音小游戏有实际广告）
    /// </summary>
    private static bool IsMiniGamePlatform()
    {
        if (!AdManager.Instance.IsInitialized) return false;
        var p = AdManager.Instance.CurrentPlatform;
        return p == AdPlatform.WeChatMiniGame || p == AdPlatform.DouyinMiniGame;
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
    /// 展示激励视频广告，看完回调 action (已废弃，建议使用 ShowRewardedVideo)
    /// </summary>
    [Obsolete("Use ShowRewardedVideo instead")]
    public void ShowRewardedVideoAd(string adId, Action action)
    {
        ShowRewardedVideo(adId, (isRewarded) =>
        {
            if (isRewarded)
            {
                action?.Invoke();
            }
        });
    }

    /// <summary>
    /// 展示激励视频广告，看完回调 success (已废弃，建议使用 ShowRewardedVideo)
    /// </summary>
    [Obsolete("Use ShowRewardedVideo instead")]
    public void CreateRewardedVideoAd(string adId, Action<string> success)
    {
        ShowRewardedVideo(adId, (isRewarded) =>
        {
            if (isRewarded)
            {
                success?.Invoke(string.Empty);
            }
        });
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

        var banner = AdManager.Instance.LoadAd(AdType.Banner, adId);
        if (banner is IBannerAdUnit bannerUnit)
        {
            bannerUnit.SetPosition(left, top);
            bannerUnit.SetSize(width, height);
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

    #endregion

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

#if UNITY_WEBGL || WEIXINMINIGAME
        WX.ShareAppMessage(new ShareAppMessageOption()
        {
            title = title,
            query = query
        });
#endif

#if DOUYINMINIGAME
        JsonData shareJson = new JsonData();
        shareJson["title"] = title;
        shareJson["query"] = query;

        TT.ShareAppMessage(shareJson, 
            (data) =>
            {
                Debug.Log("[MiniGameKit] 抖音分享成功");
            },
            (errMsg) =>
            {
                Debug.LogWarning($"[MiniGameKit] 抖音分享失败: {errMsg}");
            },
            () =>
            {
                Debug.Log("[MiniGameKit] 抖音分享取消");
            });
#endif
    }

    /// <summary>
    /// 打开微信客服会话
    /// </summary>
    public void OpenCustomerService()
    {
#if UNITY_WEBGL || WEIXINMINIGAME
        WX.OpenCustomerServiceConversation(new OpenCustomerServiceConversationOption()
        {
            success = (s) =>
            {
                Debug.Log("[MiniGameKit] 打开微信客服会话成功");
            },
            fail = (res) =>
            {
                Debug.LogError($"[MiniGameKit] 打开微信客服会话失败: {res.errMsg}");
            }
        });
#endif
    }

    /// <summary>
    /// 打开微信特定业务场景面板（如客服会话、游戏评星等）
    /// </summary>
    public void OpenBusinessView(string businessType = "servicecommentpage", Action<string> fail = null, Action<string> success = null)
    {
#if UNITY_WEBGL || WEIXINMINIGAME
        WX.OpenBusinessView(new OpenBusinessViewOption()
        {
            businessType = businessType,
            fail = (s) =>
            {
                Debug.LogWarning($"[MiniGameKit] OpenBusinessView 失败: {s.errMsg}");
                fail?.Invoke(s.errMsg);
            },
            success = (s) =>
            {
                Debug.Log("[MiniGameKit] OpenBusinessView 成功");
                success?.Invoke(s.ToString());
            }
        });
#endif
    }

    #endregion

    #region 震动接口

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void Vibrate(int duration);
#endif

    /// <summary>
    /// 短震动（轻微震动反馈）
    /// </summary>
    public void VibrateShort()
    {
#if UNITY_WEBGL || WEIXINMINIGAME
        WX.VibrateShort(new VibrateShortOption()
        {
            type = "heavy",
            success = (s) => { },
            fail = (s) => { },
            complete = (s) => { }
        });
#elif DOUYINMINIGAME
        TT.VibrateShort(new TTSDK.VibrateShortOption()
        {
            type = "heavy"
        });
#elif UNITY_WEBGL && !UNITY_EDITOR
        Vibrate(15);
#elif UNITY_IOS || UNITY_ANDROID
        Handheld.Vibrate();
#endif
    }

    /// <summary>
    /// 长震动（明显震动反馈）
    /// </summary>
    public void VibrateLong()
    {
#if UNITY_WEBGL || WEIXINMINIGAME
        WX.VibrateLong(new VibrateLongOption()
        {
            success = (s) => { },
            fail = (s) => { },
            complete = (s) => { }
        });
#elif DOUYINMINIGAME
        TT.VibrateLong(new TTSDK.VibrateLongOption()
        {
            success = (s) => { },
            fail = (s) => { },
            complete = (s) => { }
        });
#elif UNITY_WEBGL && !UNITY_EDITOR
        Vibrate(400);
#elif UNITY_IOS || UNITY_ANDROID
        Handheld.Vibrate();
#endif
    }

    #endregion

    #region 平台兼容保留接口

    /// <summary>
    /// 上报游戏开始状态 (微信专用)
    /// </summary>
    public void WXReportGameStart()
    {
#if UNITY_WEBGL || WEIXINMINIGAME
        WX.ReportGameStart();
#endif
    }

    /// <summary>
    /// 微信小游戏 OnShow 事件注册 (已废弃，请直接订阅 MiniGameKit.OnMiniGameShow 事件)
    /// </summary>
    [Obsolete("Use MiniGameKit.OnMiniGameShow event instead")]
    public void WXOnShow()
    {
    }

    #endregion

    private void OnDestroy()
    {
        isDestroyed = true;
    }
}
