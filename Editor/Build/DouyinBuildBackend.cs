#if UNITY_EDITOR

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MiniGameKit.Editor
{
    /// <summary>
    /// 抖音小游戏构建后端：反射调用 TTSDK/BGDT 构建 API（Unity 2022.3 无团结子平台时使用）。
    /// </summary>
    public static class DouyinBuildBackend
    {
        public static bool TryBuild(string buildPath, out string outputPath, out string error)
        {
            outputPath = null;
            error = null;

            if (string.IsNullOrWhiteSpace(buildPath))
            {
                var projectRoot = MiniGameKitEditorPaths.ProjectRoot;
                buildPath = PathCombine(projectRoot, "build/DouyinMiniGame");
            }

            buildPath = Path.GetFullPath(buildPath);
            Directory.CreateDirectory(buildPath);

            if (TryInvokeBuildManager(buildPath, out outputPath, out error))
                return true;

            error ??= "未找到可用的 TTSDK BuildManager 构建方法。请在 Unity 中打开 ByteGame/BGDT 完成首次配置，或升级 TTSDK。";
            return false;
        }

        static bool TryInvokeBuildManager(string buildPath, out string outputPath, out string error)
        {
            outputPath = null;
            error = null;

            var playerSettings = AssetDatabase.LoadAssetAtPath<PlayerSettings>("ProjectSettings/ProjectSettings.asset");
            if (playerSettings == null)
                playerSettings = null;

            var methodNames = new[]
            {
                "BuildForTuanjie",
                "BuildForWebGL",
                "BuildWebGL",
                "Build",
            };

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var name = asm.GetName().Name ?? string.Empty;
                if (!name.Contains("ttsdk", StringComparison.OrdinalIgnoreCase)
                    && !name.Contains("stark", StringComparison.OrdinalIgnoreCase)
                    && !name.Contains("bgdt", StringComparison.OrdinalIgnoreCase))
                    continue;

                Type[] types;
                try
                {
                    types = asm.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types?.Where(t => t != null).ToArray() ?? Array.Empty<Type>();
                }

                foreach (var type in types)
                {
                    if (type == null || type.Name != "BuildManager")
                        continue;

                    foreach (var methodName in methodNames)
                    {
                        if (TryInvokeStatic(type, methodName, buildPath, playerSettings, out outputPath))
                        {
                            Debug.Log($"[Build][Douyin] {type.FullName}.{methodName} 成功 → {outputPath}");
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        static bool TryInvokeStatic(
            Type buildManagerType,
            string methodName,
            string buildPath,
            PlayerSettings playerSettings,
            out string outputPath)
        {
            outputPath = null;
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

            foreach (var method in buildManagerType.GetMethods(flags))
            {
                if (!string.Equals(method.Name, methodName, StringComparison.Ordinal))
                    continue;
                if (method.ReturnType != typeof(string))
                    continue;

                var parameters = method.GetParameters();
                object result;
                try
                {
                    if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
                    {
                        result = method.Invoke(null, new object[] { buildPath });
                    }
                    else if (parameters.Length == 2
                             && parameters[0].ParameterType == typeof(string)
                             && parameters[1].ParameterType == typeof(PlayerSettings))
                    {
                        result = method.Invoke(null, new object[] { buildPath, playerSettings });
                    }
                    else
                    {
                        continue;
                    }
                }
                catch (TargetInvocationException ex)
                {
                    Debug.LogWarning($"[Build][Douyin] {buildManagerType.FullName}.{methodName} 调用异常: {ex.InnerException?.Message ?? ex.Message}");
                    continue;
                }

                var path = result as string;
                if (string.IsNullOrEmpty(path))
                {
                    Debug.LogWarning($"[Build][Douyin] {buildManagerType.FullName}.{methodName} 返回空路径。");
                    continue;
                }

                outputPath = path;
                return true;
            }

            return false;
        }

        static string PathCombine(string a, string b) =>
            Path.Combine(a, b).Replace('\\', '/');
    }
}

#endif
