---
stepsCompleted:
  - 1
  - 2
  - 3
  - 4
inputDocuments:
  - l:\ARGame\Slot-Car-Racing-AR\_bmad-output\planning-artifacts\gdd-slot-car-racing-ar.md
  - l:\ARGame\Slot-Car-Racing-AR\_bmad-output\planning-artifacts\game-architecture.md
  - l:\ARGame\Slot-Car-Racing-AR\_bmad-output\planning-artifacts\ux-design-specification.md
workflowType: epics-and-stories
project_name: Slot Car Racing AR
user_name: Jpinzon
date: 2026-05-01
language: en
status: validation-complete
lastStep: 4
---

# Slot Car Racing AR - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for Slot Car Racing AR, decomposing the requirements from the GDD, UX Design, and Architecture into implementable stories.

## Requirements Inventory

### Functional Requirements

FR1: The game shall allow one player to create a local multiplayer session as host.
FR2: The game shall allow a second player to join an existing session on the same local Wi-Fi network or hotspot.
FR3: The game shall guide both players to point their cameras at the same shared physical marker placed on the table.
FR4: The system shall detect the shared physical marker and render the predefined AR track anchored to the table.
FR5: The system shall validate that track placement is stable enough before the session can enter a race-ready state.
FR6: The system shall let both players confirm that the visible track is sufficiently aligned and ready for play.
FR7: The system shall assign player identity and car color automatically by join order, with player 1 as red and player 2 as green.
FR8: The MVP shall provide exactly one predefined closed track.
FR9: The system shall start a shared countdown only after both players are connected and ready.
FR10: The system shall give each player control of one slot car on a fixed lane.
FR11: The player control scheme shall use one acceleration input button that increases speed while held.
FR12: The system shall allow players to release acceleration to regain control before or during curves.
FR13: The system shall detect when a car exceeds the safe speed threshold in a curve.
FR14: The system shall apply a readable penalty for unsafe curve speed, including temporary speed loss and visible loss-of-control feedback.
FR15: The penalized car shall automatically return to its lane after the penalty resolves.
FR16: The system shall track lap progress and current lap count for each car over the fixed rail or spline track.
FR17: The system shall determine the winner as the first player to complete the configured number of laps.
FR18: The race HUD shall display the minimum race information needed during play: acceleration control, current lap, position, and brief critical messages.
FR19: The system shall present onboarding and contextual instructions for local network setup, marker scanning, shared-track confirmation, and the acceleration rule.
FR20: The system shall present explicit status feedback for marker detection, tracking stability, rival connection, readiness, and countdown state.
FR21: The system shall present explicit error and recovery states for marker not detected, unstable tracking, tracking loss, missing local network, rival not connected, and disconnect during race.
FR22: The system shall display a result screen after each race and allow players to choose rematch or exit.
FR23: The system shall support rematch without repeating the full setup when tracking remains valid.
FR24: The multiplayer race shall run with host-authoritative race resolution where the client sends input and the host resolves shared race state.
FR25: The networking layer shall synchronize shared race and session state rather than raw AR world transforms.
FR26: The MVP shall support one quick-race mode for exactly two local players.

### NonFunctional Requirements

NFR1: The MVP shall run on Android only.
NFR2: Setup from app open to race-ready shall complete in under 120 seconds under normal conditions.
NFR3: Session join shall complete in under 30 seconds once both players are on the same network.
NFR4: A race session shall target 60 to 90 seconds of active gameplay.
NFR5: Result presentation and rematch decision shall complete in under 20 seconds.
NFR6: The game shall sustain at least 30 FPS on the target Android device during a full race session.
NFR7: The application shall not crash during a continuous 5-minute session that includes setup, race, and rematch or recovery behavior.
NFR8: Device heating shall remain tolerable and shall not cause severe gameplay degradation during a normal session.
NFR9: First-time players shall understand how to start and use the one-button control within one race, without requiring a long tutorial.
NFR10: Critical information such as player identity, lap, position, and race readiness shall be understandable in under one second.
NFR11: The UI shall use minimal, non-technical language and prioritize one primary action per screen.
NFR12: The AR experience shall remain legible and usable in typical indoor tabletop conditions with moderate or good lighting.
NFR13: The system shall achieve at least a 90% full-race completion rate in internal tabletop tests.
NFR14: The experience shall feel fair, with race outcomes attributable to timing skill rather than hidden technical behavior.

