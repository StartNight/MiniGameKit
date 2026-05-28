#if UNITY_EDITOR && UNITY_ADDRESSABLES

using System;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace MiniGameKit.Editor
{
    /// <summary>构建前捕获 Addressables Bundled Provider，构建结束后还原（方案 4A）。</summary>
    [Serializable]
    public class AddressablesProviderSnapshot
    {
        [Serializable]
        public class GroupProviderState
        {
            public string GroupName;
            public string BundleProviderType;
            public string BundledAssetProviderType;
        }

        public List<GroupProviderState> Groups = new List<GroupProviderState>();

        public static AddressablesProviderSnapshot Capture()
        {
            var snapshot = new AddressablesProviderSnapshot();
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                return snapshot;

            foreach (var group in settings.groups)
            {
                if (group == null)
                    continue;
                var schema = group.GetSchema<BundledAssetGroupSchema>();
                if (schema == null)
                    continue;

                snapshot.Groups.Add(new GroupProviderState
                {
                    GroupName = group.Name,
                    BundleProviderType = schema.BundleProviderType?.Value ?? string.Empty,
                    BundledAssetProviderType = schema.BundledAssetProviderType?.Value ?? string.Empty,
                });
            }

            return snapshot;
        }

        public void Restore()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null || Groups.Count == 0)
                return;

            int restored = 0;
            foreach (var state in Groups)
            {
                var group = settings.FindGroup(state.GroupName);
                if (group == null)
                    continue;
                var schema = group.GetSchema<BundledAssetGroupSchema>();
                if (schema == null)
                    continue;

                var changed = false;
                if (!string.IsNullOrEmpty(state.BundleProviderType)
                    && schema.BundleProviderType.Value != state.BundleProviderType)
                {
                    schema.BundleProviderType.SetValue(state.BundleProviderType);
                    changed = true;
                }

                if (!string.IsNullOrEmpty(state.BundledAssetProviderType)
                    && schema.BundledAssetProviderType.Value != state.BundledAssetProviderType)
                {
                    schema.BundledAssetProviderType.SetValue(state.BundledAssetProviderType);
                    changed = true;
                }

                if (changed)
                {
                    EditorUtility.SetDirty(group);
                    restored++;
                }
            }

            if (restored > 0)
            {
                EditorUtility.SetDirty(settings);
                UnityEditor.AssetDatabase.SaveAssets();
                Debug.Log($"[Build] 已还原 {restored} 个 Addressables 分组的 Provider。");
            }
        }
    }
}

#endif
