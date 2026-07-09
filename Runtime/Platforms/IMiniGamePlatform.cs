using System;

namespace MGKit
{
    public interface IMiniGamePlatform
    {
        event Action OnShow;

        event Action OnHide;

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
    }
}