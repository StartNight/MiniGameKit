using UnityEditor;
using UnityEngine;

namespace MGKit.Editor.AutoBuild
{
    public class AutoBuildWindow : EditorWindow
    {
        private int selectedPlatformIndex = 0;
        private readonly string[] platforms = { "WeChat", "Douyin", "Android", "iOS", "WebGL", "CrazyGames" };

        [MenuItem("Tools/自动构建/构建配置面板", priority = 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<AutoBuildWindow>("自动构建配置");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            GUILayout.Label("远程构建触发 (GitHub Actions)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("选择平台并触发远程构建。你可以通过打 Tag 或者直接调用 GitHub API 触发（推荐打 Tag 做版本记录）。", MessageType.Info);

            selectedPlatformIndex = EditorGUILayout.Popup("目标平台", selectedPlatformIndex, platforms);
            var selectedPlatform = platforms[selectedPlatformIndex];

            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"创建 Tag 触发 {selectedPlatform}"))
            {
                SubRepoUpdater.TriggerViaTag(selectedPlatform);
            }

            if (GUILayout.Button($"API 触发 {selectedPlatform}"))
            {
                SubRepoUpdater.TriggerViaAPI(selectedPlatform);
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.Space(20);
            GUILayout.Label("GitHub Actions 配置 (用于 API 触发)", EditorStyles.boldLabel);
            AutoBuildConfig.GithubOwnerRepo = EditorGUILayout.TextField("仓库 (Owner/Repo)", AutoBuildConfig.GithubOwnerRepo);
            AutoBuildConfig.GithubPAT = EditorGUILayout.PasswordField("GitHub PAT", AutoBuildConfig.GithubPAT);

            EditorGUILayout.Space(20);
            GUILayout.Label("构建产物子仓库设置 (Submodule)", EditorStyles.boldLabel);
            AutoBuildConfig.SubmodulePath = EditorGUILayout.TextField("本地相对路径", AutoBuildConfig.SubmodulePath);
            AutoBuildConfig.SubmoduleBranch = EditorGUILayout.TextField("目标推拉分支", AutoBuildConfig.SubmoduleBranch);

            if (GUILayout.Button("更新本地子仓库 (Git Submodule Update)"))
            {
                SubRepoUpdater.UpdateSubmodule();
            }

            EditorGUILayout.Space(20);
            GUILayout.Label("工作流生成", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("点击生成按钮，会在项目根目录的 .github/workflows/ 目录下生成 minigame-build.yml，随后你需要将其推送至代码库以启用 Actions。", MessageType.Warning);

            if (GUILayout.Button("生成 GitHub Actions 工作流文件"))
            {
                GitHubActionGenerator.GenerateWorkflow();
            }
        }
    }
}
