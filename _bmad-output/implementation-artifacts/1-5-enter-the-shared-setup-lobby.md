# Story 1.5: Enter the Shared Setup Lobby

Status: ready-for-dev

## Story

As either player,
I want to see both players connected with clear next-step guidance,
So that we can move into AR setup with confidence.

## Acceptance Criteria

1. **Given** both host and guest are connected, **When** the shared lobby is displayed, **Then** both players can see their roles, player identities, and color assignments, **And** the interface clearly indicates that the next step is to point both phones at the same marker.

2. **Given** the second player connects to the shared lobby, **When** both devices update their session state, **Then** a short-lived contextual confirmation is shown for the successful connection, **And** that confirmation does not block the next primary action.

3. **Given** the shared lobby shows session state, **When** ready, waiting, or disconnected conditions are displayed, **Then** player identity uses the red and green player colors while technical confirmation, warning, and critical failure states use consistent semantic status colors, **And** critical changes are communicated with at least text plus a second signal such as color or icon.

4. **Given** one player disconnects before setup handoff completes, **When** the lobby state updates, **Then** the remaining player sees a clear disconnected status, **And** the session returns to a recoverable waiting or retry state.

## Tasks / Subtasks

- [ ] Task 1: Create SharedLobbyState NetworkBehaviour (AC: 1, 3, 4)
  - [ ] Create `SharedLobbyState.cs` in `Scripts/Runtime/Infrastructure/`
  - [ ] Use NetworkVariable<byte> for player count (host-authoritative)
  - [ ] Track connected player count via OnClientConnectedCallback/OnClientDisconnectCallback
  - [ ] Spawn as a NetworkObject owned by the host (via NetworkManager prefab or spawn)
  - [ ] Expose `PlayerCount` observable for UI binding

- [ ] Task 2: Create SharedLobbyUI component (AC: 1, 2, 3, 4)
  - [ ] Create `SharedLobbyUI.cs` in `Scripts/Runtime/UI/`
  - [ ] Show Player 1 (red) and Player 2 (green) slots with connection status
  - [ ] Show "Connected!" confirmation briefly (2s) then fade/replace with next-step guidance
  - [ ] Show next step: "Apunten ambos teléfonos al mismo marcador en la mesa"
  - [ ] Show disconnected state if a player drops (red warning)
  - [ ] "Continue" / "Start AR" button visible when 2 players connected

- [ ] Task 3: Update LobbyCompositionRoot for shared lobby flow (AC: 1, 2, 4)
  - [ ] After Connected state → instead of immediate TransitionToRace, show SharedLobbyUI
  - [ ] Remove the 1.5s auto-transition to Race (replaced by explicit lobby screen)
  - [ ] On "Continue" button → TransitionToRace
  - [ ] On disconnect → show disconnected state, optionally return to waiting

- [ ] Task 4: Ensure NetworkObject spawning works (AC: 1)
  - [ ] Register SharedLobbyState prefab in NetworkManager's NetworkPrefabs list
  - [ ] Host spawns SharedLobbyState after session is connected
  - [ ] Client receives the spawned object and reads NetworkVariables

- [ ] Task 5: Verify compilation and state sync (AC: 1, 2, 3, 4)
  - [ ] Confirm no compile errors
  - [ ] Verify both instances see player count = 2
  - [ ] Verify disconnect detection works

## Dev Notes

### Architecture Requirements

- **First networked state sync**: This is the first story using NetworkVariable
- **Host-authoritative**: Only the host writes NetworkVariables; client reads
- NetworkObject must be spawned by host after both connect
- Use `NetworkVariable<byte>` (lightweight) for player count
- Do NOT sync transforms, AR poses, or heavy state — just session metadata

### NGO NetworkVariable Pattern

```csharp
public class SharedLobbyState : NetworkBehaviour
{
    public NetworkVariable<byte> PlayerCount = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            PlayerCount.Value = (byte)NetworkManager.ConnectedClientsIds.Count;
            NetworkManager.OnClientConnectedCallback += OnPlayerJoined;
            NetworkManager.OnClientDisconnectCallback += OnPlayerLeft;
        }
        PlayerCount.OnValueChanged += OnPlayerCountChanged;
    }
}
```

### Spawning the NetworkObject

Option A (simplest for MVP): Host instantiates a registered prefab and spawns it:
```csharp
GameObject lobbyStateObj = Instantiate(lobbyStatePrefab);
lobbyStateObj.GetComponent<NetworkObject>().Spawn();
```

Option B (no prefab needed): Use an in-scene placed NetworkObject that activates on connect.

**For this MVP**: Use Option B — place SharedLobbyState on a GameObject in the Lobby scene that has a NetworkObject component. It auto-syncs when NetworkManager starts. This avoids needing prefab registration.

Actually, in-scene placed NetworkObjects require the scene to be loaded network-aware. **Simpler approach for programmatic creation**: Register via `NetworkManager.AddNetworkPrefab()` at runtime before spawning.

### UX Requirements

- **UX-DR19**: Player 1 = Red (#E53935), Player 2 = Green (#43A047)
- **UX-DR2**: Session state visible and clear
- **UX-DR13**: Understandable in under 1 second
- **UX-DR15**: One primary action (Continue to AR setup)
- **UX-DR25**: Critical changes = text + color (not just color)
- Confirmation "Connected!" shown for 2s, then replaced by next-step guidance
- Disconnect = red warning text + "Waiting for reconnection..."

### File Structure

| File | Location | Purpose |
|------|----------|---------|
| SharedLobbyState.cs | Scripts/Runtime/Infrastructure/ | NetworkBehaviour with PlayerCount NetworkVariable |
| SharedLobbyUI.cs | Scripts/Runtime/UI/ | Two-player lobby display with roles and guidance |
| LobbyCompositionRoot.cs | Scripts/Runtime/App/ | UPDATE: wire shared lobby after connection |

### Previous Story Intelligence (from 1.3 / 1.4)

- SessionManager fires `OnSessionStateChanged(Connected)` for both host and guest
- Currently `Connected` triggers 1.5s delay then TransitionToRace — this must be replaced with SharedLobbyUI
- LobbyCompositionRoot pattern: create UI objects programmatically, show/hide based on state
- The Canvas is already set up (1920x1080, ScreenSpaceOverlay)

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.5]
- [Source: _bmad-output/planning-artifacts/game-architecture.md#Networking]
- [Source: _bmad-output/project-context.md#Critical Implementation Rules]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Completion Notes List

### File List
