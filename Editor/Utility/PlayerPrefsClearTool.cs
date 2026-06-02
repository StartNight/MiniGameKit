using UnityEditor;
using UnityEngine;

namespace MGKit.Editor
{
    /// <summary>
    /// 按配置键名清理 PlayerPrefs（调试用）。
    /// </summary>
    public static class PlayerPrefsClearTool
    {
        [MenuItem(MGKitEditorPaths.UtilityMenu + "清理本地存档 (PlayerPrefs)", false, 100)]
        public static void ClearSaveData()
        {
            var keys = MGKitEditorPaths.SplitSemicolonPaths(MGKitEditorPaths.PlayerPrefsClearKeys);
            if (keys.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", "未配置要清理的 PlayerPrefs 键名。", "确定");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "清理存档",
                    $"将删除以下 PlayerPrefs 键：\n{string.Join("\n", keys)}\n\n是否继续？",
                    "清理",
                    "取消"))
                return;

            foreach (var key in keys)
            {
                var trimmed = key.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    PlayerPrefs.DeleteKey(trimmed);
            }

            PlayerPrefs.Save();
            Debug.Log("[PlayerPrefsClear] 已清理配置的本地存档键。");
        }
    }
}
