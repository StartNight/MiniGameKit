using System;
using System.Diagnostics;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace MiniGameKit.Editor.AutoBuild
{
    public static class SubRepoUpdater
    {
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

        public static void TriggerViaCommit(string platform)
        {
            Debug.Log($"[AutoBuild] 尝试通过提交空 commit 来触发 {platform} 的构建...");
            var commitMsg = $"Build_{platform} via Editor trigger";
            var result = RunGitCommand($"commit --allow-empty -m \"{commitMsg}\"");
            if (result)
            {
                Debug.Log("[AutoBuild] 提交成功，正在推送到远端...");
                RunGitCommand("push");
                EditorUtility.DisplayDialog("构建触发成功", $"已推送 Commit:\n{commitMsg}\n\nGitHub Actions 应该很快会启动。", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("构建触发失败", "Git commit 失败，请检查 Console 日志。", "确定");
            }
        }

        public static void TriggerViaAPI(string platform)
        {
            var repo = AutoBuildConfig.GithubOwnerRepo;
            var token = AutoBuildConfig.GithubPAT;
            if (string.IsNullOrEmpty(repo) || string.IsNullOrEmpty(token))
            {
                EditorUtility.DisplayDialog("API 触发失败", "请先在构建配置面板中配置 GitHub Repo (Owner/Repo) 和 Personal Access Token (PAT)！", "确定");
                return;
            }

            var url = $"https://api.github.com/repos/{repo}/actions/workflows/minigame-build.yml/dispatches";
            var json = $"{{\"ref\":\"main\",\"inputs\":{{\"targetPlatform\":\"{platform}\"}}}}";

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
                        WorkingDirectory = MiniGameKitEditorPaths.ProjectRoot
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
    }
}
