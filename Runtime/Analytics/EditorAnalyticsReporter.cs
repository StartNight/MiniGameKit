using System.Collections.Generic;
using UnityEngine;

namespace MGKit.Analytics
{
    public class EditorAnalyticsReporter : IAnalyticsReporter
    {
        public void ReportEvent(string eventId, Dictionary<string, string> data)
        {
            var payload = data == null ? "{}" : string.Join(", ", data);
            Debug.Log($"[Analytics] {eventId} | {payload}");
        }
    }
}
