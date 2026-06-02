using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MGKit.Editor
{
    /// <summary>
    /// 从项目材质收集 Shader 变体并生成 ShaderVariantCollection 资源。
    /// </summary>
    public static class ShaderVariantCollectionGenerator
    {
        [MenuItem(MGKitEditorPaths.BuildOptimizeMenu + "生成 Shader Variant Collection", false, 40)]
        public static void Generate()
        {
            var svc = new ShaderVariantCollection();
            var guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
            var count = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null || mat.shader == null)
                    continue;

                var variant = new ShaderVariantCollection.ShaderVariant
                {
                    shader = mat.shader,
                    passType = PassType.Normal,
                    keywords = mat.shaderKeywords
                };

                if (!svc.Contains(variant))
                {
                    svc.Add(variant);
                    count++;
                }
            }

            var savePath = MGKitEditorPaths.ShaderVariantCollectionAssetPath;
            var dir = Path.GetDirectoryName(savePath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
            {
                CreateFoldersRecursively(dir);
            }

            var existing = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(savePath);
            if (existing != null)
                AssetDatabase.DeleteAsset(savePath);

            AssetDatabase.CreateAsset(svc, savePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ShaderVariantCollection] 已保存 {savePath}，变体数 {count}。");
        }

        private static void CreateFoldersRecursively(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
                return;

            var parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
                CreateFoldersRecursively(parent);

            var folderName = Path.GetFileName(assetPath);
            var parentPath = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            AssetDatabase.CreateFolder(parentPath ?? "Assets", folderName);
        }
    }
}