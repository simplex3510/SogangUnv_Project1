# Unity 6.3 — Networking Module Reference

**Last verified:** 2026-02-13
**Knowledge Gap:** Unity 6 uses Netcode for GameObjects (UNet deprecated)

---

## Overview

Unity 6 networking options:
- **Netcode for GameObjects** (RECOMMENDED): Official Unity multiplayer framework
- **Mirror**: Community-driven (UNet successor)
- **Photon**: Third-party service (PUN2)
- **Custom**: Low-level sockets

**UNet (Legacy)**: Deprecated, do not use.

---

## Netcode for GameObjects

### Installation
1. `Window > Package Manager`
2. Search "Netcode for GameObjects"
3. Install `com.unity.netcode.gameobjects`

---

## Basic Setup

### NetworkManager

```csharp
// Add to scene: GameObject > Add Component > NetworkManager

// Or create custom NetworkManager:
using Unity.Netcode;

public class CustomNetworkManager : MonoBehaviour {
    void Start() {
        NetworkManager.Singleton.StartHost(); // Server + client
        // OR
        NetworkManager.Singleton.StartServer(); // Dedicated server
        // OR
        NetworkManager.Singleton.StartClient(); // Client only
    }
}
```

---

## NetworkObject (Networked GameObjects)

### Mark GameObject as Networked

1. Add `NetworkObject` component to GameObject
2. Must be in root of prefab (not nested)
3. Register prefab in `NetworkManager > NetworkPrefabs List`

### Spawn Network Objects

```csharp
using Unity.Netcode;

public class GameManager : NetworkBehaviour {
    public GameObject playerPrefab;

    [ServerRpc(RequireOwnership = false)]
    public void SpawnPlayerServerRpc(ulong clientId) {
        GameObject player = Instantiate(playerPrefab);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
    }
}
```

---

## NetworkBehaviour (Networked Scripts)

### NetworkBehaviour Base Class

```csharp
using Unity.Netcode;

public class Player : NetworkBehaviour {
    // Called when spawned on network
    public override void OnNetworkSpawn() {
        if (IsOwner) {
            // Only run on owner's client
            GetComponent<Camera>().enabled = true;
        }
    }

    void Update() {
        if (!IsOwner) return; // Only owner can control

        // Handle input
        if (Input.GetKey(KeyCode.W)) {
            MoveServerRpc(Vector3.forward);
        }
    }

    [ServerRpc]
    void MoveServerRpc(Vector3 direction) {
        // Runs on server
        transform.position += direction * Time.deltaTime;
    }
}
```

---

## Network Variables (Synchronized State)

### NetworkVariable<T>

```csharp
using Unity.Netcode;

public class Player : NetworkBehaviour {
    // ✅ Auto-synced across clients
    private NetworkVariable<int> health = new NetworkVariable<int>(100);

    public override void OnNetworkSpawn() {
        // Subscribe to value changes
        health.OnValueChanged += OnHealthChanged;
    }

    void OnHealthChanged(int oldValue, int newValue) {
        Debug.Log($"Health changed: {oldValue} -> {newValue}");
        UpdateHealthUI(newValue);
    }

    [ServerRpc]
    public void TakeDamageServerRpc(int damage) {
        // Only server can modify NetworkVariable
        health.Value -= damage;
    }
}
```

### NetworkVariable Permissions

```csharp
// Server can write, clients read-only (default)
private NetworkVariable<int> score = new NetworkVariable<int>();

// Owner can write
private NetworkVariable<int> ammo = new NetworkVariable<int>(
    default,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Owner
);
```

---

## RPCs (Remote Procedure Calls)

### ServerRpc (Client → Server)

```csharp
// Client calls, server executes
[ServerRpc]
void FireWeaponServerRpc() {
    // Runs on server
    Debug.Log("Server: Weapon fired");
}

// Call from client:
if (IsOwner && Input.GetKeyDown(KeyCode.Space)) {
    FireWeaponServerRpc();
}
```

### ClientRpc (Server → All Clients)

```csharp
// Server calls, all clients execute
[ClientRpc]
void PlayExplosionClientRpc(Vector3 position) {
    // Runs on all clients
    Instantiate(explosionPrefab, position, Quaternion.identity);
}

// Call from server:
[ServerRpc]
void ExplodeServerRpc(Vector3 position) {
    // Server logic
    DealDamageToNearbyPlayers(position);

    // Notify all clients
    PlayExplosionClientRpc(position);
}
```

### RPC Parameters

```csharp
// ✅ Supported: Primitives, structs, strings, arrays
[ServerRpc]
void SetNameServerRpc(string playerName) { }

[ClientRpc]
void UpdateScoresClientRpc(int[] scores) { }

// ❌ Not supported: MonoBehaviour, GameObject (use NetworkObjectReference)
```

---

## Network Ownership

### Check Ownership

```csharp
if (IsOwner) {
    // This client owns this NetworkObject
}

if (IsServer) {
    // Running on server
}

if (IsClient) {
    // Running on client
}

if (IsLocalPlayer) {
    // This is the local player object
}
```

### Transfer Ownership

```csharp
// Server transfers ownership
NetworkObject netObj = GetComponent<NetworkObject>();
netObj.ChangeOwnership(newOwnerClientId);
```

---

## NetworkObjectReference (Pass GameObjects in RPCs)

```csharp
using Unity.Netcode;

[ServerRpc]
void AttackTargetServerRpc(NetworkObjectReference targetRef) {
    if (targetRef.TryGet(out NetworkObject target)) {
        // Got the target object
        target.GetComponent<Health>().TakeDamage(10);
    }
}

// Call:
NetworkObject targetNetObj = target.GetComponent<NetworkObject>();
AttackTargetServerRpc(targetNetObj);
```

