#if UNITY_EDITOR

using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace MiniGameKit.Editor
{
    public static class WeChatLoadingPagePatchTool
    {
        private const string SourceLogoPath = "Assets/WebGLTemplates/WYMinigame2022/TemplateData/logo-256.png";
        private const string TargetLogoPath = "Assets/WX-WASM-SDK-V2/Runtime/wechat-default/images/unity_logo.png";
        private const string GameJsPath = "Assets/WX-WASM-SDK-V2/Runtime/wechat-default/game.js";

        // textConfig.style
        private const int TextStyleBottom = 75;
        private const int TextStyleHeight = 24;
        private const int TextStyleWidth = 240;
        private const int TextStyleLineHeight = 24;
        private const string TextStyleColor = "#ffffff";
        private const int TextStyleFontSize = 12;

        // barConfig.style
        private const int BarStyleWidth = 240;
        private const int BarStyleHeight = 24;
        private const int BarStylePadding = 2;
        private const int BarStyleBottom = 75;
        private const string BarStyleBackgroundColor = "#7D56F4";

        // iconConfig.style
        private const int ProjectIconDisplayWidth = 32;
        private const int ProjectIconDisplayHeight = 32;
        private const int ProjectIconDisplayBottom = 42;

        private static readonly Regex TextStyleRegex = new Regex(
            @"(// 文字样式\s*\n\s*style:\s*\{\s*)bottom:\s*\d+,\s*height:\s*\d+,\s*width:\s*\d+,\s*lineHeight:\s*\d+,\s*color:\s*'[^']*',\s*fontSize:\s*\d+,",
            RegexOptions.Singleline);

        private static readonly Regex BarStyleRegex = new Regex(
            @"(barConfig:\s*\{\s*style:\s*\{\s*)width:\s*\d+,\s*height:\s*\d+,\s*padding:\s*\d+,\s*bottom:\s*\d+,\s*backgroundColor:\s*'[^']*',",
            RegexOptions.Singleline);

        private static readonly Regex IconStyleRegex = new Regex(
            @"(iconConfig:\s*\{\s*visible:\s*true,\s*style:\s*\{\s*)width:\s*\d+,\s*height:\s*\d+,\s*bottom:\s*\d+,",
            RegexOptions.Singleline);

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

            if (!UpdateGameJsConfig())
                return;

            File.Copy(ToFullPath(SourceLogoPath), ToFullPath(TargetLogoPath), true);

            AssetDatabase.ImportAsset(TargetLogoPath);
            AssetDatabase.ImportAsset(GameJsPath);
            AssetDatabase.Refresh();

            Debug.Log(
                $"[MiniGame] 已应用微信小游戏加载页配置。文字/进度条 bottom={TextStyleBottom}，" +
                $"icon {ProjectIconDisplayWidth}x{ProjectIconDisplayHeight} bottom={ProjectIconDisplayBottom}。");
            EditorUtility.DisplayDialog(
                "微信小游戏加载页配置",
                $"已完成 Logo、文案与加载页样式更新。\nicon：{ProjectIconDisplayWidth}×{ProjectIconDisplayHeight}，bottom {ProjectIconDisplayBottom}",
                "确定");
        }

        private static bool UpdateGameJsConfig()
        {
            var gameJsFullPath = ToFullPath(GameJsPath);
            var content = File.ReadAllText(gameJsFullPath);
            var updated = content;

            if (!TryPatchTextStyle(ref updated))
                return false;

            if (!TryPatchBarStyle(ref updated))
                return false;

            if (!TryPatchIconStyle(ref updated))
                return false;

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
                    "game.js 中的目标文案未完全匹配，请检查 WX SDK 模板是否已变更。",
                    "确定");
                return false;
            }

            if (updated != content)
                File.WriteAllText(gameJsFullPath, updated);

            return true;
        }

        private static bool TryPatchTextStyle(ref string content)
        {
            var match = TextStyleRegex.Match(content);
            if (!match.Success)
            {
                EditorUtility.DisplayDialog(
                    "微信小游戏加载页配置",
                    "game.js 中未找到 textConfig 文字样式块，请检查 WX SDK 模板是否已变更。",
                    "确定");
                return false;
            }

            var replacement =
                $"{match.Groups[1].Value}bottom: {TextStyleBottom},\n                        height: {TextStyleHeight},\n                        width: {TextStyleWidth},\n                        lineHeight: {TextStyleLineHeight},\n                        color: '{TextStyleColor}',\n                        fontSize: {TextStyleFontSize},";
            content = TextStyleRegex.Replace(content, replacement, 1);
            return true;
        }

        private static bool TryPatchBarStyle(ref string content)
        {
            var match = BarStyleRegex.Match(content);
            if (!match.Success)
            {
                EditorUtility.DisplayDialog(
                    "微信小游戏加载页配置",
                    "game.js 中未找到 barConfig 进度条样式块，请检查 WX SDK 模板是否已变更。",
                    "确定");
                return false;
            }

            var replacement =
                $"{match.Groups[1].Value}width: {BarStyleWidth},\n                        height: {BarStyleHeight},\n                        padding: {BarStylePadding},\n                        bottom: {BarStyleBottom},\n                        backgroundColor: '{BarStyleBackgroundColor}',";
            content = BarStyleRegex.Replace(content, replacement, 1);
            return true;
        }

        private static bool TryPatchIconStyle(ref string content)
        {
            var match = IconStyleRegex.Match(content);
            if (!match.Success)
            {
                EditorUtility.DisplayDialog(
                    "微信小游戏加载页配置",
                    "game.js 中未找到 iconConfig.style 配置，请检查 WX SDK 模板是否已变更。",
                    "确定");
                return false;
            }

            var replacement =
                $"{match.Groups[1].Value}width: {ProjectIconDisplayWidth},\n                        height: {ProjectIconDisplayHeight},\n                        bottom: {ProjectIconDisplayBottom},";
            content = IconStyleRegex.Replace(content, replacement, 1);
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
