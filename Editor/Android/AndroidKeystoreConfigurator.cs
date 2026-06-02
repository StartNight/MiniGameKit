using System.IO;
using UnityEditor;
using UnityEngine;

namespace MGKit.Editor
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

        [MenuItem(MGKitEditorPaths.AndroidMenu + "应用签名配置", false, 300)]
        public static void OpenSettings() => MGKitEditorSettingsWindow.OpenAndroidTab();

        [MenuItem(MGKitEditorPaths.AndroidMenu + "立即应用签名配置", false, 301)]
        public static void ApplyFromMenu()
        {
            ApplyIfConfigured();
            Debug.Log("[MGKit] Android 签名配置已应用。");
        }

        public static void ApplyIfConfigured()
        {
            var keystore = MGKitEditorPaths.AndroidKeystorePath;
            if (string.IsNullOrEmpty(keystore))
            {
                var legacy = Path.Combine(MGKitEditorPaths.ProjectRoot, "key/user.keystore");
                if (File.Exists(legacy))
                    keystore = legacy;
            }

            if (!string.IsNullOrEmpty(keystore) && File.Exists(keystore))
                PlayerSettings.Android.keystoreName = keystore;

            if (!string.IsNullOrEmpty(MGKitEditorPaths.AndroidKeystorePass))
                PlayerSettings.Android.keystorePass = MGKitEditorPaths.AndroidKeystorePass;

            if (!string.IsNullOrEmpty(MGKitEditorPaths.AndroidKeyaliasName))
                PlayerSettings.Android.keyaliasName = MGKitEditorPaths.AndroidKeyaliasName;

            if (!string.IsNullOrEmpty(MGKitEditorPaths.AndroidKeyaliasPass))
                PlayerSettings.Android.keyaliasPass = MGKitEditorPaths.AndroidKeyaliasPass;
        }
    }
}