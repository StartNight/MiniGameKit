using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MiniGameKit.Editor
{
    /// <summary>
    /// WebGL CI 构建入口。本地构建请使用 MiniGameBuildWindow 或 MiniGameBuildMenu。
    /// </summary>
    /// <remarks>
    /// CI workflow：<c>buildMethod: MiniGameKit.Editor.WebGLCiBuild.Build</c>
    /// CI workflow (微信)：<c>buildMethod: MiniGameKit.Editor.WebGLCiBuild.BuildWeChat</c>
    /// </remarks>
    public static class WebGLCiBuild
    {
        static readonly string Eol = Environment.NewLine;

        const string DefaultLocalOutputDir = "build/WebGL";

        const string LocalOutputPathEditorPrefKey = "MiniGameKit.WebGLCiBuild.LocalOutputPath";

        const string BrowserWebGLTemplate = "PROJECT:WYMinigame2022";

        const string ReleaseEmscriptenArgs =
            " -s EXPORTED_FUNCTIONS=_main,_sbrk,_emscripten_stack_get_base,_emscripten_stack_get_end" +
            " -s ERROR_ON_UNDEFINED_SYMBOLS=0 -s TOTAL_MEMORY=256MB" +
            " -s EXPORTED_RUNTIME_METHODS='[\"ccall\",\"cwrap\",\"stackTrace\",\"addRunDependency\",\"removeRunDependency\"," +
            "\"FS_createPath\",\"FS_createDataFile\",\"stackTrace\",\"writeStackCookie\",\"checkStackCookie\"," +
            "\"lengthBytesUTF8\",\"stringToUTF8\"]'";

        /// <summary>
        /// 微信「生成并转换」前确保生成 External 符号文件（build.js.symbols），供 WX SDK preprocessSymbols 使用。
        /// 勿与 <see cref="ApplyReleaseSizeOptimizations"/> 混用。
        /// </summary>
        public static void EnsureWeChatExportPlayerSettings()
        {
#if UNITY_2021_2_OR_NEWER
            PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.External;
#else
            PlayerSettings.WebGL.debugSymbols = true;
#endif
        }

        public static void ApplyReleaseSizeOptimizations()
        {
#if UNITY_2021_2_OR_NEWER
            PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
#else
            PlayerSettings.WebGL.debugSymbols = false;
#endif
            PlayerSettings.WebGL.template = BrowserWebGLTemplate;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
            PlayerSettings.WebGL.emscriptenArgs = EnsureMainExport(PlayerSettings.WebGL.emscriptenArgs);

            // Fix: set IL2CPP compiler configuration to Release (avoids -O3 Master crashes)
            PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.WebGL, Il2CppCompilerConfiguration.Release);
            // Fix: set managed stripping level to High to reduce C++ code size
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.WebGL, ManagedStrippingLevel.High);
#if UNITY_2022_1_OR_NEWER
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, Il2CppCodeGeneration.OptimizeSize);
#endif
        }

        public static void ApplyLinkerSafeWebGLSettings()
        {
#if UNITY_2021_2_OR_NEWER
            PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
#else
            PlayerSettings.WebGL.debugSymbols = false;
#endif
            PlayerSettings.WebGL.emscriptenArgs = EnsureMainExport(PlayerSettings.WebGL.emscriptenArgs);
            PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.WebGL, Il2CppCompilerConfiguration.Release);
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.WebGL, ManagedStrippingLevel.High);
#if UNITY_2022_1_OR_NEWER
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, Il2CppCodeGeneration.OptimizeSize);
#endif
        }

        public static string EnsureMainExport(string emscriptenArgs)
        {
            if (string.IsNullOrWhiteSpace(emscriptenArgs))
                return ReleaseEmscriptenArgs;

            if (emscriptenArgs.Contains("_main"))
                return emscriptenArgs;

            const string marker = "EXPORTED_FUNCTIONS=";
            var markerIndex = emscriptenArgs.IndexOf(marker, StringComparison.Ordinal);
            if (markerIndex < 0)
                return emscriptenArgs + " -s EXPORTED_FUNCTIONS=_main";

            var insertIndex = markerIndex + marker.Length;
            return emscriptenArgs.Insert(insertIndex, "_main,");
        }

        /// <summary>CI 入口：WebGL H5（兼容旧 workflow，内部转 CiBuild）。</summary>
        public static void Build() => CiBuild.BuildWebGL();

        /// <summary>CI 入口：微信小游戏构建（兼容旧 workflow，内部转 CiBuild）。</summary>
        public static void BuildWeChat() => CiBuild.BuildWeChat();

        public static string GetLocalBuildPath()
        {
            var projectRoot = GetProjectRoot();
            var stored = EditorPrefs.GetString(LocalOutputPathEditorPrefKey, DefaultLocalOutputDir);
            if (string.IsNullOrWhiteSpace(stored))
                stored = DefaultLocalOutputDir;

            stored = stored.Trim().Replace('\\', '/');
            if (Path.IsPathRooted(stored))
                return Path.GetFullPath(stored);

            return Path.GetFullPath(Path.Combine(projectRoot, stored));
        }

        static string GetProjectRoot() =>
            Path.GetDirectoryName(Application.dataPath) ?? Directory.GetCurrentDirectory();

        static bool EnsureWebGLBuildTarget(bool interactive)
        {
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL)
                return true;

            if (!interactive)
                Debug.LogWarning("[WebGLCiBuild] 激活平台不是 WebGL，正在自动切换…");

            if (EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
                return true;

            Debug.LogError("[WebGLCiBuild] 切换到 WebGL 失败。");
            return false;
        }

        static Dictionary<string, string> GetValidatedOptions()
        {
            ParseCommandLineArguments(out var options);

            if (!options.TryGetValue("projectPath", out _))
            {
                Debug.LogError("Missing argument -projectPath");
                EditorApplication.Exit(110);
            }

            if (!options.TryGetValue("buildTarget", out var buildTarget)
                || !Enum.IsDefined(typeof(BuildTarget), buildTarget ?? string.Empty))
            {
                Debug.LogError($"Missing or invalid argument -buildTarget ({buildTarget})");
                EditorApplication.Exit(121);
            }

            if (!options.TryGetValue("customBuildPath", out _))
            {
                Debug.LogError("Missing argument -customBuildPath");
                EditorApplication.Exit(130);
            }

            return options;
        }

        static void ParseCommandLineArguments(out Dictionary<string, string> providedArguments)
        {
            providedArguments = new Dictionary<string, string>();
            var args = Environment.GetCommandLineArgs();

            Debug.Log(
                $"{Eol}###########################{Eol}" +
                $"# WebGLCiBuild settings #{Eol}" +
                $"###########################{Eol}");

            for (var current = 0; current < args.Length; current++)
            {
                if (!args[current].StartsWith("-", StringComparison.Ordinal))
                    continue;

                var flag = args[current].TrimStart('-');
                var next = current + 1;
                var flagHasValue = next < args.Length && !args[next].StartsWith("-", StringComparison.Ordinal);
                var value = flagHasValue ? args[next] : string.Empty;
                if (flagHasValue)
                    current++;

                Debug.Log($"Found flag \"{flag}\" with value \"{value}\".");
                providedArguments[flag] = value;
            }
        }

        static void ReportSummaryToLog(BuildSummary summary)
        {
            Debug.Log(
                $"{Eol}###########################{Eol}" +
                $"# Build results #{Eol}" +
                $"###########################{Eol}" +
                $"Duration: {summary.totalTime}{Eol}" +
                $"Warnings: {summary.totalWarnings}{Eol}" +
                $"Errors: {summary.totalErrors}{Eol}" +
                $"Size: {summary.totalSize} bytes{Eol}");
        }

        static void ExitAborted()
        {
            Debug.LogError("[WebGLCiBuild] Build aborted before BuildPlayer.");
            EditorApplication.Exit(200);
        }

        static void ExitWithResult(BuildResult result)
        {
            switch (result)
            {
                case BuildResult.Succeeded:
                    Debug.Log("[WebGLCiBuild] Build succeeded.");
                    EditorApplication.Exit(0);
                    break;
                case BuildResult.Failed:
                    Debug.LogError("[WebGLCiBuild] Build failed.");
                    EditorApplication.Exit(101);
                    break;
                case BuildResult.Cancelled:
                    Debug.LogError("[WebGLCiBuild] Build cancelled.");
                    EditorApplication.Exit(102);
                    break;
                default:
                    Debug.LogError("[WebGLCiBuild] Build result unknown.");
                    EditorApplication.Exit(103);
                    break;
            }
        }
    }
}
