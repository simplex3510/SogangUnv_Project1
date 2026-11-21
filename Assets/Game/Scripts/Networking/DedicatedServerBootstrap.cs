using System;
using System.IO;
using System.Linq;
using Unity.Multiplayer;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Game.Networking
{
    /// <summary>
    /// 역할(MultiplayerRolesManager.ActiveMultiplayerRoleMask) 기반 부트스트랩. 역할이 Server면 NetworkManager를 만들어 StartServer하고
    /// 종료 사유를 파일에 남긴다(에디터 서버 인스턴스와 서버 빌드가 같은 코드). 역할이 Client이고 개발용 직접 접속 플래그가 켜져 있으면 StartClient한다.
    /// 포트는 UnityTransport가 -port 인자로도 덮어쓴다.
    /// </summary>
    public static class DedicatedServerBootstrap
    {
        private static string _exitLogPath;
        private static bool _exitLogged;
        private static float _autoQuitAt = -1f;

        // 도메인 리로드를 꺼도 이전 Play 세션 값과 이벤트 구독이 남지 않게 한다
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _exitLogPath = null;
            _exitLogged = false;
            _autoQuitAt = -1f;
            Application.quitting -= OnQuitting;
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        }

        // 구성만 여기서 한다. NGO ILPP 메시지 등록도 AfterSceneLoad라 순서가 보장되지 않으므로 시작은 첫 프레임(DeferredStart.Start)으로 미룬다
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var role = MultiplayerRolesManager.ActiveMultiplayerRoleMask;
            bool isServer = role == MultiplayerRoleFlags.Server;
            var config = ServerNetworkConfig.Find();
            bool devClient = config != null && config.DevDirectConnect && role == MultiplayerRoleFlags.Client;
            if (!isServer && !devClient) return;
            if (config == null)
            {
                Fail("ServerNetworkConfig를 찾지 못함(Preloaded Assets 누락)");
                return;
            }

            if (isServer)
            {
                _exitLogPath = GetArg("-exitLog") ?? Path.Combine(Application.persistentDataPath, config.ExitReasonFileName);
                if (float.TryParse(GetArg("-autoQuitSeconds"), out float sec)) _autoQuitAt = Time.realtimeSinceStartup + sec;

                Application.quitting += OnQuitting;
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            }

            var nm = CreateManager(config, transport =>
            {
                if (isServer) transport.SetConnectionData(config.ListenAddress, config.Port, config.ListenAddress);
                else transport.SetConnectionData(config.DevConnectAddress, config.Port);
            });
            nm.gameObject.AddComponent<DeferredStart>().Run = () => StartNetwork(nm, nm.GetComponent<UnityTransport>(), config, isServer);
        }

        /// <summary>
        /// 서버·개발용 클라이언트·로비 클라이언트가 공유하는 NetworkManager 생성. 호출마다 Transport 설정을 처음부터 다시 구성한다.
        /// ConnectionApproval은 서버와 클라이언트가 같아야 접속 설정 해시가 맞으므로 여기서 한 번만 켠다.
        /// </summary>
        internal static NetworkManager CreateManager(ServerNetworkConfig config, Action<UnityTransport> configureTransport)
        {
            // 비활성 상태로 구성한 뒤 켠다: NetworkConfig를 Awake 전에 채워야 NullReference가 나지 않는다
            var go = new GameObject("DedicatedServerNetworkManager");
            go.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(go);
            var transport = go.AddComponent<UnityTransport>();
            transport.DisconnectTimeoutMS = config.DisconnectTimeoutMs;
            configureTransport(transport);

            var nm = go.AddComponent<NetworkManager>();
            nm.NetworkConfig = new NetworkConfig { NetworkTransport = transport, TickRate = config.TickRate, ConnectionApproval = true };
            if (config.PrefabsList != null) nm.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(config.PrefabsList);
            go.SetActive(true);
            return nm;
        }

        private static void StartNetwork(NetworkManager nm, UnityTransport transport, ServerNetworkConfig config, bool isServer)
        {
            if (!isServer)
            {
                // 클라이언트에서는 자기 접속에만 호출된다. 서버 로그의 id와 맞춰 보기 위해 남긴다
                nm.OnClientConnectedCallback += id => Debug.Log($"[Client] 접속 완료 내 id={id}");
                Debug.Log($"[Client] 개발용 직접 접속 {config.DevConnectAddress}:{config.Port} StartClient={nm.StartClient()}");
                return;
            }

            nm.OnClientConnectedCallback += id => Debug.Log($"[Server] 클라이언트 접속 id={id}");
            nm.OnTransportFailure += () => Fail("OnTransportFailure 이벤트");
            nm.OnServerStopped += _ => LogExit("OnServerStopped 이벤트");
            ServerSession.Attach(nm, config);

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
            ServerSession.Ready(transport.ConnectionData.Port);

            // 유휴 서버 틱: 자동 종료 옵션 검사용(-autoQuitSeconds, 검증 전용)
            if (_autoQuitAt > 0f) nm.gameObject.AddComponent<AutoQuit>().Deadline = _autoQuitAt;
        }

        private static void OnQuitting() => LogExit("Application.quitting (정상 종료)");

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e) => LogExit("UnhandledException: " + e.ExceptionObject);

        private static void Fail(string reason)
        {
            ServerSession.WriteFail(reason);
            LogExit("실패: " + reason);
            Application.Quit(1);
        }

        private static void LogExit(string reason)
        {
            if (_exitLogged) return;
            _exitLogged = true;
            string line = $"{DateTime.Now:O} exit-reason: {reason}";
            Debug.Log("[Server] " + line);
            if (Application.isEditor) return; // 에디터 서버 인스턴스는 로그만 남긴다
            try { File.AppendAllText(_exitLogPath ?? "server-exit.log", line + Environment.NewLine); }
            catch (Exception e) { Debug.LogError("[Server] 종료 사유 파일 쓰기 실패: " + e.Message); }
        }

        internal static string GetArg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        private class DeferredStart : MonoBehaviour
        {
            public Action Run;
            private void Start() => Run();
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
