---
project_name: 'Proyecto-Movimiento'
user_name: 'Jpinzon'
date: '2026-04-16'
sections_completed: ['technology_stack', 'engine_rules', 'performance_rules', 'organization_rules', 'testing_rules', 'platform_rules', 'anti_patterns']
status: 'complete'
rule_count: 103
optimized_for_llm: true
existing_patterns_found: 7
---

# Project Context for AI Agents

_This file contains critical rules and patterns that AI agents must follow when implementing game code in this project. Focus on unobvious details that agents might otherwise miss._

---

## Technology Stack & Versions

- Engine: Unity 6.3 LTS
- Target platform: Android
- Render pipeline: URP 17.6
- AR stack: AR Foundation 6.5 + ARCore XR Plugin 6.5
- Networking stack: Netcode for GameObjects 2.11 + Unity Transport 6.5
- UI stack: UGUI
- Audio stack: Unity Audio + Mixer
- Persistence: local minimal config using ScriptableObjects + constants + PlayerPrefs/JSON where needed
- Architecture posture: host-authoritative local multiplayer with TrackFreeze, Race Space Snapshot and Spatial Trust/Recovery
- Project organization: hybrid structure under Assets/ProyectoMovimiento with minimal asmdefs

## Critical Implementation Rules

### Engine-Specific Rules

- During MVP, the only allowed versions are Unity 6.3 LTS, URP 17.6, AR Foundation 6.5, ARCore XR Plugin 6.5, Netcode for GameObjects 2.11, and Unity Transport 6.5. Do not upgrade the editor or these packages. The only exception is a documented blocking issue explicitly approved by architecture.
- A file may live in `Core` only if it compiles without references to `UnityEngine`, `MonoBehaviour`, `ScriptableObject`, scene objects, AR Foundation, NGO, Unity Transport, or platform APIs.
- `Core` is pure domain code. If a class imports or directly depends on Unity, AR, networking, scene objects, or Android/platform APIs, it belongs in `Runtime` or `Editor`, never in `Core`.
- `MonoBehaviour` and `NetworkBehaviour` classes are thin engine adapters only. They may serialize references, bridge lifecycle callbacks, and delegate to plain C# services or state machines, but they must not own race rules, authority decisions, or shared snapshot mutation.
- Every runtime scene must declare an explicit composition root for wiring. Do not use `GameObject.Find`, `FindObjectOfType`, `FindFirstObjectByType`, `FindAnyObjectByType`, tag/name lookups, `SendMessage`, `BroadcastMessage`, or hierarchy assumptions for runtime wiring.
- Do not rely on Script Execution Order, implicit `Awake`/`Start` timing, static initialization side effects, or scene-load side effects for correctness. Composition roots must initialize collaborators explicitly in deterministic order.
- The only runtime scenes in the MVP are `Boot`, `Lobby`, and `Race`. `Boot` only initializes and routes. `Lobby` only orchestrates join, readiness, and session setup. `Race` only composes and runs the active race. No scene may host the primary responsibilities of another scene.
- Do not introduce mutable global singletons for gameplay, AR flow, networking, UI flow, or race state. Only the minimal approved boot/session root may persist across scenes with a documented responsibility.
- Cross-scene data moves through explicit session state or snapshot handoff. Never retain runtime scene objects from a previous scene after a transition.
- `ScriptableObject` assets are read-only runtime inputs for authored configuration and tuning. They must not store mutable session, player, calibration, or race state, and must not be used as event channels or service locators.
- Any value that can alter race geometry, calibration thresholds, session rules, shared gameplay truth, randomness seeds, or authority tolerances must be serialized in the approved host-owned runtime artifact and applied atomically. Clients may not derive their own equivalent truth locally.
- AR Foundation, NGO, Unity Transport, input, and platform-facing APIs must be wrapped behind replaceable `Runtime` adapters. Gameplay services and shared race logic consume plain C# commands, DTOs, and immutable state.
- Every AR pose, anchor, plane, or Unity world transform must be converted at the runtime boundary into the approved race-space contract. Shared gameplay truth never uses raw Unity world space as its source of truth.
- Only the host may create or destroy networked objects and write authoritative shared state. Do not use continuous transform synchronization for AR poses, frozen geometry, or race truth.
- Rigidbody, PhysX, Animator, Timeline, or visual interpolation may represent presentation only. They are never the source of truth for shared race gameplay.
- Use typed immutable events only for one-way notifications between decoupled systems. Do not use `UnityEvent`, ScriptableObject event channels, string-based events, or the event bus for commands, request/response flows, sequential orchestration, or per-frame signaling.
- Use `[SerializeField] private` for inspector wiring. Required collaborators must be assigned via serialized references, explicit initialization, or scene-owned factories, and missing required references must fail fast in `UNITY_EDITOR` or `DEVELOPMENT_BUILD`.
- All runtime-spawned objects must be created and destroyed by a scene-owned factory or spawner. Networked objects are spawned only by the approved host-owned session or race spawner.
- Do not use `Resources.Load`, path-based loading, or Addressables as an architectural shortcut during MVP. Runtime assets enter through serialized references or approved registries only.
- Coroutines are allowed only for short-lived local view sequencing. Session flow, race flow, authority flow, and trust-state flow must live in explicit state machines or services.
- Every subscription, coroutine, async operation, delayed action, and callback chain must have a clear owner plus explicit cleanup on scene exit, `OnDisable`, or `OnDestroy`.
- Do not add `Update`, `LateUpdate`, or `FixedUpdate` unless the component owns genuinely time-based behavior. Hot-path loops must remain allocation-free and must not split shared truth across multiple update mechanisms.
- Use `OnValidate` only for editor-time validation and authoring feedback. Never rely on `OnValidate` for runtime state creation or hidden auto-wiring.
- During MVP, only these asmdefs are allowed: `ProyectoMovimiento.Core`, `ProyectoMovimiento.Runtime`, `ProyectoMovimiento.Editor`, `ProyectoMovimiento.Tests.EditMode`, and `ProyectoMovimiento.Tests.PlayMode`. Creating a new asmdef or changing the reference graph requires explicit architecture approval.
- Allowed dependency direction is strict: `Core` cannot reference `Runtime` or `Editor`; `Runtime` may depend on `Core`; `Editor` may depend on `Core` and `Runtime`; tests may depend on `Core` and `Runtime`, never the reverse. `UnityEditor` must never appear outside `Editor`, not even behind conditional compilation.
- If an implementation appears to require an exception to these rules, the agent must stop and request explicit approval rather than assume the exception is allowed.

