using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGameKit.Editor
{
    /// <summary>
    /// 一键将指定文件夹中预制体上的 Mask 替换为 RectMask2D，
    /// 若同物体上有 Image 组件则禁用 Image（RectMask2D 不需要 Graphic）。
    /// </summary>
    public static class MaskToRectMask2DTool
    {
        [MenuItem(MiniGameKitEditorPaths.UiMenu + "Mask → RectMask2D 替换", false, 25)]
        public static void ReplaceMasks()
        {
            var folder = EditorUtility.OpenFolderPanel("选择预制体文件夹", "Assets/Prefabs/UI", "");
            if (string.IsNullOrEmpty(folder))
                return;

            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            var relativePath = folder.Replace('\\', '/')
                .Replace(projectRoot.Replace('\\', '/') + "/", "");

            if (!relativePath.StartsWith("Assets/"))
            {
                EditorUtility.DisplayDialog("错误", "请选择项目 Assets 目录下的文件夹。", "确定");
                return;
            }

            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { relativePath });
            var totalReplace = 0;
            var totalImageDisabled = 0;
            var modifiedPrefabs = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                if (root == null)
                    continue;

                var changed = false;
                var masks = root.GetComponentsInChildren<Mask>(true);

                foreach (var mask in masks)
                {
                    var go = mask.gameObject;

                    // 记录 Image 以便之后禁用
                    var image = go.GetComponent<Image>();

                    // 检查是否已有 RectMask2D，有则跳过
                    if (go.GetComponent<RectMask2D>() != null)
                    {
                        Debug.LogWarning($"[MaskReplace] {path}:{go.name} 已存在 RectMask2D，跳过");
                        continue;
                    }

                    // 替换 Mask → RectMask2D（RectMask2D 不需要 Graphic 即可工作）
                    go.AddComponent<RectMask2D>();

                    Object.DestroyImmediate(mask);
                    totalReplace++;
                    changed = true;

                    // 禁用同物体上的 Image
                    if (image != null && image.enabled)
                    {
                        image.enabled = false;
                        totalImageDisabled++;
                    }
                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    modifiedPrefabs++;
                }

                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[MaskReplace] 完成：处理 {modifiedPrefabs} 个预制体，替换 {totalReplace} 个 Mask，禁用 {totalImageDisabled} 个 Image。");
            EditorUtility.DisplayDialog("Mask → RectMask2D 替换",
                $"处理预制体：{modifiedPrefabs} 个\n" +
                $"替换 Mask：{totalReplace} 个\n" +
                $"禁用 Image：{totalImageDisabled} 个",
                "确定");
        }
    }
}
