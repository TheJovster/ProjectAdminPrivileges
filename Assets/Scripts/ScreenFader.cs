using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance;

    [SerializeField] private Image fadeImage;
    [SerializeField] private float defaultFadeDuration = 0.5f;

    private Coroutine activeFade;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Start fully black
        SetAlpha(1f);

    }

    private void Start()
    {
        FadeIn();
    }

    /// <summary>
    /// Fades from black to clear (1 → 0). Call at game start or after loading.
    /// </summary>
    public void FadeIn(float duration = -1f)
    {
        if (duration < 0f) duration = defaultFadeDuration;

        if (GetAlpha() <= 0f) return;

        if (activeFade != null) StopCoroutine(activeFade);
        activeFade = StartCoroutine(FadeRoutine(GetAlpha(), 0f, duration));
    }

    /// <summary>
    /// Fades from clear to black (0 → 1). Call before scene transitions or between waves.
    /// </summary>
    public void FadeOut(float duration = -1f)
    {
        if (duration < 0f) duration = defaultFadeDuration;

        if (GetAlpha() >= 1f) return;

        if (activeFade != null) StopCoroutine(activeFade);
        activeFade = StartCoroutine(FadeRoutine(GetAlpha(), 1f, duration));
    }

    public bool IsFading => activeFade != null;

    private IEnumerator FadeRoutine(float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetAlpha(Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetAlpha(to);
        activeFade = null;
    }

    private void SetAlpha(float alpha)
    {
        if (fadeImage == null) return;
        Color c = fadeImage.color;
        c.a = alpha;
        fadeImage.color = c;
    }

    public float GetAlpha()
    {
        return fadeImage != null ? fadeImage.color.a : 0f;
    }

    private void OnDestroy()
    {
        if(Instance == this)
        {
            Instance = null;
        }
    }
}