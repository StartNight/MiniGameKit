using UnityEditor;
using UnityEngine;

namespace MGKit.Editor
{
    /// <summary>
    /// MGKit Editor 可配置路径（通过 EditorPrefs 覆盖，便于不同项目复用）。
    /// </summary>
    public static class MGKitEditorPaths
    {
        private const string Prefix = "MGKit.";

        public const string MenuRoot = "Tools/Minigame/";

        public const string LocalizationMenu = MenuRoot + "本地化/";
        public const string UiMenu = MenuRoot + "UI/";
        public const string FontMenu = MenuRoot + "字体/";
        public const string AdMenu = MenuRoot + "广告/";
        public const string AdPlatformMenu = AdMenu + "平台/";
        public const string BuildMenu = MenuRoot + "构建/";
        public const string BuildAddressablesMenu = BuildMenu + "Addressables/";
        public const string BuildWeChatMenu = BuildMenu + "微信小游戏/";
        public const string BuildOptimizeMenu = BuildMenu + "优化/";
        public const string BuildLightmapMenu = BuildMenu + "光照/";
        public const string AndroidMenu = MenuRoot + "Android/";
        public const string ScriptMenu = MenuRoot + "脚本/";
        public const string UtilityMenu = MenuRoot + "工具/";

        public static string UiPrefabSearchRoot
        {
            get => EditorPrefs.GetString(Prefix + "UiPrefabRoot", "Assets/Prefabs/UI");
            set => EditorPrefs.SetString(Prefix + "UiPrefabRoot", value);
        }

        public static string TextureAstcSearchFolders
        {
            get => EditorPrefs.GetString(Prefix + "TexAstcFolders", "Assets/Textrue");
            set => EditorPrefs.SetString(Prefix + "TexAstcFolders", value);
        }

        public static string ShaderVariantCollectionAssetPath
        {
            get => EditorPrefs.GetString(Prefix + "SvcPath", "Assets/Resources/ProjectSVC.shadervariants");
            set => EditorPrefs.SetString(Prefix + "SvcPath", value);
        }

        /// <summary>分号分隔的 PlayerPrefs 键名，用于清理本地存档。</summary>
        public static string PlayerPrefsClearKeys
        {
            get => EditorPrefs.GetString(Prefix + "ClearKeys", "user_OutBearkBall_info_key;user_info_key");
            set => EditorPrefs.SetString(Prefix + "ClearKeys", value);
        }

        /// <summary>分号分隔，批量添加组件时搜索的 Prefab 根目录。</summary>
        public static string PrefabComponentBatchRoots
        {
            get => EditorPrefs.GetString(Prefix + "PrefabCompRoots", "Assets/Resources/UI");
            set => EditorPrefs.SetString(Prefix + "PrefabCompRoots", value);
        }

        public static string PrefabComponentTypeName
        {
            get => EditorPrefs.GetString(Prefix + "PrefabCompType", "DestroyOnDisable");
            set => EditorPrefs.SetString(Prefix + "PrefabCompType", value);
        }

        public static string[] SplitSemicolonPaths(string raw) =>
            string.IsNullOrWhiteSpace(raw)
                ? new string[0]
                : raw.Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries);

        public static string I2CsvAssetPath
        {
            get => EditorPrefs.GetString(Prefix + "I2Csv", "Assets/Localization/I2UITable.csv");
            set => EditorPrefs.SetString(Prefix + "I2Csv", value);
        }

        public static string I2LanguageSourceAssetPath
        {
            get => EditorPrefs.GetString(Prefix + "I2LanguageAsset", "Assets/Resources/I2Languages.asset");
            set => EditorPrefs.SetString(Prefix + "I2LanguageAsset", value);
        }

        public static string FontSubsetOutputFolder
        {
            get => EditorPrefs.GetString(Prefix + "FontOutput", "Assets/Fonts/FontSubsetPack");
            set => EditorPrefs.SetString(Prefix + "FontOutput", value);
        }

        public static string FontScanTargetTtf
        {
            get => EditorPrefs.GetString(Prefix + "FontTtf", "Assets/Fonts/simhei.ttf");
            set => EditorPrefs.SetString(Prefix + "FontTtf", value);
        }

        public static string[] FontScanScriptFolders
        {
            get
            {
                var raw = EditorPrefs.GetString(Prefix + "FontScriptFolders", "Assets/Scripts");
                return raw.Split(';');
            }
            set => EditorPrefs.SetString(Prefix + "FontScriptFolders", string.Join(";", value));
        }

        public static string FontScanLocalizationFolder
        {
            get => EditorPrefs.GetString(Prefix + "FontLocFolder", "Assets/Localization");
            set => EditorPrefs.SetString(Prefix + "FontLocFolder", value);
        }

        public static string FontScanPrefabRoots
        {
            get => EditorPrefs.GetString(Prefix + "FontPrefabRoots", "Assets");
            set => EditorPrefs.SetString(Prefix + "FontPrefabRoots", value);
        }

        public static string AndroidKeystorePath
        {
            get => EditorPrefs.GetString(Prefix + "AndroidKeystore", "");
            set => EditorPrefs.SetString(Prefix + "AndroidKeystore", value);
        }

        public static string AndroidKeystorePass
        {
            get => EditorPrefs.GetString(Prefix + "AndroidKeystorePass", "");
            set => EditorPrefs.SetString(Prefix + "AndroidKeystorePass", value);
        }

        public static string AndroidKeyaliasName
        {
            get => EditorPrefs.GetString(Prefix + "AndroidKeyalias", "key");
            set => EditorPrefs.SetString(Prefix + "AndroidKeyalias", value);
        }

        public static string AndroidKeyaliasPass
        {
            get => EditorPrefs.GetString(Prefix + "AndroidKeyaliasPass", "");
            set => EditorPrefs.SetString(Prefix + "AndroidKeyaliasPass", value);
        }

        public const string DefaultPlatformFeatureConfigAssetPath = "Assets/Resources/MGKit/PlatformFeatureConfig.asset";

        public static string PlatformFeatureConfigAssetPath
        {
            get => EditorPrefs.GetString(Prefix + "PlatformFeatureConfig", DefaultPlatformFeatureConfigAssetPath);
            set => EditorPrefs.SetString(Prefix + "PlatformFeatureConfig", value);
        }

        public static string ProjectRoot =>
            Application.dataPath.Replace("/Assets", "").Replace("\\Assets", "");

        public static string ToFullPath(string assetPath) =>
            System.IO.Path.Combine(ProjectRoot, assetPath.Replace('/', System.IO.Path.DirectorySeparatorChar));
    }
}