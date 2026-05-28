using UnityEditor;
using UnityEngine;

namespace MiniGameKit.Editor
{
    public class MiniGameKitEditorSettingsWindow : EditorWindow
    {
        int _tab;
        string _scriptFoldersRaw;

        [MenuItem(MiniGameKitEditorPaths.MenuRoot + "项目设置", false, 0)]
        public static void Open() => GetWindow<MiniGameKitEditorSettingsWindow>("MiniGameKit 设置");

        public static void OpenAndroidTab()
        {
            var w = GetWindow<MiniGameKitEditorSettingsWindow>("MiniGameKit 设置");
            w._tab = 4;
        }

        void OnEnable()
        {
            _scriptFoldersRaw = string.Join(";", MiniGameKitEditorPaths.FontScanScriptFolders);
        }

        void OnGUI()
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

        void DrawLocalization()
        {
            MiniGameKitEditorPaths.I2CsvAssetPath =
                EditorGUILayout.TextField("I2 CSV", MiniGameKitEditorPaths.I2CsvAssetPath);
            MiniGameKitEditorPaths.I2LanguageSourceAssetPath =
                EditorGUILayout.TextField("LanguageSource Asset", MiniGameKitEditorPaths.I2LanguageSourceAssetPath);
        }

        void DrawFont()
        {
            MiniGameKitEditorPaths.FontScanTargetTtf =
                EditorGUILayout.TextField("静态 TTF", MiniGameKitEditorPaths.FontScanTargetTtf);
            MiniGameKitEditorPaths.FontSubsetOutputFolder =
                EditorGUILayout.TextField("字符集输出目录", MiniGameKitEditorPaths.FontSubsetOutputFolder);
            MiniGameKitEditorPaths.FontScanLocalizationFolder =
                EditorGUILayout.TextField("本地化扫描目录", MiniGameKitEditorPaths.FontScanLocalizationFolder);
            MiniGameKitEditorPaths.FontScanPrefabRoots =
                EditorGUILayout.TextField("Prefab 扫描根目录", MiniGameKitEditorPaths.FontScanPrefabRoots);

            EditorGUILayout.LabelField("脚本扫描目录（分号分隔）");
            _scriptFoldersRaw = EditorGUILayout.TextField(_scriptFoldersRaw);
            if (GUI.changed)
                MiniGameKitEditorPaths.FontScanScriptFolders = _scriptFoldersRaw.Split(';');
        }

        void DrawOptimization()
        {
            MiniGameKitEditorPaths.UiPrefabSearchRoot =
                EditorGUILayout.TextField("UI Prefab 根目录", MiniGameKitEditorPaths.UiPrefabSearchRoot);
            MiniGameKitEditorPaths.TextureAstcSearchFolders =
                EditorGUILayout.TextField("ASTC 贴图目录 (;)", MiniGameKitEditorPaths.TextureAstcSearchFolders);
            MiniGameKitEditorPaths.ShaderVariantCollectionAssetPath =
                EditorGUILayout.TextField("SVC 输出路径", MiniGameKitEditorPaths.ShaderVariantCollectionAssetPath);
        }

        void DrawDebug()
        {
            MiniGameKitEditorPaths.PlayerPrefsClearKeys =
                EditorGUILayout.TextField("清理存档键名 (;)", MiniGameKitEditorPaths.PlayerPrefsClearKeys);
            MiniGameKitEditorPaths.PrefabComponentBatchRoots =
                EditorGUILayout.TextField("批量组件 Prefab (;)", MiniGameKitEditorPaths.PrefabComponentBatchRoots);
            MiniGameKitEditorPaths.PrefabComponentTypeName =
                EditorGUILayout.TextField("组件类型名", MiniGameKitEditorPaths.PrefabComponentTypeName);
        }

        void DrawAndroid()
        {
            EditorGUILayout.HelpBox("密码保存在本机 EditorPrefs，请勿提交到版本库。", MessageType.Info);
            MiniGameKitEditorPaths.AndroidKeystorePath =
                EditorGUILayout.TextField("Keystore 路径", MiniGameKitEditorPaths.AndroidKeystorePath);
            MiniGameKitEditorPaths.AndroidKeystorePass =
                EditorGUILayout.PasswordField("Keystore 密码", MiniGameKitEditorPaths.AndroidKeystorePass);
            MiniGameKitEditorPaths.AndroidKeyaliasName =
                EditorGUILayout.TextField("Keyalias", MiniGameKitEditorPaths.AndroidKeyaliasName);
            MiniGameKitEditorPaths.AndroidKeyaliasPass =
                EditorGUILayout.PasswordField("Keyalias 密码", MiniGameKitEditorPaths.AndroidKeyaliasPass);

            if (GUILayout.Button("立即应用到 PlayerSettings"))
                AndroidKeystoreConfigurator.ApplyIfConfigured();
        }
    }
}
