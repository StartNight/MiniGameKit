/****************************************************
 * FileName:		WeChatMiniGameRuntime
 * Description:		运行时识别微信小游戏环境（不依赖 MGKIT_WECHAT 编译宏）
 *
*****************************************************/

using UnityEngine;

namespace MGKit
{
    public static class WeChatMiniGameRuntime
    {
        private static bool? _isAvailable;

        public static bool IsAvailable
        {
            get
            {
                if (_isAvailable.HasValue)
                    return _isAvailable.Value;

#if UNITY_EDITOR
                _isAvailable = false;
#elif MGKIT_WECHAT
                _isAvailable = true;
#elif UNITY_WEBGL && MGKIT_WECHAT
                _isAvailable = TryDetectWeChat();
#else
                _isAvailable = false;
#endif
                return _isAvailable.Value;
            }
        }

#if MGKIT_WECHAT
        private static bool TryDetectWeChat()
        {
            try
            {
                var info = global::WeChatWASM.WX.GetSystemInfoSync();
                return info != null && !string.IsNullOrEmpty(info.platform);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[WeChatMiniGameRuntime] detect failed: {ex.Message}");
                return false;
            }
        }
#endif
    }
}