using System;
using System.Collections.Generic;
using UnityEngine;

namespace MGKit
{
    public class EditorMockPlatform : IPlatformSDK
    {
        public MiniGamePlatform Platform => MiniGamePlatform.Editor;
        public string PlatformName => "Editor(模拟)";
        public bool IsInitialized { get; private set; }

        public event Action OnShow;

        public event Action OnHide;

        public event Action<Dictionary<string, object>> OnShowWithOptions;

        public void Initialize()
        {
            IsInitialized = true;
            Debug.Log("[EditorMockPlatform] 大一统SDK 初始化完成.");
        }

        public void Destroy()
        {
            Dispose();
        }

        public void Dispose()
        {
            IsInitialized = false;
            Debug.Log("[EditorMockPlatform] 大一统SDK Destroyed.");
        }

        #region IMiniGamePlatform 实现

        public void GetBannerRect(int defaultLeft, int defaultTop, int defaultWidth, int defaultHeight, out int left, out int top, out int width, out int height)
        {
            // 在编辑器中返回标准 1080x1920 比例下的默认坐标
            left = defaultLeft;
            top = defaultTop;
            width = defaultWidth;
            height = defaultHeight;
        }

        public void ShareApp(string title, string query)
        {
            Debug.Log($"[EditorMockPlatform] 模拟分享 App: title={title}, query={query}");
        }

        public void OpenCustomerService()
        {
            Debug.Log("[EditorMockPlatform] 模拟打开客服界面");
        }

        public void PreloadRecommendPage(Action onComplete = null)
        {
            PlatformRecommendSupport.PreloadNoOp(onComplete);
        }

        public void ShowRecommendPage(Action onSuccess = null, Action<RecommendPageError> onFail = null)
        {
            Debug.Log("[EditorMockPlatform] 模拟打开推荐组件");
            onSuccess?.Invoke();
        }

        public void ShowRecommendPageWithReward(
            Action onRecommended,
            Action onSuccess = null,
            Action<RecommendPageError> onFail = null)
        {
            PlatformRecommendSupport.ShowWithRewardEditorMock(onRecommended, onSuccess, onFail);
        }

        public void OpenBusinessView(string businessType, Action<string> fail, Action<string> success)
        {
            Debug.Log($"[EditorMockPlatform] 模拟打开业务视图: {businessType}");
            success?.Invoke("success_mock");
        }

        public void VibrateShort()
        {
            Debug.Log("[EditorMockPlatform] 模拟短震动");
#if UNITY_IOS || UNITY_ANDROID
        Handheld.Vibrate();
#endif
        }

        public void VibrateLong()
        {
            Debug.Log("[EditorMockPlatform] 模拟长震动");
#if UNITY_IOS || UNITY_ANDROID
        Handheld.Vibrate();
#endif
        }

        public void ReportGameStart()
        {
            Debug.Log("[EditorMockPlatform] 模拟上报游戏开始");
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
            return PlatformSidebarSupport.IsFromSidebarOptions(options);
        }

        // 提供测试方法，方便在 Editor 里模拟微信的 OnShow 和 OnHide
        public void MockTriggerOnShow() => OnShow?.Invoke();

        public void MockTriggerOnHide() => OnHide?.Invoke();

        public void MockTriggerOnShowWithOptions(Dictionary<string, object> options)
        {
            OnShowWithOptions?.Invoke(options);
            OnShow?.Invoke();
        }

        #endregion IMiniGamePlatform 实现

        #region IAdAdapter 实现

        public IAdUnit CreateAd(AdType type, string adUnitId)
        {
            return new EditorAdUnit(type, adUnitId);
        }

        public bool IsAdSupported(AdType type)
        {
            return true;
        }

        private class EditorAdUnit : IAdUnit, IBannerAdUnit, IInterstitialAdUnit, IRewardedVideoAdUnit, ICustomAdUnit
        {
            public string AdUnitId { get; }
            public AdType Type { get; }
            public AdState State { get; private set; }

            public event Action<IAdUnit> OnLoaded;

            public event Action<IAdUnit, string> OnError;

            public event Action<IAdUnit> OnClosed;

            public event Action<IAdUnit> OnClicked;

            public event Action<IRewardedVideoAdUnit, bool> OnRewarded;

            public EditorAdUnit(AdType type, string adUnitId)
            {
                Type = type;
                AdUnitId = adUnitId;
                State = AdState.None;
            }

            public void Load()
            {
                State = AdState.Loading;
                Debug.Log($"[Ad-Editor] 加载 {Type} 广告: {AdUnitId}");
                State = AdState.Loaded;
                OnLoaded?.Invoke(this);
            }

            public void Show(Action onDisplayed = null)
            {
                if (State != AdState.Loaded)
                {
                    Debug.LogWarning($"[Ad-Editor] 广告未加载，无法展示: {Type}");
                    return;
                }
                State = AdState.Showing;
                Debug.Log($"[Ad-Editor] 展示 {Type} 广告: {AdUnitId}");
                onDisplayed?.Invoke();

                // 如果是激励视频，直接模拟看完并发放奖励
                if (Type == AdType.RewardedVideo)
                {
                    Debug.Log($"[Ad-Editor] 模拟激励视频看完回调...");
                    State = AdState.Closed;
                    OnRewarded?.Invoke(this, true);
                    OnClosed?.Invoke(this);
                }
            }

            public void Hide()
            {
                Debug.Log($"[Ad-Editor] 隐藏 {Type} 广告: {AdUnitId}");
            }

            public void SetPosition(int left, int top)
            { }

            public void SetSize(int width, int height)
            { }

            public void Dispose()
            {
                State = AdState.None;
            }
        }

        #endregion IAdAdapter 实现
    }
}