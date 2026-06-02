using UnityEditor;
using UnityEngine;

namespace MGKit.Editor
{
    /// <summary>
    /// 一键性能护航构建工具。
    /// 整合了光照贴图注册、纹理优化、合规性检查、TrueShadow 剥离以及 Addressables 构建。
    /// </summary>
    public static class OneClickBuildTool
    {
#if UNITY_ADDRESSABLES
        [MenuItem(MGKitEditorPaths.BuildMenu + "一键性能护航构建 (微信小游戏)", false, 50)]
        public static void BuildForWeChatWithPerformanceGuard()
        {
            Debug.Log("[BuildTool] === 开始性能护航构建流程 ===");

            Debug.Log("[BuildTool] 步骤 1/5: 注册光照贴图到 Addressables...");
            LightmapAddressableTool.AddLightmapsToAddressables();

            Debug.Log("[BuildTool] 步骤 2/5: 批量优化 UI 纹理 MipMaps...");
            TextureOptimizationTool.OptimizeUITextures();

            Debug.Log("[BuildTool] 步骤 3/5: 剥离高性能开销 UI 组件 (TrueShadow)...");
            TrueShadowStripper.StripTrueShadows();

            Debug.Log("[BuildTool] 步骤 4/5: 执行资源合规性自动修复 (物理碰撞体)...");
            FixAllRoomColliders();

            Debug.Log("[BuildTool] 步骤 5/5: 切换微信 Provider 并执行 Addressables 构建...");
            AddressablesWeChatBuildMenu.BuildWithWeChatProviders();

            Debug.Log("[BuildTool] === 性能护航构建流程完成！请检查 Console 是否有错误。 ===");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
#endif

        private static void FixAllRoomColliders()
        {
            string prefabFolder = "Assets/Prefabs/Rooms";
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabFolder });
            int fixedCount = 0;
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject instance = PrefabUtility.LoadPrefabContents(path);
                var meshColliders = instance.GetComponentsInChildren<MeshCollider>(true);

                if (meshColliders.Length > 0)
                {
                    foreach (var mc in meshColliders)
                    {
                        var go = mc.gameObject;
                        Object.DestroyImmediate(mc);
                        go.AddComponent<BoxCollider>();
                        fixedCount++;
                    }
                    PrefabUtility.SaveAsPrefabAsset(instance, path);
                }
                PrefabUtility.UnloadPrefabContents(instance);
            }
            Debug.Log($"[BuildTool] 已自动修复 {fixedCount} 个违规 MeshCollider。");
        }
    }
}