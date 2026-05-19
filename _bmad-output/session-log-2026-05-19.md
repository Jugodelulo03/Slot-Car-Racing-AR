# Session Log — 2026-05-19

## Objective
Implement Epic 1 networking stories (1.3, 1.4, 1.5) for local multiplayer session creation and lobby.

## Completed

### Story 1.3: Host Creates a Local Session
- **SessionManager.cs** (`Scripts/Runtime/Infrastructure/`) — Session state machine wrapping NGO. States: Idle → Creating → WaitingForPlayer → Connected / Failed. Creates NetworkManager + UnityTransport programmatically. Binds to 0.0.0.0:7777.
- **LobbySessionUI.cs** (`Scripts/Runtime/UI/`) — Host-side status display: creating, waiting (shows IP), connected, failed (retry/back).
- **LobbyCompositionRoot.cs** updated — "Crear Partida" now calls `SessionManager.StartHostSession()`, shows session UI.

### Story 1.4: Guest Joins an Existing Local Session
- **SessionManager.cs** extended — `StartGuestSession(ip)`, `RetryGuestSession()`, 5s timeout coroutine, saves last IP to PlayerPrefs, `Joining` state added to enum.
- **LobbyJoinUI.cs** (`Scripts/Runtime/UI/`) — IP input field (numeric keyboard, auto-fill from PlayerPrefs), "Conectar" button, connecting/connected/failed states.
- **LobbyCompositionRoot.cs** updated — "Unirse" shows JoinUI, wires connect/retry/back events.

### Story 1.5: Enter the Shared Setup Lobby
- **SharedLobbyState.cs** (`Scripts/Runtime/Infrastructure/`) — `NetworkBehaviour` with `NetworkVariable<byte> PlayerCount`. Host-authoritative, tracks connects/disconnects, fires `OnPlayerCountChanged`.
- **SharedLobbyUI.cs** (`Scripts/Runtime/UI/`) — Two-player lobby: Player 1 (red) / Player 2 (green) slots, 2.5s connection confirmation, next-step guidance ("apunten al marcador"), "Continuar" button when 2 connected, disconnect warning.
- **LobbyCompositionRoot.cs** updated — On Connected → shows SharedLobbyUI. Host spawns SharedLobbyState NetworkObject (registered at runtime via `AddNetworkPrefab`). Guest finds it via `FindAnyObjectByType`. "Continuar" → TransitionToRace.

## Architecture Decisions
- All UI built programmatically (UGUI, no prefabs) — consistent with project pattern
- NetworkManager created at runtime by SessionManager (no scene dependency)
- SharedLobbyState prefab registered dynamically via `NetworkManager.AddNetworkPrefab()` to avoid prefab asset requirement
- Host-authoritative: only host writes NetworkVariables
- Session state machine pattern with typed C# events (no UnityEvent)

## Sprint Status After Session
| Story | Status |
|-------|--------|
| 1.1 | done |
| 1.2 | done |
| 1.3 | done |
| 1.4 | done |
| 1.5 | in-progress |
| Epic 2+ | backlog |

## Files Created/Modified
| File | Action |
|------|--------|
| `Scripts/Runtime/Infrastructure/SessionManager.cs` | Created |
| `Scripts/Runtime/Infrastructure/SharedLobbyState.cs` | Created |
| `Scripts/Runtime/UI/LobbySessionUI.cs` | Created |
| `Scripts/Runtime/UI/LobbyJoinUI.cs` | Created |
| `Scripts/Runtime/UI/SharedLobbyUI.cs` | Created |
| `Scripts/Runtime/App/LobbyCompositionRoot.cs` | Modified |
| `_bmad-output/implementation-artifacts/1-3-*.md` | Created |
| `_bmad-output/implementation-artifacts/1-4-*.md` | Created |
| `_bmad-output/implementation-artifacts/1-5-*.md` | Created |

## Next Steps
- Test Epic 1 flow on two Android devices (same Wi-Fi/hotspot)
- Mark 1.5 as done after validation
- Begin Epic 2: AR marker detection and shared track
