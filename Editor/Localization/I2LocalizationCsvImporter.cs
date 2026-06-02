using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MGKit.Editor
{
    /// <summary>
    /// 将 CSV 词条导入 I2 LanguageSource（反射调用，不硬依赖 I2 程序集）。
    /// </summary>
    public static class I2LocalizationCsvImporter
    {
        [MenuItem(MGKitEditorPaths.LocalizationMenu + "导入 I2 CSV", false, 100)]
        public static void ImportFromMenu() => Import(MGKitEditorPaths.I2CsvAssetPath, MGKitEditorPaths.I2LanguageSourceAssetPath);

        public static void Import(string csvAssetPath, string languageSourceAssetPath)
        {
            try
            {
                ImportInternal(csvAssetPath, languageSourceAssetPath);
            }
            catch (Exception ex)
            {
                var logPath = Path.Combine(MGKitEditorPaths.ProjectRoot, "I2ImportError.txt");
                File.WriteAllText(logPath, ex.ToString());
                EditorUtility.DisplayDialog("导入失败", "请查看项目根目录 I2ImportError.txt", "确定");
                Debug.LogException(ex);
            }
        }

        private static void ImportInternal(string csvAssetPath, string languageSourceAssetPath)
        {
            var languageSourceAssetType = ReflectionTypeUtility.FindType("I2.Loc.LanguageSourceAsset");
            var languageSourceDataType = ReflectionTypeUtility.FindType("I2.Loc.LanguageSourceData");
            var termDataType = ReflectionTypeUtility.FindType("I2.Loc.TermData");

            if (languageSourceAssetType == null || languageSourceDataType == null || termDataType == null)
            {
                EditorUtility.DisplayDialog("错误", "未找到 I2 Localization，请确认项目中已安装 I2 插件。", "确定");
                return;
            }

            var resourcesDir = Path.Combine(Application.dataPath, "Resources");
            if (!Directory.Exists(resourcesDir))
            {
                Directory.CreateDirectory(resourcesDir);
                AssetDatabase.Refresh();
            }

            var sourceAsset = AssetDatabase.LoadAssetAtPath(languageSourceAssetPath, languageSourceAssetType) as ScriptableObject;
            if (sourceAsset == null)
            {
                sourceAsset = ScriptableObject.CreateInstance(languageSourceAssetType);
                AssetDatabase.CreateAsset(sourceAsset, languageSourceAssetPath);
            }

            var sourceDataProp = languageSourceAssetType.GetProperty("SourceData");
            object sourceData;
            if (sourceDataProp != null)
            {
                sourceData = sourceDataProp.GetValue(sourceAsset);
            }
            else
            {
                var mSourceField = ReflectionTypeUtility.FindInstanceField(languageSourceAssetType, "mSource");
                sourceData = mSourceField?.GetValue(sourceAsset);
            }

            ImportIntoSourceData(sourceData, languageSourceDataType, termDataType, csvAssetPath);

            EditorUtility.SetDirty(sourceAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("导入成功", $"词条已写入：\n{languageSourceAssetPath}", "确定");
        }

        private static void ImportIntoSourceData(object sourceData, Type sourceDataType, Type termDataType, string csvAssetPath)
        {
            if (sourceData == null)
            {
                Debug.LogError("[I2Import] SourceData 为 null。");
                return;
            }

            var mLanguages = ReflectionTypeUtility.FindInstanceField(sourceDataType, "mLanguages");
            var mTerms = ReflectionTypeUtility.FindInstanceField(sourceDataType, "mTerms");
            var languages = mLanguages?.GetValue(sourceData) as IList;
            var terms = mTerms?.GetValue(sourceData) as IList;

            if (languages == null || terms == null)
            {
                Debug.LogError("[I2Import] 无法访问 mLanguages / mTerms。");
                return;
            }

            languages.Clear();
            terms.Clear();

            var updateDict = sourceDataType.GetMethod("UpdateDictionary",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            updateDict?.Invoke(sourceData, new object[] { true });

            AddLanguage(sourceData, sourceDataType, "Chinese Simplified", "zh-CN");
            AddLanguage(sourceData, sourceDataType, "English", "en");

            var csvFullPath = MGKitEditorPaths.ToFullPath(csvAssetPath);
            if (!File.Exists(csvFullPath))
            {
                Debug.LogError("[I2Import] CSV 不存在：" + csvFullPath);
                return;
            }

            var eTermTypeEnum = ReflectionTypeUtility.FindType("I2.Loc.eTermType");
            MethodInfo addTermFast = null;
            object eTermTypeText = null;
            if (eTermTypeEnum != null)
            {
                eTermTypeText = Enum.Parse(eTermTypeEnum, "Text");
                addTermFast = sourceDataType.GetMethod("AddTerm",
                    new[] { typeof(string), eTermTypeEnum, typeof(bool) });
            }

            var addTermMethod = sourceDataType.GetMethod("AddTerm", new[] { typeof(string) });
            var lines = File.ReadAllLines(csvFullPath, Encoding.UTF8);
            var imported = 0;

            for (var i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                var cols = CsvLineParser.Parse(line);
                if (cols.Length < 4)
                    continue;

                var key = cols[0].Trim();
                var zhText = cols[3].Replace("\\n", "\n");
                var enText = cols.Length > 4 ? cols[4].Replace("\\n", "\n") : "";
                if (string.IsNullOrEmpty(key))
                    continue;

                object termData = null;
                if (addTermFast != null && eTermTypeText != null)
                    termData = addTermFast.Invoke(sourceData, new[] { key, eTermTypeText, false });
                else if (addTermMethod != null)
                    termData = addTermMethod.Invoke(sourceData, new object[] { key });
                else
                {
                    termData = Activator.CreateInstance(termDataType);
                    ReflectionTypeUtility.FindInstanceField(termDataType, "Term")?.SetValue(termData, key);
                    terms.Add(termData);
                }

                var langsField = ReflectionTypeUtility.FindInstanceField(termDataType, "Languages");
                langsField?.SetValue(termData, new[] { zhText, enText });
                imported++;
            }

            Debug.Log($"[I2Import] 共导入 {imported} 条词条。");
        }

        private static void AddLanguage(object sourceData, Type sourceDataType, string langName, string code)
        {
            var addLangMethod = sourceDataType.GetMethod("AddLanguage", new[] { typeof(string), typeof(string) });
            if (addLangMethod != null)
            {
                addLangMethod.Invoke(sourceData, new object[] { langName, code });
                return;
            }

            var mLanguages = ReflectionTypeUtility.FindInstanceField(sourceDataType, "mLanguages");
            var list = mLanguages?.GetValue(sourceData) as IList;
            var langDataType = ReflectionTypeUtility.FindType("I2.Loc.LanguageData");
            if (list == null || langDataType == null)
                return;

            var ld = Activator.CreateInstance(langDataType);
            ReflectionTypeUtility.FindInstanceField(langDataType, "Name")?.SetValue(ld, langName);
            ReflectionTypeUtility.FindInstanceField(langDataType, "Code")?.SetValue(ld, code);
            list.Add(ld);
        }
    }
}