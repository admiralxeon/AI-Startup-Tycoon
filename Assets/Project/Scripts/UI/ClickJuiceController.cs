using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AIStartupTycoon.Core;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// Tactile click feedback layered on top of ClickAndRevenueController's existing
    /// click handling: scale punch, particle burst, floating "+$" popup, subtle shake.
    /// Driven by CurrencyManager.OnClickEarned (not the button's onClick) so the popup
    /// always shows the real earned amount, combo multiplier included, instead of
    /// re-deriving it and risking drift from ClickComboManager's live state. Escalates
    /// burst/punch intensity with the current combo so sustained tapping visibly ramps up.
    /// </summary>
    public class ClickJuiceController : MonoBehaviour
    {
        [Header("Refs")]
        public RectTransform punchTarget;
        public Image burstDotPrefab;
        public TextMeshProUGUI popupTextPrefab;
        public RectTransform popupSpawnPoint;
        public RectTransform shakeTarget;

        [Header("Tuning")]
        public float punchScale = 1.15f;
        public float punchDuration = 0.12f;
        public float popupRiseDistance = 50f;
        public float popupDuration = 0.6f;
        public float shakeMagnitude = 3f;
        public float shakeDuration = 0.08f;
        public int burstDotCount = 10;
        public float burstDistance = 70f;
        public float burstDuration = 0.4f;

        [Header("Combo Escalation")]
        [Tooltip("Extra punch scale added per combo step, on top of the base punchScale.")]
        public float punchScalePerCombo = 0.01f;
        [Tooltip("Extra burst dots added per combo step.")]
        public float burstDotsPerCombo = 0.3f;
        [Tooltip("Combo step count at which escalation caps out, independent of ClickComboManager's own cap.")]
        public int maxEscalationSteps = 25;

        private Vector3 _originalScale;
        private Vector2 _shakeOriginalPos;
        private Coroutine _punchRoutine;
        private Coroutine _shakeRoutine;

        private void Start()
        {
            if (punchTarget != null) _originalScale = punchTarget.localScale;
            if (shakeTarget != null) _shakeOriginalPos = shakeTarget.anchoredPosition;
            if (CurrencyManager.Instance != null) CurrencyManager.Instance.OnClickEarned += OnClickEarned;
        }

        private void OnDestroy()
        {
            if (CurrencyManager.Instance != null) CurrencyManager.Instance.OnClickEarned -= OnClickEarned;
        }

        private void OnClickEarned(Utils.BigNumber earned)
        {
            int combo = ClickComboManager.Instance != null
                ? Mathf.Min(ClickComboManager.Instance.ComboCount, maxEscalationSteps)
                : 0;

            if (punchTarget != null)
            {
                if (_punchRoutine != null) StopCoroutine(_punchRoutine);
                _punchRoutine = StartCoroutine(PunchScale(punchScale + combo * punchScalePerCombo));
            }
            if (burstDotPrefab != null) SpawnBurst(burstDotCount + Mathf.RoundToInt(combo * burstDotsPerCombo));
            if (popupTextPrefab != null) SpawnPopup(earned.ToDouble());
            if (shakeTarget != null)
            {
                if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
                _shakeRoutine = StartCoroutine(Shake());
            }
        }

        private IEnumerator PunchScale(float targetPunchScale)
        {
            float t = 0f;
            while (t < punchDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / punchDuration);
                float scaleFactor = Mathf.Lerp(targetPunchScale, 1f, EaseOutBack(p));
                punchTarget.localScale = _originalScale * scaleFactor;
                yield return null;
            }
            punchTarget.localScale = _originalScale;
        }

        private static float EaseOutBack(float t)
        {
            return 1f - Mathf.Pow(1f - t, 3f);
        }

        private void SpawnBurst(int dotCount)
        {
            Transform parent = popupSpawnPoint != null ? (Transform)popupSpawnPoint : transform;
            for (int i = 0; i < dotCount; i++)
            {
                Image dot = Instantiate(burstDotPrefab, parent);
                dot.gameObject.SetActive(true);
                float angle = (360f / dotCount) * i + Random.Range(-12f, 12f);
                float dist = burstDistance * Random.Range(0.7f, 1.15f);
                Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                StartCoroutine(AnimateBurstDot(dot, dir * dist));
            }
        }

        private IEnumerator AnimateBurstDot(Image dot, Vector2 targetOffset)
        {
            RectTransform rt = dot.rectTransform;
            Vector2 start = rt.anchoredPosition;
            Vector3 startScale = rt.localScale;
            Color startColor = dot.color;
            float t = 0f;
            while (t < burstDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / burstDuration);
                float eased = 1f - Mathf.Pow(1f - p, 2f);
                rt.anchoredPosition = start + targetOffset * eased;
                rt.localScale = startScale * Mathf.Lerp(1f, 0.2f, p);
                Color c = startColor;
                c.a = Mathf.Lerp(1f, 0f, p);
                dot.color = c;
                yield return null;
            }
            if (dot != null) Destroy(dot.gameObject);
        }

        private void SpawnPopup(double amount)
        {
            Transform parent = popupSpawnPoint != null ? (Transform)popupSpawnPoint : transform;
            TextMeshProUGUI popup = Instantiate(popupTextPrefab, parent);
            popup.text = "+$" + FormatShort(amount);
            popup.gameObject.SetActive(true);
            StartCoroutine(AnimatePopup(popup));
        }

        private IEnumerator AnimatePopup(TextMeshProUGUI popup)
        {
            RectTransform rt = popup.rectTransform;
            Vector2 start = rt.anchoredPosition;
            Color startColor = popup.color;
            float t = 0f;
            while (t < popupDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / popupDuration);
                rt.anchoredPosition = start + Vector2.up * (popupRiseDistance * p);
                Color c = startColor;
                c.a = Mathf.Lerp(1f, 0f, p);
                popup.color = c;
                yield return null;
            }
            if (popup != null) Destroy(popup.gameObject);
        }

        private IEnumerator Shake()
        {
            float t = 0f;
            while (t < shakeDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = t / shakeDuration;
                Vector2 offset = Random.insideUnitCircle * shakeMagnitude * (1f - p);
                shakeTarget.anchoredPosition = _shakeOriginalPos + offset;
                yield return null;
            }
            shakeTarget.anchoredPosition = _shakeOriginalPos;
        }

        private string FormatShort(double amount)
        {
            if (amount >= 1000000.0) return (amount / 1000000.0).ToString("0.##") + "M";
            if (amount >= 1000.0) return (amount / 1000.0).ToString("0.##") + "K";
            return amount.ToString("0.#");
        }
    }
}
