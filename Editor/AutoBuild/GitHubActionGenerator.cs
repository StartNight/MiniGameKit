using System.IO;
using UnityEditor;
using UnityEngine;

namespace MGKit.Editor.AutoBuild
{
    public static class GitHubActionGenerator
    {
        public static void GenerateWorkflow()
        {
            var projectRoot = MGKitEditorPaths.ProjectRoot;
            var githubDir = Path.Combine(projectRoot, ".github", "workflows");
            if (!Directory.Exists(githubDir))
            {
                Directory.CreateDirectory(githubDir);
            }

            var filePath = Path.Combine(githubDir, "minigame-build.yml");
            var yamlContent = GetYamlContent();

            File.WriteAllText(filePath, yamlContent);
            Debug.Log($"[AutoBuild] 成功生成 GitHub Actions 工作流文件：{filePath}");
            AssetDatabase.Refresh();
        }

        private static string GetYamlContent()
        {
            return $@"name: MGKit Auto Build

on:
  workflow_dispatch:
    inputs:
      targetPlatform:
        description: 'Target Platform to Build'
        required: true
        default: 'WeChat'
        type: choice
        options:
          - WeChat
          - Douyin
          - Android
          - iOS
          - WebGL
          - CrazyGames
  push:
    tags:
      - 'Build_*'

jobs:
  build:
    name: Build MiniGame
    runs-on: ubuntu-latest
    steps:
      - name: Determine Target Platform
        id: determine_platform
        run: |
          if [ ""${{{{ github.event_name }}}}"" == ""workflow_dispatch"" ]; then
            INPUT_PLAT=""${{{{ github.event.inputs.targetPlatform }}}}""
            if [ ""$INPUT_PLAT"" == ""WeChat"" ]; then echo ""PLATFORM=WeChatMiniGame"" >> $GITHUB_ENV; fi
            if [ ""$INPUT_PLAT"" == ""Douyin"" ]; then echo ""PLATFORM=DouyinMiniGame"" >> $GITHUB_ENV; fi
            if [ ""$INPUT_PLAT"" == ""Android"" ]; then echo ""PLATFORM=Android"" >> $GITHUB_ENV; fi
            if [ ""$INPUT_PLAT"" == ""iOS"" ]; then echo ""PLATFORM=iOS"" >> $GITHUB_ENV; fi
            if [ ""$INPUT_PLAT"" == ""WebGL"" ]; then echo ""PLATFORM=WebGL"" >> $GITHUB_ENV; fi
            if [ ""$INPUT_PLAT"" == ""CrazyGames"" ]; then echo ""PLATFORM=CrazyGames"" >> $GITHUB_ENV; fi
          else
            TAG_NAME=""${{{{ github.ref_name }}}}""
            if [[ ""$TAG_NAME"" == Build_WeChat* ]]; then echo ""PLATFORM=WeChatMiniGame"" >> $GITHUB_ENV; fi
            if [[ ""$TAG_NAME"" == Build_Douyin* ]]; then echo ""PLATFORM=DouyinMiniGame"" >> $GITHUB_ENV; fi
            if [[ ""$TAG_NAME"" == Build_Android* ]]; then echo ""PLATFORM=Android"" >> $GITHUB_ENV; fi
            if [[ ""$TAG_NAME"" == Build_iOS* ]]; then echo ""PLATFORM=iOS"" >> $GITHUB_ENV; fi
            if [[ ""$TAG_NAME"" == Build_WebGL* ]]; then echo ""PLATFORM=WebGL"" >> $GITHUB_ENV; fi
            if [[ ""$TAG_NAME"" == Build_CrazyGames* ]]; then echo ""PLATFORM=CrazyGames"" >> $GITHUB_ENV; fi
          fi

      - name: Check Platform Validity
        run: |
          if [ -z ""${{{{ env.PLATFORM }}}}"" ]; then
            echo ""No matching Build_PLATFORM trigger found in tag or dispatch. Skipping build.""
            exit 0
          fi
          echo ""Building for Platform: ${{{{ env.PLATFORM }}}}""

      - name: Checkout Main Repository with Submodules
        if: env.PLATFORM != ''
        uses: actions/checkout@v4
        with:
          submodules: true
          token: ${{{{ secrets.PAT_FOR_SUBMODULE }}}} # Requires a PAT secret to push to submodules

      - name: Run Unity Build
        if: env.PLATFORM != ''
        uses: game-ci/unity-builder@v4
        env:
          UNITY_LICENSE: ${{{{ secrets.UNITY_LICENSE }}}}
          UNITY_EMAIL: ${{{{ secrets.UNITY_EMAIL }}}}
          UNITY_PASSWORD: ${{{{ secrets.UNITY_PASSWORD }}}}
        with:
          projectPath: .
          buildMethod: MGKit.Editor.CiBuild.BuildFromAction
          customParameters: -buildFlavor ${{{{ env.PLATFORM }}}} -customBuildPath build/${{{{ env.PLATFORM }}}}

      - name: Copy Artifacts to Submodule
        if: env.PLATFORM != ''
        run: |
          mkdir -p {AutoBuildConfig.SubmodulePath}/${{{{ env.PLATFORM }}}}
          cp -r build/${{{{ env.PLATFORM }}}}/* {AutoBuildConfig.SubmodulePath}/${{{{ env.PLATFORM }}}}/

      - name: Commit and Push to Submodule
        if: env.PLATFORM != ''
        run: |
          cd {AutoBuildConfig.SubmodulePath}
          git config user.name ""github-actions[bot]""
          git config user.email ""github-actions[bot]@users.noreply.github.com""
          git add .
          git commit -m ""Auto update build artifacts for ${{{{ env.PLATFORM }}}} from $GITHUB_SHA"" || echo ""No changes to commit""

          # Ensure branch exists and pull latest before push to avoid conflicts
          BRANCH=""{AutoBuildConfig.SubmoduleBranch}""
          git fetch origin $BRANCH
          git checkout $BRANCH || git checkout -b $BRANCH origin/$BRANCH

          # Push changes
          git push origin $BRANCH
";
        }
    }
}