### Additional Requirements

- Start from a clean Unity 6.3 LTS project created from Unity Hub, not from a learning template.
- Pin the approved package versions for MVP: URP 17.6, AR Foundation 6.5, ARCore XR Plugin 6.5, Netcode for GameObjects 2.11, and Unity Transport 6.5.
- Include a minimum technical scaffold from the first commit: Android configuration, marker detection, visible track, one functional car, one acceleration button, and initial instrumentation.
- Restrict productive runtime scenes to Boot, Lobby, and Race, with separate technical sandboxes for calibration, networking, and performance validation.
- Use explicit composition roots per runtime scene; do not rely on GameObject.Find, tag lookups, or script execution order for wiring.
- Keep Core as pure C# domain code, Runtime for Unity and package integrations, and Editor for editor-only tooling.
- Keep the MVP assembly definition set limited to SlotCarRacingAR.Core, SlotCarRacingAR.Runtime, SlotCarRacingAR.Editor, SlotCarRacingAR.Tests.EditMode, and SlotCarRacingAR.Tests.PlayMode.
- Use a host-authoritative session model; only the host may create or destroy networked objects and mutate shared race-critical state.
- Never synchronize raw AR world transforms across the network; share race state and frozen spatial contracts only.
- Use multi-marker calibration during setup with one primary marker and optional secondary boundary markers for surface validation.
- Allow track adaptation only during setup; freeze the shared race geometry before countdown by explicit TrackFreeze confirmation.
- Represent shared spatial truth as an immutable, versioned Race Space Snapshot that is applied atomically on clients.
- Implement spatial trust states Trusted, Degraded, and Lost, with host-driven pause, recovery, or invalidation decisions.
- End the session and return to lobby if the host fails; do not implement host migration in the MVP.
- Support landscape gameplay as the only gameplay orientation.
- Keep the UI camera-first by protecting the central AR zone and moving HUD and controls to the periphery.
- Use UGUI for HUDs, overlays, menus, and result screens.
- Use scene-based asset loading; do not introduce Addressables or Resources-based shortcuts for MVP runtime architecture.
- Treat ScriptableObjects as read-only authored configuration, not mutable session state or event channels.
- Persist only minimal local preferences, while host-owned runtime session snapshots hold shared gameplay-affecting values.
- Use typed immutable events for cross-system notifications only; do not use an event bus for per-frame commands or gameplay simulation.
- Restrict coroutines to short-lived local view sequencing; keep session flow, race flow, and recovery logic in explicit state machines or services.
- Add runtime telemetry for FPS, GC, RTT, jitter, drift, tracking loss, calibration events, and race invalidation.
- Add development-only debug overlays and fault injection hooks for AR loss, network issues, and recovery validation.
- Enforce a pinned-version policy during MVP; no editor or package upgrades unless a critical blocker is explicitly approved.
- Validate the technical baseline on at least two ARCore Android devices before considering the stack proven.
- Support recovery handling for tracking loss, client disconnect, and app background or foreground transitions.
- Keep race simulation truth separate from AR presentation truth at all times.

### UX Design Requirements

