/****************************************************
 * FileName:		PlatformSidebarSupport
 * Description:		非抖音平台侧边栏能力的统一降级实现
 *
 *****************************************************/

using System;
using System.Collections.Generic;

namespace MGKit
{
    internal static class PlatformSidebarSupport
    {
        internal static void CheckUnsupported(
            Action<bool> onResult,
            Action onComplete = null,
            Action<int, string> onError = null)
        {
            onResult?.Invoke(false);
            onComplete?.Invoke();
        }

        internal static void NavigateUnsupported(
            Action onSuccess = null,
            Action onComplete = null,
            Action<int, string> onError = null)
        {
            onError?.Invoke(-1, "Sidebar not supported on this platform");
            onComplete?.Invoke();
        }

        internal static bool IsFromSidebarOptions(IReadOnlyDictionary<string, object> options)
        {
            if (options == null)
                return false;

            var launchFrom = GetDictString(options, "launch_from");
            if (string.IsNullOrEmpty(launchFrom))
                launchFrom = GetDictString(options, "launchFrom");

            return launchFrom == "homepage" && GetDictString(options, "location") == "sidebar_card";
        }

        static string GetDictString(IReadOnlyDictionary<string, object> options, string key)
        {
            if (!options.TryGetValue(key, out var value) || value == null)
                return string.Empty;
            return value.ToString();
        }
    }
}
