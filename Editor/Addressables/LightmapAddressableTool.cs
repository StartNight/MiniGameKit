using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using System.IO;
using UnityEngine;
using System.Collections.Generic;

namespace MGKit.Editor
{
    public static class LightmapAddressableTool
    {
        private const string GroupName = "Lightmaps";
        private const string LightmapsPath = "Assets/Lightmaps/Rooms";

        [MenuItem("Tools/Minigame/构建/光照/将所有烘焙贴图加入 Addressables", false, 10)]
        public static void AddLightmapsToAddressables()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;

            var group = settings.FindGroup(GroupName);
            if (group == null)
            {
                group = settings.CreateGroup(GroupName, false, false, true, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            }

            var entriesAdded = 0;
            var subDirs = Directory.GetDirectories(LightmapsPath);

            foreach (var dir in subDirs)
            {
                var files = Directory.GetFiles(dir, "*", SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    var ext = Path.GetExtension(file).ToLower();
                    if (ext == ".exr" || ext == ".png" || ext == ".asset")
                    {
                        var assetPath = file.Replace('\\', '/');
                        var guid = AssetDatabase.AssetPathToGUID(assetPath);
                        if (string.IsNullOrEmpty(guid)) continue;

                        var entry = settings.CreateOrMoveEntry(guid, group);
                        if (entry != null)
                        {
                            entry.address = assetPath; // 使用完整路径作为地址
                            entriesAdded++;
                        }
                    }
                }
            }

            Debug.Log($"[LightmapAddressableTool] 已将 {entriesAdded} 个光照资源添加到 Addressables 组 '{GroupName}'。");
        }
    }
}
