/****************************************************
 * FileName:		PlatformFeatureConfigMenu
 * Description:		创建 / 补全项目 PlatformFeatureConfig 资源
 *
*****************************************************/

using UnityEditor;
using UnityEngine;

namespace MGKit.Editor
{
    public static class PlatformFeatureConfigMenu
    {
        public const string DefaultAssetPath = MGKitEditorPaths.DefaultPlatformFeatureConfigAssetPath;

        [MenuItem(MGKitEditorPaths.MenuRoot + "平台/创建 PlatformFeature 配置", false, 120)]
        public static void CreateOrRefreshConfig()
        {
            EnsureProjectConfig(forceRefreshDefaults: true);
        }

        [InitializeOnLoadMethod]
        private static void EnsureOnLoad()
        {
            EditorApplication.delayCall += () => EnsureProjectConfig(forceRefreshDefaults: false);
        }

        internal static PlatformFeatureConfig EnsureProjectConfig(bool forceRefreshDefaults)
        {
            var asset = AssetDatabase.LoadAssetAtPath<PlatformFeatureConfig>(DefaultAssetPath);
            if (asset == null)
            {
                EnsureFolder("Assets/Resources/MGKit");
                asset = ScriptableObject.CreateInstance<PlatformFeatureConfig>();
                asset.EnsureAllPlatforms();
                AssetDatabase.CreateAsset(asset, DefaultAssetPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[MGKit] 已创建平台能力配置: {DefaultAssetPath}");
                return asset;
            }

            if (forceRefreshDefaults)
            {
                asset.EnsureAllPlatforms();
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
                Debug.Log($"[MGKit] 已补全平台能力配置条目: {DefaultAssetPath}");
            }

            return asset;
        }

        private static void EnsureFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder))
                return;

            var parts = assetFolder.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