---

## Client-Server Architecture

### Server-Authoritative Pattern (RECOMMENDED)

```csharp
public class Player : NetworkBehaviour {
    private NetworkVariable<Vector3> position = new NetworkVariable<Vector3>();

    void Update() {
        if (IsOwner) {
            // Client: Send input to server
            Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            MoveServerRpc(input);
        }

        // All clients: Sync to networked position
        transform.position = position.Value;
    }

    [ServerRpc]
    void MoveServerRpc(Vector3 input) {
        // Server: Validate and apply movement
        position.Value += input * Time.deltaTime * moveSpeed;
    }
}
```

---

## Network Transport

### Unity Transport (Default)

```csharp
// Configured in NetworkManager:
// - Transport: Unity Transport
// - Address: 127.0.0.1 (localhost) or server IP
// - Port: 7777 (default)
```

### Connection Events

```csharp
void Start() {
    NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
}

void OnClientConnected(ulong clientId) {
    Debug.Log($"Client {clientId} connected");
}

void OnClientDisconnected(ulong clientId) {
    Debug.Log($"Client {clientId} disconnected");
}
```

---

## Performance Tips

### Reduce Network Traffic
- Use `NetworkVariable` for state that changes infrequently
- Batch multiple changes before syncing
- Use delta compression for large data

### Prediction & Reconciliation
- Run movement locally for responsiveness
- Reconcile with server authoritative state
- Use interpolation for smooth movement

---

## Debugging

### Network Profiler
- `Window > Analysis > Network Profiler`
- Monitor bandwidth, RPC calls, variable updates

### Network Simulator (Test Latency/Packet Loss)
- `NetworkManager > Network Simulator`
- Add artificial lag and packet loss for testing

---

## 6.6 Installed-Editor Check (verified 2026-11-21)

Source: the 6000.6.8f1 install itself (`Editor/Data/Resources/PackageManager/BuiltInPackages`, `Editor/manifest.json`, each package's `Documentation~`) and https://packages.unity.com.

> 이 모듈의 RPC 예시(`[ServerRpc]`/`[ClientRpc]`)는 NGO 1.x 방식이다. NGO 2.x는 통합 `[Rpc(SendTo.…)]`를 쓴다(스파이크 2026-11-21 확인, ADR-0001).

**에디터에 동봉되거나 등록된 버전**
- Built-in: Multiplayer Play Mode 3.0.0, Dedicated Server 3.0.0, Multiplayer Center, Transport 6.6.0
- Editor manifest: Netcode for GameObjects 2.13.3(기본 번들, 레지스트리 최신 2.14.0), Multiplayer Tools 2.2.12

**Multiplayer Play Mode (MPPM)**
- 지원: Unity 6.3 LTS 이상, Windows/Mac. 2.0부터 기능 대부분이 엔진(Play Mode Framework, Multiplayer 모듈)으로 옮겨졌다.
- 한도: 에디터 플레이어 최대 4(메인 + 추가 3), 로컬 빌드 인스턴스 최대 4.
- 추가 에디터는 별도 자식 프로세스이며 `Library`를 공유한다(`Library/VP/`). 코드·에셋 변경이 그대로 반영된다.
- Play Mode Scenario(`Window > Play Mode > Scenarios`): 인스턴스마다 Multiplayer Role(Client/Server/Client+Server)과 Tag를 정한다. 로컬 빌드 인스턴스는 현재 플랫폼과 같은 Build Profile이 필요하다.
- 제약: 추가 에디터에서는 오브젝트 생성·수정 불가, Package Manager 동작 안 함. 로컬 인스턴스는 일시정지·스텝 미지원.
- 6.5·6.6 이상: Active Scenario 창에서 인스턴스 Activate/Deactivate·Keep Active. 6.6 이상: Refresh Virtual Project folder.
- 코드에서 플레이어 식별: `Unity.Multiplayer.Playmode.CurrentPlayer.IsMainEditor`, `CurrentPlayer.ReadOnlyTags()`, 실행 인자 `-name Player1~4`.
- 알려진 문제(커뮤니티): 네트워크 프리팹 수정 후 가상 플레이어의 `GlobalObjectIdHash` 불일치로 접속 실패, 가상 플레이어 재시작으로 해소(6000.0.29f1 / NGO 2.1.1 보고, 6.6 미확인).

**Dedicated Server 패키지 — Multiplayer Roles**
- `Project Settings > Multiplayer`에서 Multiplayer Roles를 켜면 에디터 툴바와 Build Profile에 역할 선택이 생긴다.
- 런타임 역할: `Unity.Multiplayer.MultiplayerRolesManager.ActiveMultiplayerRoleMask`(`MultiplayerRoleFlags.Server`/`Client`). 에디터 서버 인스턴스에는 `UNITY_SERVER`가 정의되지 않으므로 역할 분기는 이 API로 한다.
- Content Selection으로 역할별로 GameObject·Component를 뺄 수 있다.

**Network Simulator**: Multiplayer Tools 패키지의 `NetworkSimulator` 컴포넌트와 프리셋(`NetworkSimulatorPresets`). NGO 2.14의 `UnityTransport.DebugSimulator`는 폐기(효과 없음).

---

## Sources
- https://docs-multiplayer.unity3d.com/netcode/current/about/
- https://docs-multiplayer.unity3d.com/netcode/current/learn/bossroom/
- https://docs.unity3d.com/6000.6/Documentation/Manual/com.unity.multiplayer.playmode.html
- https://docs.unity3d.com/Packages/com.unity.multiplayer.playmode@2.0/manual/index.html
- https://discussions.unity.com/t/multiplayer-play-mode-error-when-connecting-from-to-virtual-player-after-editing-networked-prefab/1564025
