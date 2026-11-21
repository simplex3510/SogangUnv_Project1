using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Game.Networking
{
    /// <summary>서버·클라이언트 공용 네트워크 설정. 포트·주기·타임아웃을 코드 상수 대신 여기서 읽는다.</summary>
    [CreateAssetMenu(fileName = "ServerNetworkConfig", menuName = "Game/Networking/Server Network Config")]
    public class ServerNetworkConfig : ScriptableObject
    {
        [Tooltip("서버가 바인딩할 UDP 포트. 커맨드라인 -port로 덮어쓸 수 있다.")]
        public ushort Port = 7777;

        [Tooltip("서버가 바인딩할 주소. 0.0.0.0이면 모든 인터페이스.")]
        public string ListenAddress = "0.0.0.0";

        [Tooltip("네트워크 틱 주기(Hz). NetworkManager.NetworkConfig.TickRate에 반영된다.")]
        public uint TickRate = 30;

        [Tooltip("연결 타임아웃(ms). UnityTransport.DisconnectTimeoutMS에 반영된다.")]
        public int DisconnectTimeoutMs = 10000;

        [Tooltip("서버·클라이언트가 공유하는 NetworkPrefabs 목록.")]
        public NetworkPrefabsList PrefabsList;

        [Tooltip("서버 종료 사유를 남기는 파일 이름. 커맨드라인 -exitLog <경로>가 있으면 그 경로를 쓴다.")]
        public string ExitReasonFileName = "server-exit.log";

        [Tooltip("개발용 직접 접속. 켜져 있고 역할이 Client면 승인 없이 DevConnectAddress:Port로 접속한다. 기본 꺼짐.")]
        public bool DevDirectConnect = false;

        [Tooltip("개발용 직접 접속 주소(loopback).")]
        public string DevConnectAddress = "127.0.0.1";

        [Header("LAN Direct 세션 (Story 003)")]
        [Tooltip("호스트가 자식 프로세스로 띄울 Dedicated Server 실행 파일의 전체 경로. 비어 있으면 세션 생성이 실패한다.")]
        public string ServerExecutablePath = "";

        [Tooltip("서버가 상태 파일에 READY/FAIL을 쓸 때까지 호스트가 기다리는 시간(초). 넘으면 서버를 종료하고 세션 생성 실패.")]
        public float ReadyTimeoutSeconds = 15f;

        [Tooltip("클라이언트가 서버에 접속 승인을 받을 때까지 기다리는 시간(초). 넘으면 참가 실패.")]
        public float ConnectTimeoutSeconds = 10f;

        [Tooltip("상태 파일 이름 접두사. 사용자 임시 폴더에 접두사 + 새 GUID로 만든다.")]
        public string StatusFilePrefix = "dedicated-server-";

        [Tooltip("접속 요청 ConnectionData 최대 바이트 수. 넘으면 서버가 거부한다.")]
        public int MaxConnectionDataBytes = 64;

        [Tooltip("서버가 부모(호스트) 프로세스 생존을 확인하는 주기(초).")]
        public float ParentCheckIntervalSeconds = 1f;

        [Tooltip("임시 로비 GUI 표시 여부. DevDirectConnect가 꺼져 있고 역할이 Server가 아닐 때만 뜬다.")]
        public bool LobbyGuiEnabled = false;

        /// <summary>로드된 설정 에셋을 찾는다. 에디터 Play에서는 Preloaded Assets가 없으므로 AssetDatabase로 찾는다.</summary>
        public static ServerNetworkConfig Find()
        {
            var config = Resources.FindObjectsOfTypeAll<ServerNetworkConfig>().FirstOrDefault();
#if UNITY_EDITOR
            if (config == null)
            {
                string guid = UnityEditor.AssetDatabase.FindAssets("t:ServerNetworkConfig").FirstOrDefault();
                if (guid != null) config = UnityEditor.AssetDatabase.LoadAssetAtPath<ServerNetworkConfig>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
            }
#endif
            return config;
        }
    }
}
