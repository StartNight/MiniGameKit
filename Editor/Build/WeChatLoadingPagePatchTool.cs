#if UNITY_EDITOR

using System.IO;
using UnityEditor;
using UnityEngine;

namespace MiniGameKit.Editor
{
    public static class WeChatLoadingPagePatchTool
    {
        private const string SourceLogoPath = "Assets/WebGLTemplates/WYMinigame2022/TemplateData/logo-256.png";
        private const string TargetLogoPath = "Assets/WX-WASM-SDK-V2/Runtime/wechat-default/images/unity_logo.png";
        private const string GameJsPath = "Assets/WX-WASM-SDK-V2/Runtime/wechat-default/game.js";

        [MenuItem(MiniGameKitEditorPaths.BuildWeChatMenu + "应用加载页配置", false, 56)]
        public static void ApplyLoadingPageConfig()
        {
            if (!File.Exists(ToFullPath(SourceLogoPath)))
            {
                EditorUtility.DisplayDialog("微信小游戏加载页配置", $"源Logo不存在：\n{SourceLogoPath}", "确定");
                return;
            }

            if (!File.Exists(ToFullPath(TargetLogoPath)))
            {
                EditorUtility.DisplayDialog("微信小游戏加载页配置", $"目标Logo不存在：\n{TargetLogoPath}", "确定");
                return;
            }

            if (!File.Exists(ToFullPath(GameJsPath)))
            {
                EditorUtility.DisplayDialog("微信小游戏加载页配置", $"game.js不存在：\n{GameJsPath}", "确定");
                return;
            }

            if (!UpdateGameJsTextConfig())
                return;

            File.Copy(ToFullPath(SourceLogoPath), ToFullPath(TargetLogoPath), true);

            AssetDatabase.ImportAsset(TargetLogoPath);
            AssetDatabase.ImportAsset(GameJsPath);
            AssetDatabase.Refresh();

            Debug.Log("[MiniGame] 已应用微信小游戏加载页Logo与文案配置。");
            EditorUtility.DisplayDialog("微信小游戏加载页配置", "已完成Logo覆盖和加载页文案更新。", "确定");
        }

        private static bool UpdateGameJsTextConfig()
        {
            var gameJsFullPath = ToFullPath(GameJsPath);
            var content = File.ReadAllText(gameJsFullPath);
            var updated = content;

            var ok = ReplaceTextConfigValue(ref updated,
                "firstStartText: '首次加载请耐心等待'",
                "firstStartText: '加载游戏中，请耐心等待！'");
            ok &= ReplaceTextConfigValue(ref updated,
                "initText: '初始化中'",
                "initText: '快好了'");
            ok &= ReplaceTextConfigValue(ref updated,
                "completeText: '开始游戏'",
                "completeText: '开始游戏，祝你玩的开心！'");

            if (!ok)
            {
                EditorUtility.DisplayDialog(
                    "微信小游戏加载页配置",
                    "game.js中的目标文案未完全匹配，请检查WX SDK模板是否已变更。",
                    "确定");
                return false;
            }

            if (updated != content)
            {
                File.WriteAllText(gameJsFullPath, updated);
            }

            return true;
        }

        private static bool ReplaceTextConfigValue(ref string content, string oldValue, string newValue)
        {
            if (content.Contains(oldValue))
            {
                content = content.Replace(oldValue, newValue);
                return true;
            }

            return content.Contains(newValue);
        }

        private static string ToFullPath(string assetPath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }
    }
}

#endif
