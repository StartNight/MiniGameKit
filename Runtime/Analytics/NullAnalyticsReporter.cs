using System.Collections.Generic;

namespace MGKit.Analytics
{
    public class NullAnalyticsReporter : IAnalyticsReporter
    {
        public void ReportEvent(string eventId, Dictionary<string, string> data) { }
    }
}
