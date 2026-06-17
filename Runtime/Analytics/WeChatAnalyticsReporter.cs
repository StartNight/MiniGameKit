#if MGKIT_WECHAT
using System.Collections.Generic;
using WeChatWASM;

namespace MGKit.Analytics
{
    public class WeChatAnalyticsReporter : IAnalyticsReporter
    {
        public void ReportEvent(string eventId, Dictionary<string, string> data)
        {
            if (string.IsNullOrEmpty(eventId)) return;
            WX.ReportEvent(eventId, data ?? new Dictionary<string, string>());
        }
    }
}
#endif
