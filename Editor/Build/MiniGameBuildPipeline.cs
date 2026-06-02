/****************************************************
 * FileName:		MiniGameBuildPipeline
 * CompanyName:		苏州微游科技有限公司
 * Author:			Felix/李康康
 * Email:			kangkang.li@outlook.com
 * CreateTime:		2026-05-18 10:00:00
 * Version:			2.0
 * UnityVersion:	2022.3.43f1c1
 * Description:		通用构建流程，Editor窗口和CI共用
 *               	微信小游戏构建等同于插件"生成并转换"流程
 *
*****************************************************/

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using MGKit;

namespace MGKit.Editor
{
    

    public class BuildConfig
    {
        public MiniGamePlatform Platform = MiniGamePlatform.WebGL;
        public bool UseWeChatProvider = false;
        public bool ApplyWebGLOptimizations = false;
        public bool BuildAddressables = false;
        public bool SwitchBuildTarget = true;
        public bool AutoRun = false;
        public bool DevelopmentBuild = false;
        public bool Interactive = true;
        public string OutputPath = "";
        public string DefaultOutputDir = "build/WebGL";

        public static BuildConfig Create(MiniGamePlatform platform)
        {
            var config = new BuildConfig { Platform = platform };

            switch (platform)
            {
                case MiniGamePlatform.WeChatMiniGame:
                    config.UseWeChatProvider = true;
                    // 勿默认开启：ApplyReleaseSizeOptimizations 会关闭 debugSymbolMode，导致微信 preprocessSymbols 失败
                    config.ApplyWebGLOptimizations = false;
                    config.DefaultOutputDir = "build/WeChatMiniGame";
#if UNITY_ADDRESSABLES
                    config.BuildAddressables = true;
#endif
                    break;
                case MiniGamePlatform.DouyinMiniGame:
                    config.UseWeChatProvider = false;
                    config.DefaultOutputDir = "build/DouyinMiniGame";
#if UNITY_ADDRESSABLES
                    config.BuildAddressables = true;
#endif
                    break;
                case MiniGamePlatform.WebGL:
                    config.UseWeChatProvider = false;
                    config.ApplyWebGLOptimizations = true;
                    config.DefaultOutputDir = "build/WebGL";
#if UNITY_ADDRESSABLES
                    config.BuildAddressables = true;
#endif
                    break;
                case MiniGamePlatform.Android:
                    config.DefaultOutputDir = "build/Android";
                    break;
                case MiniGamePlatform.iOS:
                    config.DefaultOutputDir = "build/iOS";
                    break;
            }

            return config;
        }

        public BuildTarget GetBuildTarget()
        {
            return Platform switch
            {
                MiniGamePlatform.WeChatMiniGame => BuildTarget.WebGL,
                MiniGamePlatform.DouyinMiniGame => BuildTarget.WebGL,
                MiniGamePlatform.WebGL => BuildTarget.WebGL,
                MiniGamePlatform.Android => BuildTarget.Android,
                MiniGamePlatform.iOS => BuildTarget.iOS,
                _ => BuildTarget.WebGL
            };
        }

        public BuildTargetGroup GetBuildTargetGroup()
        {
            return Platform switch
            {
                MiniGamePlatform.WeChatMiniGame => BuildTargetGroup.WebGL,
                MiniGamePlatform.DouyinMiniGame => BuildTargetGroup.WebGL,
                MiniGamePlatform.WebGL => BuildTargetGroup.WebGL,
                MiniGamePlatform.Android => BuildTargetGroup.Android,
                MiniGamePlatform.iOS => BuildTargetGroup.iOS,
                _ => BuildTargetGroup.WebGL
            };
        }

        public string GetPlatformLabel()
        {
            return Platform switch
            {
                MiniGamePlatform.WeChatMiniGame => "微信小游戏",
                MiniGamePlatform.DouyinMiniGame => "抖音小游戏",
                MiniGamePlatform.WebGL => "WebGL",
                MiniGamePlatform.Android => "Android",
                MiniGamePlatform.iOS => "iOS",
                _ => Platform.ToString()
            };
        }
    }

