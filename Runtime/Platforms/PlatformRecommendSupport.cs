using System;

namespace MGKit
{
    internal static class PlatformRecommendSupport
    {
        internal static void PreloadNoOp(Action onComplete = null)
        {
            onComplete?.Invoke();
        }

        internal static void ShowUnsupported(Action onSuccess, Action<RecommendPageError> onFail)
        {
            onFail?.Invoke(RecommendPageError.Unsupported);
        }

        internal static void ShowWithRewardUnsupported(
            Action onRecommended,
            Action onSuccess,
            Action<RecommendPageError> onFail)
        {
            onFail?.Invoke(RecommendPageError.Unsupported);
        }

        internal static void ShowWithRewardEditorMock(
            Action onRecommended,
            Action onSuccess,
            Action<RecommendPageError> onFail)
        {
            UnityEngine.Debug.Log("[EditorMockPlatform] 模拟推荐组件完成");
            onSuccess?.Invoke();
            onRecommended?.Invoke();
        }
    }
}
