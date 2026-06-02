using System.Collections;
using UnityEngine;

namespace SlotCarRacingAR.Runtime.UI
{
    public sealed class RetroUiAnimator : MonoBehaviour
    {
        private Coroutine _scaleRoutine;
        private Coroutine _slideRoutine;
        private Coroutine _fadeRoutine;
        private Vector2 _slideRestPosition;
        private bool _hasSlideRestPosition;

        public static RetroUiAnimator Attach(GameObject target)
        {
            if (target == null)
            {
                return null;
            }

            RetroUiAnimator animator = target.GetComponent<RetroUiAnimator>();
            if (animator == null)
            {
                animator = target.AddComponent<RetroUiAnimator>();
            }

            return animator;
        }

        public void PlayPop(float overshoot = 1.08f, float duration = 0.22f)
        {
            RectTransform rect = transform as RectTransform;
            if (rect == null)
            {
                return;
            }

            if (_scaleRoutine != null)
            {
                StopCoroutine(_scaleRoutine);
            }

            _scaleRoutine = StartCoroutine(PopRoutine(rect, overshoot, duration));
        }

        public void PlayPressBounce()
        {
            PlayPop(1.035f, 0.16f);
        }

        public void PlaySlideIn(Vector2 fromOffset, float duration = 0.25f)
        {
            RectTransform rect = transform as RectTransform;
            if (rect == null)
            {
                return;
            }

            if (!_hasSlideRestPosition || _slideRoutine == null)
            {
                _slideRestPosition = rect.anchoredPosition;
                _hasSlideRestPosition = true;
            }

            if (_slideRoutine != null)
            {
                StopCoroutine(_slideRoutine);
            }

            _slideRoutine = StartCoroutine(SlideInRoutine(rect, _slideRestPosition, fromOffset, duration));
        }

        public void PlayFadeIn(float duration = 0.18f)
        {
            CanvasGroup group = GetOrCreateCanvasGroup(gameObject);
            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
            }

            _fadeRoutine = StartCoroutine(FadeRoutine(group, 0f, 1f, duration));
        }

        private static IEnumerator PopRoutine(RectTransform rect, float overshoot, float duration)
        {
            rect.localScale = Vector3.one * 0.86f;
            float half = Mathf.Max(0.01f, duration * 0.55f);
            float elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = EaseOutBack(Mathf.Clamp01(elapsed / half));
                rect.localScale = Vector3.LerpUnclamped(Vector3.one * 0.86f, Vector3.one * overshoot, t);
                yield return null;
            }

            elapsed = 0f;
            float settle = Mathf.Max(0.01f, duration - half);
            Vector3 start = rect.localScale;
            while (elapsed < settle)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = EaseOut(Mathf.Clamp01(elapsed / settle));
                rect.localScale = Vector3.LerpUnclamped(start, Vector3.one, t);
                yield return null;
            }

            rect.localScale = Vector3.one;
        }

        private IEnumerator SlideInRoutine(RectTransform rect, Vector2 rest, Vector2 fromOffset, float duration)
        {
            rect.anchoredPosition = rest + fromOffset;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = EaseOutBack(Mathf.Clamp01(elapsed / duration));
                rect.anchoredPosition = Vector2.LerpUnclamped(rest + fromOffset, rest, t);
                yield return null;
            }

            rect.anchoredPosition = rest;
            _slideRoutine = null;
        }

        private static IEnumerator FadeRoutine(CanvasGroup group, float from, float to, float duration)
        {
            group.alpha = from;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = EaseOut(Mathf.Clamp01(elapsed / duration));
                group.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }

            group.alpha = to;
        }

        private static CanvasGroup GetOrCreateCanvasGroup(GameObject target)
        {
            CanvasGroup group = target.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = target.AddComponent<CanvasGroup>();
            }

            return group;
        }

        private static float EaseOut(float t)
        {
            return 1f - Mathf.Pow(1f - t, 3f);
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
