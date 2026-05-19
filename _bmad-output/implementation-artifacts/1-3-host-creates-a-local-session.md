# Story 1.3: Host Creates a Local Session

Status: ready-for-dev

## Story

As a host player,
I want to create a local session and become the session owner,
So that I can invite another player into the race setup flow.

## Acceptance Criteria

1. **Given** the player selects "Create Match", **When** local session creation succeeds, **Then** the system assigns the player the Host role and Player 1 identity, **And** the player is assigned the red car color.

2. **Given** the host session has been created, **When** the host enters the waiting state, **Then** the UI clearly shows that the session is active and waiting for a second player, **And** the next step is explained in non-technical language.

3. **Given** local session creation fails or the local network is unavailable, **When** the host attempts to create a match, **Then** the system shows a clear recovery message with retry and back options, **And** the failure state is distinguishable from normal waiting state.

## Tasks / Subtasks

- [ ] Task 1: Add NetworkManager + UnityTransport to Lobby scene (AC: 1)
  - [ ] Create a "NetworkManager" GameObject in the Lobby scene
  - [ ] Add NetworkManager component with UnityTransport
  - [ ] Configure UnityTransport for LAN: ConnectionData address=0.0.0.0, port=7777, ServerListenAddress=0.0.0.0
  - [ ] Set NetworkManager to NOT auto-start (manual start via SessionManager)
  - [ ] Ensure NetworkManager persists across scenes (DontDestroyOnLoad)