UX-DR1: Provide an initial screen with exactly two dominant actions: Create Match and Join Match.
UX-DR2: Use role-aware session entry UI that clearly shows whether the user is host or guest, current network state, and the next action.
UX-DR3: Guide marker scanning with non-technical copy that tells players to point both phones at the same shared marker.
UX-DR4: Provide a shared-track verification panel that translates technical tracking state into player-facing readiness states such as searching, track visible, unstable, and ready.
UX-DR5: Show both local confirmation and rival confirmation before allowing the race to start.
UX-DR6: Provide a compact ready-sync strip that shows host-ready, guest-ready, and both-ready states.
UX-DR7: Use a centered countdown overlay as the final transition from verification to race start.
UX-DR8: Implement a large hold-to-accelerate control designed for continuous press, not a simple tap button.
UX-DR9: Allow the acceleration control to be mirrored for left-handed or right-handed comfort before the race starts.
UX-DR10: Keep the race HUD minimal and place it in safe peripheral zones rather than over the track.
UX-DR11: Show only the essential race HUD data during play: lap, position, race state, and short critical prompts.
UX-DR12: Provide a short penalty feedback banner that explains the reason for the penalty, such as "Too fast in curve."
UX-DR13: Provide contextual toast messaging for marker detected, rival connected, track ready, and other short-lived confirmations.
UX-DR14: Provide a recovery action sheet or equivalent interruption pattern for tracking loss, network loss, rival disconnect, and restart decisions.
UX-DR15: Keep a single dominant primary action on every non-race screen.
UX-DR16: Reserve the central visual area for AR content and protect at least 60% of width by 55% of height from persistent HUD occlusion.
UX-DR17: Structure gameplay screens in landscape with a top status band, a clear center AR zone, and a bottom action zone.
UX-DR18: Use high-contrast backplates for all critical text rendered over the camera feed.
UX-DR19: Use semantic color roles consistently: player 1 red, player 2 green, technical confirmation cyan, primary CTA amber, warnings amber, and critical error red.
UX-DR20: Use a compact typography system that keeps large display treatment for countdown and results, and highly legible interface text for status and instructions.
UX-DR21: Keep onboarding copy extremely short and centered on the core rule: hold to accelerate and release in curves.
UX-DR22: Teach the curve rule through in-context feedback during the first race rather than through a long tutorial screen.
UX-DR23: Keep rematch as the primary post-race action whenever the track is still valid.
UX-DR24: Reuse the current validated track state for rematch when safe, instead of forcing full recalibration.
UX-DR25: Ensure critical states are communicated through at least two channels, such as text plus color, icon, shape, or haptics.
UX-DR26: Use minimum touch target sizes of 48x48 dp for secondary controls and 64x64 dp or larger for the acceleration control.
UX-DR27: Respect safe areas, notches, punch-holes, and gesture zones so critical HUD and controls never overlap them.
UX-DR28: Simplify the UI automatically in degraded conditions by hiding secondary elements and prioritizing tracking, connection, and recovery messaging.
UX-DR29: Maintain legibility of HUD and state text at 40 to 60 cm viewing distance under typical indoor lighting conditions.
UX-DR30: Use result presentation that makes the winner, next action, and rematch option understandable at a glance.

### FR Coverage Map

FR1: Epic 1 - create local session as host
FR2: Epic 1 - join local session as guest
FR3: Epic 2 - guide both players to the same shared marker
FR4: Epic 2 - detect marker and render the anchored track
FR5: Epic 2 - validate stable track placement before race-ready state
FR6: Epic 2 - confirm both players see a ready and aligned track
FR7: Epic 1 - assign player identity and car color by join order
FR8: Epic 2 - provide the one predefined closed track used for setup and play
FR9: Epic 2 - start countdown only after both players are connected and ready
FR10: Epic 3 - provide each player one slot car on a fixed lane
FR11: Epic 3 - support one-button hold-to-accelerate control
FR12: Epic 3 - allow releasing acceleration to regain control in curves
FR13: Epic 3 - detect unsafe curve speed
FR14: Epic 3 - apply readable curve penalties
FR15: Epic 3 - auto-return penalized cars to lane
FR16: Epic 3 - track lap progress and current lap count
FR17: Epic 3 - determine the winner by completed laps
FR18: Epic 3 - show essential race HUD information
FR19: Epic 1 - provide onboarding and contextual guidance
FR20: Epic 2 - show marker, tracking, readiness, and countdown status
FR21: Epic 4 - present error and recovery states
FR22: Epic 4 - show result screen with rematch or exit
FR23: Epic 4 - support rematch without full setup when tracking remains valid
FR24: Epic 3 - use host-authoritative race resolution
FR25: Epic 3 - synchronize shared race and session state instead of raw AR transforms
FR26: Epic 3 - support one quick-race mode for exactly two players

