using System.Collections.Generic;
using UnityEngine;

namespace SlotCarRacingAR.Runtime.Infrastructure
{
    public enum GameSfx
    {
        UiClick,
        MarkerFound,
        Ready,
        Error,
        CountdownTick,
        CountdownGo,
        RaceStart,
        Penalty,
        Finish
    }

    public enum GameMusic
    {
        Menu,
        Race
    }

    /// <summary>
    /// Central SFX player. It loads optional clips from Resources/SFX and falls back to generated arcade tones.
    /// </summary>
    public sealed class GameAudio : MonoBehaviour
    {
        private const int SampleRate = 44100;
        private const float DefaultVolume = 0.55f;

        private static GameAudio _instance;

        private readonly Dictionary<GameSfx, AudioClip> _clips = new();
        private readonly Dictionary<GameMusic, AudioClip> _musicClips = new();
        private AudioSource _source;
        private AudioSource _musicSource;
        private AudioSource _engineSource;
        private AudioClip _engineClip;
        private GameMusic? _currentMusic;
        private bool _engineAccelerating;
        private float _engineThrottle;
        private float _lastClickTime;

        public static void Play(GameSfx sfx)
        {
            Instance.PlayInternal(sfx);
        }

        public static void PlayCountdown(byte value)
        {
            Play(value == 0 ? GameSfx.CountdownGo : GameSfx.CountdownTick);
        }

        public static void PlayMusic(GameMusic music)
        {
            Instance.PlayMusicInternal(music);
        }

        public static void StopMusic()
        {
            Instance.StopMusicInternal();
        }

        public static void SetLocalEngineAccelerating(bool accelerating)
        {
            Instance.SetLocalEngineAcceleratingInternal(accelerating);
        }

        public static void StopLocalEngine()
        {
            Instance.SetLocalEngineAcceleratingInternal(false);
        }

        private static GameAudio Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }

