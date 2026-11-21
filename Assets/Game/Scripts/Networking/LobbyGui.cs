using Unity.Multiplayer;
using UnityEngine;

namespace Game.Networking
{
    /// <summary>
    /// 임시 로비 IMGUI(UI Toolkit 전환 때 다시 만든다). 참가자 목록, 호스트 LAN IP:포트, 참가 입력만 표시하고
    /// 모든 상태는 <see cref="NetworkSessionAdapter"/>에서 읽는다. 설정의 LobbyGuiEnabled가 켜져 있고
    /// DevDirectConnect가 꺼져 있으며 역할이 Server가 아닐 때만 자동 생성된다.
    /// </summary>
    public class LobbyGui : MonoBehaviour
    {
        // 최대 "255.255.255.255:65535" 길이
        private const int MaxJoinInputLength = 21;

        private NetworkSessionAdapter _session;
        private string _joinInput = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (MultiplayerRolesManager.ActiveMultiplayerRoleMask == MultiplayerRoleFlags.Server) return;
            var config = ServerNetworkConfig.Find();
            if (config == null || !config.LobbyGuiEnabled || config.DevDirectConnect) return;

            var go = new GameObject("LobbyGui");
            DontDestroyOnLoad(go);
            var session = go.AddComponent<NetworkSessionAdapter>();
            session.Init(config);
            go.AddComponent<LobbyGui>()._session = session;
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(20, 20, 360, 400), GUI.skin.box);
            switch (_session.State)
            {
                case SessionState.Idle:
                case SessionState.Failed:
                    DrawStart();
                    break;
                case SessionState.StartingServer:
                    GUILayout.Label("서버 기동 중...");
                    break;
                case SessionState.Connecting:
                    GUILayout.Label("접속 중...");
                    if (GUILayout.Button("취소")) _session.LeaveSession();
                    break;
                case SessionState.Connected:
                    DrawLobby();
                    break;
            }
            GUILayout.EndArea();
        }

        private void DrawStart()
        {
            if (_session.State == SessionState.Failed) GUILayout.Label(_session.FailureMessage);
            if (GUILayout.Button("세션 만들기")) _session.HostSession();
            GUILayout.Space(10);
            GUILayout.Label("참가할 호스트 주소 (IP:포트)");
            _joinInput = GUILayout.TextField(_joinInput, MaxJoinInputLength);
            if (GUILayout.Button("참가")) _session.JoinSession(_joinInput);
        }

        private void DrawLobby()
        {
            if (_session.IsHostSession) GUILayout.Label("내 LAN 주소: " + (_session.LanEndpoint ?? "LAN IP를 찾지 못함"));
            GUILayout.Label("참가자");
            foreach (ulong id in _session.Participants)
            {
                string tag = (id == _session.HostClientId ? " (호스트)" : "") + (id == _session.LocalClientId ? " (나)" : "");
                GUILayout.Label($"플레이어 {id}{tag}");
            }
            if (GUILayout.Button("나가기")) _session.LeaveSession();
        }
    }
}
