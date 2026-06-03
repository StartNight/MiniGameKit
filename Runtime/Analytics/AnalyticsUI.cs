using UnityEngine.Events;
using UnityEngine.UI;

namespace MGKit.Analytics
{
    public static class AnalyticsUI
    {
        public static void BindClick(Button button, UnityAction handler, string buttonId, string screen, int level = -1)
        {
            if (button == null) return;
            button.onClick.AddListener(() =>
            {
                GameAnalytics.TrackUIClick(buttonId, screen, level);
                handler?.Invoke();
            });
        }
    }
}
