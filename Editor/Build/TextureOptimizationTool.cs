using UnityEditor;
using UnityEngine;
using System.IO;

namespace MiniGameKit.Editor
{
    public static class TextureOptimizationTool
    {
        [MenuItem(MiniGameKitEditorPaths.BuildOptimizeMenu + "批量关闭 UI 贴图 MipMaps (微信)", false, 20)]
        public static void OptimizeUITextures()
        {
            string[] searchFolders = { "Assets/Texture/RoomIcon", "Assets/FindIt/UI" };
            int count = 0;

            string[] guids = AssetDatabase.FindAssets("t:Texture", searchFolders);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                
                if (importer != null && importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    importer.SaveAndReimport();
                    count++;
                }
            }

            Debug.Log($"[TextureOptimizationTool] 已关闭 {count} 张 UI 贴图的 MipMaps 以节省显存。");
        }
    }
}
