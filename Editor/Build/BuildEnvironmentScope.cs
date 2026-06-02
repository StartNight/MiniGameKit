#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;
using MGKit;

namespace MGKit.Editor
{
    /// <summary>
    /// 构建期环境：宏定义、PluginProfile、Addressables Provider 的快照与还原。
    /// </summary>
    public sealed class BuildEnvironmentScope : IDisposable
    {
        readonly MiniGamePlatform _platform;
        readonly BuildTargetGroup _group;
        readonly string _previousDefines;
        readonly BuildPluginProfileManager.Snapshot _pluginSnapshot;
#if UNITY_ADDRESSABLES
        readonly AddressablesProviderSnapshot _addressablesSnapshot;
#endif
        bool _disposed;

        BuildEnvironmentScope(
            MiniGamePlatform platform,
            BuildTargetGroup group,
            string previousDefines,
            BuildPluginProfileManager.Snapshot pluginSnapshot
#if UNITY_ADDRESSABLES
            ,
            AddressablesProviderSnapshot addressablesSnapshot
#endif
        )
        {
            _platform = platform;
            _group = group;
            _previousDefines = previousDefines;
            _pluginSnapshot = pluginSnapshot;
#if UNITY_ADDRESSABLES
            _addressablesSnapshot = addressablesSnapshot;
#endif
        }

        public static BuildEnvironmentScope Begin(MiniGamePlatform platform, BuildTargetGroup group)
        {
            var pluginSnapshot = BuildPluginProfileManager.CaptureWebGlPluginStates();
#if UNITY_ADDRESSABLES
            var addrSnapshot = AddressablesProviderSnapshot.Capture();
#endif
            var previousDefines = MiniGameBuildPipeline.ApplyScriptingDefines(platform, group);
            BuildPluginProfileManager.Apply(BuildPluginProfileManager.ForPlatform(platform));

            return new BuildEnvironmentScope(
                platform,
                group,
                previousDefines,
                pluginSnapshot
#if UNITY_ADDRESSABLES
                ,
                addrSnapshot
#endif
            );
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            try
            {
                MiniGameBuildPipeline.RestoreScriptingDefines(_previousDefines, _group);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Build] 还原 Scripting Defines 失败: {e.Message}");
            }

#if UNITY_ADDRESSABLES
            try
            {
                _addressablesSnapshot?.Restore();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Build] 还原 Addressables Provider 失败: {e.Message}");
            }
#endif

            try
            {
                BuildPluginProfileManager.Restore(_pluginSnapshot);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Build] 还原 PluginProfile 失败: {e.Message}");
            }
        }
    }
}

#endif
