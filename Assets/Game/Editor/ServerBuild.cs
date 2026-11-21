using System;
using System.Linq;
using Game.Networking;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>Dedicated Server 빌드와 네트워크 설정 에셋 생성(배치 모드 -executeMethod용).</summary>
    public static class ServerBuild
    {
        private const string ConfigPath = "Assets/Game/Configs/ServerNetworkConfig.asset";

        /// <summary>설정 에셋이 없으면 만들고 Preloaded Assets에 등록한다. 서버 빌드가 Resources 없이 읽기 위함.</summary>
        [MenuItem("Game/Networking/Ensure Server Network Config")]
        public static void EnsureConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<ServerNetworkConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ServerNetworkConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
                AssetDatabase.SaveAssets();
            }

            var prefabs = AssetDatabase.LoadAssetAtPath<Unity.Netcode.NetworkPrefabsList>("Assets/DefaultNetworkPrefabs.asset");
            if (config.PrefabsList == null && prefabs != null)
            {
                config.PrefabsList = prefabs;
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }

            var preloaded = PlayerSettings.GetPreloadedAssets().ToList();
            if (!preloaded.Contains(config))
            {
                preloaded.Add(config);
                PlayerSettings.SetPreloadedAssets(preloaded.ToArray());
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>-executeMethod Game.EditorTools.ServerBuild.Build -buildOutput &lt;exe 경로&gt;</summary>
        public static void Build()
        {
            string output = GetArg("-buildOutput");
            if (string.IsNullOrEmpty(output))
            {
                Debug.LogError("[ServerBuild] -buildOutput 인자가 없다.");
                EditorApplication.Exit(2);
                return;
            }

            EnsureConfig();

            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                subtarget = (int)StandaloneBuildSubtarget.Server,
                options = BuildOptions.None,
            };

            // BuildPlayer가 에디터의 활성 서브타깃을 Server로 바꿔 두면 에디터에 UNITY_SERVER가 정의되므로 되돌린다
            var previousSubtarget = EditorUserBuildSettings.standaloneBuildSubtarget;
            BuildReport report = BuildPipeline.BuildPlayer(options);
            EditorUserBuildSettings.standaloneBuildSubtarget = previousSubtarget;
            Debug.Log($"[ServerBuild] result={report.summary.result} size={report.summary.totalSize} errors={report.summary.totalErrors} warnings={report.summary.totalWarnings} time={report.summary.totalTime}");
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        private static string GetArg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }
    }
}
