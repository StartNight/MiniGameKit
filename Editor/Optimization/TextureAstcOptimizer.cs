using UnityEditor;
using UnityEngine;

namespace MGKit.Editor
{
    /// <summary>
    /// 为 WebGL 平台批量设置贴图压缩与最大尺寸。
    /// WebGL1 下 ASTC 通常不可用，强制改为 DXT 避免运行时解压告警。
    /// </summary>
    public static class TextureAstcOptimizer
    {
        [MenuItem(MGKitEditorPaths.BuildOptimizeMenu + "贴图 WebGL1 DXT 压缩", false, 30)]
        public static void OptimizeForWebGL()
        {
            var folders = MGKitEditorPaths.SplitSemicolonPaths(MGKitEditorPaths.TextureAstcSearchFolders);
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
                var targetFormat = importer.DoesSourceTextureHaveAlpha()
                    ? TextureImporterFormat.DXT5
                    : TextureImporterFormat.DXT1;
                if (!webgl.overridden || webgl.format != targetFormat)
                {
                    webgl.overridden = true;
                    webgl.maxTextureSize = importer.maxTextureSize;
                    webgl.format = targetFormat;
                    importer.SetPlatformTextureSettings(webgl);
                    changed = true;
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                    count++;
                }
            }

            Debug.Log($"[TextureAstcOptimizer] 已优化 {count} 张贴图 (WebGL1 DXT)。");
        }
    }
}