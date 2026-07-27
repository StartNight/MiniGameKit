using System;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;

namespace MGKit.Editor
{
    public enum AddressablesProviderMode
    {
        Default,
        WeChat,
        Douyin,
    }

    /// <summary>
    /// Addressables「Content Packing &amp; Loading」：切换 AssetBundle / Bundled Asset Provider。
    /// 微信：<see cref="WXAssetBundleProvider"/> + <see cref="WXBundledAssetProvider"/>；
    /// 抖音：TTAssetBundleProvider + TTBundledAssetProvider（反射，无硬引用）；
    /// 其它平台：<see cref="AssetBundleProvider"/> + <see cref="BundledAssetProvider"/>。
    /// Localization 分组在 Addressables 里为只读，但仍会切换其 BundledAssetGroupSchema 上的 Provider。
    /// </summary>
    public static class AddressablesWeChatBuildMenu
    {
        const string AddrMenu = "Tools/Minigame/构建/Addressables/";
        const string MenuWeChatProviders = AddrMenu + "切换到微信 Provider";
        const string MenuDouyinProviders = AddrMenu + "切换到抖音 Provider";
        const string MenuDefaultProviders = AddrMenu + "切换到 Unity 默认 Provider";

        const string TTAssetBundleProviderTypeName =
            "UnityEngine.ResourceManagement.ResourceProviders.TTAssetBundleProvider, MiniGameKit.Runtime.Addressables.Douyin";
        const string TTBundledAssetProviderTypeName =
            "UnityEngine.ResourceManagement.ResourceProviders.TTBundledAssetProvider, MiniGameKit.Runtime.Addressables.Douyin";

        // ── 仅切换 Provider ──────────────────────────────────────────

        [MenuItem(MenuWeChatProviders, false, 10)]
        public static void ApplyWeChatMiniGameProviders() => ApplyProviders(AddressablesProviderMode.WeChat);

        [MenuItem(MenuWeChatProviders, true, 10)]
        public static bool ValidateWeChatProviders()
        {
            RefreshProviderMenuChecks();
            return true;
        }

        [MenuItem(MenuDouyinProviders, false, 11)]
        public static void ApplyDouyinMiniGameProviders() => ApplyProviders(AddressablesProviderMode.Douyin);

        [MenuItem(MenuDouyinProviders, true, 11)]
        public static bool ValidateDouyinProviders()
        {
            RefreshProviderMenuChecks();
            return true;
        }

        [MenuItem(MenuDefaultProviders, false, 12)]
        public static void ApplyDefaultProviders() => ApplyProviders(AddressablesProviderMode.Default);

        [MenuItem(MenuDefaultProviders, true, 12)]
        public static bool ValidateDefaultProviders()
        {
            RefreshProviderMenuChecks();
            return true;
        }

        /// <summary>
        /// Unity 校验函数返回 false 会禁用菜单项；勾选状态用 <see cref="Menu.SetChecked"/> 单独设置。
        /// </summary>
        static void RefreshProviderMenuChecks()
        {
            bool weChat = IsUsingWeChatProviders();
            bool douyin = IsUsingDouyinProviders();
            bool hasBundled = HasAnyBundledGroup();
            Menu.SetChecked(MenuWeChatProviders, weChat);
            Menu.SetChecked(MenuDouyinProviders, douyin);
            Menu.SetChecked(MenuDefaultProviders, hasBundled && !weChat && !douyin);
        }

        // ── 仅构建内容（保持当前 Provider）──────────────────────────

        [MenuItem(AddrMenu + "诊断 Provider 状态（输出到 Console）", false, 15)]
        public static void DiagnoseProvidersFromMenu() => LogProviderDiagnostics();

        [MenuItem(AddrMenu + "构建内容（不切换 Provider）", false, 20)]
        public static void BuildAddressablesOnly() => BuildAddressablesContent();

        // ── 切换 + 构建 ─────────────────────────────────────────────