                GameObject obj = new GameObject("GameAudio");
                _instance = obj.AddComponent<GameAudio>();
                DontDestroyOnLoad(obj);
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureSource();
            EnsureMusicSource();
            EnsureEngineSource();
        }

        private void Update()
        {
            UpdateEngineLoop();
        }

        private void PlayInternal(GameSfx sfx)
        {
            EnsureSource();

            if (sfx == GameSfx.UiClick && Time.unscaledTime - _lastClickTime < 0.06f)
            {
                return;
            }

            if (sfx == GameSfx.UiClick)
            {
                _lastClickTime = Time.unscaledTime;
            }

            AudioClip clip = GetClip(sfx);
            if (clip != null)
            {
                _source.PlayOneShot(clip, DefaultVolume);
            }
        }

        private void EnsureSource()
        {
            if (_source == null)
            {
                _source = GetComponent<AudioSource>();
            }

            if (_source == null)
            {
                _source = gameObject.AddComponent<AudioSource>();
            }

            _source.playOnAwake = false;
            _source.loop = false;
            _source.spatialBlend = 0f;
            _source.ignoreListenerPause = true;
        }

        private void EnsureMusicSource()
        {
            if (_musicSource == null)
            {
                GameObject musicObj = new GameObject("MusicSource");
                musicObj.transform.SetParent(transform, false);
                _musicSource = musicObj.AddComponent<AudioSource>();
            }

            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
            _musicSource.spatialBlend = 0f;
            _musicSource.ignoreListenerPause = true;
            _musicSource.volume = 0.32f;
        }

        private void EnsureEngineSource()
        {
            if (_engineSource == null)
            {
                GameObject engineObj = new GameObject("LocalEngineSource");
                engineObj.transform.SetParent(transform, false);
                _engineSource = engineObj.AddComponent<AudioSource>();
            }

            _engineSource.playOnAwake = false;
            _engineSource.loop = true;
            _engineSource.spatialBlend = 0f;
            _engineSource.ignoreListenerPause = true;
            _engineSource.volume = 0f;
            _engineSource.pitch = 0.75f;
        }

        private void PlayMusicInternal(GameMusic music)
        {
            EnsureMusicSource();

            if (_currentMusic.HasValue && _currentMusic.Value == music && _musicSource.isPlaying)
            {
                return;
            }

            AudioClip clip = GetMusicClip(music);
            if (clip == null)
            {
                return;
            }

            _currentMusic = music;
            _musicSource.clip = clip;
            _musicSource.Play();
        }

        private void StopMusicInternal()
        {
            if (_musicSource != null)
            {
                _musicSource.Stop();
            }

            _currentMusic = null;
        }

        private void SetLocalEngineAcceleratingInternal(bool accelerating)
        {
            EnsureEngineSource();
            _engineAccelerating = accelerating;

            if (accelerating && !_engineSource.isPlaying)
            {
                _engineClip ??= Resources.Load<AudioClip>("SFX/engine_loop") ?? CreateEngineLoop();
                _engineSource.clip = _engineClip;
                _engineSource.volume = Mathf.Max(_engineSource.volume, 0.04f);
                _engineSource.Play();
            }
        }

        private void UpdateEngineLoop()
        {
            if (_engineSource == null)
            {
                return;
            }

            float targetThrottle = _engineAccelerating ? 1f : 0f;
            float rate = _engineAccelerating ? 1.8f : 1.15f;
            _engineThrottle = Mathf.MoveTowards(_engineThrottle, targetThrottle, rate * Time.unscaledDeltaTime);

            if (!_engineAccelerating && _engineThrottle <= 0.01f)
            {
                _engineThrottle = 0f;
                _engineSource.volume = 0f;
                if (_engineSource.isPlaying)
                {
                    _engineSource.Stop();
                }

                return;
            }

            if (_engineThrottle > 0f && !_engineSource.isPlaying)
            {
                _engineClip ??= Resources.Load<AudioClip>("SFX/engine_loop") ?? CreateEngineLoop();
                _engineSource.clip = _engineClip;
                _engineSource.Play();
            }

            float shapedThrottle = Mathf.SmoothStep(0f, 1f, _engineThrottle);
            _engineSource.volume = Mathf.Lerp(0.03f, 0.52f, shapedThrottle);
            _engineSource.pitch = Mathf.Lerp(0.72f, 1.55f, shapedThrottle);
        }

        private AudioClip GetClip(GameSfx sfx)
        {
            if (_clips.TryGetValue(sfx, out AudioClip cachedClip))
            {
                return cachedClip;
            }

            AudioClip loadedClip = Resources.Load<AudioClip>("SFX/" + GetResourceName(sfx));
            AudioClip clip = loadedClip != null ? loadedClip : CreateFallbackClip(sfx);
            _clips[sfx] = clip;
            return clip;
        }

        private AudioClip GetMusicClip(GameMusic music)
        {
            if (_musicClips.TryGetValue(music, out AudioClip cachedClip))
            {
                return cachedClip;
            }

            AudioClip loadedClip = Resources.Load<AudioClip>("Music/" + GetMusicResourceName(music));
            AudioClip clip = loadedClip != null ? loadedClip : CreateFallbackMusic(music);
            _musicClips[music] = clip;
            return clip;
        }

        private static string GetResourceName(GameSfx sfx)
        {
            switch (sfx)
            {
                case GameSfx.UiClick:
                    return "ui_click";
                case GameSfx.MarkerFound:
                    return "marker_found";
                case GameSfx.Ready:
                    return "ready";
                case GameSfx.Error:
                    return "error";
                case GameSfx.CountdownTick:
                    return "countdown_tick";
                case GameSfx.CountdownGo:
                    return "countdown_go";
                case GameSfx.RaceStart:
                    return "race_start";
                case GameSfx.Penalty:
                    return "penalty";
                case GameSfx.Finish:
                    return "finish";
                default:
                    return sfx.ToString().ToLowerInvariant();
            }
        }

        private static string GetMusicResourceName(GameMusic music)
        {
            switch (music)
            {
                case GameMusic.Menu:
                    return "menu_loop";
                case GameMusic.Race:
                    return "race_loop";
                default:
                    return music.ToString().ToLowerInvariant();
            }
        }

        private static AudioClip CreateFallbackClip(GameSfx sfx)
        {
            switch (sfx)
            {
                case GameSfx.UiClick:
                    return CreateTone("fallback_ui_click", new[] { 660f }, new[] { 0.045f }, 0.38f);
                case GameSfx.MarkerFound:
                    return CreateTone("fallback_marker_found", new[] { 523f, 784f }, new[] { 0.08f, 0.12f }, 0.45f);
                case GameSfx.Ready:
                    return CreateTone("fallback_ready", new[] { 392f, 659f, 880f }, new[] { 0.06f, 0.06f, 0.10f }, 0.45f);
                case GameSfx.Error:
                    return CreateTone("fallback_error", new[] { 220f, 165f }, new[] { 0.09f, 0.16f }, 0.50f);
                case GameSfx.CountdownTick:
                    return CreateTone("fallback_countdown_tick", new[] { 440f }, new[] { 0.12f }, 0.48f);
                case GameSfx.CountdownGo:
                    return CreateTone("fallback_countdown_go", new[] { 659f, 988f }, new[] { 0.12f, 0.20f }, 0.52f);
                case GameSfx.RaceStart:
                    return CreateTone("fallback_race_start", new[] { 784f, 1046f }, new[] { 0.08f, 0.18f }, 0.48f);
                case GameSfx.Penalty:
                    return CreateTone("fallback_penalty", new[] { 196f, 147f, 110f }, new[] { 0.08f, 0.08f, 0.12f }, 0.46f);
                case GameSfx.Finish:
                    return CreateTone("fallback_finish", new[] { 523f, 659f, 784f, 1046f }, new[] { 0.08f, 0.08f, 0.08f, 0.22f }, 0.50f);
                default:
                    return CreateTone("fallback_default", new[] { 440f }, new[] { 0.10f }, 0.42f);
            }
        }

        private static AudioClip CreateFallbackMusic(GameMusic music)
        {
            switch (music)
            {
                case GameMusic.Race:
                    return CreateLoop("fallback_race_loop", new[] { 196f, 247f, 294f, 392f, 330f, 294f, 247f, 220f }, 0.18f, 0.16f);
                case GameMusic.Menu:
                default:
                    return CreateLoop("fallback_menu_loop", new[] { 147f, 196f, 220f, 196f, 165f, 196f, 247f, 220f }, 0.22f, 0.12f);
            }
        }

        private static AudioClip CreateTone(string name, float[] frequencies, float[] durations, float volume)
        {
            int totalSamples = 0;
            for (int i = 0; i < durations.Length; i++)
            {
                totalSamples += Mathf.Max(1, Mathf.CeilToInt(durations[i] * SampleRate));
            }

            float[] data = new float[totalSamples];
            int cursor = 0;
            for (int note = 0; note < frequencies.Length && note < durations.Length; note++)
            {
                int samples = Mathf.Max(1, Mathf.CeilToInt(durations[note] * SampleRate));
                float frequency = frequencies[note];
                int fadeSamples = Mathf.Min(samples / 3, Mathf.CeilToInt(0.012f * SampleRate));

                for (int i = 0; i < samples && cursor + i < data.Length; i++)
                {
                    float t = i / (float)SampleRate;
                    float envelope = 1f;
                    if (fadeSamples > 0)
                    {
                        envelope = Mathf.Min(i / (float)fadeSamples, (samples - i - 1) / (float)fadeSamples);
                        envelope = Mathf.Clamp01(envelope);
                    }

                    data[cursor + i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * volume * envelope;
                }

                cursor += samples;
            }

            AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateLoop(string name, float[] notes, float noteDuration, float volume)
        {
            int samplesPerNote = Mathf.Max(1, Mathf.CeilToInt(noteDuration * SampleRate));
            float[] data = new float[samplesPerNote * notes.Length];

            for (int noteIndex = 0; noteIndex < notes.Length; noteIndex++)
            {
                float frequency = notes[noteIndex];
                int start = noteIndex * samplesPerNote;
                for (int i = 0; i < samplesPerNote; i++)
                {
                    float t = i / (float)SampleRate;
                    float envelope = Mathf.Sin(Mathf.PI * i / samplesPerNote);
                    float main = Mathf.Sin(2f * Mathf.PI * frequency * t);
                    float overtone = Mathf.Sin(2f * Mathf.PI * frequency * 2f * t) * 0.22f;
                    data[start + i] = (main + overtone) * volume * envelope;
                }
            }

            AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateEngineLoop()
        {
            int sampleCount = Mathf.CeilToInt(0.5f * SampleRate);
            float[] data = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)SampleRate;
                float low = Mathf.Sin(2f * Mathf.PI * 80f * t) * 0.58f;
                float mid = Mathf.Sin(2f * Mathf.PI * 160f * t) * 0.24f;
                float high = Mathf.Sin(2f * Mathf.PI * 320f * t) * 0.10f;
                float pulse = 0.72f + Mathf.Sin(2f * Mathf.PI * 16f * t) * 0.18f;
                data[i] = Mathf.Clamp((low + mid + high) * pulse * 0.55f, -0.9f, 0.9f);
            }

            AudioClip clip = AudioClip.Create("fallback_engine_loop", sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
