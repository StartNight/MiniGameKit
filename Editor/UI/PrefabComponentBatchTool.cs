using UnityEditor;
using UnityEngine;

namespace MGKit.Editor
{
    /// <summary>
    /// 为指定目录下的 Prefab 批量添加组件（通过类型名反射，避免 Editor 程序集依赖游戏脚本）。
    /// </summary>
    public static class PrefabComponentBatchTool
    {
        [MenuItem(MGKitEditorPaths.UiMenu + "批量添加组件到 Prefab", false, 30)]
        public static void AddComponentToPrefabs()
        {
            var typeName = MGKitEditorPaths.PrefabComponentTypeName;
            var componentType = ReflectionTypeUtility.FindType(typeName);
            if (componentType == null)
            {
                EditorUtility.DisplayDialog("错误", $"找不到类型: {typeName}\n请在「项目设置」中检查组件类型名。", "确定");
                return;
            }

            if (!typeof(Component).IsAssignableFrom(componentType))
            {
                EditorUtility.DisplayDialog("错误", $"{typeName} 不是 Component 类型。", "确定");
                return;
            }

            var roots = MGKitEditorPaths.SplitSemicolonPaths(MGKitEditorPaths.PrefabComponentBatchRoots);
            if (roots.Length == 0)
            {
                Debug.LogWarning("[PrefabComponentBatch] 未配置 Prefab 搜索目录。");
                return;
            }

            var added = 0;
            var guids = AssetDatabase.FindAssets("t:Prefab", roots);
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                if (root == null)
                    continue;

                if (root.GetComponent(componentType) == null)
                {
                    root.AddComponent(componentType);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    added++;
                }

                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[PrefabComponentBatch] 已向 {added} 个 Prefab 添加 {typeName}。");
        }
    }
}