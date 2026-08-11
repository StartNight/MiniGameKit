/****************************************************
 * FileName:		DouyinEarlyInit
 * Description:		抖音小游戏在 BeforeSceneLoad 尽早 TT.InitSDK，避免 UNBridge 未就绪
 *
 *****************************************************/

using UnityEngine;

#if DOUYINMINIGAME
using TTSDK;
#endif

namespace MGKit
{
    /// <summary>
    /// 抖音宿主尽早初始化。侧边栏冷启动参数可读 <see cref="Env"/>。
    /// </summary>
    public static class DouyinEarlyInit
    {
#if DOUYINMINIGAME
        static bool _invoked;

        /// <summary>是否已调用过 InitSDK（含进行中，不等待回调）。</summary>
        public static bool HasInvokedInit => _invoked;

        /// <summary>InitSDK 成功回调里的宿主环境。</summary>
        public static ContainerEnv Env { get; private set; }

        /// <summary>InitSDK 回调是否成功完成。</summary>
        public static bool IsSdkReady { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void InitBeforeSceneLoad()
        {
            EnsureInit();
        }

        /// <summary>幂等：仅首次真正调用 TT.InitSDK。</summary>
        public static void EnsureInit()
        {
            if (_invoked)
                return;

            _invoked = true;
            TT.InitSDK((code, env) =>
            {
                if (code != 0)
                {
                    Debug.LogError($"[DouyinEarlyInit] TT.InitSDK failed: {code}");
                    _invoked = false;
                    IsSdkReady = false;
                    Env = null;
                    return;
                }

                Env = env;
                IsSdkReady = true;
            });
        }
#else
        public static bool HasInvokedInit => false;
        public static bool IsSdkReady => false;
#endif
    }
}
