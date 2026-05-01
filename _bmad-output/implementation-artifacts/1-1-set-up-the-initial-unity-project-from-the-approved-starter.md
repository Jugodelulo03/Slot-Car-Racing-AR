# Story 1.1: Set Up the Initial Unity Project from the Approved Starter

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As the development team,
I want to initialize the project from the approved Unity baseline,
so that every later gameplay story builds on the correct Android, AR, networking, and telemetry foundation.

## Acceptance Criteria

1. Given no production project baseline exists, when the initial project is created, then it is created as a clean Unity 6.3 LTS project from Unity Hub targeting Android and no learning-template sample content is carried into the production structure.
2. Given the project baseline is being configured, when core packages and build support are installed, then URP 17.6, AR Foundation 6.5, ARCore XR Plugin 6.5, Netcode for GameObjects 2.11, and Unity Transport 6.5 are pinned to the approved MVP versions and Android Build Support with SDK, NDK, and OpenJDK is enabled.
3. Given the first technical scaffold is prepared, when the baseline commit is completed, then Boot, Lobby, and Race scenes, the approved folder structure and asmdefs, and a minimum vertical slice with marker detection, visible track, one functional car, one acceleration button, and initial FPS, tracking-loss, and GC telemetry are present and later stories can build without redoing project initialization.

## Tasks / Subtasks

- [ ] Create the clean Unity baseline and Android target setup. (AC: 1, 2)
  - [ ] Create a new Unity 6.3 LTS project from Unity Hub without using a learning template.
  - [ ] Confirm Android is the active target platform.
  - [ ] Confirm Android Build Support with SDK, NDK, and OpenJDK is available for the editor installation.
- [ ] Pin the approved MVP package set. (AC: 2)
  - [ ] Add and pin `com.unity.render-pipelines.universal@17.6`.
  - [ ] Add and pin `com.unity.xr.arfoundation@6.5`.
  - [ ] Add and pin `com.unity.xr.arcore@6.5`.
  - [ ] Add and pin `com.unity.netcode.gameobjects@2.11`.
  - [ ] Add and pin `com.unity.transport@6.5`.
  - [ ] Do not introduce unapproved package upgrades or substitute packages.
- [ ] Create the approved initial project structure. (AC: 3)
  - [ ] Create `Assets/SlotCarRacingAR/Scenes/` with `Boot.unity`, `Lobby.unity`, and `Race.unity`.
  - [ ] Create the approved runtime, core, editor, data, and test folder layout under `Assets/SlotCarRacingAR/`.
  - [ ] Create only the allowed asmdefs: `SlotCarRacingAR.Core`, `SlotCarRacingAR.Runtime`, `SlotCarRacingAR.Editor`, and `SlotCarRacingAR.Tests` if the implementation keeps a single test assembly, or `SlotCarRacingAR.Tests.EditMode` and `SlotCarRacingAR.Tests.PlayMode` if the project keeps the split test assembly model defined in architecture.
- [ ] Establish the minimum technical scaffold for future slices. (AC: 3)
  - [ ] Prepare a Boot composition root that initializes global prerequisites and routes into the next flow state.
  - [ ] Prepare scene ownership boundaries so Boot only boots, Lobby only handles session setup, and Race only handles active race composition.
  - [ ] Add the first vertical-slice scaffold targets: marker detection entry point, visible track placeholder, one functional car placeholder, one acceleration input placeholder, and telemetry hooks.
- [ ] Add baseline observability and validation hooks. (AC: 3)
  - [ ] Add initial telemetry seams for FPS, tracking loss, GC spikes, and network latency when applicable.
  - [ ] Keep debug and instrumentation development-only and non-blocking for release builds.
  - [ ] Ensure the initial baseline is shaped so later stories do not need to undo project setup decisions.

## Dev Notes

- This story is the only approved technical bootstrap exception in the epic sequence. It is valid because the architecture explicitly requires a clean starter, pinned package policy, initial scenes, asmdefs, and baseline telemetry before gameplay stories proceed.
- This story implements architecture starter requirements rather than a player-facing FR. Do not expand it into unrelated gameplay work beyond the minimum vertical slice scaffold.
- The minimum vertical slice here is a scaffold target, not a full gameplay implementation. The objective is to establish the project baseline and the first safe extension points for later stories.

### Technical Requirements

- Unity version must remain `Unity 6.3 LTS`.
- MVP target platform is Android only.
- Required package versions are fixed: URP 17.6, AR Foundation 6.5, ARCore XR Plugin 6.5, Netcode for GameObjects 2.11, and Unity Transport 6.5.
- Android Build Support with SDK, NDK, and OpenJDK is mandatory from the start.
- The baseline must support validation on at least two ARCore-compatible Android phones, with Samsung S24 FE as the primary reference device.
- The project must start clean from Unity Hub and must not inherit learning-template sample content.

### Architecture Compliance

- Productive runtime scenes are limited to `Boot`, `Lobby`, and `Race`.
- Boot must initialize prerequisites and route only. Lobby must own session creation, join, readiness, and pre-race coordination. Race must own active race runtime only.
- Composition must be explicit per scene. Do not rely on `GameObject.Find`, tag lookups, script execution order, or hidden scene wiring.
- `Core` must remain pure C# with no `UnityEngine`, `MonoBehaviour`, `ScriptableObject`, AR, NGO, or platform dependencies.
- `MonoBehaviour` and `NetworkBehaviour` classes must stay thin and delegate logic to plain C# services or state machines.
- `ScriptableObject` assets are read-only authored configuration only, not mutable session state or event channels.
- No raw AR world transform synchronization may be introduced. Shared gameplay truth must remain separate from AR presentation truth.

