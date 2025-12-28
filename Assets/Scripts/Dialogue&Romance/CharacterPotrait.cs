using UnityEngine;
using UnityEngine.UI;

namespace ProjectAdminPrivileges.Dialogue
{
    /// <summary>
    /// Handles portrait scaling and overlay transparency animation.
    /// Attach to MCPortrait and QueenPortrait GameObjects.
    /// </summary>
    public class CharacterPortrait : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image portraitImage;
        [SerializeField] private Image overlayImage;

        [Header("Visual Settings")]
        [SerializeField] private float speakingScale = 1.1f;
        [SerializeField] private float idleScale = 1.0f;
        [SerializeField] private float speakingOverlayAlpha = 0f;
        [SerializeField] private float idleOverlayAlpha = 0.4f;
        [SerializeField] private float transitionSpeed = 5f;

        private float targetScale;
        private float targetAlpha;

        private void Awake()
        {
            if (portraitImage == null)
            {
                portraitImage = GetComponent<Image>();
            }

            SetIdle(); // Start in idle state
        }

        private void Update()
        {
            // Smooth lerp to target scale
            float currentScale = portraitImage.transform.localScale.x;
            float newScale = Mathf.Lerp(currentScale, targetScale, transitionSpeed * Time.unscaledDeltaTime);
            portraitImage.transform.localScale = Vector3.one * newScale;

            // Smooth lerp to target overlay alpha
            if (overlayImage != null)
            {
                Color overlayColor = overlayImage.color;
                overlayColor.a = Mathf.Lerp(overlayColor.a, targetAlpha, transitionSpeed * Time.unscaledDeltaTime);
                overlayImage.color = overlayColor;
            }
        }

        public void SetSpeaking()
        {
            targetScale = speakingScale;
            targetAlpha = speakingOverlayAlpha;
        }

        public void SetIdle()
        {
            targetScale = idleScale;
            targetAlpha = idleOverlayAlpha;
        }
    }
}