## Epic List

### Epic 1: Create and Join a Shared Session
Two players can create or join the same local session, understand their role, and enter the AR setup flow with clear onboarding.
**FRs covered:** FR1, FR2, FR7, FR19

### Epic 2: Confirm a Trustworthy Shared Track
Two players can point at the same marker, see the same anchored track, confirm stability, and reach a synchronized countdown with confidence.
**FRs covered:** FR3, FR4, FR5, FR6, FR8, FR9, FR20

### Epic 3: Race Head-to-Head with One-Button Skill
Two players can complete a fair local AR race with one-button acceleration, readable penalties, lap tracking, HUD feedback, and authoritative winner resolution.
**FRs covered:** FR10, FR11, FR12, FR13, FR14, FR15, FR16, FR17, FR18, FR24, FR25, FR26

### Epic 4: Recover, Finish, and Replay Confidently
Players can understand failures, recover when possible, see a clear result, and launch a fast rematch when the shared track remains valid.
**FRs covered:** FR21, FR22, FR23

## Epic 1: Create and Join a Shared Session

Two players can create or join the same local session, understand their role, and enter the AR setup flow with clear onboarding.

### Story 1.1: Set Up the Initial Unity Project from the Approved Starter

As the development team,
I want to initialize the project from the approved Unity baseline,
So that every later gameplay story builds on the correct Android, AR, networking, and telemetry foundation.

**Implements:** Architecture project-initialization and starter requirements.
**Supports:** NFR1, NFR6, NFR7

**Acceptance Criteria:**

**Given** no production project baseline exists
**When** the initial project is created
**Then** it is created as a clean Unity 6.3 LTS project from Unity Hub targeting Android
**And** no learning-template sample content is carried into the production structure.

**Given** the project baseline is being configured
**When** core packages and build support are installed
**Then** URP 17.6, AR Foundation 6.5, ARCore XR Plugin 6.5, Netcode for GameObjects 2.11, and Unity Transport 6.5 are pinned to the approved MVP versions
**And** Android Build Support with SDK, NDK, and OpenJDK is enabled.

**Given** the first technical scaffold is prepared
**When** the baseline commit is completed
**Then** Boot, Lobby, and Race scenes, the approved folder structure and asmdefs, and a minimum vertical slice with marker detection, visible track, one functional car, one acceleration button, and initial FPS, tracking-loss, and GC telemetry are present
**And** later stories can build without redoing project initialization.

### Story 1.2: Choose How to Start a Match

As a first-time player,
I want a simple start screen with clear create and join options,
So that I can begin the multiplayer flow without confusion.

**Implements:** FR19
**Supports:** NFR9, NFR11, UX-DR1, UX-DR15, UX-DR21

**Acceptance Criteria:**

**Given** the player opens the app on a supported Android device
**When** the start screen is shown
**Then** the screen displays exactly two dominant actions, Create Match and Join Match
**And** the copy uses simple, non-technical language.

**Given** the player is on the start screen
**When** they read the onboarding guidance
**Then** they can understand that both players need the same local network and the same shared marker
**And** the guidance remains brief and includes the core control rule, hold to accelerate and release in curves.

**Given** the start screen is rendered in landscape
**When** the player reviews the available actions
**Then** only one primary action is visually dominant at a time
**And** the layout remains readable without conflicting with safe-area constraints and provides touch targets of at least 48x48 dp for actionable controls on this screen.

### Story 1.3: Host Creates a Local Session

As a host player,
I want to create a local session and become the session owner,
So that I can invite another player into the race setup flow.

**Implements:** FR1, FR7
**Supports:** UX-DR2, UX-DR19

