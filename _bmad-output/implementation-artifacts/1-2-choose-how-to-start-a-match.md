# Story 1.2: Choose How to Start a Match

Status: complete

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a first-time player,
I want a simple start screen with clear create and join options,
So that I can begin the multiplayer flow without confusion.

## Acceptance Criteria

1. **Given** the player opens the app on a supported Android device, **When** the start screen is shown, **Then** the screen displays exactly two dominant actions: "Create Match" and "Join Match", **And** the copy uses simple, non-technical language.

2. **Given** the player is on the start screen, **When** they read the onboarding guidance, **Then** they can understand that both players need the same local network and the same shared marker, **And** the guidance remains brief and includes the core control rule: hold to accelerate and release in curves.

3. **Given** the start screen is rendered in landscape, **When** the player reviews the available actions, **Then** only one primary action is visually dominant at a time, **And** the layout remains readable without conflicting with safe-area constraints and provides touch targets of at least 48x48 dp for actionable controls on this screen.

## Tasks / Subtasks

- [x] Create the Lobby start screen UI with two dominant actions (AC: 1, 3)
  - [x] Replace `LobbyStartRaceButtonPlaceholder` with a proper start screen layout built programmatically via UGUI
  - [x] Create "Crear Partida" (Create Match) button as the primary CTA with amber color (#FFD740) and minimum 48x48dp touch target
  - [x] Create "Unirse" (Join Match) button as secondary action with clear visual distinction and minimum 48x48dp touch target
  - [x] Position buttons in the bottom action zone (below 40% screen height) per UX layout rules
  - [x] Ensure landscape layout respects safe-area insets (notches, punch-holes, gesture zones)
  - [x] Add game title/logo text at the top of the screen
- [x] Add onboarding guidance text (AC: 2)
  - [x] Add a brief centered text panel between title and buttons explaining: both players need same Wi-Fi/hotspot and same marker on table
  - [x] Include the core control rule: "Mantén para acelerar, suelta en las curvas" (Hold to accelerate, release in curves)
  - [x] Use high-contrast backplate behind text for readability over any background
  - [x] Keep copy to 2-3 short lines maximum, non-technical language
- [x] Wire button actions to LobbyCompositionRoot (AC: 1)
  - [x] "Create Match" button calls a new method `OnCreateMatchSelected()` on LobbyCompositionRoot
  - [x] "Join Match" button calls a new method `OnJoinMatchSelected()` on LobbyCompositionRoot
  - [x] Both methods log the selection and transition to Race scene (placeholder behavior until Stories 1.3/1.4 implement real session logic)
- [x] Update LobbyCompositionRoot for start screen flow (AC: 1)
  - [x] Add `OnCreateMatchSelected()` and `OnJoinMatchSelected()` public methods
  - [x] Remove dependency on `LobbyStartRaceButtonPlaceholder` (replaced by new start screen)
  - [x] Preserve camera permission request flow in `TransitionToRace()` (reused by both paths)
- [x] Verify compilation and layout on landscape Android resolution (AC: 3)
  - [x] Confirm no compile errors across all assemblies
  - [x] Verify layout renders correctly at 1920x1080 and 2400x1080 reference resolutions in Game view

## Dev Notes

### Context from Story 1.1

Story 1.1 established the full project baseline. The Lobby scene currently has:
- `LobbyCompositionRoot.cs` in `Scripts/Runtime/App/` — handles InputSystem UI module setup, camera permission flow, and scene transition to Race
- `LobbyStartRaceButtonPlaceholder.cs` in `Scripts/Runtime/App/` — a green button (35%-65% horizontal, 8%-20% vertical) that triggers `TransitionToRace()` directly. **This must be replaced**, not extended
- The Lobby scene has a Canvas with `PlaceholderCanvasScreenOverlay` that forces ScreenSpaceOverlay mode with 1920x1080 reference resolution and matchWidthOrHeight=1

### Architecture Requirements

- **UI stack**: UGUI only. Do not use UI Toolkit, TextMeshPro, or third-party UI packages
- **Composition**: LobbyCompositionRoot owns all wiring. Do not use `GameObject.Find`, `FindObjectOfType`, tag lookups, or `SendMessage`
- **Scene boundaries**: Lobby only orchestrates session setup. It must not contain race logic, AR logic, or boot responsibilities
- **MonoBehaviours must be thin**: They serialize references, bridge lifecycle callbacks, and delegate. They do not own business logic
- **Naming**: Classes use `PascalCase`, private fields use `_camelCase`, namespaces follow folder: `SlotCarRacingAR.Runtime.App` and `SlotCarRacingAR.Runtime.UI`
- **No singletons**: No mutable global singletons for UI flow
- **Cleanup**: Every subscription, callback, and delayed action must have explicit cleanup on `OnDisable`/`OnDestroy`

### UX Design Requirements (from UX Specification)

- **UX-DR1**: Exactly two dominant actions: Create Match and Join Match
- **UX-DR15**: Single dominant primary action on every non-race screen
- **UX-DR21**: Onboarding copy extremely short, centered on "hold to accelerate, release in curves"
- **UX-DR19**: Semantic color roles — primary CTA amber, player 1 red, player 2 green
- **UX-DR26**: Minimum touch targets 48x48 dp for secondary controls
- **UX-DR27**: Respect safe areas, notches, punch-holes, and gesture zones
- **UX-DR18**: High-contrast backplates for critical text over camera feed
- **UX-DR17**: Landscape layout: top status band, clear center zone, bottom action zone
- **Navigation pattern**: "Inicio binario: crear o unirse" — binary start, create or join

### Shared Session Card Component (from UX Specification)

The UX spec defines a "Shared Session Card" component for session entry:
- Title, subtitle, session state, primary action, optional secondary action, local network indicator
- States: idle, creating, joining, waiting, connected, network-error
- Variants: Host / Guest
- Large primary button, simple language, textual state visible (not just color)
- For this story, only the **idle** state is needed (the card showing Create/Join options before any session is started)

### File Structure

All new UI code goes in `Assets/SlotCarRacingAR/Scripts/Runtime/UI/`.
Updates to LobbyCompositionRoot stay in `Assets/SlotCarRacingAR/Scripts/Runtime/App/`.
The `LobbyStartRaceButtonPlaceholder.cs` file should be **deleted or emptied** after replacement (confirm with user before deleting).

### Technical Requirements

- Unity 6.3 LTS — do not use any API that requires a different version
- UGUI components: `Canvas`, `CanvasScaler`, `Image`, `Text`, `Button`, `RectTransform`, `VerticalLayoutGroup`/`HorizontalLayoutGroup` as needed
- Input System: The `InputSystemUIInputModule` is already ensured by LobbyCompositionRoot
- All UI must be built programmatically (no prefabs needed for this placeholder stage), consistent with Story 1.1's approach
- Use `[SerializeField] private` for inspector wiring where applicable
- The existing `PlaceholderCanvasScreenOverlay` handles Canvas setup (ScreenSpaceOverlay, 1920x1080 reference, matchWidthOrHeight=1). Reuse it or replicate its settings

### Project Context Rules

- Only asmdefs allowed: `SlotCarRacingAR.Core`, `SlotCarRacingAR.Runtime`, `SlotCarRacingAR.Editor`, `SlotCarRacingAR.Tests.EditMode`, `SlotCarRacingAR.Tests.PlayMode`
- Core is pure C# — no UnityEngine. This story should not need Core changes
- Runtime/UI presents state and emits UI intent only. It must not own gameplay rules
- ScriptableObjects are read-only config. Do not create mutable session state in ScriptableObjects
- Coroutines only for short-lived local view sequencing
- No `Update`/`LateUpdate` unless genuinely time-based
- Per-frame logging forbidden in hot paths

### Previous Story Intelligence (Story 1.1)

- Programmatic UI pattern established: `TrackSizePanel.cs` builds Canvas, sliders, labels, and background entirely in code
- `PlaceholderCanvasScreenOverlay.cs` standardizes Canvas setup: ScreenSpaceOverlay, CanvasScaler with 1920x1080 reference
- `LobbyCompositionRoot` already handles camera permission and scene transition — reuse `TransitionToRace()` for both Create and Join paths
- The existing green placeholder button is at 35%-65% horizontal, 8%-20% vertical — replace with proper layout

### References

- [Source: _bmad-output/planning-artifacts/epics.md — Epic 1, Story 1.2]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md — Shared Session Card, UX-DR1, UX-DR15, UX-DR17-19, UX-DR21, UX-DR26-27]
- [Source: _bmad-output/planning-artifacts/game-architecture.md — UI stack UGUI, composition root pattern]
- [Source: _bmad-output/project-context.md — code organization rules, naming, assembly restrictions]
- [Source: Scripts/Runtime/App/LobbyCompositionRoot.cs — current lobby wiring and camera permission flow]
- [Source: Scripts/Runtime/App/LobbyStartRaceButtonPlaceholder.cs — placeholder to replace]
- [Source: Scripts/Runtime/UI/PlaceholderCanvasScreenOverlay.cs — canvas setup pattern]
- [Source: Scripts/Runtime/UI/TrackSizePanel.cs — programmatic UI construction pattern]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (GitHub Copilot)

