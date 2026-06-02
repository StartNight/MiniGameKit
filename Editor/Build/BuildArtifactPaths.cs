#if UNITY_EDITOR

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using MGKit;

namespace MGKit.Editor
{
    /// <summary>解析各平台构建产物路径，供 CI 与同步脚本读取。</summary>
    public static class BuildArtifactPaths
    {
        public const string CiOutputFileName = "minigamekit-build-output.txt";

        public static string GetBuildRepoName() =>
            $"{PlayerSettings.productName}-build";

        public static string ResolveArtifactDirectory(MiniGamePlatform platform, BuildConfig config = null)
        {
            config ??= BuildConfig.Create(platform);

            switch (platform)
            {
                case MiniGamePlatform.WeChatMiniGame:
#if WEIXINMINIGAME
                    var dst = WeChatWASM.WXConvertCore.config?.ProjectConf?.DST;
                    if (!string.IsNullOrEmpty(dst))
                        return Path.GetFullPath(dst);
#endif
                    return Path.GetFullPath(
                        Path.Combine(MGKitEditorPaths.ProjectRoot, config.DefaultOutputDir));

                case MiniGamePlatform.DouyinMiniGame:
                    var marker = ReadCiOutputMarker(platform);
                    if (!string.IsNullOrEmpty(marker))
                        return Path.GetFullPath(marker);
                    return Path.GetFullPath(
                        Path.Combine(MGKitEditorPaths.ProjectRoot, config.DefaultOutputDir));

                case MiniGamePlatform.WebGL:
                case MiniGamePlatform.Android:
                case MiniGamePlatform.iOS:
                    var output = MiniGameBuildPipeline.ResolveOutputPath(
                        config,
                        config.GetBuildTarget());
                    if (platform == MiniGamePlatform.iOS)
                        return Path.GetFullPath(output);
                    return Path.GetFullPath(
                        File.Exists(output) ? Path.GetDirectoryName(output) : output);

                default:
                    return Path.GetFullPath(
                        Path.Combine(MGKitEditorPaths.ProjectRoot, config.DefaultOutputDir));
            }
        }

        public static void WriteCiOutputMarker(MiniGamePlatform platform, string fullPath)
        {
            var ciDir = Path.Combine(MGKitEditorPaths.ProjectRoot, "build", "ci");
            Directory.CreateDirectory(ciDir);
            var file = Path.Combine(ciDir, $"{platform}-{CiOutputFileName}");
            File.WriteAllText(file, Path.GetFullPath(fullPath));
            Debug.Log($"[Build] CI 产物标记: {file} → {fullPath}");
        }

        public static string ReadCiOutputMarker(MiniGamePlatform platform)
        {
            var file = Path.Combine(
                MGKitEditorPaths.ProjectRoot,
                "build",
                "ci",
                $"{platform}-{CiOutputFileName}");
            return File.Exists(file) ? File.ReadAllText(file).Trim() : null;
        }

        public static string GetFlavorFolderName(MiniGamePlatform platform) =>
            platform switch
            {
                MiniGamePlatform.WeChatMiniGame => "wechat",
                MiniGamePlatform.DouyinMiniGame => "douyin",
                MiniGamePlatform.WebGL => "webgl",
                MiniGamePlatform.Android => "android",
                MiniGamePlatform.iOS => "ios",
                _ => platform.ToString().ToLowerInvariant(),
            };
    }
}

#endif
