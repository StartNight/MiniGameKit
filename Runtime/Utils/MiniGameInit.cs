using UnityEngine;

#if WEIXINMINIGAME
using WeChatWASM;
#endif

namespace MGKit
{
    public class MiniGameInit : MonoBehaviour
    {
        private void Start()
        {
#if WEIXINMINIGAME
        WX.ReportGameStart();
#endif
        }
    }
}