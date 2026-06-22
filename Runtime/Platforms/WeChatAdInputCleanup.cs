#if MGKIT_WECHAT
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using WeChatWASM;

namespace MGKit
{
    /// <summary>
    /// 全屏广告前后清理微信输入层，避免广告关闭后 TextView 残留更新报错。
    /// </summary>
    internal static class WeChatAdInputCleanup
    {
        internal static void PrepareBeforeFullscreenAd()
        {
            CleanupNow();
        }

        internal static void CleanupAfterFullscreenAd()
        {
            CleanupNow();
            ScheduleDelayedCleanup();
        }

        static void CleanupNow()
        {
#if !UNITY_EDITOR
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);

            SetMobileKeyboardSupport(false);

            try
            {
                WX.HideKeyboard(new HideKeyboardOption());
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[WeChatAdInputCleanup] HideKeyboard failed: {ex.Message}");
            }
#endif
        }

        static void ScheduleDelayedCleanup()
        {
#if !UNITY_EDITOR
            var runner = new GameObject("[WeChatAdInputCleanup]");
            Object.DontDestroyOnLoad(runner);
            runner.hideFlags = HideFlags.HideAndDontSave;
            runner.AddComponent<DelayedCleanupRunner>();
#endif
        }

        static void SetMobileKeyboardSupport(bool enabled)
        {
#if PLATFORM_WEIXINMINIGAME
            WeixinMiniGameInput.mobileKeyboardSupport = enabled;
#elif PLATFORM_PLAYABLEADS
            PlayableAdsInput.mobileKeyboardSupport = enabled;
#elif UNITY_WEBGL && UNITY_2022_1_OR_NEWER
            WebGLInput.mobileKeyboardSupport = enabled;
#endif
        }

        sealed class DelayedCleanupRunner : MonoBehaviour
        {
            IEnumerator Start()
            {
                yield return null;
                CleanupNow();
                yield return new WaitForSecondsRealtime(0.2f);
                CleanupNow();
                Destroy(gameObject);
            }
        }
    }
}
#endif
