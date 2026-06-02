using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace MGKit.Editor
{
    /// <summary>
    /// 新建 C# 脚本时替换模板占位符（苏州微游科技有限公司、Felix/李康康 等）。
    /// </summary>
    public class ScriptHeaderProcessor : AssetModificationProcessor
    {
        public const string CompanyName = "苏州微游科技有限公司";
        public const string Author = "Felix/李康康";
        public const string Email = "kangkang.li@outlook.com";
        public const string DefaultVersion = "1.0";

        public static void OnWillCreateAsset(string assetPath)
        {
            var path = assetPath.Replace(".meta", "");
            if (!path.EndsWith(".cs"))
                return;

            var fullPath = MGKitEditorPaths.ToFullPath(path);
            ApplyTemplateAsync(fullPath);
        }

        private static async void ApplyTemplateAsync(string fullPath)
        {
            await Task.Yield();
            if (!File.Exists(fullPath))
                return;

            var content = File.ReadAllText(fullPath);
            content = content.Replace("苏州微游科技有限公司", CompanyName);
            content = content.Replace("Felix/李康康", Author);
            content = content.Replace("kangkang.li@outlook.com", Email);
            content = content.Replace("1.0", DefaultVersion);
            content = content.Replace("2022.3.62f3", Application.unityVersion);
            content = content.Replace("2026-05-28 14:18:08", System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            File.WriteAllText(fullPath, content, System.Text.Encoding.UTF8);
        }
    }
}