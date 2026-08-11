#if UNITY_EDITOR

using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace MGKit.Editor
{
    /// <summary>
    /// 抖音小游戏加载页配置：对齐 <see cref="WeChatLoadingPagePatchTool"/> 的 Logo、文案与样式，
    /// 写入 StarkSDK DefaultTemplate（构建时会带入导出包）。
    /// </summary>
    public static class DouyinLoadingPagePatchTool
    {
        private const string SourceLogoPath = "Assets/WebGLTemplates/WYMinigame2022/TemplateData/logo-256.png";
        private const string StarkDefaultTemplateRel = "DefaultTemplate";
        private const string GameJsFileName = "game.js";
        private const string LogoRelPath = "images/unity_logo.png";

        // textConfig.style — 与微信工具保持一致
        private const int TextStyleBottom = 75;
        private const int TextStyleHeight = 24;
        private const int TextStyleWidth = 240;
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

        private const string PlaceholderOrNumber = @"(?:\$[A-Z0-9_]+|\d+)";

        private static readonly Regex TextStyleRegex = new Regex(
            $@"(// 文字样式\s*\n\s*style:\s*\{{\s*)bottom:\s*{PlaceholderOrNumber},\s*height:\s*{PlaceholderOrNumber},\s*width:\s*{PlaceholderOrNumber},\s*color:\s*'[^']*',\s*fontSize:\s*\d+,",
            RegexOptions.Singleline);

        private static readonly Regex BarStyleRegex = new Regex(
            $@"(barConfig:\s*\{{\s*style:\s*\{{\s*)width:\s*{PlaceholderOrNumber},\s*height:\s*{PlaceholderOrNumber},\s*padding:\s*\d+,\s*bottom:\s*{PlaceholderOrNumber},\s*backgroundColor:\s*'[^']*',",
            RegexOptions.Singleline);

        private static readonly Regex IconStyleRegex = new Regex(
            $@"(iconConfig:\s*\{{\s*visible:\s*true,\s*style:\s*\{{\s*)width:\s*{PlaceholderOrNumber},\s*height:\s*{PlaceholderOrNumber},\s*bottom:\s*{PlaceholderOrNumber},",
            RegexOptions.Singleline);

        [MenuItem(MGKitEditorPaths.BuildDouyinMenu + "应用加载页配置", false, 56)]
        public static void ApplyLoadingPageConfig()
        {
            if (!File.Exists(MGKitEditorPaths.ToFullPath(SourceLogoPath)))
            {
                EditorUtility.DisplayDialog("抖音小游戏加载页配置", $"源 Logo 不存在：\n{SourceLogoPath}", "确定");
                return;
            }

            if (!TryResolveStarkTemplatePaths(out var gameJsPath, out var logoPath))
            {
                EditorUtility.DisplayDialog(
                    "抖音小游戏加载页配置",
                    "未找到 StarkSDK DefaultTemplate。\n\n请切换到「抖音小游戏」平台并确保 StarkSDK 已安装（Assets/Plugins/ByteGame 或 SDKs/Douyin 归档）。",
                    "确定");
                return;
            }

            if (!UpdateGameJsConfig(gameJsPath))
                return;

            File.Copy(MGKitEditorPaths.ToFullPath(SourceLogoPath), MGKitEditorPaths.ToFullPath(logoPath), true);

            AssetDatabase.ImportAsset(logoPath);
            AssetDatabase.ImportAsset(gameJsPath);
            AssetDatabase.Refresh();

            Debug.Log(
                $"[MiniGame] 已应用抖音小游戏加载页配置。文字/进度条 bottom={TextStyleBottom}，" +
                $"icon {ProjectIconDisplayWidth}x{ProjectIconDisplayHeight} bottom={ProjectIconDisplayBottom}。");
            EditorUtility.DisplayDialog(
                "抖音小游戏加载页配置",
                $"已完成 Logo、文案与加载页样式更新。\nicon：{ProjectIconDisplayWidth}×{ProjectIconDisplayHeight}，bottom {ProjectIconDisplayBottom}",
                "确定");
        }

        [MenuItem(MGKitEditorPaths.BuildDouyinMenu + "应用加载页配置", true, 56)]
        public static bool ValidateApplyLoadingPageConfig() =>
            TryResolveStarkTemplatePaths(out _, out _);

        static bool TryResolveStarkTemplatePaths(out string gameJsPath, out string logoPath)
        {
            gameJsPath = null;
            logoPath = null;

            var candidates = new[]
            {
                Path.Combine(DouyinSdkBootstrap.StarkSdkActiveRelPath, StarkDefaultTemplateRel),
                Path.Combine(DouyinSdkBootstrap.ArchiveRelPath, DouyinSdkBootstrap.StarkSdkFolderName, StarkDefaultTemplateRel)
            };

            foreach (var templateRoot in candidates)
            {
                var gameJsAsset = Path.Combine(templateRoot, GameJsFileName).Replace('\\', '/');
                var logoAsset = Path.Combine(templateRoot, LogoRelPath).Replace('\\', '/');
                if (File.Exists(MGKitEditorPaths.ToFullPath(gameJsAsset)))
                {
                    gameJsPath = gameJsAsset;
                    logoPath = logoAsset;
                    return true;
                }
            }

            return false;
        }

        static bool UpdateGameJsConfig(string gameJsAssetPath)
        {
            var gameJsFullPath = MGKitEditorPaths.ToFullPath(gameJsAssetPath);
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
                    "抖音小游戏加载页配置",
                    "game.js 中的目标文案未完全匹配，请检查 StarkSDK DefaultTemplate 是否已变更。",
                    "确定");
                return false;
            }

            if (updated != content)
                File.WriteAllText(gameJsFullPath, updated);

            return true;
        }

        static bool TryPatchTextStyle(ref string content)
        {
            var match = TextStyleRegex.Match(content);
            if (!match.Success)
            {
                EditorUtility.DisplayDialog(
                    "抖音小游戏加载页配置",
                    "game.js 中未找到 textConfig 文字样式块，请检查 StarkSDK DefaultTemplate 是否已变更。",
                    "确定");
                return false;
            }

            var replacement =
                $"{match.Groups[1].Value}bottom: {TextStyleBottom},\n          height: {TextStyleHeight},\n          width: {TextStyleWidth},\n          color: '{TextStyleColor}',\n          fontSize: {TextStyleFontSize},";
            content = TextStyleRegex.Replace(content, replacement, 1);
            return true;
        }

        static bool TryPatchBarStyle(ref string content)
        {
            var match = BarStyleRegex.Match(content);
            if (!match.Success)
            {
                EditorUtility.DisplayDialog(
                    "抖音小游戏加载页配置",
                    "game.js 中未找到 barConfig 进度条样式块，请检查 StarkSDK DefaultTemplate 是否已变更。",
                    "确定");
                return false;
            }

            var replacement =
                $"{match.Groups[1].Value}width: {BarStyleWidth},\n          height: {BarStyleHeight},\n          padding: {BarStylePadding},\n          bottom: {BarStyleBottom},\n          backgroundColor: '{BarStyleBackgroundColor}',";
            content = BarStyleRegex.Replace(content, replacement, 1);
            return true;
        }

        static bool TryPatchIconStyle(ref string content)
        {
            var match = IconStyleRegex.Match(content);
            if (!match.Success)
            {
                EditorUtility.DisplayDialog(
                    "抖音小游戏加载页配置",
                    "game.js 中未找到 iconConfig.style 配置，请检查 StarkSDK DefaultTemplate 是否已变更。",
                    "确定");
                return false;
            }

            var replacement =
                $"{match.Groups[1].Value}width: {ProjectIconDisplayWidth},\n          height: {ProjectIconDisplayHeight},\n          bottom: {ProjectIconDisplayBottom},";
            content = IconStyleRegex.Replace(content, replacement, 1);
            return true;
        }

        static bool ReplaceTextConfigValue(ref string content, string oldValue, string newValue)
        {
            if (content.Contains(oldValue))
            {
                content = content.Replace(oldValue, newValue);
                return true;
            }

            return content.Contains(newValue);
        }
    }
}

#endif