### Performance Rules

- Shipping floor is a stable minimum of 30 FPS on validated target-device tiers.
- Hot-path target is a smoother race experience closer to 60 FPS where feasible, but that is not a blocking MVP promise.
- Race-critical loops, per-frame AR presentation, and session flow must avoid preventable allocations, heavy logging, and per-frame signaling patterns.
- `Result` objects must not be used as the main control mechanism in hot paths such as `Update`, `FixedUpdate`, per-tick race simulation, or per-frame AR presentation.
- Per-frame logging is forbidden in critical paths, and allocation-heavy string formatting is forbidden inside race-critical loops.
- The event bus must not carry per-frame simulation, position streaming, or continuous UI update traffic.
- Pooling is out of scope for MVP unless profiling proves frame-time spikes or GC pressure. If introduced, it must define explicit reset and lifecycle contracts.
- Debug overlays and telemetry panels must be read-only by default, refresh on a throttled cadence when possible, and must not meaningfully affect race performance.
- Performance decisions must be driven by profiling and telemetry, not intuition. Minimum tracked metrics include FPS p50/p95, frame-time spikes, GC alloc/frame, RTT, jitter, drift, and thermal degradation indicators.
- Device thermal degradation and tier differences are first-class product constraints. Performance claims are valid only for validated device tiers.
- Telemetry must be rich enough to correlate runtime slowdowns with AR instability, network degradation, recovery behavior, and session invalidation.
- If a solution improves flexibility or architecture but degrades the race hot path beyond validated thresholds, it is rejected until proven acceptable by measurement.

### Code Organization Rules

