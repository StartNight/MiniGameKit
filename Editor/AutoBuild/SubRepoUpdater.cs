using System;
using System.Diagnostics;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace MGKit.Editor.AutoBuild
{
    public static class SubRepoUpdater
    {
        private static DateTime _lastTriggerTime = DateTime.MinValue;

        public static void UpdateSubmodule()
        {
            var path = AutoBuildConfig.SubmodulePath;
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("[AutoBuild] 未配置子仓库路径 (Submodule Path)！");
                return;
            }

            Debug.Log($"[AutoBuild] 开始更新子仓库: {path} ...");
            RunGitCommand($"submodule update --init --remote -- {path}");
        }

        public static void TriggerViaTag(string platform)
        {
            if ((DateTime.Now - _lastTriggerTime).TotalSeconds < 5)
            {
                Debug.LogWarning("[AutoBuild] 触发过于频繁，请稍后再试！");
                return;
            }
            _lastTriggerTime = DateTime.Now;

            var tagName = $"Build_{platform}_{DateTime.Now:yyyyMMdd_HHmmss}";
            Debug.Log($"[AutoBuild] 尝试通过打标签 (Tag) 来触发 {platform} 的构建: {tagName}");
            
            var result = RunGitCommand($"tag {tagName}");
            if (result)
            {
                Debug.Log("[AutoBuild] Tag 创建成功，正在推送到远端...");
                RunGitCommand($"push origin {tagName}");
                EditorUtility.DisplayDialog("构建触发成功", $"已推送 Tag:\n{tagName}\n\nGitHub Actions 应该很快会启动。", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("构建触发失败", "Git tag 创建失败，请检查 Console 日志。", "确定");
            }
        }

        public static void TriggerViaAPI(string platform)
        {
            if ((DateTime.Now - _lastTriggerTime).TotalSeconds < 5)
            {
                Debug.LogWarning("[AutoBuild] 触发过于频繁，请稍后再试！");
                return;
            }
            _lastTriggerTime = DateTime.Now;

            var repo = AutoBuildConfig.GithubOwnerRepo;
            if (string.IsNullOrEmpty(repo) || repo == "Owner/Repo")
            {
                var autoRepo = GetAutoOwnerRepo();
                if (!string.IsNullOrEmpty(autoRepo))
                {
                    repo = autoRepo;
                    AutoBuildConfig.GithubOwnerRepo = repo;
                    Debug.Log($"[AutoBuild] 自动检测到 GitHub 仓库为: {repo}");
                }
            }

            var token = AutoBuildConfig.GithubPAT;
            if (string.IsNullOrEmpty(repo) || repo == "Owner/Repo" || string.IsNullOrEmpty(token))
            {
                EditorUtility.DisplayDialog("API 触发失败", "请先在构建配置面板 (MGKit -> AutoBuild Settings) 中配置正确的 GitHub Repo (例如 StartNight/OutbreakBowling-U3D) 和 Personal Access Token (PAT)！", "确定");
                return;
            }

            var url = $"https://api.github.com/repos/{repo}/actions/workflows/minigame-build.yml/dispatches";
            var json = $"{{\"ref\":\"master\",\"inputs\":{{\"targetPlatform\":\"{platform}\"}}}}";

            EditorApplication.delayCall += () =>
            {
                var request = new UnityWebRequest(url, "POST");
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {token}");
                request.SetRequestHeader("Accept", "application/vnd.github.v3+json");
                request.SetRequestHeader("User-Agent", "Unity-Editor-AutoBuild");

                var operation = request.SendWebRequest();
                operation.completed += _ =>
                {
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        Debug.Log($"[AutoBuild] 成功通过 API 触发 {platform} 的工作流！");
                        EditorUtility.DisplayDialog("API 触发成功", $"已成功触发 {platform} 的工作流！请前往 GitHub Actions 页面查看进度。", "确定");
                    }
                    else
                    {
                        Debug.LogError($"[AutoBuild] API 触发失败: {request.error}\n{request.downloadHandler.text}");
                        EditorUtility.DisplayDialog("API 触发失败", $"失败原因:\n{request.error}\n详情请看 Console。", "确定");
                    }
                    request.Dispose();
                };
            };
        }

        private static bool RunGitCommand(string arguments)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WorkingDirectory = MGKitEditorPaths.ProjectRoot
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    Debug.LogError($"[Git] git {arguments} 失败 (Code: {process.ExitCode}):\n{error}\n{output}");
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(output))
                    Debug.Log($"[Git] {output}");
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }
        public static string GetGitOutput(string arguments)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WorkingDirectory = MGKitEditorPaths.ProjectRoot
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    return null;
                }
                
                return output;
            }
            catch
            {
                return null;
            }
        }

        public static string GetAutoOwnerRepo()
        {
            var url = GetGitOutput("remote get-url origin");
            if (string.IsNullOrEmpty(url)) return null;

            url = url.Replace(".git", "").Trim();
            
            // Handle git@github.com:Owner/Repo
            int idx = url.LastIndexOf(':');
            if (idx > 0 && url.Contains("git@")) 
            {
                var parts = url.Substring(idx + 1).Split('/');
                if (parts.Length >= 2) return $"{parts[parts.Length - 2]}/{parts[parts.Length - 1]}";
            }
            
            // Handle https://github.com/Owner/Repo
            idx = url.IndexOf("github.com/");
            if (idx > 0)
            {
                var parts = url.Substring(idx + 11).Split('/');
                if (parts.Length >= 2) return $"{parts[0]}/{parts[1]}";
            }
            
            return null;
        }
    }
}
