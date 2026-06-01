# Face2Race SFX

`GameAudio` loads optional clips from this folder before using generated fallback tones.

Expected clip names:

- `ui_click`
- `marker_found`
- `ready`
- `error`
- `countdown_tick`
- `countdown_go`
- `race_start`
- `penalty`
- `finish`
- `engine_loop`

Use short mono WAV/OGG clips when possible. `engine_loop` should loop cleanly because the runtime changes its pitch and volume while the local player holds accelerate.