**Acceptance Criteria:**

**Given** the player selects Create Match
**When** local session creation succeeds
**Then** the system assigns the player the Host role and Player 1 identity
**And** the player is assigned the red car color.

**Given** the host session has been created
**When** the host enters the waiting state
**Then** the UI clearly shows that the session is active and waiting for a second player
**And** the next step is explained in non-technical language.

**Given** local session creation fails or the local network is unavailable
**When** the host attempts to create a match
**Then** the system shows a clear recovery message with retry and back options
**And** the failure state is distinguishable from normal waiting state.

### Story 1.4: Guest Joins an Existing Local Session

As a guest player,
I want to join the host's local session,
So that both players can prepare the shared race together.

**Implements:** FR2, FR7
**Supports:** NFR3, UX-DR2, UX-DR19

**Acceptance Criteria:**

**Given** a host session is available on the same local network
**When** the second player joins successfully
**Then** the system assigns the player the Guest role and Player 2 identity
**And** the player is assigned the green car color.

**Given** the guest joins successfully under normal local-network conditions
**When** the connection is established
**Then** both devices show that two players are connected in the same session
**And** the join flow completes within the MVP local-session time budget.

**Given** no valid session is found or the join attempt fails
**When** the guest tries to join
**Then** the system explains that both devices must be on the same local network
**And** the guest can retry or return to the start screen.

### Story 1.5: Enter the Shared Setup Lobby

As either player,
I want to see both players connected with clear next-step guidance,
So that we can move into AR setup with confidence.

**Implements:** FR19
**Supports:** UX-DR2, UX-DR13, UX-DR15, UX-DR19, UX-DR25

**Acceptance Criteria:**

**Given** both host and guest are connected
**When** the shared lobby is displayed
**Then** both players can see their roles, player identities, and color assignments
**And** the interface clearly indicates that the next step is to point both phones at the same marker.

**Given** the second player connects to the shared lobby
**When** both devices update their session state
**Then** a short-lived contextual confirmation is shown for the successful connection
**And** that confirmation does not block the next primary action.

**Given** the shared lobby shows session state
**When** ready, waiting, or disconnected conditions are displayed
**Then** player identity uses the red and green player colors while technical confirmation, warning, and critical failure states use consistent semantic status colors
**And** critical changes are communicated with at least text plus a second signal such as color or icon.

**Given** one player disconnects before setup handoff completes
**When** the lobby state updates
**Then** the remaining player sees a clear disconnected status
**And** the session returns to a recoverable waiting or retry state.

## Epic 2: Confirm a Trustworthy Shared Track

Two players can point at the same marker, see the same anchored track, confirm stability, and reach a synchronized countdown with confidence.

### Story 2.1: Detect the Shared Marker and Preview the Track

As either player,
I want the app to detect our shared marker and show the same track preview,
So that we can verify we are looking at the same race space.

**Implements:** FR3, FR4, FR8
**Supports:** NFR12, UX-DR3, UX-DR13, UX-DR16, UX-DR17, UX-DR18

**Acceptance Criteria:**

**Given** both players are in the AR setup flow
**When** the shared marker becomes visible
**Then** the predefined closed track appears anchored to the tabletop marker reference
**And** the preview is shown in landscape with readable status elements outside the protected central AR zone.

**Given** the marker has not been detected yet
**When** the players are scanning
**Then** the UI shows short, non-technical guidance about pointing both phones at the same marker
**And** the central AR viewing area retains at least 60% of screen width by 55% of screen height as protected play space.

**Given** the marker is first detected successfully
**When** the setup state advances to preview
**Then** a short-lived contextual confirmation is shown for the detection event
**And** the session remains out of the race-ready state until stability is confirmed.

### Story 2.2: Validate Track Stability Before Ready

As either player,
I want the system to confirm whether the track preview is stable enough to play,
So that we do not start from an untrustworthy setup.

**Implements:** FR5, FR20
**Supports:** NFR2, UX-DR4, UX-DR13, UX-DR19, UX-DR28

