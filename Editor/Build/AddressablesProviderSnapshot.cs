#if UNITY_EDITOR && UNITY_ADDRESSABLES

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.ResourceManagement.Util;

namespace MGKit.Editor
{
    /// <summary>构建前捕获 Addressables Bundled Provider，构建结束后还原（方案 4A）。</summary>
    [Serializable]
    public class AddressablesProviderSnapshot
    {
        [Serializable]
        public class GroupProviderState
        {
            public string GroupName;
            public string AssetBundleProviderType;
            public string BundledAssetProviderType;
        }

        public List<GroupProviderState> Groups = new List<GroupProviderState>();

        static readonly PropertyInfo AssetBundleProviderTypeProperty =
            typeof(BundledAssetGroupSchema).GetProperty(
                nameof(BundledAssetGroupSchema.AssetBundleProviderType),
                BindingFlags.Instance | BindingFlags.Public);

        static readonly PropertyInfo BundledAssetProviderTypeProperty =
            typeof(BundledAssetGroupSchema).GetProperty(
                nameof(BundledAssetGroupSchema.BundledAssetProviderType),
                BindingFlags.Instance | BindingFlags.Public);

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

                var assetBundleProviderType = (SerializedType)AssetBundleProviderTypeProperty.GetValue(schema);
                var bundledAssetProviderType = (SerializedType)BundledAssetProviderTypeProperty.GetValue(schema);

                snapshot.Groups.Add(new GroupProviderState
                {
                    GroupName = group.Name,
                    AssetBundleProviderType = assetBundleProviderType.Value?.AssemblyQualifiedName ?? string.Empty,
                    BundledAssetProviderType = bundledAssetProviderType.Value?.AssemblyQualifiedName ?? string.Empty,
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
                if (TryRestoreProviderType(schema, AssetBundleProviderTypeProperty, state.AssetBundleProviderType))
                    changed = true;
                if (TryRestoreProviderType(schema, BundledAssetProviderTypeProperty, state.BundledAssetProviderType))
                    changed = true;

                if (changed)
                {
                    EditorUtility.SetDirty(group);
                    restored++;
                }
            }

            if (restored > 0)
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                Debug.Log($"[Build] 已还原 {restored} 个 Addressables 分组的 Provider。");
            }
        }

        static bool TryRestoreProviderType(
            BundledAssetGroupSchema schema,
            PropertyInfo property,
            string typeName)
        {
            if (property == null || string.IsNullOrEmpty(typeName))
                return false;

            var targetType = Type.GetType(typeName);
            if (targetType == null)
                return false;

            var current = (SerializedType)property.GetValue(schema);
            if (current.Value?.AssemblyQualifiedName == typeName)
                return false;

            property.SetValue(schema, new SerializedType { Value = targetType, ValueChanged = true });
            EditorUtility.SetDirty(schema);
            return true;
        }
    }
}

#endif
