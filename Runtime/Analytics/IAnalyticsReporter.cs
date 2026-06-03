using System.Collections.Generic;

namespace MGKit.Analytics
{
    public interface IAnalyticsReporter
    {
        void ReportEvent(string eventId, Dictionary<string, string> data);
    }
}
