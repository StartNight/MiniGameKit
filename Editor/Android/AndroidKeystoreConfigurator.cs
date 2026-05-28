using System.IO;
using UnityEditor;
using UnityEngine;

namespace MiniGameKit.Editor
{
    /// <summary>
    /// Android 签名配置（凭据存 EditorPrefs，不写死在代码中）。
    /// </summary>
    [InitializeOnLoad]
    public static class AndroidKeystoreConfigurator
    {
        static AndroidKeystoreConfigurator()
        {
            ApplyIfConfigured();
        }

        [MenuItem(MiniGameKitEditorPaths.AndroidMenu + "应用签名配置", false, 300)]
        public static void OpenSettings() => MiniGameKitEditorSettingsWindow.OpenAndroidTab();

        [MenuItem(MiniGameKitEditorPaths.AndroidMenu + "立即应用签名配置", false, 301)]
        public static void ApplyFromMenu()
        {
            ApplyIfConfigured();
            Debug.Log("[MiniGameKit] Android 签名配置已应用。");
        }

        public static void ApplyIfConfigured()
        {
            var keystore = MiniGameKitEditorPaths.AndroidKeystorePath;
            if (string.IsNullOrEmpty(keystore))
            {
                var legacy = Path.Combine(MiniGameKitEditorPaths.ProjectRoot, "key/user.keystore");
                if (File.Exists(legacy))
                    keystore = legacy;
            }

            if (!string.IsNullOrEmpty(keystore) && File.Exists(keystore))
                PlayerSettings.Android.keystoreName = keystore;

            if (!string.IsNullOrEmpty(MiniGameKitEditorPaths.AndroidKeystorePass))
                PlayerSettings.Android.keystorePass = MiniGameKitEditorPaths.AndroidKeystorePass;

            if (!string.IsNullOrEmpty(MiniGameKitEditorPaths.AndroidKeyaliasName))
                PlayerSettings.Android.keyaliasName = MiniGameKitEditorPaths.AndroidKeyaliasName;

            if (!string.IsNullOrEmpty(MiniGameKitEditorPaths.AndroidKeyaliasPass))
                PlayerSettings.Android.keyaliasPass = MiniGameKitEditorPaths.AndroidKeyaliasPass;
        }
    }
}