- All runtime code lives under `Assets/ProyectoMovimiento`, with asset folders organized by Unity resource type and script folders organized by responsibility.
- Use only the approved hybrid layout: `Scripts/Core`, `Scripts/Runtime/App`, `Scripts/Runtime/Infrastructure`, `Scripts/Runtime/Features`, `Scripts/Runtime/UI`, `Scripts/Runtime/Debug`, `Scripts/Editor`, and `Tests`.
- `Core` contains shared contracts, primitives, result types, domain state rules, and other cross-feature code that is truly engine-agnostic.
- `Runtime/App` owns bootstrapping, composition roots, and visible scene wiring.
- `Runtime/Infrastructure` owns AR, networking, persistence, platform, and telemetry adapters. It does not own race rules.
- `Runtime/Features` is limited in MVP to `Calibration`, `Session`, `Race`, and `Results`. Do not invent new feature folders casually.
- `Runtime/UI` presents state and emits UI intent only. It must not own gameplay rules or AR/network authority.
- `Runtime/Debug` contains runtime-only development helpers gated for editor or development builds only.
- `Scripts/Editor` contains editor-only tooling, validators, and build helpers. Nothing under `Editor` may compile into player runtime.
- Runtime scenes are limited to `Boot`, `Lobby`, and `Race`. Technical experiments belong in `Scenes/Sandboxes` and must never become hidden production dependencies.
- Sandboxes are single-purpose technical probes only: calibration, networking, or performance. If one becomes part of the real flow, it must be promoted or removed.
- Config assets belong under `Data/Config`, track definitions under `Data/Tracks`, and marker profiles under `Data/MarkerProfiles`.
- EditMode tests belong in `Tests/EditMode` for pure logic, contracts, configuration, and utilities. PlayMode tests belong in `Tests/PlayMode` for smoke tests, scene integration, and AR/network harnesses.
- Every script must be classifiable into exactly one area. If placement is ambiguous, the design must be clarified before implementation.
- Correct placement matters: `LapTimeRules` belongs in `Scripts/Core`, `ARSessionAdapter` in `Scripts/Runtime/Infrastructure/AR`, `RaceCoordinator` in `Scripts/Runtime/Features/Race`, and `CountdownPresenter` in `Scripts/Runtime/UI/Presenters`.
- Invalid placement is an architecture violation, not a style nit. Examples: a presenter in `Core`, an AR repository in `UI`, a `MonoBehaviour` spawner in `Core`, or production logic inside a sandbox.
- Naming stays conventional: classes, methods, scenes, prefabs, and ScriptableObjects use `PascalCase`; interfaces use `IPascalCase`; private fields use `_camelCase`; locals and parameters use `camelCase`.
- Namespaces follow folder intent, for example `ProyectoMovimiento.Runtime.Features.Race`.
- ScriptableObjects use semantic suffixes such as `RaceConfig`, `MarkerProfile`, and `DeviceTierConfig`.
- Events use completed-state or domain-action naming such as `CalibrationLocked`, `TrackFreezeCompleted`, and `RaceInvalidated`.

### Testing Rules

- Use `Tests/EditMode` for pure logic, contracts, configuration validation, repositories, state machines, and utilities that run without scene loading, AR session startup, or live networking.
- Use `Tests/PlayMode` for scene smoke tests, runtime integration, and AR/network harnesses that require Unity runtime behavior.
- Test code may depend on `Core` and `Runtime`, but production code must never depend on test assemblies, test doubles, or harnesses.
- Every race-critical subsystem must expose at least one EditMode-testable seam in plain C#.
- All critical systems must support testable failure injection and observable state transitions.
- At minimum, tests and harnesses must be able to simulate AR tracking loss, marker loss during setup, host disconnect, client disconnect, packet loss/jitter, app pause/resume, and delayed calibration confirmation.
- Host-authoritative flows must be verifiable through explicit diagnostics for TrackFreeze, Race Space Snapshot creation/application, Spatial Trust transitions, session join/leave, race start, race finish, and invalidation/recovery paths.
- PlayMode tests should focus on smoke coverage for `Boot`, `Lobby`, and `Race` scene composition, plus key AR/network integration boundaries.
- EditMode tests should cover contracts, thresholds, configuration resolution, snapshot validation, state transitions, and failure handling without requiring device-only dependencies.
- Test doubles belong under `Tests/EditMode/TestDoubles` or the equivalent test-only location, never inside production runtime folders.
- Any config or threshold that affects branching behavior must be validated in tests and must fail fast when invalid in editor or development builds.
- Shared gameplay payloads and snapshots must be covered by deterministic round-trip and version-compatibility tests.
- Development builds must preserve enough diagnostics to reproduce multiplayer, AR, and recovery failures on device; essential observability cannot exist only in the editor.
- If a subsystem cannot be injected, observed, and driven in tests, it is not ready to own gameplay truth.