    public static class MiniGameBuildPipeline
    {
        public static event Action<string> OnLog;
        public static event Action<float, string> OnProgress;

        static void Log(string message)
        {
            Debug.Log($"[Build] {message}");
            OnLog?.Invoke(message);
        }

        static void LogError(string message)
        {
            Debug.LogError($"[Build] {message}");
            OnLog?.Invoke($"[ERROR] {message}");
        }

        public static BuildReport Run(BuildConfig config)
        {
            var label = config.GetPlatformLabel();
            var target = config.GetBuildTarget();
            var group = config.GetBuildTargetGroup();

            Log($"===== 开始构建: {label} =====");

            // ── 微信小游戏：调用插件完整「生成并转换」流程 ─────────────────
            if (config.Platform == MiniGamePlatform.WeChatMiniGame)
            {
                RunWeChatBuild(config);
                return null;
            }

            if (config.Platform == MiniGamePlatform.DouyinMiniGame)
            {
                RunDouyinBuild(config);
                return null;
            }

            using (BuildEnvironmentScope.Begin(config.Platform, group))
            {
                OnProgress?.Invoke(0.1f, "切换构建平台...");
                if (config.SwitchBuildTarget && !EnsureBuildTarget(target, group, label, config.Interactive))
                {
                    LogError("切换构建平台失败");
                    return null;
                }

#if UNITY_ADDRESSABLES
                OnProgress?.Invoke(0.2f, "切换Addressables Provider...");
                if (config.BuildAddressables)
                {
                    if (!AddressablesWeChatBuildMenu.ApplyProviders(weChat: config.UseWeChatProvider))
                    {
                        LogError("切换Addressables Provider失败");
                        return null;
                    }

                    OnProgress?.Invoke(0.4f, "构建Addressables内容...");
                    if (!AddressablesWeChatBuildMenu.BuildAddressablesContent())
                    {
                        LogError("Addressables构建失败");
                        return null;
                    }
                }
#else
                if (config.BuildAddressables)
                {
                    Log("未安装 Addressables，已跳过 Addressables 构建（本项目使用 Resources 等资源方案）。");
                }
#endif

                if (config.ApplyWebGLOptimizations && target == BuildTarget.WebGL)
                {
                    OnProgress?.Invoke(0.5f, "应用WebGL发布设置...");
                    WebGLCiBuild.ApplyReleaseSizeOptimizations();
                }

                var outputPath = ResolveOutputPath(config, target);
                Log($"输出路径: {outputPath}");

                OnProgress?.Invoke(0.6f, "构建Player...");
                var scenes = GetEnabledScenes();
                if (scenes == null)
                {
                    LogError("无可用构建场景");
                    return null;
                }

                var outputDir = Path.GetDirectoryName(Path.GetFullPath(outputPath));
                if (!string.IsNullOrEmpty(outputDir))
                    Directory.CreateDirectory(outputDir);

                var options = BuildOptions.None;
                if (config.DevelopmentBuild)
                    options |= BuildOptions.Development;
                if (config.AutoRun)
                    options |= BuildOptions.AutoRunPlayer;

                var buildPlayerOptions = new BuildPlayerOptions
                {
                    scenes = scenes,
                    target = target,
                    targetGroup = group,
                    locationPathName = outputPath,
                    options = options
                };

                Log($"BuildPlayer → {buildPlayerOptions.locationPathName}");
                var report = BuildPipeline.BuildPlayer(buildPlayerOptions);

                OnProgress?.Invoke(1f, "构建完成");

                if (report != null)
                {
                    Log($"构建结果: {report.summary.result} | 耗时: {report.summary.totalTime} | 大小: {report.summary.totalSize} bytes");
                }

                return report;
            }
        }