**Acceptance Criteria:**

**Given** the track preview is visible
**When** the setup system evaluates marker and surface stability
**Then** the session enters a Track Ready state only when the required stability conditions are satisfied
**And** the status uses readable stable, unstable, and recovery-needed states.

**Given** tracking is unstable or the playable surface is invalid
**When** the system evaluates readiness
**Then** the players see a clear unstable-state message with a rescan or recovery action
**And** secondary UI is reduced so recovery guidance is the priority.

**Given** the shared preview becomes valid after a previous unstable state
**When** the session transitions to ready
**Then** a short-lived track-ready confirmation is shown
**And** technical confirmation, warning, and critical failure states use consistent semantic status colors.

### Story 2.3: Confirm Both Players Are Ready on the Same Track

As either player,
I want to confirm that I see a valid shared track and know whether my rival is ready,
So that we start only when both of us trust the setup.

**Implements:** FR6, FR20
**Supports:** UX-DR5, UX-DR6, UX-DR25

**Acceptance Criteria:**

**Given** the track preview is in a ready state
**When** one player confirms readiness
**Then** the UI records that player's ready state locally and remotely
**And** the ready-sync strip shows which of the two players is ready.

**Given** only one player is ready
**When** the second player has not yet confirmed the shared track
**Then** the countdown cannot start
**And** both devices continue to show the missing confirmation state clearly.

**Given** readiness changes because a player cancels or setup validity is lost
**When** the ready state updates
**Then** both devices revert to a not-fully-ready setup state
**And** the readiness change is communicated with at least text plus a second visual signal.

### Story 2.4: Freeze the Shared Track and Start the Countdown

As both players,
we want the race to start from one frozen shared track snapshot,
So that the countdown begins only after a synchronized and trustworthy setup.

**Implements:** FR9, FR20
**Supports:** NFR2, UX-DR7, UX-DR20

**Acceptance Criteria:**

**Given** both players are ready on a stable shared preview
**When** the host confirms the final setup
**Then** the session creates a frozen shared track snapshot for the race
**And** the client applies that same snapshot before countdown begins.

**Given** the frozen snapshot is valid on both devices
**When** setup transitions to race start
**Then** a centered countdown begins on both devices in sync
**And** countdown numerals use display typography while supporting status and instruction text remains highly legible.

**Given** both players have required permissions and are on the same local network under normal tabletop conditions
**When** the session reaches countdown-ready state
**Then** total setup time from app open to race-ready remains within the MVP budget
**And** if the snapshot cannot be validated or applied consistently, countdown is canceled and recovery is shown instead.

## Epic 3: Race Head-to-Head with One-Button Skill

Two players can complete a fair local AR race with one-button acceleration, readable penalties, lap tracking, HUD feedback, and authoritative winner resolution.

### Story 3.1: Start a Quick Race on the Frozen Track

As a player,
I want the quick race to start with my car placed on a fixed lane of the frozen track,
So that the match begins consistently for both players.

**Implements:** FR10, FR24, FR25, FR26
**Supports:** NFR4

**Acceptance Criteria:**

**Given** the synchronized countdown has completed
**When** the race starts
**Then** each player sees one assigned car on the predefined lane for that player
**And** the race uses the frozen shared track state established during setup.

**Given** the active race session begins
**When** the host initializes the race state
**Then** the shared race session is created authoritatively by the host
**And** the client receives shared race state rather than raw AR world transform synchronization.

**Given** the MVP quick-race mode is active under normal conditions
**When** a race is completed successfully
**Then** the active race duration fits the intended short-session target
**And** race initialization failures return players to a safe pre-race state with a clear explanation.

### Story 3.2: Control My Car with Hold-to-Accelerate Input

As a player,
I want a large hold-to-accelerate control that matches my hand preference,
So that I can drive with minimal learning friction.

**Implements:** FR11, FR12
**Supports:** NFR9, UX-DR8, UX-DR9, UX-DR26, UX-DR27

**Acceptance Criteria:**