        [MenuItem(AddrMenu + "切换为微信并构建内容", false, 30)]
        public static void BuildWithWeChatProviders()
        {
            if (!ApplyProviders(AddressablesProviderMode.WeChat))
                return;
            BuildAddressablesContent();
        }

        [MenuItem(AddrMenu + "切换为抖音并构建内容", false, 31)]
        public static void BuildWithDouyinProviders()
        {
            if (!ApplyProviders(AddressablesProviderMode.Douyin))
                return;
            BuildAddressablesContent();
        }

        [MenuItem(AddrMenu + "切换为默认并构建内容", false, 32)]
        public static void BuildWithDefaultProviders()
        {
            if (!ApplyProviders(AddressablesProviderMode.Default))
                return;
            BuildAddressablesContent();
        }

        // ── CI（无菜单）──────────────────────────────────────────────

        /// <summary>供 CI：<c>-executeMethod MGKit.Editor.AddressablesWeChatBuildMenu.BatchWeChat</c></summary>
        public static void BatchWeChat()
        {
            if (!ApplyProviders(AddressablesProviderMode.WeChat))
                EditorApplication.Exit(200);
            else if (!BuildAddressablesContent())
                EditorApplication.Exit(201);
            else
                EditorApplication.Exit(0);
        }

        /// <summary>供 CI：<c>-executeMethod MGKit.Editor.AddressablesWeChatBuildMenu.BatchDouyin</c></summary>
        public static void BatchDouyin()
        {
            if (!ApplyProviders(AddressablesProviderMode.Douyin))
                EditorApplication.Exit(200);
            else if (!BuildAddressablesContent())
                EditorApplication.Exit(201);
            else
                EditorApplication.Exit(0);
        }

        /// <summary>供 CI：<c>-executeMethod MGKit.Editor.AddressablesWeChatBuildMenu.BatchDefault</c></summary>
        public static void BatchDefault()
        {
            if (!ApplyProviders(AddressablesProviderMode.Default))
                EditorApplication.Exit(200);
            else if (!BuildAddressablesContent())
                EditorApplication.Exit(201);
            else
                EditorApplication.Exit(0);
        }

        /// <summary>供 MCP / 命令行：<c>-executeMethod ...BatchDiagnoseProviders</c></summary>
        public static void BatchDiagnoseProviders()
        {
            LogProviderDiagnostics();
            EditorApplication.Exit(0);
        }

        /// <summary>供 MCP 自动化：切换微信 → 诊断 → 切换默认 → 诊断。</summary>
        public static void BatchTestProviderSwitch()
        {
            Debug.Log("[Addressables] === Provider 切换测试开始 ===");
            ApplyProviders(AddressablesProviderMode.WeChat);
            LogProviderDiagnostics();
            ApplyProviders(AddressablesProviderMode.Default);
            LogProviderDiagnostics();
            Debug.Log("[Addressables] === Provider 切换测试结束 ===");
            EditorApplication.Exit(0);
        }

        public static bool ApplyProviders(bool weChat) =>
            ApplyProviders(weChat ? AddressablesProviderMode.WeChat : AddressablesProviderMode.Default);

        /// <returns>是否成功（找不到 Settings 或 Provider 类型不可用时返回 false）。</returns>
        public static bool ApplyProviders(AddressablesProviderMode mode)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[Addressables] 未找到 AddressableAssetSettings。");
                return false;
            }

            if (!TryResolveProviderTypes(mode, out var bundleProvider, out var bundledAssetProvider, out var modeLabel))
                return false;

            if (BundledAssetProviderTypeProperty == null || AssetBundleProviderTypeProperty == null)
            {
                Debug.LogError("[Addressables] 无法反射 BundledAssetGroupSchema Provider 属性，请检查 Addressables 包版本。");
                return false;
            }