        /// <summary>
        /// 微信小游戏专用构建入口（返回 bool 而非 BuildReport）。
        /// 1. 切换 Addressables 为微信 Provider 并构建内容
        /// 2. 调用 WXConvertCore.DoExport()（等同于插件「生成并转换」）
        /// </summary>
        public static bool RunWeChatBuild(BuildConfig config)
        {
            var group = config.GetBuildTargetGroup();

            using (BuildEnvironmentScope.Begin(MiniGamePlatform.WeChatMiniGame, group))
            {
                if (config.SwitchBuildTarget
                    && !EnsureBuildTarget(BuildTarget.WebGL, group, "微信小游戏", config.Interactive))
                {
                    LogError("切换 WebGL 平台失败");
                    return false;
                }

                OnProgress?.Invoke(0.05f, "准备微信导出 PlayerSettings（External 调试符号）...");
                WebGLCiBuild.EnsureWeChatExportPlayerSettings();
                if (config.ApplyWebGLOptimizations)
                {
                    Log(
                        "已忽略「WebGL 发布优化」：该选项会关闭 Debug Symbols 并切换 H5 模板，与微信「生成并转换」冲突。");
                }

#if UNITY_ADDRESSABLES
                if (config.BuildAddressables)
                {
                    OnProgress?.Invoke(0.1f, "切换 Addressables 为微信 Provider...");
                    if (!AddressablesWeChatBuildMenu.ApplyProviders(weChat: true))
                    {
                        LogError("切换 Addressables Provider 失败");
                        return false;
                    }

                    OnProgress?.Invoke(0.2f, "构建 Addressables 内容...");
                    if (!AddressablesWeChatBuildMenu.BuildAddressablesContent())
                    {
                        LogError("Addressables 构建失败");
                        return false;
                    }

                    Log("Addressables 构建完成（微信 Provider）");
                }
#else
                if (config.BuildAddressables)
                {
                    Log("未安装 Addressables，已跳过 Addressables 构建（本项目使用 Resources 等资源方案）。");
                }
#endif

                OnProgress?.Invoke(0.4f, "执行微信小游戏「生成并转换」...");
                Log("调用 WXConvertCore.DoExport()（等同于插件面板「生成并转换」）");

#if WEIXINMINIGAME
#if TUANJIE_1_9_OR_NEWER
                WeChatWASM.WXConvertCore.RefreshEnableRenderThread();
#endif
                var exportError = WeChatWASM.WXConvertCore.DoExport(buildWebGL: true);

                OnProgress?.Invoke(1f, "构建完成");

                if (exportError != WeChatWASM.WXConvertCore.WXExportError.SUCCEED)
                {
                    LogError($"微信小游戏「生成并转换」失败，错误码: {exportError}，请查看 Console。");
                    return false;
                }
#else
                LogError("未检测到 WEIXINMINIGAME 宏，无法调用微信打包。请先通过 Platform Switcher 切换到微信小游戏。");
                return false;
#endif

                Log("微信小游戏「生成并转换」完成");
                var artifactDir = BuildArtifactPaths.ResolveArtifactDirectory(MiniGamePlatform.WeChatMiniGame, config);
                BuildArtifactPaths.WriteCiOutputMarker(MiniGamePlatform.WeChatMiniGame, artifactDir);
                BuildCiManifest.Write(MiniGamePlatform.WeChatMiniGame, artifactDir);
                return true;
            }
        }

