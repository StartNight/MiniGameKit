using UnityEditor;
using UnityEngine;

namespace MGKit.Editor
{
    public class MGKitEditorSettingsWindow : EditorWindow
    {
        private int _tab;
        private string _scriptFoldersRaw;

        [MenuItem(MGKitEditorPaths.MenuRoot + "项目设置", false, 0)]
        public static void Open() => GetWindow<MGKitEditorSettingsWindow>("MGKit 设置");

        public static void OpenAndroidTab()
        {
            var w = GetWindow<MGKitEditorSettingsWindow>("MGKit 设置");
            w._tab = 4;
        }

        private void OnEnable()
        {
            _scriptFoldersRaw = string.Join(";", MGKitEditorPaths.FontScanScriptFolders);
        }

        private void OnGUI()
        {
            _tab = GUILayout.Toolbar(_tab, new[] { "本地化", "字体", "优化", "调试", "Android" });

            EditorGUILayout.Space(6);
            switch (_tab)
            {
                case 0:
                    DrawLocalization();
                    break;

                case 1:
                    DrawFont();
                    break;

                case 2:
                    DrawOptimization();
                    break;

                case 3:
                    DrawDebug();
                    break;

                case 4:
                    DrawAndroid();
                    break;
            }
        }

        private void DrawLocalization()
        {
            MGKitEditorPaths.I2CsvAssetPath =
                EditorGUILayout.TextField("I2 CSV", MGKitEditorPaths.I2CsvAssetPath);
            MGKitEditorPaths.I2LanguageSourceAssetPath =
                EditorGUILayout.TextField("LanguageSource Asset", MGKitEditorPaths.I2LanguageSourceAssetPath);
        }

        private void DrawFont()
        {
            MGKitEditorPaths.FontScanTargetTtf =
                EditorGUILayout.TextField("静态 TTF", MGKitEditorPaths.FontScanTargetTtf);
            MGKitEditorPaths.FontSubsetOutputFolder =
                EditorGUILayout.TextField("字符集输出目录", MGKitEditorPaths.FontSubsetOutputFolder);
            MGKitEditorPaths.FontScanLocalizationFolder =
                EditorGUILayout.TextField("本地化扫描目录", MGKitEditorPaths.FontScanLocalizationFolder);
            MGKitEditorPaths.FontScanPrefabRoots =
                EditorGUILayout.TextField("Prefab 扫描根目录", MGKitEditorPaths.FontScanPrefabRoots);

            EditorGUILayout.LabelField("脚本扫描目录（分号分隔）");
            _scriptFoldersRaw = EditorGUILayout.TextField(_scriptFoldersRaw);
            if (GUI.changed)
                MGKitEditorPaths.FontScanScriptFolders = _scriptFoldersRaw.Split(';');
        }

        private void DrawOptimization()
        {
            MGKitEditorPaths.UiPrefabSearchRoot =
                EditorGUILayout.TextField("UI Prefab 根目录", MGKitEditorPaths.UiPrefabSearchRoot);
            MGKitEditorPaths.TextureAstcSearchFolders =
                EditorGUILayout.TextField("ASTC 贴图目录 (;)", MGKitEditorPaths.TextureAstcSearchFolders);
            MGKitEditorPaths.ShaderVariantCollectionAssetPath =
                EditorGUILayout.TextField("SVC 输出路径", MGKitEditorPaths.ShaderVariantCollectionAssetPath);
        }

        private void DrawDebug()
        {
            MGKitEditorPaths.PlayerPrefsClearKeys =
                EditorGUILayout.TextField("清理存档键名 (;)", MGKitEditorPaths.PlayerPrefsClearKeys);
            MGKitEditorPaths.PrefabComponentBatchRoots =
                EditorGUILayout.TextField("批量组件 Prefab (;)", MGKitEditorPaths.PrefabComponentBatchRoots);
            MGKitEditorPaths.PrefabComponentTypeName =
                EditorGUILayout.TextField("组件类型名", MGKitEditorPaths.PrefabComponentTypeName);
        }

        private void DrawAndroid()
        {
            EditorGUILayout.HelpBox("密码保存在本机 EditorPrefs，请勿提交到版本库。", MessageType.Info);
            MGKitEditorPaths.AndroidKeystorePath =
                EditorGUILayout.TextField("Keystore 路径", MGKitEditorPaths.AndroidKeystorePath);
            MGKitEditorPaths.AndroidKeystorePass =
                EditorGUILayout.PasswordField("Keystore 密码", MGKitEditorPaths.AndroidKeystorePass);
            MGKitEditorPaths.AndroidKeyaliasName =
                EditorGUILayout.TextField("Keyalias", MGKitEditorPaths.AndroidKeyaliasName);
            MGKitEditorPaths.AndroidKeyaliasPass =
                EditorGUILayout.PasswordField("Keyalias 密码", MGKitEditorPaths.AndroidKeyaliasPass);

            if (GUILayout.Button("立即应用到 PlayerSettings"))
                AndroidKeystoreConfigurator.ApplyIfConfigured();
        }
    }
}