### Platform & Build Rules

- The MVP targets Android only. Do not add iOS, desktop, or cross-platform abstractions unless a real requirement appears.
- Supported devices must be ARCore-compatible Android phones. Public compatibility may be stated broadly, but official support is defined only by validated device tiers.
- Android build support is mandatory from the start: SDK, NDK, and OpenJDK must be installed and aligned with Unity 6.3 LTS.
- Every runtime build must assume real-device AR validation, not editor-only confidence. A feature is not ready until it runs on at least two ARCore-compatible Android devices.
- The project must validate device compatibility, permissions, and AR readiness before entering calibration or race flow.
- Gameplay is landscape-only. Do not introduce portrait assumptions, adaptive reflows, or alternative gameplay orientations for MVP.
- The AR presentation must preserve a protected central AR zone with peripheral controls in landscape.
- Multiplayer is local host-client only. Testing and support assumptions must use local Wi-Fi or hotspot conditions, not internet-hosted networking.
- The host is authoritative. Clients send only acceleration input and intention events; they do not publish authoritative race state.
- No host migration exists in MVP. If the host fails, the session ends and returns to lobby.
- Do not synchronize raw AR world state over the network. The network synchronizes session and race state; the shared spatial reference comes from calibration and frozen race-space data.
- Platform and tier differences must be resolved through approved configuration such as platform-tier assets, not scattered ad hoc conditionals across gameplay code.
- Release-capable builds may keep essential error and warning diagnostics, but debug commands and development-only tooling must never remain player-accessible in release.
- Sandboxes, debug helpers, and technical probes must stay out of the production build flow unless explicitly promoted.

### Critical Don't-Miss Rules

- Never allow the race to continue, pause, resume, or invalidate after a race-critical failure without an explicit authoritative reason code, visible player-facing state, and structured diagnostics.
- Never create multiple mutable sources of truth for calibration, race configuration, race-space state, or shared gameplay outcomes.
- Never allow any client, smoothing layer, or local AR correction to reinterpret, rescale, replace, or patch the host-owned frozen race space after TrackFreeze.
- Never recalculate, rescale, or reshape the shared track during active race flow, even if a local device appears to have found a better fit.
- Never treat smooth visuals, stable FPS, or plausible local alignment as evidence that shared spatial trust is still valid.
- Never let Degraded or Lost spatial trust remain advisory-only; those states must deterministically gate input, progression, pause, resume, and invalidation.
- Never implement race-critical failure handling as UI-only; invalid trust, authority, or transport must atomically stop simulation, timers, lap adjudication, and race-critical network emission.
- Never resume, rejoin, or unpause from stale calibration, stale snapshot data, stale session epoch, stale race phase, or mixed snapshot versions; stale participants must fully resync or fail closed.
- Never allow recovery, reconnect, or invalidation to bypass the state machine; every such transition must be explicit, phase-gated, idempotent, and testable.
- Never apply snapshot-defined spatial or race state partially, incrementally, or from mixed versions; apply atomically or reject.
- Never use the event bus for commands, acknowledgements, request-response flows, sequential orchestration, or per-frame simulation traffic.
- Never hide recovery or invalidation behind silent fallbacks, opaque retries, auto-healing, or empty catch blocks; preserve first-failure evidence before attempting recovery.
- Never attempt host migration, temporary client authority, or peer failover in the MVP; host failure is a hard stop that returns the session to lobby.
- Never let countdown, start, pause, resume, lap confirmation, finish, or results resolve at visibly different times or trust states across devices.
- Never keep race-speed audio, hype UI, or celebration feedback running when the session is no longer trustworthy enough to defend the shared-race fantasy.
- Never expose debug commands or debug-only helpers in release-capable player flows.
- Never assume editor behavior or ideal local Wi-Fi conditions prove that race-critical behavior is safe on real Android target devices.

---

## Usage Guidelines

**For AI Agents:**

- Read this file before implementing any game code.
- Follow all rules exactly as documented.
- When in doubt, prefer the more restrictive option.
- Update this file if new non-obvious patterns emerge.

**For Humans:**

- Keep this file lean and focused on agent needs.
- Update it when the technology stack or architectural rules change.
- Review it periodically to remove rules that became obvious or obsolete.
- Use it as the first consistency checkpoint before accepting architecture-affecting code.

Last Updated: 2026-04-16