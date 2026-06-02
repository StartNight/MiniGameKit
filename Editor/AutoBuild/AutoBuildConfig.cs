using UnityEditor;

namespace MGKit.Editor.AutoBuild
{
    public static class AutoBuildConfig
    {
        private const string Prefix = "MGKit.AutoBuild.";

        public static string GithubOwnerRepo
        {
            get => EditorPrefs.GetString(Prefix + "GithubOwnerRepo", "Owner/Repo");
            set => EditorPrefs.SetString(Prefix + "GithubOwnerRepo", value);
        }

        public static string GithubPAT
        {
            get => EditorPrefs.GetString(Prefix + "GithubPAT", "");
            set => EditorPrefs.SetString(Prefix + "GithubPAT", value);
        }

        public static string SubmodulePath
        {
            get => EditorPrefs.GetString(Prefix + "SubmodulePath", "Builds");
            set => EditorPrefs.SetString(Prefix + "SubmodulePath", value);
        }

        public static string SubmoduleBranch
        {
            get => EditorPrefs.GetString(Prefix + "SubmoduleBranch", "main");
            set => EditorPrefs.SetString(Prefix + "SubmoduleBranch", value);
        }
    }
}