**Given** the race is active
**When** the player presses and holds the acceleration control
**Then** the system sends acceleration intent while the input remains held
**And** the control gives immediate visual pressed-state feedback.

**Given** the player releases the acceleration control
**When** the hold ends
**Then** acceleration intent stops immediately
**And** the player can use release timing to regain control before or during curves.

**Given** the player selects a preferred control side before the race starts
**When** the race HUD is displayed
**Then** the acceleration control appears on the chosen side of the screen with a touch target of at least 64x64 dp
**And** it remains inside safe touch zones and disabled during countdown and after race finish.

### Story 3.3: Resolve Race Progress Authoritatively

As both players,
we want the host to resolve movement, laps, and positions for both cars,
So that the race feels fair and yields one trusted winner.

**Implements:** FR16, FR17, FR24, FR25
**Supports:** NFR14

**Acceptance Criteria:**

**Given** both players are sending race input
**When** the race simulation advances
**Then** the host resolves authoritative progress, lap count, current position, and race state for both cars
**And** the client does not create its own conflicting authoritative result.

**Given** race state is updated by the host
**When** the client receives synchronized race data
**Then** the client updates presentation from the shared race state
**And** no raw AR world synchronization is required for race truth.

**Given** one player completes the configured lap count first
**When** the host evaluates the finish condition
**Then** the host declares the winner and closes the active race loop
**And** both devices enter the finish transition consistently.

### Story 3.4: Penalize Unsafe Curve Speed

As a player,
I want unsafe curve speed to trigger a clear and understandable penalty,
So that I learn the timing rule and trust the race outcome.

**Implements:** FR13, FR14, FR15
**Supports:** NFR14, UX-DR12, UX-DR22

**Acceptance Criteria:**

**Given** a car enters a curve above the safe speed threshold
**When** the host validates the curve state
**Then** the car receives a temporary speed-loss penalty
**And** a visible loss-of-control reaction is shown.

**Given** the penalty window has ended
**When** the car recovers from the penalty
**Then** the car returns automatically to its assigned lane
**And** the race continues without manual lane recovery input.

**Given** a player receives a curve penalty
**When** the feedback appears
**Then** the UI explains the cause in a short readable message
**And** the message teaches the timing rule without relying on a long tutorial.

### Story 3.5: Read the Race HUD at a Glance

As a player,
I want a minimal readable race HUD,
So that I can track the race without losing the AR track visually.

**Implements:** FR18
**Supports:** NFR10, NFR12, UX-DR10, UX-DR11, UX-DR16, UX-DR17, UX-DR18, UX-DR19, UX-DR27, UX-DR29

**Acceptance Criteria:**

**Given** the race is active
**When** the HUD is visible
**Then** it shows only essential information: lap, position, acceleration control, and short critical prompts
**And** it avoids clutter that would compete with the AR track.

**Given** the HUD is rendered over the camera feed
**When** it is displayed in gameplay
**Then** critical text uses high-contrast backplates and safe-area placement
**And** the layout preserves a clear center AR zone with status information in the upper band and primary action in the lower band.

**Given** a player checks the HUD from normal tabletop viewing distance
**When** they look for lap, position, or critical status
**Then** the information is readable at a glance
**And** player identity and technical states use consistent semantic colors without relying on color alone.

### Story 3.6: Keep the Race Smooth and Observable on Device

As a player,
I want the race to stay smooth and stable on the target Android phones,
So that the competition feels fair and reliable through a full session.

**Implements:** FR26
**Supports:** NFR4, NFR6, NFR7, NFR8, NFR13

**Acceptance Criteria:**

**Given** the MVP quick-race mode runs on the target reference device under normal tabletop conditions
**When** a full race session is played
**Then** the game sustains at least the MVP performance floor
**And** the experience remains playable without severe frame degradation.

**Given** a setup-race-replay flow is executed for approximately five minutes
**When** the session completes
**Then** the application does not crash
**And** thermal or stability degradation remains within acceptable MVP limits.