        /// <summary>
        /// 抖音小游戏：TTSDK 官方构建 API + 环境快照还原。
        /// </summary>
        public static bool RunDouyinBuild(BuildConfig config)
        {
            var group = config.GetBuildTargetGroup();

            using (BuildEnvironmentScope.Begin(MiniGamePlatform.DouyinMiniGame, group))
            {
                if (config.SwitchBuildTarget
                    && !EnsureBuildTarget(BuildTarget.WebGL, group, "抖音小游戏", config.Interactive))
                {
                    LogError("切换 WebGL 平台失败");
                    return false;
                }

#if UNITY_ADDRESSABLES
                if (config.BuildAddressables)
                {
                    OnProgress?.Invoke(0.15f, "Addressables 使用 Unity 默认 Provider...");
                    if (!AddressablesWeChatBuildMenu.ApplyProviders(weChat: false))
                    {
                        LogError("切换 Addressables Provider 失败");
                        return false;
                    }

                    OnProgress?.Invoke(0.3f, "构建 Addressables 内容...");
                    if (!AddressablesWeChatBuildMenu.BuildAddressablesContent())
                    {
                        LogError("Addressables 构建失败");
                        return false;
                    }
                }
#else
                if (config.BuildAddressables)
                {
                    Log("未安装 Addressables，已跳过 Addressables 构建（本项目使用 Resources 等资源方案）。");
                }
#endif

                OnProgress?.Invoke(0.5f, "调用 TTSDK 构建...");
                var buildPath = string.IsNullOrEmpty(config.OutputPath)
                    ? ResolveOutputPath(config, BuildTarget.WebGL)
                    : config.OutputPath;

                if (!DouyinBuildBackend.TryBuild(buildPath, out var outputPath, out var error))
                {
                    LogError($"抖音小游戏构建失败: {error}");
                    return false;
                }

                Log($"抖音小游戏构建完成: {outputPath}");
                BuildArtifactPaths.WriteCiOutputMarker(MiniGamePlatform.DouyinMiniGame, outputPath);
                BuildCiManifest.Write(MiniGamePlatform.DouyinMiniGame, outputPath);
                return true;
            }
        }

        // ── Scripting Define Symbols 辅助方法 ──────────────────────────────

        /// <summary>
        /// 平台专属宏定义映射表。
        /// 每个平台只添加自己的宏，构建结束后通过 RestoreScriptingDefines 恢复原状。
        /// </summary>
        private static readonly Dictionary<MiniGamePlatform, string> PlatformDefines = new Dictionary<MiniGamePlatform, string>
        {
            { MiniGamePlatform.WeChatMiniGame, "WEIXINMINIGAME" },
            { MiniGamePlatform.DouyinMiniGame, "DOUYINMINIGAME" },
        };

        /// <summary>
        /// 为指定平台写入对应的 Scripting Define Symbol，并返回修改前的原始定义列表（用于事后还原）。
        /// 对于没有专属宏的平台（WebGL / Android / iOS），此方法会移除所有平台专属宏。
        /// </summary>
        public static string ApplyScriptingDefines(MiniGamePlatform platform, BuildTargetGroup group)
        {
            PlayerSettings.GetScriptingDefineSymbolsForGroup(group, out var currentDefines);
            var defineList = new List<string>(currentDefines);
            var originalDefines = string.Join(";", defineList);

            // 移除所有平台专属宏，避免旧宏残留
            foreach (var define in PlatformDefines.Values)
                defineList.Remove(define);

            // 添加当前平台对应的宏
            if (PlatformDefines.TryGetValue(platform, out var targetDefine))
            {
                if (!defineList.Contains(targetDefine))
                    defineList.Add(targetDefine);
                Log($"已添加 Scripting Define: {targetDefine}");
            }

            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, defineList.ToArray());
            return originalDefines;
        }

