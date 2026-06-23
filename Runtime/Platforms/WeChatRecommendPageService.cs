#if MGKIT_WECHAT
using System;
using UnityEngine;
using WeChatWASM;

namespace MGKit
{
    /// <summary>微信推荐组件（wx.createPageManager）平台层实现。</summary>
    internal sealed class WeChatRecommendPageService
    {
        private const string RecommendOpenLink =
            "TWFRCqV5WeM2AkMXhKwJ03MhfPOieJfAsvXKUbWvQFQtLyyA5etMPabBehga950uzfZcH3Vi3QeEh41xRGEVFw";

        private WXPageManager _pageManager;
        private bool _isLoaded;
        private Action _onRecommendSuccess;

        public bool IsSupported => WX.CanIUse("createPageManager");

        public void Load(Action onComplete = null)
        {
            if (!IsSupported)
            {
                Debug.LogWarning("[WeChatPlatform] 当前基础库版本暂不支持推荐。");
                onComplete?.Invoke();
                return;
            }

            if (_isLoaded && _pageManager != null)
            {
                onComplete?.Invoke();
                return;
            }

            _pageManager = WX.CreatePageManager();
            RegisterDestroyListener();

            _pageManager.Load(new LoadOption
            {
                openlink = RecommendOpenLink,
                success = _ =>
                {
                    _isLoaded = true;
                    onComplete?.Invoke();
                },
                fail = err =>
                {
                    Debug.LogWarning($"[WeChatPlatform] 推荐组件加载失败: {err?.errMsg}");
                    _isLoaded = false;
                    onComplete?.Invoke();
                },
            });
        }

        public void Show(Action onSuccess, Action<RecommendPageError> onFail)
        {
            _onRecommendSuccess = null;
            ShowInternal(onSuccess, onFail);
        }

        public void ShowWithReward(
            Action onRecommended,
            Action onSuccess,
            Action<RecommendPageError> onFail)
        {
            _onRecommendSuccess = onRecommended;
            ShowInternal(onSuccess, onFail);
        }

        private void ShowInternal(Action onSuccess, Action<RecommendPageError> onFail)
        {
            if (!IsSupported)
            {
                Debug.LogWarning("[WeChatPlatform] 当前基础库版本暂不支持推荐。");
                onFail?.Invoke(RecommendPageError.Unsupported);
                return;
            }

            if (_pageManager == null || !_isLoaded)
            {
                Load(() =>
                {
                    if (_pageManager == null || !_isLoaded)
                    {
                        onFail?.Invoke(RecommendPageError.LoadFailed);
                        return;
                    }

                    ShowInternal(onSuccess, onFail);
                });
                return;
            }

            RegisterDestroyListener();

            _pageManager.Show(new ShowOption
            {
                success = _ => onSuccess?.Invoke(),
                fail = err =>
                {
                    Debug.LogWarning($"[WeChatPlatform] 推荐组件展示失败: {err?.errMsg}");
                    onFail?.Invoke(RecommendPageError.ShowFailed);
                },
            });
        }

        private void RegisterDestroyListener()
        {
            if (_pageManager == null)
                return;

            _pageManager.Off("destroy");
            _pageManager.On("destroy", res =>
            {
                Debug.Log($"[WeChatPlatform] recommend component destroy: {res.isRecommended}");
                if (res.isRecommended && _onRecommendSuccess != null)
                {
                    var cb = _onRecommendSuccess;
                    _onRecommendSuccess = null;
                    cb.Invoke();
                }
            });
        }
    }
}
#endif