            int changed = 0;
            int already = 0;
            int failed = 0;
            int skippedNoSchema = 0;
            var log = new StringBuilder();
            foreach (var group in settings.groups)
            {
                if (group == null)
                    continue;

                var schema = group.GetSchema<BundledAssetGroupSchema>();
                if (schema == null)
                {
                    skippedNoSchema++;
                    continue;
                }

                bool providerOk = SchemaUsesProviders(schema, bundleProvider, bundledAssetProvider);
                bool settingsOk = mode != AddressablesProviderMode.Douyin || SchemaMatchesDouyinSettings(schema);
                if (providerOk && settingsOk)
                {
                    already++;
                    continue;
                }

                bool providerChanged = false;
                if (!providerOk)
                {
                    if (!TrySetBundledSchemaProviders(schema, bundleProvider, bundledAssetProvider))
                    {
                        failed++;
                        continue;
                    }

                    providerChanged = true;
                }

                bool douyinSettingsChanged = mode == AddressablesProviderMode.Douyin
                    && !settingsOk
                    && TryApplyDouyinSchemaSettings(schema);

                if (providerChanged || douyinSettingsChanged)
                {
                    changed++;
                    log.AppendLine(FormatGroupLogLine(group));
                    EditorUtility.SetDirty(group);
                }
            }

            if (failed > 0)
            {
                Debug.LogError($"[Addressables] {failed} 个分组 Provider 写入失败，切换未完成。");
                RefreshProviderMenuChecks();
                return false;
            }

