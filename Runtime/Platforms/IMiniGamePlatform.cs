using System;
using System.Collections.Generic;

namespace MGKit
{
    public interface IMiniGamePlatform
    {
        event Action OnShow;

        event Action OnHide;

        /// <summary>
        /// OnShow 携带的启动/复访参数（抖音为完整字典；其它平台可能为 null）。
        /// </summary>
        event Action<Dictionary<string, object>> OnShowWithOptions;

        void Initialize();

        void Destroy();

        void GetBannerRect(int defaultLeft, int defaultTop, int defaultWidth, int defaultHeight, out int left, out int top, out int width, out int height);

        void ShareApp(string title, string query);

        void OpenCustomerService();

        void PreloadRecommendPage(Action onComplete = null);

        void ShowRecommendPage(Action onSuccess = null, Action<RecommendPageError> onFail = null);

        void ShowRecommendPageWithReward(
            Action onRecommended,
            Action onSuccess = null,
            Action<RecommendPageError> onFail = null);

        void OpenBusinessView(string businessType, Action<string> fail, Action<string> success);

        void VibrateShort();

        void VibrateLong();

        void ReportGameStart();

        /// <summary>检测是否支持侧边栏场景（抖音 CheckScene）。</summary>
        void CheckSidebarSupported(
            Action<bool> onResult,
            Action onComplete = null,
            Action<int, string> onError = null);

        /// <summary>跳转到侧边栏（抖音 NavigateToScene sidebar）。</summary>
        void NavigateToSidebar(
            Action onSuccess = null,
            Action onComplete = null,
            Action<int, string> onError = null);

        /// <summary>
        /// 是否从首页侧边栏卡片进入。
        /// <paramref name="options"/> 为 null 时使用冷启动环境参数（若可用）。
        /// </summary>
        bool IsFromSidebar(IReadOnlyDictionary<string, object> options = null);
    }
}
