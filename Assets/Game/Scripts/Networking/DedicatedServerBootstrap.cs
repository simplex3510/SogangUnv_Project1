#if UNITY_SERVER
using System;
using System.IO;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Game.Networking
{
    /// <summary>
    /// Dedicated Server 빌드(UNITY_SERVER) 전용 부트스트랩. NetworkManager를 만들어 StartServer하고 종료 사유를 파일에 남긴다.
    /// 카메라·UI 생성 생략 분기는 이후 스토리에서 이 기호로 나눈다. 포트는 UnityTransport가 -port 인자로도 덮어쓴다.
    /// </summary>
    public static class DedicatedServerBootstrap
    {
        private static string _exitLogPath;
        private static bool _exitLogged;
        private static float _autoQuitAt = -1f;

        // NGO ILPP 메시지 등록이 AfterSceneLoad라 그보다 먼저 StartServer하면 실패한다
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var config = Resources.FindObjectsOfTypeAll<ServerNetworkConfig>().FirstOrDefault();
            if (config == null)
            {
                Fail("ServerNetworkConfig를 찾지 못함(Preloaded Assets 누락)");
                return;
            }

            _exitLogPath = GetArg("-exitLog") ?? Path.Combine(Application.persistentDataPath, config.ExitReasonFileName);
            if (float.TryParse(GetArg("-autoQuitSeconds"), out float sec)) _autoQuitAt = Time.realtimeSinceStartup + sec;

            Application.quitting += () => LogExit("Application.quitting (정상 종료)");
            AppDomain.CurrentDomain.UnhandledException += (_, e) => LogExit("UnhandledException: " + e.ExceptionObject);

            // 비활성 상태로 구성한 뒤 켠다: NetworkConfig를 Awake 전에 채워야 NullReference가 나지 않는다
            var go = new GameObject("DedicatedServerNetworkManager");
            go.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(go);
            var transport = go.AddComponent<UnityTransport>();
            transport.DisconnectTimeoutMS = config.DisconnectTimeoutMs;
            transport.SetConnectionData(config.ListenAddress, config.Port, config.ListenAddress);

            var nm = go.AddComponent<NetworkManager>();
            nm.NetworkConfig = new NetworkConfig { NetworkTransport = transport, TickRate = config.TickRate };
            if (config.PrefabsList != null) nm.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(config.PrefabsList);
            go.SetActive(true);
            nm.OnTransportFailure += () => Fail("OnTransportFailure 이벤트");
            nm.OnServerStopped += _ => LogExit("OnServerStopped 이벤트");

            bool started;
            try { started = nm.StartServer(); }
            catch (Exception e) { Fail("StartServer 예외: " + e); return; }

            Debug.Log($"[Server] StartServer 반환값={started}");
            if (!started)
            {
                Fail($"StartServer가 false 반환 (포트 {transport.ConnectionData.Port} 바인딩 실패 추정)");
                return;
            }
            Debug.Log($"[Server] 기동 완료 port={transport.ConnectionData.Port} tick={config.TickRate}");

            // 유휴 서버 틱: 자동 종료 옵션 검사용(-autoQuitSeconds, 검증 전용)
            if (_autoQuitAt > 0f) go.AddComponent<AutoQuit>().Deadline = _autoQuitAt;
        }

        private static void Fail(string reason)
        {
            LogExit("실패: " + reason);
            Application.Quit(1);
        }

        private static void LogExit(string reason)
        {
            if (_exitLogged) return;
            _exitLogged = true;
            string line = $"{DateTime.Now:O} exit-reason: {reason}";
            Debug.Log("[Server] " + line);
            try { File.AppendAllText(_exitLogPath ?? "server-exit.log", line + Environment.NewLine); }
            catch (Exception e) { Debug.LogError("[Server] 종료 사유 파일 쓰기 실패: " + e.Message); }
        }

        private static string GetArg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        private class AutoQuit : MonoBehaviour
        {
            public float Deadline;
            private void Update()
            {
                if (Time.realtimeSinceStartup >= Deadline) Application.Quit(0);
            }
        }
    }
}
#endif
