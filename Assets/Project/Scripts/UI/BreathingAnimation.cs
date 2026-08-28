using UnityEngine;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// Gentle looping scale pulse to draw the eye to an interactive element - e.g. the HQ
    /// tap button, so it visibly invites tapping instead of sitting static. Reusable on any
    /// RectTransform; caches whatever scale it started at so it composes with existing setup.
    /// </summary>
    public class BreathingAnimation : MonoBehaviour
    {
        [Tooltip("How much bigger at the peak of the breath, e.g. 0.05 = 5% larger.")]
        public float scaleAmount = 0.05f;
        [Tooltip("Higher = faster breathing cycle.")]
        public float speed = 1.4f;

        private Vector3 _baseScale;

        private void OnEnable()
        {
            _baseScale = transform.localScale;
        }

        private void Update()
        {
            float t = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f; // 0-1
            transform.localScale = _baseScale * (1f + t * scaleAmount);
        }

        private void OnDisable()
        {
            transform.localScale = _baseScale;
        }
    }
}
