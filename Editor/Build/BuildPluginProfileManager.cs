#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MiniGameKit.Editor
{
    /// <summary>
    /// 构建前切换 PluginImporter（WebGL），避免微信/抖音 jslib 同包链接冲突。
    /// </summary>
    public static class BuildPluginProfileManager
    {
        public const string WeChatPluginsRoot = "Assets/WX-WASM-SDK-V2/Runtime/Plugins";
        public const string DouyinWebGlRoot = "Plugins/ByteGame/com.bytedance.starksdk/WebGL";

        [Serializable]
        public class WebGlPluginState
        {
            public string AssetPath;
            public bool WebGlEnabled;
        }

        [Serializable]
        public class Snapshot
        {
            public List<WebGlPluginState> States = new List<WebGlPluginState>();
        }

        public static Snapshot CaptureWebGlPluginStates()
        {
            var snapshot = new Snapshot();
            foreach (var path in CollectManagedPluginPaths())
            {
                var importer = AssetImporter.GetAtPath(path) as PluginImporter;
                if (importer == null)
                    continue;

                snapshot.States.Add(new WebGlPluginState
                {
                    AssetPath = path,
                    WebGlEnabled = importer.GetCompatibleWithPlatform(BuildTarget.WebGL),
                });
            }

            return snapshot;
        }

        public static void Restore(Snapshot snapshot)
        {
            if (snapshot?.States == null || snapshot.States.Count == 0)
                return;

            foreach (var state in snapshot.States)
            {
                if (string.IsNullOrEmpty(state.AssetPath))
                    continue;

                var importer = AssetImporter.GetAtPath(state.AssetPath) as PluginImporter;
                if (importer == null)
                    continue;

                if (importer.GetCompatibleWithPlatform(BuildTarget.WebGL) == state.WebGlEnabled)
                    continue;

                importer.SetCompatibleWithPlatform(BuildTarget.WebGL, state.WebGlEnabled);
                importer.SaveAndReimport();
            }

            AssetDatabase.Refresh();
            Debug.Log($"[Build] 已还原 {snapshot.States.Count} 个 WebGL 插件导入设置。");
        }

        public static void Apply(BuildPluginProfile profile)
        {
            bool enableWeChat = profile == BuildPluginProfile.WeChatMiniGame;
            bool enableDouyin = profile == BuildPluginProfile.DouyinMiniGame;

            int changed = 0;
            foreach (var path in CollectManagedPluginPaths())
            {
                var importer = AssetImporter.GetAtPath(path) as PluginImporter;
                if (importer == null)
                    continue;

                bool shouldEnable = false;
                if (path.Replace('\\', '/').StartsWith(WeChatPluginsRoot, StringComparison.OrdinalIgnoreCase))
                    shouldEnable = enableWeChat;
                else if (path.Replace('\\', '/').Contains(DouyinWebGlRoot.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase))
                    shouldEnable = enableDouyin;

                if (importer.GetCompatibleWithPlatform(BuildTarget.WebGL) == shouldEnable)
                    continue;

                importer.SetCompatibleWithPlatform(BuildTarget.WebGL, shouldEnable);
                importer.SaveAndReimport();
                changed++;
            }

            if (changed > 0)
            {
                AssetDatabase.Refresh();
                Debug.Log($"[Build] PluginProfile={profile}，已更新 {changed} 个 WebGL 插件项。");
            }
            else
            {
                Debug.Log($"[Build] PluginProfile={profile}，WebGL 插件项无需变更。");
            }
        }

        public static BuildPluginProfile ForPlatform(BuildPlatform platform) =>
            platform switch
            {
                BuildPlatform.WeChatMiniGame => BuildPluginProfile.WeChatMiniGame,
                BuildPlatform.DouyinMiniGame => BuildPluginProfile.DouyinMiniGame,
                BuildPlatform.WebGL => BuildPluginProfile.WebGL,
                _ => BuildPluginProfile.WebGL,
            };

        static IEnumerable<string> CollectManagedPluginPaths()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (AssetDatabase.IsValidFolder(WeChatPluginsRoot))
            {
                foreach (var guid in AssetDatabase.FindAssets(string.Empty, new[] { WeChatPluginsRoot }))
                    TryAddPlugin(guid, set);
            }

            foreach (var guid in AssetDatabase.FindAssets("glob:\"**/*.jslib\""))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Replace('\\', '/').Contains("bytedance.starksdk/WebGL", StringComparison.OrdinalIgnoreCase))
                    TryAddPlugin(guid, set);
            }

            return set;
        }

        static void TryAddPlugin(string guid, HashSet<string> set)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
                return;
            if (!path.EndsWith(".jslib", StringComparison.OrdinalIgnoreCase)
                && !path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                && !path.EndsWith(".bc", StringComparison.OrdinalIgnoreCase)
                && !path.EndsWith(".a", StringComparison.OrdinalIgnoreCase))
                return;

            if (AssetImporter.GetAtPath(path) is PluginImporter)
                set.Add(path);
        }
    }
}

#endif
