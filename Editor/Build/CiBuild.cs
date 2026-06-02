#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MiniGameKit.Editor
{
    /// <summary>
    /// GitHub Actions / 命令行统一构建入口。
    /// </summary>
    public static class CiBuild
    {
        const string FlavorArg = "buildFlavor";

        public static void BuildWebGL() => RunFlavor(BuildPlatform.WebGL);

        public static void BuildWeChat() => RunFlavor(BuildPlatform.WeChatMiniGame);

        public static void BuildDouyin() => RunFlavor(BuildPlatform.DouyinMiniGame);

        public static void BuildAndroid() => RunFlavor(BuildPlatform.Android);

        public static void BuildIOS() => RunFlavor(BuildPlatform.iOS);

        public static void BuildFromAction()
        {
            var options = ParseCommandLine(out var flavor);
            if (flavor.HasValue)
            {
                RunFlavor(flavor.Value);
            }
            else
            {
                Debug.LogError("[CiBuild] 缺少或无法解析 -buildFlavor 参数，无法执行 Action 构建");
                EditorApplication.Exit(101);
            }
        }

        static void RunFlavor(BuildPlatform platform)
        {
            var options = ParseCommandLine(out var flavorFromArgs);
            var flavor = flavorFromArgs ?? platform;

            if (!options.TryGetValue("customBuildPath", out var customPath) || string.IsNullOrEmpty(customPath))
            {
                Debug.LogError("[CiBuild] 缺少 -customBuildPath");
                EditorApplication.Exit(130);
                return;
            }

            if (!EnsureBuildTargetForFlavor(flavor))
            {
                EditorApplication.Exit(122);
                return;
            }

            var config = BuildConfig.Create(flavor);
            config.OutputPath = customPath;
            config.SwitchBuildTarget = false;
            config.Interactive = false;
            config.AutoRun = false;

            bool success;
            BuildReport report = null;

            switch (flavor)
            {
                case BuildPlatform.WeChatMiniGame:
                    success = MiniGameBuildPipeline.RunWeChatBuild(config);
                    break;

                case BuildPlatform.DouyinMiniGame:
                    success = MiniGameBuildPipeline.RunDouyinBuild(config);
                    break;

                default:
                    report = MiniGameBuildPipeline.Run(config);
                    success = report != null && report.summary.result == BuildResult.Succeeded;
                    break;
            }

            if (!success)
            {
                Debug.LogError($"[CiBuild] {flavor} 构建失败");
                EditorApplication.Exit(101);
                return;
            }

            var artifactDir = BuildArtifactPaths.ResolveArtifactDirectory(flavor, config);
            BuildArtifactPaths.WriteCiOutputMarker(flavor, artifactDir);
            BuildCiManifest.Write(flavor, artifactDir);

            Debug.Log($"[CiBuild] {flavor} 构建成功 → {artifactDir}");
            EditorApplication.Exit(0);
        }

        static bool EnsureBuildTargetForFlavor(BuildPlatform flavor)
        {
            var target = flavor switch
            {
                BuildPlatform.Android => BuildTarget.Android,
                BuildPlatform.iOS => BuildTarget.iOS,
                _ => BuildTarget.WebGL,
            };
            var group = flavor switch
            {
                BuildPlatform.Android => BuildTargetGroup.Android,
                BuildPlatform.iOS => BuildTargetGroup.iOS,
                _ => BuildTargetGroup.WebGL,
            };

            return MiniGameBuildPipeline.EnsureBuildTarget(target, group, flavor.ToString(), interactive: false);
        }

        static Dictionary<string, string> ParseCommandLine(out BuildPlatform? flavor)
        {
            flavor = null;
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            var args = Environment.GetCommandLineArgs();

            for (var i = 0; i < args.Length; i++)
            {
                if (!args[i].StartsWith("-", StringComparison.Ordinal))
                    continue;
                var key = args[i].TrimStart('-');
                var value = (i + 1 < args.Length && !args[i + 1].StartsWith("-", StringComparison.Ordinal))
                    ? args[++i]
                    : string.Empty;
                map[key] = value;

                if (key.Equals(FlavorArg, StringComparison.OrdinalIgnoreCase)
                    && Enum.TryParse<BuildPlatform>(value, true, out var parsed))
                {
                    flavor = parsed;
                }
            }

            return map;
        }
    }
}

#endif