### Debug Log References

- No runtime errors. All compilation checks pass.

### Completion Notes List

- Created `LobbyStartScreen.cs` in `Scripts/Runtime/UI/` — programmatic UGUI start screen with title, onboarding guidance, and two action buttons
- Updated `LobbyCompositionRoot.cs` — replaced `LobbyStartRaceButtonPlaceholder` dependency with programmatic `LobbyStartScreen` creation; added `OnCreateMatchSelected()` and `OnJoinMatchSelected()` methods; deactivates old placeholder button at runtime
- `LobbyStartRaceButtonPlaceholder.cs` kept in codebase (not deleted) to avoid breaking scene references — deactivated at runtime by composition root
- Both action buttons are placeholder: they log the selection and transition directly to Race scene. Stories 1.3 and 1.4 will add real session creation/join logic
- Onboarding text in Spanish matching target audience: "Ambos jugadores necesitan estar en la misma red Wi-Fi y apuntar al mismo marcador. Regla: Mantén para acelerar, suelta en las curvas."
- Create Match button uses amber (#FFD740) per UX-DR19 semantic color for primary CTA
- All UI built programmatically consistent with Story 1.1 patterns (TrackSizePanel)

### File List

- `My project/Assets/SlotCarRacingAR/Scripts/Runtime/UI/LobbyStartScreen.cs` (NEW)
- `My project/Assets/SlotCarRacingAR/Scripts/Runtime/App/LobbyCompositionRoot.cs` (MODIFIED)

### Change Log

- 2026-05-10: Story implemented — start screen with Create/Join buttons and onboarding guidance
