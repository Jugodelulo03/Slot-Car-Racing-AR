# Bug Context: SharedLobbyState Never Replicates to Guest

## Project Overview

2-player AR slot car racing game for Android. Players connect via LAN (same WiFi), detect an AR marker to place a virtual track, then race.

## Tech Stack

- **Engine**: Unity 6.3 LTS (6000.3.14f1), URP 17.6
- **AR**: AR Foundation 6.5 + ARCore XR Plugin 6.5 (Android only)
- **Networking**: Netcode for GameObjects (NGO) 2.11.2 + Unity Transport 6.5
- **Network Config**: `EnableSceneManagement = false`, `ForceSamePrefabs = false`
- **Scene Transitions**: `SceneManager.LoadScene("Race")` — NOT NGO's NetworkSceneManager
- **Architecture**: Host-authoritative, composition roots per scene, programmatic UGUI (no prefab assets)
- **LAN Discovery**: UDP broadcast on port 47777, game server on ports 7777-7780

## Test Devices

- **Host**: Xiaomi M2103K19G, Android 12 (API 31)
- **Guest**: Samsung SM-S721B, Android 16 (API 36)

## The Bug

**SharedLobbyState (a NetworkBehaviour) NEVER replicates from host to guest device.** The guest connects successfully via NGO but never receives the spawned NetworkObject.

## Architecture of the Problem

### SharedLobbyState.cs

A `NetworkBehaviour` holding all shared session state:
- `NetworkVariable<byte> PlayerCount`
- `NetworkVariable<bool> HostReady, GuestReady`
- `NetworkVariable<RacePhase> Phase` (Setup/Countdown/Racing/Finished)
- `NetworkVariable<int> CountdownValue`
- Events: `OnReadyStateChanged`, `OnPhaseChanged`, `OnCountdownTick`
- `OnNetworkSpawn()`: parents itself under NetworkManager (to survive scene change)

### LobbyCompositionRoot.cs — Prefab Creation (Awake-time)

```csharp
_lobbyStatePrefab = new GameObject("SharedLobbyStatePrefab");
var netObj = _lobbyStatePrefab.AddComponent<NetworkObject>();
_lobbyStatePrefab.AddComponent<SharedLobbyState>();
_lobbyStatePrefab.SetActive(false);
DontDestroyOnLoad(_lobbyStatePrefab);

// Attempted fix: force deterministic hash via reflection
var hashField = typeof(NetworkObject).GetField("GlobalObjectIdHash",
    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
hashField.SetValue(netObj, (uint)0xF2CA0E01);
```

### Registration (both host and guest, after Connected state + 1s delay)

```csharp
private void RegisterLobbyStatePrefab()
{
    if (_lobbyStatePrefabRegistered) return;
    NetworkManager nm = NetworkManager.Singleton;
    nm.AddNetworkPrefab(_lobbyStatePrefab);
    _lobbyStatePrefabRegistered = true;
}
```

### Spawn (host only, immediately after RegisterLobbyStatePrefab)

```csharp
private void SpawnSharedLobbyState()
{
    GameObject instance = Instantiate(_lobbyStatePrefab);
    instance.SetActive(true);
    instance.GetComponent<NetworkObject>().Spawn();
    _sharedLobbyState = instance.GetComponent<SharedLobbyState>();
}
```

### Guest Detection (polls for the object)

```csharp
private void FindSharedLobbyState()
{
    SharedLobbyState found = FindAnyObjectByType<SharedLobbyState>();
    // retries with Invoke every 0.5-1.0s
}
```

Also has a coroutine `WaitForSharedStateAndTransition()` that retries 20 times (10s total).

## What We've Verified

1. **Connection works**: Guest receives `ConnectionApprovedMessage`, `OnClientConnected` fires
2. **Host spawns successfully**: Host log shows `[SharedLobbyState] Spawned. PlayerCount=2 IsServer=True`
3. **Guest NEVER gets the object**: After 10+ seconds of polling, `FindAnyObjectByType<SharedLobbyState>()` always returns null
4. **No NGO errors on guest**: Completely silent — no "unknown prefab", no "hash mismatch" warning
5. **Tested with `ForceSamePrefabs = false`**: NGO should defer unknown spawn messages, not reject them

## Root Cause Analysis (from NGO 2.11.2 source code)

### NGO Internal Fields (NetworkObject.cs)

```csharp
[SerializeField] internal uint GlobalObjectIdHash;        // prefab identity
internal uint PrefabGlobalObjectIdHash;                    // set by NGO during spawn pipeline
```

- `GlobalObjectIdHash` is `internal` — NOT publicly settable
- `PrefabGlobalObjectIdHash` is what gets sent in `CreateObjectMessage.ObjectInfo.Hash`

### How Spawn Message Hash is Determined (NetworkObject.cs:3596)

```csharp
internal uint CheckForGlobalObjectIdHashOverride()
{
    // If PrefabGlobalObjectIdHash != 0 and differs from GlobalObjectIdHash, use it
    if (!IsSceneObject.Value && GlobalObjectIdHash != PrefabGlobalObjectIdHash)
    {
        if (PrefabGlobalObjectIdHash != 0) return PrefabGlobalObjectIdHash;
        if (OverrideToNetworkPrefab.TryGetValue(GlobalObjectIdHash, out var h)) return h;
    }
    return GlobalObjectIdHash;
}
```

### How Guest Looks Up Prefab (CreateObjectMessage.cs:123)

