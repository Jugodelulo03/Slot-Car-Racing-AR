# AI Session Context — Slot Car Racing AR

> Este archivo sirve como handoff para la siguiente sesión de AI. Resume qué se ha hecho, qué decisiones se tomaron, y qué queda pendiente.

---

## Estado General del Proyecto

**Proyecto:** Slot Car Racing AR  
**Engine:** Unity 6.3 LTS  
**Plataforma:** Android (ARCore)  
**Stack:** URP 17.6, AR Foundation 6.5, ARCore XR Plugin 6.5, Netcode for GameObjects 2.11, Unity Transport 6.5  
**Arquitectura:** Host-authoritative local multiplayer (todavía sin networking implementado)

El proyecto NO sigue el orden estricto de épicas del sprint plan BMAD. Se construyó primero un **vertical slice single-player funcional en AR**: detección de marcador → anclaje → pista visible → carro corriendo sobre spline → input de aceleración táctil.

---

## Qué Está Implementado

### Escenas (Boot → Lobby → Race)
- **Boot:** Configura 60 FPS, landscape-only, carga Lobby
- **Lobby:** Pantalla con botones "Create Match" / "Join Match", pide permiso de cámara en Android, transiciona a Race (placeholder — no hay sesión real aún)
- **Race:** Composition root completo que:
  - Inicia AR subsystems en dispositivo (desactivados en editor)
  - Detecta marcador con ARTrackedImageManager
  - Crea ARAnchor acumulando muestras de pose
  - Monta la pista (3D model o waypoints) como hijo del anchor
  - Enlaza el carro al spline
  - Panel de tamaño de pista ajustable
  - Overlay de debug AR (UGUI)

### Sistema de Pista (Track)
- **OvalTrackDefinition:** Spline cerrado Catmull-Rom con múltiples constructores:
  - Desde design points hardcodeados (fallback)
  - Desde RacingLineData asset (waypoints exportados del editor)
  - Desde rawWaypoints (TrackSceneSetup)
- **TrackVisualBuilder:** Renderiza la pista con LineRenderer (superficie negra, bordes blancos, curbs rojo/blanco)
- **TrackSceneSetup:** Waypoints colocados directamente en la escena sobre el modelo 3D
- **TrackModelLoader:** Carga GLB/FBX bajo el anchor

### Sistema de Curvas (ACTUALIZADO EN ESTA SESIÓN)
- **Antes:** Detección automática de curvas por ángulo de curvatura local (tenía bugs — no penalizaba correctamente, umbrales inconsistentes)
- **Ahora:** Sistema dual:
  - Si `RacingLineData.HasManualCurveData` → usa dificultades manuales marcadas por el usuario en el editor
  - Si no hay datos manuales → fallback a detección automática por curvatura
- **CurveDifficulty:** enum con 5 niveles: Straight, Gentle, Medium, Sharp, Hairpin
- Cada waypoint del RacingLineData ahora tiene un campo `WaypointDifficulties[]` que se exporta desde el Waypoint Placer

### Editor Tools
- **TrackWaypointPlacer** (Window → Slot Car Racing → Waypoint Placer):
  - Click en el track model para colocar waypoints
  - **NUEVO:** Cada waypoint tiene un dropdown de CurveDifficulty (coloreado)
  - **NUEVO:** "Brush" de dificultad — el siguiente punto se crea con la dificultad seleccionada
  - **NUEVO:** "Paint Selected" y "Paint All" para asignar dificultad en batch
  - **NUEVO:** Gizmos de escena coloreados por dificultad (amarillo=recto, verde=gentle, naranja=sharp, rojo=hairpin)
  - Export guarda posiciones + dificultades al ScriptableObject
- **TrackedImageSetupUtility:** Helper para librería de marcadores
- **TrackSceneSetupEditor:** Inspector custom para TrackSceneSetup

### Carro y Input
- **CarPlaceholder:** Simulación completa sobre spline:
  - Aceleración/frenado suaves
  - Sistema de penalización por curvas graduado: soft drag → instability (vibración) → spin-out (2 rotaciones completas + parada)
  - Usa `GetDifficultyAtProgress()` que ahora puede venir de datos manuales
- **AccelerationInputPlaceholder:** Botón táctil UGUI (IPointerDown/Up/Exit)

### AR Runtime
- MarkerDetectionEntryPoint con sampling de poses, creación de ARAnchor, re-anchor
- DevelopmentMarkerLibrary con markers normal + invertido
- ArSurfaceProbe (ARPlaneManager + ARRaycastManager)
- ArDebugOverlay (overlay UGUI para diagnósticos en dispositivo)
- TrackedPoseDriver (InputSystem.XR) en cámara AR

