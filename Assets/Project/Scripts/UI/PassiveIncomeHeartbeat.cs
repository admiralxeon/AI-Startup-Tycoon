using System.Collections;
using UnityEngine;
using TMPro;
using AIStartupTycoon.Core;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// Gives the cash readout a slow "heartbeat" pulse while passive income is
    /// actually accruing, so idle income reads as something happening rather than
    /// a number that silently changes. Deliberately NOT driven by
    /// CurrencyManager.OnRevenueChanged, since that fires every frame during
    /// passive accrual (GameManager.Update calls EarnFromPassive per-frame) -
    /// reacting to it directly would just be a constant jitter, not a readable beat.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class PassiveIncomeHeartbeat : MonoBehaviour
    {
        public float pulseInterval = 1.0f;
        public float punchScale = 1.10f;
        public float punchDuration = 0.25f;
        public Color flashColor = new Color(0.75f, 1f, 0.85f, 1f);

        private RectTransform _rt;
        private TMP_Text _text;
        private Vector3 _baseScale;
        private Color _baseColor;
        private float _timer;

        private void Awake()
        {
            _rt = (RectTransform)transform;
            _text = GetComponent<TMP_Text>();
            _baseScale = _rt.localScale;
            if (_text != null) _baseColor = _text.color;
        }

        private void Update()
        {
            if (GameManager.Instance == null) return;

            _timer += Time.deltaTime;
            if (_timer < pulseInterval) return;
            _timer = 0f;

            if (GameManager.Instance.GetTotalPassiveOutput().ToDouble() <= 0.0) return;

            StopAllCoroutines();
            StartCoroutine(Pulse());
        }

        private IEnumerator Pulse()
        {
            float t = 0f;
            while (t < punchDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / punchDuration);
                float bump = Mathf.Sin(p * Mathf.PI); // 0 -> 1 -> 0
                _rt.localScale = _baseScale * (1f + (punchScale - 1f) * bump);
                if (_text != null) _text.color = Color.Lerp(_baseColor, flashColor, bump);
                yield return null;
            }
            _rt.localScale = _baseScale;
            if (_text != null) _text.color = _baseColor;
        }
    }
}