**Given** internal development validation is run against the MVP session flow
**When** telemetry and diagnostics are enabled in development builds
**Then** FPS, tracking loss, drift, RTT or jitter, and race invalidation events are recorded
**And** the target session-success threshold can be measured without exposing debug tooling in release builds.

## Epic 4: Recover, Finish, and Replay Confidently

Players can understand failures, recover when possible, see a clear result, and launch a fast rematch when the shared track remains valid.

### Story 4.1: Recover from Tracking Problems During Setup or Race

As a player,
I want the game to explain tracking problems and offer the shortest recovery path,
So that I can continue without unnecessary confusion or restart.

**Implements:** FR21
**Supports:** UX-DR14, UX-DR25, UX-DR28

**Acceptance Criteria:**

**Given** the marker is not detected, the shared track is unstable, or tracking is lost
**When** the system identifies the problem state
**Then** the player sees a recovery action sheet or equivalent interruption pattern with a concrete action such as rescan or repoint
**And** the message avoids technical jargon.

**Given** the session is degraded but still recoverable
**When** the system enters recovery mode
**Then** progression or gameplay is paused or blocked appropriately
**And** the UI simplifies to focus on recovery actions.

**Given** a critical tracking state is shown
**When** the player receives the warning
**Then** the state is communicated with at least text plus a second cue such as color, icon, or haptic feedback
**And** tracking recovery can return the session to the latest valid state without unnecessary full restart.

### Story 4.2: Recover from Network Loss or Host Failure

As a player,
I want network failures to be explained clearly,
So that I know whether to retry, wait, or return to lobby.

**Implements:** FR21
**Supports:** UX-DR14, UX-DR25

**Acceptance Criteria:**

**Given** the guest disconnects or the connection drops temporarily
**When** session validity changes
**Then** the remaining player sees a clear disconnected state with recovery options
**And** the system does not silently continue as if the session were still valid.

**Given** the host session fails or becomes non-recoverable
**When** the host authority is lost
**Then** both players are removed from the active race session safely
**And** the app returns them to the lobby rather than attempting unsupported host migration.

**Given** a player tries to continue with an invalid local network state
**When** the system cannot recover the session
**Then** the player sees retry and safe-exit options
**And** the failure state remains understandable without technical troubleshooting knowledge.

### Story 4.3: See a Clear Race Result and Decide What to Do Next

As a player,
I want an immediate and readable result screen,
So that I can understand who won and choose rematch or exit without delay.

**Implements:** FR22
**Supports:** NFR5, UX-DR19, UX-DR20, UX-DR23, UX-DR30

**Acceptance Criteria:**

**Given** the race has finished
**When** the result screen appears
**Then** the winner and loser states are understandable at a glance
**And** the primary call to action is rematch when the session is still valid.

**Given** player identities are shown on the result screen
**When** the outcome is displayed
**Then** player identity uses both text and player-color context
**And** the result is not communicated by color alone.

**Given** the result screen is active under normal conditions
**When** the players decide what to do next
**Then** the winner display uses display typography while action labels remain highly legible
**And** the result-to-decision flow fits the MVP goal of resolving within the post-race decision budget.

### Story 4.4: Launch a Fast Rematch When the Track Is Still Valid

As both players,
we want to restart quickly when the shared track is still trustworthy,
So that rematch feels faster than the initial setup.

**Implements:** FR23
**Supports:** UX-DR23, UX-DR24

**Acceptance Criteria:**

**Given** the result screen is active and the frozen shared track is still valid
**When** both players accept rematch
**Then** the system restarts the pre-race flow from the latest valid state
**And** the players do not repeat full calibration unnecessarily.

**Given** rematch is requested but the shared track or session is no longer valid
**When** the system evaluates the current state
**Then** the players are directed to the minimum required recovery step
**And** the failure does not silently force an invalid race restart.

**Given** rematch is available
**When** the players accept it
**Then** rematch remains the fastest path back into competition
**And** the system preserves player trust by clearly showing why it reused or rejected the current track state.