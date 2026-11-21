using System;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Game.Networking
{
    /// <summary>
    /// 서버 프로세스 쪽 세션 로직. 호스트 토큰 승인, 상태 파일(READY/FAIL), 부모 프로세스·호스트 끊김 감시, 참가자 목록 방송을 맡는다.
    /// 커맨드라인 인자: --host-token, --status-file, --parent-pid, --mode direct. 인자가 없으면(에디터 멀티 테스트) 해당 기능만 건너뛴다.
    /// </summary>
    internal static class ServerSession
    {
        /// <summary>참가자 목록 named message 이름. 클라이언트가 같은 이름으로 수신한다.</summary>
        internal const string ParticipantsMessage = "lobby.participants";

        /// <summary>참가자 목록 메시지 버전. 형식: version(1) + hostClientId(8) + count(1) + clientId(8) * count.</summary>
        internal const byte ParticipantsVersion = 1;

        /// <summary>호스트가 아직 없거나 제외할 대상이 없을 때의 값.</summary>
        internal const ulong NoClient = ulong.MaxValue;

        private static ulong _hostClientId = NoClient;
        private static byte[] _hostToken;
        private static int _maxConnectionDataBytes;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _hostClientId = NoClient;
            _hostToken = null;
        }

        /// <summary>StartServer 전에 호출. 접속 승인과 감시를 연결한다.</summary>
        internal static void Attach(NetworkManager nm, ServerNetworkConfig config)
        {
            string token = DedicatedServerBootstrap.GetArg("--host-token");
            _hostToken = token == null ? null : Encoding.UTF8.GetBytes(token);
            _maxConnectionDataBytes = config.MaxConnectionDataBytes;
            _hostClientId = NoClient;

            nm.ConnectionApprovalCallback = Approve;
            nm.OnClientConnectedCallback += _ => Broadcast(nm, NoClient);
            nm.OnClientDisconnectCallback += id =>
            {
                if (id == _hostClientId)
                {
                    Debug.Log("[Server] 호스트 접속이 끊겨 서버를 종료한다");
                    Application.Quit(0);
                    return;
                }
                Broadcast(nm, id);
            };

            if (int.TryParse(DedicatedServerBootstrap.GetArg("--parent-pid"), out int pid))
            {
                nm.gameObject.AddComponent<ParentWatch>().Init(pid, config.ParentCheckIntervalSeconds);
            }
        }

        /// <summary>StartServer 성공 뒤 호출. 상태 파일에 READY를 쓴다.</summary>
        internal static void Ready(ushort port) => WriteStatus($"READY port={port}");

        /// <summary>기동 실패 때 호출. 상태 파일에 FAIL을 쓴다.</summary>
        internal static void WriteFail(string reason) => WriteStatus("FAIL " + reason.Replace('\r', ' ').Replace('\n', ' '));

        // 호스트 토큰이 맞으면 호스트, 토큰이 없으면 일반 참가자로 최소 승인한다(정원 판정은 후속 스토리)
        private static void Approve(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            response.CreatePlayerObject = false;
            byte[] data = request.Payload;
            if (data == null || data.Length == 0)
            {
                response.Approved = true;
                return;
            }

            if (data.Length > _maxConnectionDataBytes || _hostToken == null || !data.AsSpan().SequenceEqual(_hostToken))
            {
                response.Approved = false;
                response.Reason = "host token mismatch";
                return;
            }

            _hostClientId = request.ClientNetworkId;
            response.Approved = true;
        }

        private static void Broadcast(NetworkManager nm, ulong excluded)
        {
            // 끊김 콜백 시점에 목록에서 이미 빠졌는지 보장되지 않으므로 직접 제외한다
            ulong[] ids = nm.ConnectedClientsIds.Where(id => id != excluded).ToArray();
            using var writer = new FastBufferWriter(1 + 8 + 1 + 8 * ids.Length, Allocator.Temp);
            writer.WriteValueSafe(ParticipantsVersion);
            writer.WriteValueSafe(_hostClientId);
            writer.WriteValueSafe((byte)ids.Length);
            foreach (ulong id in ids) writer.WriteValueSafe(id);
            nm.CustomMessagingManager.SendNamedMessageToAll(ParticipantsMessage, writer, NetworkDelivery.ReliableSequenced);
        }

        // 임시 이름으로 쓴 뒤 이름을 바꿔 호스트가 반쯤 쓴 파일을 읽지 않게 한다. 이미 결과가 있으면 덮어쓰지 않는다
        private static void WriteStatus(string line)
        {
            string path = DedicatedServerBootstrap.GetArg("--status-file");
            if (path == null || File.Exists(path)) return;
            try
            {
                string temp = path + ".tmp";
                File.WriteAllText(temp, line);
                File.Move(temp, path);
            }
            catch (Exception e) { Debug.LogError("[Server] 상태 파일 쓰기 실패: " + e.Message); }
        }

        // 호스트가 강제 종료돼 접속 끊김 이벤트가 늦거나 없을 때를 대비해 부모 프로세스 생존을 주기적으로 확인한다
        private class ParentWatch : MonoBehaviour
        {
            private System.Diagnostics.Process _parent;
            private float _interval;
            private float _nextCheck;

            public void Init(int pid, float interval)
            {
                _interval = interval;
                try { _parent = System.Diagnostics.Process.GetProcessById(pid); }
                catch (ArgumentException) { _parent = null; } // 이미 종료된 PID
            }

            private void Update()
            {
                if (Time.realtimeSinceStartup < _nextCheck) return;
                _nextCheck = Time.realtimeSinceStartup + _interval;
                if (_parent != null && !_parent.HasExited) return;
                Debug.Log("[Server] 부모 프로세스가 종료돼 서버를 종료한다");
                Application.Quit(0);
            }
        }
    }
}