- [ ] Task 2: Create SessionManager service class (AC: 1, 2, 3)
  - [ ] Create `SessionManager.cs` in `Scripts/Runtime/Infrastructure/`
  - [ ] Implement `StartHostSession()` that calls NetworkManager.Singleton.StartHost()
  - [ ] Expose session state: `Idle`, `Creating`, `WaitingForPlayer`, `Connected`, `Failed`
  - [ ] Expose `PlayerRole` (Host/Guest) and `PlayerId` (1 or 2)
  - [ ] Expose `HostIpAddress` property (get device's LAN IP for display)
  - [ ] Fire typed C# events on state changes: `OnSessionStateChanged`
  - [ ] Handle NetworkManager callbacks: OnClientConnectedCallback, OnClientDisconnectCallback
  - [ ] Detect when a second client connects → transition to Connected state

- [ ] Task 3: Create LobbySessionUI component (AC: 2, 3)
  - [ ] Create `LobbySessionUI.cs` in `Scripts/Runtime/UI/`
  - [ ] Show "Creating session..." state with spinner/dots
  - [ ] Show "Waiting for player" state with host IP address and brief instruction
  - [ ] Show "Player 2 connected!" state with green confirmation
  - [ ] Show "Failed" state with error message + Retry + Back buttons
  - [ ] Use semantic colors: amber for waiting, green for connected, red for failure

- [ ] Task 4: Wire everything in LobbyCompositionRoot (AC: 1, 2, 3)
  - [ ] Replace placeholder `OnCreateMatchSelected()` with real SessionManager.StartHostSession() call
  - [ ] Transition start screen → session UI when Create Match is tapped
  - [ ] Listen to SessionManager state events to update LobbySessionUI
  - [ ] When Connected → trigger TransitionToRace() (or wait for Story 1.5 lobby)
  - [ ] When Back pressed from failure → return to start screen

- [ ] Task 5: Verify compilation and basic flow (AC: 1, 2, 3)
  - [ ] Confirm no compile errors
  - [ ] Verify NetworkManager starts as host in editor
  - [ ] Verify state transitions: Idle → Creating → WaitingForPlayer
  - [ ] Verify failure path when no network adapter

## Dev Notes

### Architecture Requirements

- **Networking stack**: Netcode for GameObjects 2.11 + Unity Transport 6.5 (already in manifest)
- **Authority model**: Host-authoritative. Only the host creates/destroys networked objects and mutates shared state
- **NetworkManager**: Use `NetworkManager.Singleton` pattern (built into NGO). It persists across scenes automatically
- **MonoBehaviours must be thin**: SessionManager is a MonoBehaviour (needs NetworkManager callbacks) but delegates state logic to a simple state machine
- **No raw AR sync over network**: The network only syncs session/race state, never world transforms
- **Scene boundary**: Lobby only orchestrates session setup. It must NOT contain race logic

### NGO 2.11 Specifics

- `NetworkManager` must have a `UnityTransport` component attached
- For LAN: set `UnityTransport.ConnectionData.Address` to "0.0.0.0" and port 7777
- `NetworkManager.Singleton.StartHost()` starts both server and local client
- `NetworkManager.Singleton.OnClientConnectedCallback += (clientId) => {...}` for client connect events
- `NetworkManager.Singleton.ConnectedClientsIds.Count` gives current connected count
- The host's own clientId is `NetworkManager.Singleton.LocalClientId`
- To get device IP for display, use: `System.Net.NetworkInformation` APIs or Unity's internal transport address

### Getting Device IP (for display to guest)

```csharp
public static string GetLocalIPAddress()
{
    var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
    foreach (var ip in host.AddressList)
    {
        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            return ip.ToString();
    }
    return "127.0.0.1";
}
```

### LobbyCompositionRoot Current State

- `OnCreateMatchSelected()` currently just logs and calls `TransitionToRace()` (placeholder)
- `OnJoinMatchSelected()` same (will be replaced in Story 1.4)
- `TransitionToRace()` handles camera permissions then loads Race scene
- The LobbyStartScreen fires events `OnCreateMatchClicked` / `OnJoinMatchClicked`

### Session State Machine

```
Idle → Creating → WaitingForPlayer → Connected → (transition to Race)
                ↘ Failed → (Retry → Creating) or (Back → Idle)
```

### UX Requirements

- **UX-DR2**: Session state feedback must be clear and use semantic colors
- **UX-DR19**: Host = Player 1 = Red car, Guest = Player 2 = Green car
- **UX-DR13**: Status must be understandable in under 1 second
- **UX-DR15**: One primary action per state
- **UX-DR25**: Critical confirmation uses text + color (not just color)
- Show IP address so guest knows where to connect (temporary, Story 1.4 may add auto-discovery)

### File Structure

| File | Location | Purpose |
|------|----------|---------|
| SessionManager.cs | Scripts/Runtime/Infrastructure/ | NetworkManager wrapper, session state machine |
| LobbySessionUI.cs | Scripts/Runtime/UI/ | Session status display (waiting, connected, failed) |
| LobbyCompositionRoot.cs | Scripts/Runtime/App/ | UPDATE: wire SessionManager + LobbySessionUI |

### Project Structure Notes

- `Scripts/Runtime/Infrastructure/` currently only has `MarkerDetectionEntryPoint.cs`
- `Scripts/Runtime/UI/` has `LobbyStartScreen.cs`, `AccelerationInputPlaceholder.cs`, `RaceHud.cs`, etc.
- The NetworkManager GameObject will live in the Lobby scene initially (NGO handles DontDestroyOnLoad internally)

### Previous Story Intelligence (from 1.2)

- All UI is built programmatically via UGUI (no prefabs)
- LobbyCompositionRoot owns all wiring via events
- LobbyStartScreen uses events (`OnCreateMatchClicked`, `OnJoinMatchClicked`) — this pattern should continue for session UI
- PlaceholderCanvasScreenOverlay sets 1920x1080 reference resolution
- Buttons use minimum 48x48dp touch targets with amber/white color scheme

### References

- [Source: _bmad-output/planning-artifacts/game-architecture.md#Networking]
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 1 Story 1.3]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Shared Session Card]
- [Source: _bmad-output/project-context.md#Critical Implementation Rules]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Completion Notes List

### File List
