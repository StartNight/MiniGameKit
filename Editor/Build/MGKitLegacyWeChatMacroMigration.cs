#if UNITY_EDITOR

using System.Linq;
using UnityEditor;
using UnityEngine;
using MGKit;

namespace MGKit.Editor
{
    /// <summary>
    /// 从 ProjectSettings 移除 WEIXINMINIGAME，避免微信 WASM 插件误判 Bee 产物目录。
    /// </summary>
    [InitializeOnLoad]
    static class MGKitLegacyWeChatMacroMigration
    {
        static MGKitLegacyWeChatMacroMigration()
        {
            Migrate(BuildTargetGroup.WebGL);
        }

        static void Migrate(BuildTargetGroup group)
        {
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            if (string.IsNullOrEmpty(defines))
                return;

            var list = defines.Split(';').Where(d => !string.IsNullOrEmpty(d)).ToList();
            if (!list.Remove(MGKitScriptingDefines.LegacyWeChatPluginMacro))
                return;

            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", list));
            Debug.Log(
                $"[MiniGameKit] 已从 {group} 移除废弃宏 {MGKitScriptingDefines.LegacyWeChatPluginMacro}，请使用 {MGKitScriptingDefines.WeChat}。");
        }
    }
}

#endif
