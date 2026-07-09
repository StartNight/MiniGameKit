#if MGKIT_WECHAT
using System;
using UnityEngine;
using WeChatWASM;

namespace MGKit
{
    /// <summary>
    /// 微信推荐组件（wx.createPageManager）平台层实现。
    /// 参考：https://developers.weixin.qq.com/minigame/dev/guide/open-ability/game-evaluate.html
    /// </summary>
    internal sealed class WeChatRecommendPageService
    {
        private const string RecommendOpenLink =
            "TWFRCqV5WeM2AkMXhKwJ03MhfPOieJfAsvXKUbWvQFQtLyyA5etMPabBehga950uzfZcH3Vi3QeEh41xRGEVFw";


        private WXPageManager _pageManager;
        private bool _isLoaded;
        private bool _isLoading;
        private Action _pendingLoadComplete;
        private Action _onRecommendSuccess;
        private Action<RecommendPageError> _pendingOnFail;

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

            if (_isLoading)
            {
                if (onComplete != null)
                    _pendingLoadComplete += onComplete;
                return;
            }

            _isLoading = true;
            EnsurePageManager();
            _pageManager.Load(new LoadOption
            {
                openlink = RecommendOpenLink,
                success = _ =>
                {
                    if (_pageManager != null)
                        _isLoaded = true;
                    FinishLoad(onComplete);
                },
                fail = err =>
                {
                    Debug.LogWarning($"[WeChatPlatform] 推荐组件加载失败: {err?.errMsg}");
                    InvalidatePageManager();
                    FinishLoad(onComplete);
                },
            });
        }

        private void FinishLoad(Action onComplete)
        {
            _isLoading = false;
            onComplete?.Invoke();
            var pending = _pendingLoadComplete;
            _pendingLoadComplete = null;
            pending?.Invoke();
        }

        public void Show(Action onSuccess, Action<RecommendPageError> onFail)
        {
            _onRecommendSuccess = null;
            ShowInternal(onSuccess, onFail, allowRetry: true);
        }

        public void ShowWithReward(
            Action onRecommended,
            Action onSuccess,
            Action<RecommendPageError> onFail)
        {
            _onRecommendSuccess = onRecommended;
            ShowInternal(onSuccess, onFail, allowRetry: true);
        }

        private void ShowInternal(Action onSuccess, Action<RecommendPageError> onFail, bool allowRetry)
        {
            if (!IsSupported)
            {
                onFail?.Invoke(RecommendPageError.Unsupported);
                return;
            }

            if (_pageManager == null || !_isLoaded)
            {
                Load(() =>
                {
                    if (_pageManager == null || !_isLoaded)
                        onFail?.Invoke(RecommendPageError.LoadFailed);
                    else
                        ShowInternal(onSuccess, onFail, allowRetry);
                });
                return;
            }

            _pendingOnFail = onFail;
            _pageManager.Show(new ShowOption
            {
                openlink = RecommendOpenLink,
                success = _ =>
                {
                    _pendingOnFail = null;
                    onSuccess?.Invoke();
                },
                fail = err =>
                {
                    Debug.LogWarning($"[WeChatPlatform] 推荐组件展示失败: {err?.errMsg}");
                    _pendingOnFail = null;

                    if (allowRetry && ShouldRetryShow(err))
                    {
                        InvalidatePageManager();
                        ShowInternal(onSuccess, onFail, allowRetry: false);
                        return;
                    }

                    onFail?.Invoke(RecommendPageError.ShowFailed);
                },
            });
        }

        private void EnsurePageManager()
        {
            if (_pageManager != null)
                return;

            _pageManager = WX.CreatePageManager();
            _pageManager.On("destroy", OnDestroyEvent);
            _pageManager.On("error", OnErrorEvent);
        }

        private void OnDestroyEvent(PageManagerEventResult res)
        {
            Debug.Log($"[WeChatPlatform] recommend component destroy: {res.isRecommended}");
            if (res.isRecommended && _onRecommendSuccess != null)
            {
                var cb = _onRecommendSuccess;
                _onRecommendSuccess = null;
                cb.Invoke();
            }

            InvalidatePageManager();
        }

        private void OnErrorEvent(PageManagerEventResult res)
        {
            Debug.LogWarning($"[WeChatPlatform] recommend component error: {JsonUtility.ToJson(res)}");
            InvalidatePageManager();
            InvokePendingFail(RecommendPageError.ShowFailed);
        }

        private static bool ShouldRetryShow(PageManagerCallbackResult err)
        {
            return err != null
                && !string.IsNullOrEmpty(err.errMsg)
                && err.errMsg.IndexOf("destroy", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void InvalidatePageManager()
        {
            _isLoaded = false;
            _isLoading = false;
            _pageManager = null;
            _pendingOnFail = null;
        }

        private void InvokePendingFail(RecommendPageError error)
        {
            var fail = _pendingOnFail;
            _pendingOnFail = null;
            fail?.Invoke(error);
        }
    }
}
#endif
