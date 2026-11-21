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
    }
}
