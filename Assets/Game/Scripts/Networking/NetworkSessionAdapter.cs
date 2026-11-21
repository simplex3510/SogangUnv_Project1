using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Game.Networking
{
    /// <summary>세션 접속 방식. 1단계는 Direct뿐이다(Relay는 ADR-0002 2단계).</summary>
    public enum SessionMode { Direct }

    /// <summary>세션 어댑터 진행 상태. 로비 UI가 표시용으로 읽는다.</summary>
    public enum SessionState { Idle, StartingServer, Connecting, Connected, Failed }

    /// <summary>
    /// 네트워크 어댑터. 호스트의 서버 자식 프로세스 기동·상태 파일 대기·loopback 접속, 참가자의 IP:포트 접속,
    /// 서버가 방송하는 참가자 목록 수신을 맡는다. 모드 선택과 접속 문자열 해석은 이 계층에만 둔다.
    /// 세션마다 NetworkManager와 Transport를 새로 만든다.
    /// </summary>
    public class NetworkSessionAdapter : MonoBehaviour
    {
        private const string ReadyPrefix = "READY port=";

        private ServerNetworkConfig _config;
        private NetworkManager _nm;
        private Process _server;
        private string _statusFile;
        private float _connectDeadline;
        private string _pendingFailure;
        private bool _loggedBadMessage;

        /// <summary>현재 진행 상태.</summary>
        public SessionState State { get; private set; }

        /// <summary>마지막 실패 사유(State가 Failed일 때). 사용자에게 그대로 표시한다.</summary>
        public string FailureMessage { get; private set; }

        /// <summary>호스트 세션일 때 LAN IP:포트. LAN IP를 못 찾았거나 호스트가 아니면 null.</summary>
        public string LanEndpoint { get; private set; }

        /// <summary>이 클라이언트가 호스트 세션을 만들었는지.</summary>
        public bool IsHostSession { get; private set; }

        /// <summary>서버가 알려 준 접속 중 clientId 목록(서버 권위).</summary>
        public IReadOnlyList<ulong> Participants => _participants;

        /// <summary>서버가 알려 준 호스트 clientId. 없으면 <see cref="ServerSession.NoClient"/>.</summary>
        public ulong HostClientId { get; private set; } = ServerSession.NoClient;

        /// <summary>접속 후 내 clientId.</summary>
        public ulong LocalClientId { get; private set; }

        private readonly List<ulong> _participants = new();

        /// <summary>설정을 주입한다. 사용 전에 한 번 호출한다.</summary>
        public void Init(ServerNetworkConfig config) => _config = config;

        /// <summary>
        /// 접속 문자열을 (Direct, address, port)로 해석한다. IPv4 "a.b.c.d:port"만 허용하고 IPv6·호스트 이름·포트 0은 거부한다.
        /// </summary>
        public static bool TryParseEndpoint(string text, out SessionMode mode, out string address, out ushort port)
        {
            mode = SessionMode.Direct;
            address = null;
            port = 0;
            if (text == null) return false;

            string[] parts = text.Trim().Split(':');
            if (parts.Length != 2) return false;
            if (parts[0].Split('.').Length != 4) return false;
            if (!IPAddress.TryParse(parts[0], out IPAddress ip) || ip.AddressFamily != AddressFamily.InterNetwork) return false;
            if (!ushort.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out port) || port == 0) return false;

            address = ip.ToString();
            return true;
        }

        /// <summary>호스트 세션을 만든다: 서버 자식 프로세스 기동 → READY 대기 → loopback 접속.</summary>
        public void HostSession()
        {
            if (State != SessionState.Idle && State != SessionState.Failed) return;
            StartCoroutine(RunHost());
        }

        /// <summary>"IP:포트" 입력으로 참가한다. 형식이 틀리면 참가 실패로 표시한다.</summary>
        public void JoinSession(string input)
        {
            if (State != SessionState.Idle && State != SessionState.Failed) return;
            if (!TryParseEndpoint(input, out SessionMode _, out string address, out ushort port))
            {
                Fail("참가 실패: IP:포트 형식이 올바르지 않음");
                return;
            }
            StartClient(address, port, null);
        }

        /// <summary>세션을 나간다. 호스트가 나가면 서버는 호스트 끊김을 감지해 스스로 종료한다.</summary>
        public void LeaveSession()
        {
            // IsHostSession은 Cleanup 안에서 false가 되므로 호출 전에 읽는다. 호스트가 접속 전에 취소하면 서버는 호스트를 모르므로 직접 종료한다
            Cleanup(IsHostSession);
            State = SessionState.Idle;
        }

        private IEnumerator RunHost()
        {
            IsHostSession = true;
            State = SessionState.StartingServer;
            FailureMessage = null;

            string exe = _config.ServerExecutablePath;
            if (string.IsNullOrEmpty(exe) || !File.Exists(exe))
            {
                Fail("세션 생성 실패: 서버 실행 파일을 찾지 못함");
                yield break;
            }

            string token = Guid.NewGuid().ToString("N");
            _statusFile = Path.Combine(Path.GetTempPath(), _config.StatusFilePrefix + Guid.NewGuid().ToString("N") + ".status");
            var info = new ProcessStartInfo(exe)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = $"-port {_config.Port} --parent-pid {Process.GetCurrentProcess().Id} --host-token {token} --status-file \"{_statusFile}\" --mode direct",
            };
            try { _server = Process.Start(info); }
            catch (Exception e)
            {
                Fail("세션 생성 실패: 서버를 실행하지 못함 (" + e.Message + ")");
                yield break;
            }

            float deadline = Time.realtimeSinceStartup + _config.ReadyTimeoutSeconds;
            string status;
            while (true)
            {
                // 서버가 이름을 바꿔 달기 전에는 파일이 보이지 않으므로 읽을 수 있으면 완성본이다
                if (File.Exists(_statusFile))
                {
                    status = File.ReadAllText(_statusFile).Trim();
                    break;
                }
                if (_server.HasExited) { Fail("세션 생성 실패: 서버가 준비 전에 종료됨"); yield break; }
                if (Time.realtimeSinceStartup > deadline) { Fail("세션 생성 실패: 서버 준비 시간 초과"); yield break; }
                yield return null;
            }

            if (!status.StartsWith(ReadyPrefix, StringComparison.Ordinal)
                || !ushort.TryParse(status.Substring(ReadyPrefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out ushort port))
            {
                Fail("세션 생성 실패: " + status);
                yield break;
            }

            string lan = FindLanAddress();
            LanEndpoint = lan == null ? null : $"{lan}:{port}";
            StartClient(IPAddress.Loopback.ToString(), port, Encoding.UTF8.GetBytes(token));
        }

        private void StartClient(string address, ushort port, byte[] connectionData)
        {
            State = SessionState.Connecting;
            FailureMessage = null;
            _participants.Clear();
            HostClientId = ServerSession.NoClient;
            _loggedBadMessage = false;

            _nm = DedicatedServerBootstrap.CreateManager(_config, transport => transport.SetConnectionData(address, port));
            if (connectionData != null) _nm.NetworkConfig.ConnectionData = connectionData;
            _nm.OnClientConnectedCallback += OnConnected;
            _nm.OnClientDisconnectCallback += OnDisconnected;

            if (!_nm.StartClient())
            {
                Fail(IsHostSession ? "세션 생성 실패: 서버에 접속하지 못함" : "참가 실패: 접속을 시작하지 못함");
                return;
            }
            _nm.CustomMessagingManager.RegisterNamedMessageHandler(ServerSession.ParticipantsMessage, OnParticipants);
            _connectDeadline = Time.realtimeSinceStartup + _config.ConnectTimeoutSeconds;
        }

        private void OnConnected(ulong clientId)
        {
            // 클라이언트는 다른 참가자의 접속 콜백도 받으므로 내 id만 본다
            if (clientId != _nm.LocalClientId) return;
            LocalClientId = clientId;
            State = SessionState.Connected;
        }

        private void OnDisconnected(ulong clientId)
        {
            // 접속 전에 실패하면 서버 id(0)로 불리고, 접속 후 끊기면 내 id로 불린다. 다른 참가자의 끊김은 목록 메시지로 받는다
            if (clientId != NetworkManager.ServerClientId && clientId != _nm.LocalClientId) return;
            string reason = _nm.DisconnectReason;
            if (State == SessionState.Connected) _pendingFailure = "서버 연결이 끊김";
            else if (IsHostSession) _pendingFailure = "세션 생성 실패: 서버 접속이 거부됨" + (string.IsNullOrEmpty(reason) ? "" : " (" + reason + ")");
            else _pendingFailure = "참가 실패: 서버에 접속하지 못함" + (string.IsNullOrEmpty(reason) ? "" : " (" + reason + ")");
        }

        // 서버가 보낸 목록을 검증한 뒤 반영한다: 보낸 쪽은 서버, 버전·길이가 맞아야 한다
        private void OnParticipants(ulong senderId, FastBufferReader reader)
        {
            if (senderId != NetworkManager.ServerClientId || reader.Length < 10) { LogBadMessage(); return; }
            reader.ReadValueSafe(out byte version);
            reader.ReadValueSafe(out ulong host);
            reader.ReadValueSafe(out byte count);
            if (version != ServerSession.ParticipantsVersion || reader.Length - reader.Position != count * 8) { LogBadMessage(); return; }

            _participants.Clear();
            for (int i = 0; i < count; i++)
            {
                reader.ReadValueSafe(out ulong id);
                _participants.Add(id);
            }
            HostClientId = host;
        }

        // 잘못된 메시지 로그는 세션당 한 번만 남긴다
        private void LogBadMessage()
        {
            if (_loggedBadMessage) return;
            _loggedBadMessage = true;
            Debug.LogWarning("[Client] 잘못된 참가자 목록 메시지를 무시함");
        }

        private void Update()
        {
            if (_pendingFailure != null)
            {
                string message = _pendingFailure;
                _pendingFailure = null;
                Fail(message);
            }
            else if (State == SessionState.Connecting && _nm != null && Time.realtimeSinceStartup > _connectDeadline)
            {
                Fail(IsHostSession ? "세션 생성 실패: 서버 접속 시간 초과" : "참가 실패: 접속 시간 초과");
            }
        }

        private void Fail(string message)
        {
            Cleanup(true);
            FailureMessage = message;
            State = SessionState.Failed;
        }

        // 서버 프로세스를 죽이는 것은 기동 실패 때뿐이다. 정상 나가기는 서버가 호스트 끊김을 보고 스스로 종료한다
        private void Cleanup(bool killServer)
        {
            StopAllCoroutines();
            _pendingFailure = null;
            if (_nm != null)
            {
                // Shutdown은 지연 처리라 구독이 남으면 _nm=null 뒤에 끊김 콜백이 불린다
                _nm.OnClientConnectedCallback -= OnConnected;
                _nm.OnClientDisconnectCallback -= OnDisconnected;
                _nm.Shutdown();
                Destroy(_nm.gameObject);
                _nm = null;
            }
            if (killServer && _server != null && !_server.HasExited) _server.Kill();
            _server?.Dispose();
            _server = null;
            if (_statusFile != null)
            {
                File.Delete(_statusFile);
                _statusFile = null;
            }
            _participants.Clear();
            LanEndpoint = null;
            IsHostSession = false;
        }

        private void OnDestroy() => Cleanup(false);

        // 게이트웨이가 있는 활성 IPv4 어댑터의 첫 주소. 가상 어댑터는 게이트웨이가 없어 대부분 걸러진다
        private static string FindLanAddress()
        {
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up
                    || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback
                    || nic.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;

                IPInterfaceProperties props = nic.GetIPProperties();
                bool hasGateway = props.GatewayAddresses.Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork && !g.Address.Equals(IPAddress.Any));
                if (!hasGateway) continue;

                UnicastIPAddressInformation ip = props.UnicastAddresses.FirstOrDefault(u => u.Address.AddressFamily == AddressFamily.InterNetwork);
                if (ip != null) return ip.Address.ToString();
            }
            return null;
        }
    }
}
