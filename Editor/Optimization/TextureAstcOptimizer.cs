using UnityEditor;
using UnityEngine;

namespace MiniGameKit.Editor
{
    /// <summary>
    /// 为 WebGL 平台批量设置 ASTC 压缩与最大尺寸。
    /// </summary>
    public static class TextureAstcOptimizer
    {
        [MenuItem(MiniGameKitEditorPaths.BuildOptimizeMenu + "贴图 WebGL ASTC 压缩", false, 30)]
        public static void OptimizeForWebGL()
        {
            var folders = MiniGameKitEditorPaths.SplitSemicolonPaths(MiniGameKitEditorPaths.TextureAstcSearchFolders);
            if (folders.Length == 0)
            {
                Debug.LogWarning("[TextureAstcOptimizer] 未配置搜索目录，请在「项目设置」中配置。");
                return;
            }

            var count = 0;
            var guids = AssetDatabase.FindAssets("t:Texture2D", folders);
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    continue;

                var changed = false;
                if (importer.maxTextureSize > 1024)
                {
                    importer.maxTextureSize = 1024;
                    changed = true;
                }

                var webgl = importer.GetPlatformTextureSettings("WebGL");
                if (!webgl.overridden || webgl.format != TextureImporterFormat.ASTC_6x6)
                {
                    webgl.overridden = true;
                    webgl.maxTextureSize = importer.maxTextureSize;
                    webgl.format = TextureImporterFormat.ASTC_6x6;
                    importer.SetPlatformTextureSettings(webgl);
                    changed = true;
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                    count++;
                }
            }

            Debug.Log($"[TextureAstcOptimizer] 已优化 {count} 张贴图 (WebGL ASTC_6x6)。");
        }
    }
}