        /// <summary>
        /// 将 Scripting Define Symbols 还原为 ApplyScriptingDefines 调用前保存的原始值。
        /// </summary>
        public static void RestoreScriptingDefines(string previousDefines, BuildTargetGroup group)
        {
            if (previousDefines == null) return;
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, previousDefines.Split(';'));
            Log($"已还原 Scripting Define Symbols: {previousDefines}");
        }

        public static string ResolveOutputPath(BuildConfig config, BuildTarget target)
        {
            if (!string.IsNullOrEmpty(config.OutputPath))
                return config.OutputPath;

            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            var dir = Path.Combine(projectRoot, config.DefaultOutputDir);
            Directory.CreateDirectory(dir);

            var productName = PlayerSettings.productName;
            if (target == BuildTarget.Android)
                return Path.Combine(dir, $"{productName}.apk");
            if (target == BuildTarget.iOS)
                return dir;

            return dir;
        }

        public static bool EnsureBuildTarget(
            BuildTarget target,
            BuildTargetGroup group,
            string label,
            bool interactive = true)
        {
            if (EditorUserBuildSettings.activeBuildTarget == target)
                return true;

            if (interactive)
            {
                var msg = $"当前平台不是 {label}，构建前需要切换。\n是否现在切换？";
                if (!EditorUtility.DisplayDialog("切换构建平台", msg, "切换", "取消"))
                    return false;
            }
            else
            {
                Log($"自动切换构建平台 → {label}");
            }

            if (EditorUserBuildSettings.SwitchActiveBuildTarget(group, target))
                return true;

            if (interactive)
                EditorUtility.DisplayDialog("切换失败", $"无法切换到 {label}。", "确定");
            else
                LogError($"无法切换到 {label}。");
            return false;
        }

        public static string[] GetEnabledScenes()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("[Build] EditorBuildSettings 中无可用场景。");
                return null;
            }
            return scenes;
        }

        /// <summary>
        /// 判断某次构建是否是微信小游戏「生成并转换」成功（流程正常结束但无 BuildReport）。
        /// 用法：RunWeChatMiniGame 成功时返回 WeChatMiniGameSuccessReport（值为 null），
        /// 失败时返回 null 且日志已输出错误；所以需要额外标志位区分两种 null。
        /// 改进：通过返回一个静态 dummy 报告对象来区分「成功无报告」与「流程中断」，
        /// 但 BuildReport 无公开构造，改用 bool 标志位方案通过 ShowWeChatBuildResult。
        /// </summary>
        public static void ShowBuildResult(string label, BuildReport report)
        {
            if (report == null)
            {
                EditorUtility.DisplayDialog($"{label} 构建", "构建流程中断，请查看Console。", "确定");
                return;
            }

            if (report.summary.result == BuildResult.Succeeded)
            {
                EditorUtility.DisplayDialog($"{label} 构建完成",
                    $"产物目录：\n{report.summary.outputPath}\n\n耗时: {report.summary.totalTime}",
                    "确定");
            }
            else
            {
                EditorUtility.DisplayDialog($"{label} 构建失败",
                    $"结果：{report.summary.result}\n错误数：{report.summary.totalErrors}\n请查看 Console。",
                    "确定");
            }
        }

        /// <summary>
        /// 微信小游戏「生成并转换」专用结果弹窗。
        /// </summary>
        public static void ShowWeChatBuildResult(bool succeeded)
        {
            if (succeeded)
            {
#if WEIXINMINIGAME
                var dstPath = WeChatWASM.WXConvertCore.config?.ProjectConf?.DST ?? "（请查看微信小游戏插件配置中的导出路径）";
#else
                var dstPath = "（请查看微信小游戏插件配置中的导出路径）";
#endif
                EditorUtility.DisplayDialog("微信小游戏 构建完成",
                    $"生成并转换已完成。\n导出目录：\n{dstPath}\n\n请用微信开发者工具打开该目录。",
                    "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("微信小游戏 构建失败",
                    "生成并转换失败，请查看 Console 日志。",
                    "确定");
            }
        }

        public static void DiagnoseEnvironment()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("===== 构建环境诊断 =====");
            sb.AppendLine($"当前平台: {EditorUserBuildSettings.activeBuildTarget}");
            sb.AppendLine($"产品名: {PlayerSettings.productName}");
            sb.AppendLine($"包名: {PlayerSettings.applicationIdentifier}");
            sb.AppendLine($"Unity版本: {Application.unityVersion}");
            sb.AppendLine($"构建场景数: {EditorBuildSettings.scenes.Count(s => s.enabled)}");
#if UNITY_ADDRESSABLES
            AddressablesWeChatBuildMenu.LogProviderDiagnostics();
#else
            sb.AppendLine("Addressables: 未安装 (com.unity.addressables)");
#endif
            Debug.Log(sb.ToString());
        }
    }
}

#endif
