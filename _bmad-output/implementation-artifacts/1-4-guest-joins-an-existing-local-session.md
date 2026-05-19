# Story 1.4: Guest Joins an Existing Local Session

Status: ready-for-dev

## Story

As a guest player,
I want to join the host's local session,
So that both players can prepare the shared race together.

## Acceptance Criteria

1. **Given** a host session is available on the same local network, **When** the second player joins successfully, **Then** the system assigns the player the Guest role and Player 2 identity, **And** the player is assigned the green car color.

2. **Given** the guest joins successfully under normal local-network conditions, **When** the connection is established, **Then** both devices show that two players are connected in the same session, **And** the join flow completes within the MVP local-session time budget.

3. **Given** no valid session is found or the join attempt fails, **When** the guest tries to join, **Then** the system explains that both devices must be on the same local network, **And** the guest can retry or return to the start screen.

## Tasks / Subtasks

- [ ] Task 1: Add `StartGuestSession(string hostIp)` to SessionManager (AC: 1, 2)
  - [ ] Add `Joining` state to SessionState enum
  - [ ] Call `NetworkManager.Singleton.StartClient()` with the provided host IP
  - [ ] Subscribe to OnClientConnectedCallback → set Connected
  - [ ] Set role = Guest, playerId = 2
  - [ ] Handle connection timeout (5s) → transition to Failed

- [ ] Task 2: Create LobbyJoinUI component (AC: 2, 3)
  - [ ] Create `LobbyJoinUI.cs` in `Scripts/Runtime/UI/`
  - [ ] Show IP input field with numeric keyboard hint
  - [ ] Show "Connect" button (amber CTA)
  - [ ] Show "Connecting..." state with connecting feedback
  - [ ] Show "Connected!" success with green confirmation
  - [ ] Show "Failed" state with error message + Retry + Back buttons
  - [ ] Pre-fill last-used IP from PlayerPrefs for convenience

- [ ] Task 3: Wire Join Match flow in LobbyCompositionRoot (AC: 1, 2, 3)
  - [ ] Replace placeholder `OnJoinMatchSelected()` with join UI display
  - [ ] Create LobbyJoinUI instance (hidden initially, shown on Join)
  - [ ] On "Connect" pressed → call SessionManager.StartGuestSession(ip)
  - [ ] Listen to SessionManager state for join-specific feedback
  - [ ] On Connected → TransitionToRace after brief delay
  - [ ] On Back → return to start screen

- [ ] Task 4: Verify compilation and basic flow (AC: 1, 2, 3)
  - [ ] Confirm no compile errors
  - [ ] Verify client can start in editor with ParrelSync or two instances
  - [ ] Verify state transitions: Idle → Joining → Connected or Failed

## Dev Notes

### Architecture Requirements

- Same as Story 1.3: host-authoritative, NGO 2.11, Unity Transport 6.5
- Guest is a pure client: it sends input only, host decides truth
- The guest's StartClient connects to the IP:port provided
- Connection timeout should be ~5 seconds — if no response, fail gracefully
- PlayerPrefs key `"LastHostIP"` stores the last successfully-used IP

### NGO Client Connection

```csharp
UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
transport.ConnectionData.Address = hostIp;
transport.ConnectionData.Port = 7777;
NetworkManager.Singleton.StartClient();
```

- OnClientConnectedCallback fires on successful connection (clientId == LocalClientId for the guest)
- OnClientDisconnectCallback fires on failure/timeout
- Unity Transport default timeout is ~5s

### UX Requirements

- **UX-DR2**: Connection state must be visible and understandable
- **UX-DR19**: Guest = Player 2 = Green car
- **UX-DR15**: One primary action per screen state
- IP input: numeric keyboard, auto-fill from PlayerPrefs
- Error message must explain "same Wi-Fi/hotspot" requirement

### File Structure

| File | Location | Purpose |
|------|----------|---------|
| SessionManager.cs | Scripts/Runtime/Infrastructure/ | UPDATE: add StartGuestSession, Joining state |
| LobbyJoinUI.cs | Scripts/Runtime/UI/ | NEW: IP input + join status display |
| LobbyCompositionRoot.cs | Scripts/Runtime/App/ | UPDATE: wire join flow |

### Previous Story Intelligence (from 1.3)

- SessionManager already has: EnsureNetworkManager(), Shutdown(), state machine, events
- LobbyCompositionRoot already switches between start screen and session UI
- Same pattern of ShowXUI / hide others should apply for join UI
- LobbySessionUI handles host-side states; guest needs its own UI (different info shown)

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.4]
- [Source: _bmad-output/planning-artifacts/game-architecture.md#Networking]
- [Source: _bmad-output/project-context.md#Critical Implementation Rules]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Completion Notes List

### File List