```csharp
if (!networkManager.NetworkConfig.ForceSamePrefabs && !networkManager.SpawnManager.HasPrefab(ObjectInfo))
{
    networkManager.DeferredMessageManager.DeferMessage(
        IDeferredNetworkMessageManager.TriggerType.OnAddPrefab, ObjectInfo.Hash, reader, ref context, k_Name);
    return false;
}
```

### How HasPrefab Resolves (NetworkSpawnManager.cs:705)

```csharp
internal bool HasPrefab(SerializedObject serializedObject)
{
    if (NetworkManager.PrefabHandler.ContainsHandler(serializedObject.Hash)) return true;
    if (NetworkConfig.Prefabs.NetworkPrefabOverrideLinks.TryGetValue(serializedObject.Hash, out var np))
    {
        return np.Prefab != null;  // for Override.None
    }
    return false;
}
```

### How AddNetworkPrefab Registers (NetworkPrefabs.cs:283)

```csharp
private bool AddPrefabRegistration(NetworkPrefab networkPrefab)
{
    uint source = networkPrefab.SourcePrefabGlobalObjectIdHash;
    // SourcePrefabGlobalObjectIdHash for Override.None reads:
    //   prefab.GetComponent<NetworkObject>().GlobalObjectIdHash
    NetworkPrefabOverrideLinks.Add(source, networkPrefab);
    return true;
}
```

### How DeferredMessages Get Processed (NetworkPrefabHandler.cs:420)

```csharp
public void AddNetworkPrefab(GameObject prefab)
{
    var networkObject = prefab.GetComponent<NetworkObject>();
    bool added = m_NetworkManager.NetworkConfig.Prefabs.Add(networkPrefab);
    if (m_NetworkManager.IsListening && added)
    {
        m_NetworkManager.DeferredMessageManager.ProcessTriggers(
            TriggerType.OnAddPrefab, networkObject.GlobalObjectIdHash);
    }
}
```

## Key Insight: The Timing/Hash Mismatch Problem

The flow should be:
1. Guest calls `RegisterLobbyStatePrefab()` → `AddNetworkPrefab(prefab)` → registers with key = `prefab.NetworkObject.GlobalObjectIdHash` (0xF2CA0E01 from reflection)
2. Host sends `CreateObjectMessage` with `Hash = CheckForGlobalObjectIdHashOverride()` → should be `GlobalObjectIdHash` = 0xF2CA0E01
3. Guest receives message → `HasPrefab(hash=0xF2CA0E01)` → finds it in `NetworkPrefabOverrideLinks` → instantiates

**But it's not working.** The spawn message is either:
- Never reaching the guest (unlikely — connection is confirmed working)
- Being deferred because hashes don't match, and never processed (possible if the deferred trigger hash doesn't match the registered hash)
- The reflection might not actually be writing to the correct field (IL2CPP stripping or field layout issues on Android)

## Possible Causes Still Under Investigation

1. **Reflection failure on IL2CPP/Android**: `GetField("GlobalObjectIdHash", NonPublic|Instance)` might return null or fail silently on IL2CPP builds. The field might be stripped or renamed.

2. **Host's spawned instance hash differs from what guest registered**: When host calls `Instantiate(_lobbyStatePrefab)`, the clone gets `GlobalObjectIdHash` copied. But `PrefabGlobalObjectIdHash` stays 0. Then `CheckForGlobalObjectIdHashOverride()` sees `GlobalObjectIdHash(0xF2CA0E01) != PrefabGlobalObjectIdHash(0)` → enters the override branch → `PrefabGlobalObjectIdHash` is 0 → checks `OverrideToNetworkPrefab` → not found → falls through to `return GlobalObjectIdHash`. So it SHOULD return 0xF2CA0E01... unless the reflection didn't work.

3. **Timing issue**: Guest registers prefab AFTER receiving the spawn message. If the message was deferred with hash X, and then guest registers with hash Y (different), `ProcessTriggers(Y)` won't trigger the deferred message stored under hash X.

4. **NGO syncs spawned objects during initial connection approval**: When a client connects, the host sends ALL existing spawned objects to the new client via the connection approval/sync flow. This happens BEFORE `EnterSharedLobby()` is called (which has a 1s delay). If the prefab isn't registered yet at that point, the sync message gets deferred with whatever hash the host used. Then when guest finally registers with `AddNetworkPrefab`, if the hashes match, `ProcessTriggers` should process it. But if they DON'T match...

## Proposed Solutions (Need Expert Review)

### Option A: Register Prefab BEFORE Starting NGO
Move `AddNetworkPrefab` to happen before `NetworkManager.StartHost()`/`StartClient()`. This way the prefab is part of the initial config and the connection sync will find it.

### Option B: Use NetworkManager.InstantiateAndSpawn() 
Instead of manual `Instantiate()` + `.Spawn()`, use NGO's proper API which correctly sets `PrefabGlobalObjectIdHash`.

### Option C: Abandon Dynamic Prefab Entirely
Put SharedLobbyState as a NetworkBehaviour on NetworkManager's own GameObject (add it before starting), or use `CustomMessagingManager` / RPCs on player objects to sync state without a separate NetworkObject.

### Option D: Use INetworkPrefabInstanceHandler
Register a custom handler with a fixed hash that both devices agree on, bypassing the prefab lookup entirely.

### Option E: Create a Real Prefab Asset
Instead of `new GameObject()` at runtime, create the prefab as an actual asset in the project and reference it in NetworkManager's prefab list. This is the "normal" NGO workflow and guarantees stable hashes.

## Question for Review

Given that this project has NO prefab assets (everything is programmatic), what is the most reliable way to spawn a NetworkObject that both host and guest can resolve, when using NGO 2.11.2 with `ForceSamePrefabs = false` and `EnableSceneManagement = false`?