### Library / Framework Requirements

- Use UGUI for MVP HUDs, overlays, menus, and result screens.
- Use AR Foundation and ARCore via runtime adapters in Infrastructure, not directly as gameplay truth owners.
- Use NGO and Unity Transport under a host-authoritative session model.
- Optional MCP Unity tooling is allowed only as an accelerator after the project first opens and must remain non-blocking.
- Do not add Addressables, `Resources.Load`, or unapproved external runtime libraries as shortcuts in this baseline.

### File Structure Requirements

- Create runtime code only under `Assets/SlotCarRacingAR`.
- Follow the approved hybrid layout:
  - `Assets/SlotCarRacingAR/Scripts/Core/`
  - `Assets/SlotCarRacingAR/Scripts/Runtime/App/`
  - `Assets/SlotCarRacingAR/Scripts/Runtime/Infrastructure/`
  - `Assets/SlotCarRacingAR/Scripts/Runtime/Features/`
  - `Assets/SlotCarRacingAR/Scripts/Runtime/UI/`
  - `Assets/SlotCarRacingAR/Scripts/Runtime/Debug/`
  - `Assets/SlotCarRacingAR/Scripts/Editor/`
  - `Assets/SlotCarRacingAR/Tests/EditMode/`
  - `Assets/SlotCarRacingAR/Tests/PlayMode/`
  - `Assets/SlotCarRacingAR/Data/Config/`
  - `Assets/SlotCarRacingAR/Data/Tracks/`
  - `Assets/SlotCarRacingAR/Data/MarkerProfiles/`
- Allowed asmdefs for MVP are limited to:
  - `SlotCarRacingAR.Core`
  - `SlotCarRacingAR.Runtime`
  - `SlotCarRacingAR.Editor`
  - `SlotCarRacingAR.Tests.EditMode`
  - `SlotCarRacingAR.Tests.PlayMode`
- Do not create a feature-specific asmdef at this stage.

### Testing Requirements

- Validate the baseline on at least two ARCore-compatible Android phones before the stack is treated as proven.
- Prepare EditMode seams for pure logic and configuration validation even if most gameplay is not yet implemented.
- Prepare PlayMode smoke-testable scene composition for `Boot`, `Lobby`, and `Race`.
- Ensure baseline diagnostics can surface FPS, GC alloc pressure, tracking loss, and future RTT or jitter signals in development builds.
- Do not claim this story complete if the setup cannot reasonably support later session, calibration, and race stories without structural rework.

### Project Structure Notes

- No previous implementation story exists yet, so there are no carry-forward code learnings or file patterns from earlier story execution.
- The main structural risk is starting too loosely and forcing later refactors. Favor strong baseline boundaries now over speed shortcuts.
- Keep the scene and folder setup intentionally minimal. This story should establish the skeleton, not complete later features early.

### Project Context Rules

- During MVP, do not upgrade the editor or the pinned package set unless a documented blocker is explicitly approved.
- Every runtime scene must declare an explicit composition root and deterministic initialization order.
- Do not introduce mutable global singletons for gameplay, AR flow, networking, UI flow, or race state.
- Cross-scene data must move through explicit session state or snapshot handoff, never by holding old scene objects alive.
- Use typed immutable events only for one-way notifications, not as a command bus.
- Coroutines are allowed only for short-lived local view sequencing. Flow ownership must live in explicit services or state machines.
- Hot-path code must avoid preventable allocations and per-frame logging.
- Gameplay is landscape-only, camera-first, and must preserve a protected central AR zone with peripheral UI.
- Platform assumptions are Android only, ARCore-compatible phones only, and local host-client multiplayer only.

### References

- `l:\ARGame\Slot-Car-Racing-AR\_bmad-output\planning-artifacts\epics.md` - Epic 1, Story 1.1
- `l:\ARGame\Slot-Car-Racing-AR\_bmad-output\planning-artifacts\game-architecture.md` - Project Initialization
- `l:\ARGame\Slot-Car-Racing-AR\_bmad-output\planning-artifacts\game-architecture.md` - Allowed asmdefs for MVP
- `l:\ARGame\Slot-Car-Racing-AR\_bmad-output\planning-artifacts\game-architecture.md` - System Location Mapping
- `l:\ARGame\Slot-Car-Racing-AR\_bmad-output\planning-artifacts\game-architecture.md` - Prerequisites
- `l:\ARGame\Slot-Car-Racing-AR\_bmad-output\planning-artifacts\game-architecture.md` - Setup Commands
- `l:\ARGame\Slot-Car-Racing-AR\_bmad-output\planning-artifacts\game-architecture.md` - First Steps
- `l:\ARGame\Slot-Car-Racing-AR\_bmad-output\planning-artifacts\gdd-slot-car-racing-ar.md` - MVP technical constraints and NFR baseline
- `l:\ARGame\Slot-Car-Racing-AR\_bmad-output\project-context.md` - Technology Stack & Versions, Critical Implementation Rules, Code Organization Rules, Testing Rules, Platform & Build Rules

## Dev Agent Record

### Agent Model Used

GPT-5.4

### Debug Log References

- No implementation debug log yet. This file is a pre-dev context artifact.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- No previous story intelligence was available because this is the first implementation story.
- No external web research was required because package and engine versions are already pinned by architecture and project context.

### File List

- `_bmad-output/implementation-artifacts/1-1-set-up-the-initial-unity-project-from-the-approved-starter.md`