### Datos
- `Data/MarkerProfiles/` — Texturas de marcadores (DevelopmentMarker, TrackMarkerA-D, TrackAnchorMarker)
- `Data/Config/` — Vacío (configs futuras)
- `Data/Tracks/` — Vacío (track definitions futuras)
- `Assets/RacingLine.asset` y `RacingLineNEW.asset` — ScriptableObjects con waypoints exportados

---

## Qué NO Está Implementado

| Feature | Épica | Notas |
|---------|-------|-------|
| Networking (NGO) — sesión real host/client | 1-3, 1-4, 1-5 | Lobby transiciona directo sin sesión |
| TrackFreeze / Race Space Snapshot | 2-4 | El anchor es local, no compartido |
| Spatial Trust state machine | 2-3, 4-1 | No hay validación de confianza espacial |
| Countdown / Start / Finish flow | 3-1 | El carro corre inmediatamente |
| Lap counter HUD | 3-5 | Solo hay debug overlay |
| Penalización visual (HUD feedback) | 3-4 | El carro vibra/spins pero no hay UI |
| Recovery flows | 4-1, 4-2 | No hay manejo de pérdida de tracking/red |
| Pantalla de resultados | 4-3 | No existe |
| Rematch | 4-4 | No existe |

---

## Decisiones Arquitectónicas Clave

1. **Estructura híbrida** bajo `Assets/SlotCarRacingAR/` con separación Core/Runtime/Editor
2. **Composition Roots** por escena (BootCompositionRoot, LobbyCompositionRoot, RaceCompositionRoot)
3. **AR components deshabilitados por defecto** en escena — se activan desde el bootstrap en runtime para evitar ruido en editor
4. **Marcador físico compartido** como referencia espacial (no se sincroniza mundo AR por red)
5. **Spline Catmull-Rom** para el circuito — no Bézier, no physics
6. **Curvas marcadas manualmente** por el usuario en el editor (nuevo — resuelve bugs de detección automática)

---

## Notas Operativas (Verificadas en Device)

- Los `.cs.meta` faltantes impiden que Unity registre scripts nuevos
- Cambios en Play Mode NO persisten para builds Android
- Para rutas de menú editor nuevas: Assets/Refresh + Open C# Project
- AR Foundation 6 requiere TrackedPoseDriver (InputSystem.XR), no el legacy ARPoseDriver
- Para diagnóstico AR en device: overlay UGUI > OnGUI/IMGUI
- Si ARSession queda en Ready con 0 camera frames: reiniciar subsystems después de XR loader startup
- Forzar OpenGLES3-only si Vulkan causa problemas con ARCore
- Mantener markers normal + invertido en la library para desarrollo

---

## Archivos Clave para Empezar

| Archivo | Propósito |
|---------|-----------|
| `Scripts/Runtime/App/RaceCompositionRoot.cs` | Wiring principal de la escena Race |
| `Scripts/Runtime/Infrastructure/MarkerDetectionEntryPoint.cs` | Detección AR, anchor, track binding |
| `Scripts/Runtime/Features/OvalTrackDefinition.cs` | Definición del circuito (spline + curvas) |
| `Scripts/Runtime/Features/CarPlaceholder.cs` | Simulación del carro |
| `Scripts/Runtime/Features/RacingLineData.cs` | ScriptableObject con waypoints + dificultades |
| `Scripts/Editor/TrackWaypointPlacer.cs` | Editor tool para marcar waypoints + curvas |
| `Scripts/Runtime/UI/AccelerationInputPlaceholder.cs` | Input de aceleración |
| `Scripts/Runtime/Debug/ArDebugOverlay.cs` | Overlay de diagnóstico |
| `_bmad-output/project-context.md` | Reglas del proyecto para AI agents |
| `_bmad-output/planning-artifacts/game-architecture.md` | Arquitectura completa |
| `_bmad-output/planning-artifacts/gdd-slot-car-racing-ar.md` | Game Design Document |

---

## Próximos Pasos Sugeridos

1. **Re-exportar RacingLineData** con curvas marcadas manualmente usando el Waypoint Placer actualizado
2. **Validar penalización** en curvas con los datos manuales (probar en editor y en device)
3. **Implementar countdown/start/laps/finish** para completar el loop single-player
4. **Agregar HUD de carrera** (velocímetro, vuelta actual, posición en curva)
5. **Networking** cuando el loop single-player esté pulido

---

*Última actualización: 2026-05-18*
