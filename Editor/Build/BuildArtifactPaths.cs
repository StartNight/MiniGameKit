#if UNITY_EDITOR

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MiniGameKit.Editor
{
    /// <summary>解析各平台构建产物路径，供 CI 与同步脚本读取。</summary>
    public static class BuildArtifactPaths
    {
        public const string CiOutputFileName = "minigamekit-build-output.txt";

        public static string GetBuildRepoName() =>
            $"{PlayerSettings.productName}-build";

        public static string ResolveArtifactDirectory(BuildPlatform platform, BuildConfig config = null)
        {
            config ??= BuildConfig.Create(platform);

            switch (platform)
            {
                case BuildPlatform.WeChatMiniGame:
                    var dst = WeChatWASM.WXConvertCore.config?.ProjectConf?.DST;
                    if (!string.IsNullOrEmpty(dst))
                        return Path.GetFullPath(dst);
                    return Path.GetFullPath(
                        Path.Combine(MiniGameKitEditorPaths.ProjectRoot, config.DefaultOutputDir));

                case BuildPlatform.DouyinMiniGame:
                    var marker = ReadCiOutputMarker(platform);
                    if (!string.IsNullOrEmpty(marker))
                        return Path.GetFullPath(marker);
                    return Path.GetFullPath(
                        Path.Combine(MiniGameKitEditorPaths.ProjectRoot, config.DefaultOutputDir));

                case BuildPlatform.WebGL:
                case BuildPlatform.Android:
                case BuildPlatform.iOS:
                    var output = MiniGameBuildPipeline.ResolveOutputPath(
                        config,
                        config.GetBuildTarget());
                    if (platform == BuildPlatform.iOS)
                        return Path.GetFullPath(output);
                    return Path.GetFullPath(
                        File.Exists(output) ? Path.GetDirectoryName(output) : output);

                default:
                    return Path.GetFullPath(
                        Path.Combine(MiniGameKitEditorPaths.ProjectRoot, config.DefaultOutputDir));
            }
        }

        public static void WriteCiOutputMarker(BuildPlatform platform, string fullPath)
        {
            var ciDir = Path.Combine(MiniGameKitEditorPaths.ProjectRoot, "build", "ci");
            Directory.CreateDirectory(ciDir);
            var file = Path.Combine(ciDir, $"{platform}-{CiOutputFileName}");
            File.WriteAllText(file, Path.GetFullPath(fullPath));
            Debug.Log($"[Build] CI 产物标记: {file} → {fullPath}");
        }

        public static string ReadCiOutputMarker(BuildPlatform platform)
        {
            var file = Path.Combine(
                MiniGameKitEditorPaths.ProjectRoot,
                "build",
                "ci",
                $"{platform}-{CiOutputFileName}");
            return File.Exists(file) ? File.ReadAllText(file).Trim() : null;
        }

        public static string GetFlavorFolderName(BuildPlatform platform) =>
            platform switch
            {
                BuildPlatform.WeChatMiniGame => "wechat",
                BuildPlatform.DouyinMiniGame => "douyin",
                BuildPlatform.WebGL => "webgl",
                BuildPlatform.Android => "android",
                BuildPlatform.iOS => "ios",
                _ => platform.ToString().ToLowerInvariant(),
            };
    }
}

#endif
