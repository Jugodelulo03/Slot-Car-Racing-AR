# Slot Car Racing AR

## Overview
Augmented reality multiplayer slot car racing game for Android. Two players on the same local network point their phones at a shared marker on a table, and race slot cars on a virtual track anchored to the real world.

## Tech Stack
- **Engine**: Unity 6.3 LTS
- **Platform**: Android (landscape)
- **Render**: URP 17.6
- **AR**: AR Foundation 6.5 + ARCore XR Plugin 6.5
- **Networking**: Netcode for GameObjects 2.11 + Unity Transport 6.5
- **UI**: UGUI (programmatic, no prefabs)
- **Architecture**: Host-authoritative, composition roots per scene, no singletons

## Project Structure
```
My project/Assets/SlotCarRacingAR/
├── Scripts/
│   ├── Runtime/
│   │   ├── App/              # Composition roots (Boot, Lobby, Race)
│   │   ├── Infrastructure/   # SessionManager, SharedLobbyState, MarkerDetection
│   │   ├── Features/         # Track definition, car physics, debug viz
│   │   ├── UI/               # All UGUI screens (built programmatically)
│   │   └── Debug/            # AR debug overlay
│   └── Editor/
├── Scenes/                   # Boot, Lobby, Race
└── Resources/
```

## Scenes
| Scene | Purpose |
|-------|---------|
| Boot | Initialization, routing, target frame rate |
| Lobby | Session setup: create/join, shared lobby, transition to race |
| Race | AR marker detection, track rendering, car control, race HUD |

## Game Flow
```
Boot → Lobby (Start Screen)
         ├── Create Match → Waiting for Player → Shared Lobby → Race
         └── Join Match → Enter IP → Connect → Shared Lobby → Race

Race: Detect marker → Preview track → Countdown → Hold-to-accelerate racing
```

## Development Progress

### Epic 1: Start a Local Session ✅
Two players can discover, create, and join a local session from the same network.

| Story | Status | Description |
|-------|--------|-------------|
| 1.1 | ✅ Done | Initial Unity project setup |
| 1.2 | ✅ Done | Start screen with Create/Join buttons |
| 1.3 | ✅ Done | Host creates local session (NetworkManager + StartHost) |
| 1.4 | ✅ Done | Guest joins via IP input (StartClient + timeout) |
| 1.5 | 🔄 In Progress | Shared lobby (NetworkVariable sync, player count) |

### Epic 2: Confirm a Trustworthy Shared Track (Backlog)
Both players detect the same marker, see the track, confirm stability, countdown.

### Epic 3: Run the Race (Backlog)
Cars on track, hold-to-accelerate input, authority, penalties, HUD.

### Epic 4: Handle the Edges (Backlog)
Tracking recovery, network loss, race results, rematch.

## Key Architecture Patterns
- **Composition Roots**: Each scene has one MonoBehaviour that wires all dependencies explicitly
- **Host-Authoritative Networking**: Only host writes shared state (NetworkVariables, spawns)
- **Programmatic UI**: All UI built in code via UGUI (no prefab assets needed)
- **Thin MonoBehaviours**: Serialize references and bridge lifecycle; don't own business logic
- **No Singletons**: Except NetworkManager.Singleton (NGO built-in)
- **Session State Machine**: Idle → Creating/Joining → WaitingForPlayer → Connected → (Race)

## Controls
- **Hold anywhere**: Accelerate
- **Release**: Brake/coast (required in curves to avoid spin-out)

## Building & Testing
- Target: Android (API 24+)
- Two devices on same Wi-Fi / mobile hotspot
- Host shows IP address for guest to enter
- Port: 7777 (UDP, Unity Transport)

## Session Logs
Development session records are stored in `_bmad-output/session-log-*.md`.