            if (changed > 0)
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                Debug.Log($"[Addressables] 已切换为{modeLabel} Provider，更新 {changed} 个分组（含 Localization）：\n{log}");
            }
            else if (already > 0)
                Debug.Log($"[Addressables] 已是{modeLabel} Provider（{already} 个 Bundled 分组，含 Localization，无需修改）。");
            else
                Debug.LogWarning(
                    "[Addressables] 未更新任何 Bundled 分组。" +
                    $"（无 BundledAssetGroupSchema 的分组: {skippedNoSchema}）");

            RefreshProviderMenuChecks();
            return true;
        }

        static bool TryResolveProviderTypes(
            AddressablesProviderMode mode,
            out Type bundleProvider,
            out Type bundledAssetProvider,
            out string modeLabel)
        {
            bundleProvider = null;
            bundledAssetProvider = null;
            modeLabel = null;

            switch (mode)
            {
                case AddressablesProviderMode.WeChat:
#if MGKIT_WECHAT
                    bundleProvider = typeof(WXAssetBundleProvider);
                    bundledAssetProvider = typeof(WXBundledAssetProvider);
                    modeLabel = "微信小游戏";
                    return true;
#else
                    ReportProviderUnavailable(
                        "微信",
                        "当前未启用 MGKIT_WECHAT（微信 UPM 未安装），请先切换到微信平台并安装微信小游戏 SDK。");
                    return false;
#endif

                case AddressablesProviderMode.Douyin:
                    bundleProvider = Type.GetType(TTAssetBundleProviderTypeName);
                    bundledAssetProvider = Type.GetType(TTBundledAssetProviderTypeName);
                    if (bundleProvider == null || bundledAssetProvider == null)
                    {
                        ReportProviderUnavailable(
                            "抖音",
                            "未找到 TTAssetBundleProvider / TTBundledAssetProvider。" +
                            "请先切换到抖音平台并安装 StarkSDK（需 DOUYINMINIGAME 宏）。");
                        return false;
                    }

                    modeLabel = "抖音小游戏";
                    return true;

                default:
                    bundleProvider = typeof(AssetBundleProvider);
                    bundledAssetProvider = typeof(BundledAssetProvider);
                    modeLabel = "Unity 默认";
                    return true;
            }
        }

        static void ReportProviderUnavailable(string platformLabel, string message)
        {
            string fullMessage = $"[Addressables] 无法切换到{platformLabel} Provider：{message}";
            if (Application.isBatchMode)
                Debug.LogError(fullMessage);
            else
                EditorUtility.DisplayDialog("Addressables Provider", fullMessage, "确定");
        }

        static bool TryApplyDouyinSchemaSettings(BundledAssetGroupSchema schema)
        {
            bool changed = false;
            try
            {
                if (schema.UseAssetBundleCache)
                {
                    schema.UseAssetBundleCache = false;
                    changed = true;
                }

                if (schema.UseAssetBundleCrc)
                {
                    schema.UseAssetBundleCrc = false;
                    changed = true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Addressables] 无法设置抖音 Cache/CRC（UseAssetBundleCache / UseAssetBundleCrc）：{ex.Message}");
            }

            if (changed)
                EditorUtility.SetDirty(schema);

            return changed;
        }

        static bool SchemaMatchesDouyinSettings(BundledAssetGroupSchema schema) =>
            !schema.UseAssetBundleCache && !schema.UseAssetBundleCrc;

        /// <returns>构建是否成功。</returns>
        public static bool BuildAddressablesContent()
        {
            AddressableAssetSettings.BuildPlayerContent(out var result);
            if (!string.IsNullOrEmpty(result.Error))
            {
                Debug.LogError($"[Addressables] 构建失败：{result.Error}");
                return false;
            }

            Debug.Log("[Addressables] BuildPlayerContent 完成。");
            return true;
        }

        /// <summary>当前激活构建目标为 WeixinMiniGame 时视为微信小游戏。</summary>
        public static bool ActiveBuildTargetIsWeixinMiniGame()
        {
            if (!Enum.TryParse<BuildTarget>("WeixinMiniGame", ignoreCase: false, out var wxTarget))
                return false;
            return EditorUserBuildSettings.activeBuildTarget == wxTarget;
        }

        public static bool IsUsingWeChatProviders()
        {
#if MGKIT_WECHAT
            return AllBundledGroupsUse(typeof(WXAssetBundleProvider), typeof(WXBundledAssetProvider));
#else
            return false;
#endif
        }

        public static bool IsUsingDouyinProviders()
        {
            var bundleProvider = Type.GetType(TTAssetBundleProviderTypeName);
            var bundledAssetProvider = Type.GetType(TTBundledAssetProviderTypeName);
            if (bundleProvider == null || bundledAssetProvider == null)
                return false;

            return AllBundledGroupsUseDouyin(bundleProvider, bundledAssetProvider);
        }

        static bool AllBundledGroupsUseDouyin(Type bundleProvider, Type bundledAssetProvider)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                return false;

            bool any = false;
            foreach (var group in settings.groups)
            {
                if (group == null)
                    continue;

                var schema = group.GetSchema<BundledAssetGroupSchema>();
                if (schema == null)
                    continue;

                any = true;
                if (!SchemaUsesProviders(schema, bundleProvider, bundledAssetProvider)
                    || !SchemaMatchesDouyinSettings(schema))
                    return false;
            }

            return any;
        }

        /// <summary>将所有 Bundled 分组的 Provider 类型输出到 Console（含只读分组）。</summary>
        public static void LogProviderDiagnostics()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[Addressables][诊断] 未找到 AddressableAssetSettings。");
                return;
            }

            var report = new StringBuilder();
            report.AppendLine("[Addressables][诊断] 当前各分组 Provider：");
            report.AppendLine(
                $"  汇总：微信模式={IsUsingWeChatProviders()}，抖音模式={IsUsingDouyinProviders()}，Bundled 分组数={CountBundledGroups()}");

            foreach (var group in settings.groups)
            {
                if (group == null)
                    continue;

                var schema = group.GetSchema<BundledAssetGroupSchema>();
                if (schema == null)
                    continue;

                string bundle = FormatProviderType(schema.AssetBundleProviderType.Value);
                string bundled = FormatProviderType(schema.BundledAssetProviderType.Value);
                string flags = group.ReadOnly ? "只读" : "可写";
                report.AppendLine($"  [{flags}] {group.Name}");
                report.AppendLine($"         Bundle: {bundle}");
                report.AppendLine($"         Bundled: {bundled}");
                report.AppendLine(
                    $"         Cache={schema.UseAssetBundleCache}, CRC={schema.UseAssetBundleCrc}");
            }

            Debug.Log(report.ToString());
        }

        static int CountBundledGroups()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                return 0;

            int count = 0;
            foreach (var group in settings.groups)
            {
                if (group == null)
                    continue;
                if (group.GetSchema<BundledAssetGroupSchema>() != null)
                    count++;
            }

            return count;
        }

        static string FormatGroupLogLine(AddressableAssetGroup group)
        {
            if (group.ReadOnly || IsLocalizationGroupName(group.Name))
                return $"  - {group.Name} (Localization/只读组)";
            return $"  - {group.Name}";
        }

        static bool IsLocalizationGroupName(string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
                return false;
            return groupName.StartsWith("Localization", StringComparison.OrdinalIgnoreCase);
        }

        static string FormatProviderType(Type type) =>
            type == null ? "<null>" : type.FullName;

        static bool HasAnyBundledGroup()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                return false;

            foreach (var group in settings.groups)
            {
                if (group == null)
                    continue;
                if (group.GetSchema<BundledAssetGroupSchema>() != null)
                    return true;
            }

            return false;
        }

        static bool AllBundledGroupsUse(Type bundleProvider, Type bundledAssetProvider)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                return false;

            bool any = false;
            foreach (var group in settings.groups)
            {
                if (group == null)
                    continue;

                var schema = group.GetSchema<BundledAssetGroupSchema>();
                if (schema == null)
                    continue;

                any = true;
                if (!SchemaUsesProviders(schema, bundleProvider, bundledAssetProvider))
                    return false;
            }

            return any;
        }

        static bool SchemaUsesProviders(
            BundledAssetGroupSchema schema,
            Type bundleProvider,
            Type bundledAssetProvider)
        {
            return schema.AssetBundleProviderType.Value == bundleProvider
                && schema.BundledAssetProviderType.Value == bundledAssetProvider;
        }

        /// <summary>
        /// 通过 BundledAssetGroupSchema 属性写入（会触发 Addressables 内部 SetDirty），避免 SerializedObject 直改字段不生效。
        /// </summary>
        static bool TrySetBundledSchemaProviders(
            BundledAssetGroupSchema schema,
            Type bundleProvider,
            Type bundledAssetProvider)
        {
            if (SchemaUsesProviders(schema, bundleProvider, bundledAssetProvider))
                return false;

            var bundledType = new SerializedType { Value = bundledAssetProvider, ValueChanged = true };
            var bundleType = new SerializedType { Value = bundleProvider, ValueChanged = true };

            if (BundledAssetProviderTypeProperty == null || AssetBundleProviderTypeProperty == null)
            {
                Debug.LogError("[Addressables] 无法反射 BundledAssetGroupSchema Provider 属性，请检查 Addressables 包版本。");
                return false;
            }

            BundledAssetProviderTypeProperty.SetValue(schema, bundledType);
            AssetBundleProviderTypeProperty.SetValue(schema, bundleType);
            EditorUtility.SetDirty(schema);
            return true;
        }

        static readonly PropertyInfo BundledAssetProviderTypeProperty =
            typeof(BundledAssetGroupSchema).GetProperty(
                nameof(BundledAssetGroupSchema.BundledAssetProviderType),
                BindingFlags.Instance | BindingFlags.Public);

        static readonly PropertyInfo AssetBundleProviderTypeProperty =
            typeof(BundledAssetGroupSchema).GetProperty(
                nameof(BundledAssetGroupSchema.AssetBundleProviderType),
                BindingFlags.Instance | BindingFlags.Public);
    }
}
