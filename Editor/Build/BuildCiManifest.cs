#if UNITY_EDITOR

using System;
using System.IO;
using UnityEngine;

namespace MiniGameKit.Editor
{
    /// <summary>
    /// CI 构建完成后写入 manifest，供 GitHub Actions 同步到 {ProductName}-build 仓库。
    /// </summary>
    [Serializable]
    public class BuildCiManifest
    {
        public string productName;
        public string flavor;
        public string platform;
        public string unityVersion;
        public string gitCommit;
        public string buildTimeUtc;
        public string artifactPath;
        public string buildRepoName;

        public static string ManifestPath =>
            Path.Combine(MiniGameKitEditorPaths.ProjectRoot, "build", "ci", "manifest.json");

        public static void Write(BuildPlatform platform, string artifactPath)
        {
            var manifest = new BuildCiManifest
            {
                productName = Application.productName,
                flavor = BuildArtifactPaths.GetFlavorFolderName(platform),
                platform = platform.ToString(),
                unityVersion = Application.unityVersion,
                gitCommit = ResolveGitCommit(),
                buildTimeUtc = DateTime.UtcNow.ToString("o"),
                artifactPath = Path.GetFullPath(artifactPath),
                buildRepoName = BuildArtifactPaths.GetBuildRepoName(),
            };

            var dir = Path.GetDirectoryName(ManifestPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(ManifestPath, JsonUtility.ToJson(manifest, true));
            Debug.Log($"[Build] CI manifest → {ManifestPath}");
        }

        static string ResolveGitCommit()
        {
            var env = Environment.GetEnvironmentVariable("GITHUB_SHA");
            if (!string.IsNullOrEmpty(env))
                return env;

            try
            {
                var projectRoot = MiniGameKitEditorPaths.ProjectRoot;
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "rev-parse HEAD",
                    WorkingDirectory = projectRoot,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using var p = System.Diagnostics.Process.Start(psi);
                if (p == null)
                    return "unknown";
                var output = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit(3000);
                return string.IsNullOrEmpty(output) ? "unknown" : output;
            }
            catch
            {
                return "unknown";
            }
        }
    }
}

#endif
