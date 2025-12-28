using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;
using ProjectAdminPrivileges.ShopSystem;


public class GameOverUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private TextMeshProUGUI killsText;
    [SerializeField] private TextMeshProUGUI wavesText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI iaxpEarnedText; // NEW
    [SerializeField] private Button restartButton;
    [SerializeField] private Button arsenalButton; // NEW

    [Header("Animation Settings")]
    [SerializeField] private float textGrowDuration = 0.5f;
    [SerializeField] private float statsFadeDelay = 0.3f;
    [SerializeField] private float statsFadeDuration = 0.5f;
    [SerializeField] private float maxTextScale = 1.5f;

    [Header("Arsenal Reference")]
    [SerializeField] private ArsenalUI arsenalUI;

    private CanvasGroup statsCanvasGroup;
    private int iaxpEarned = 0;

    private void Awake()
    {
        // Add CanvasGroup to stats if not present
        if (killsText != null && statsCanvasGroup == null)
        {
            GameObject statsParent = killsText.transform.parent.gameObject;
            statsCanvasGroup = statsParent.GetComponent<CanvasGroup>();
            if (statsCanvasGroup == null)
            {
                statsCanvasGroup = statsParent.AddComponent<CanvasGroup>();
            }
        }

        // Hook up buttons
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        if (arsenalButton != null)
        {
            arsenalButton.onClick.AddListener(OnArsenalClicked);
        }

        // Auto-find ArsenalUI if not assigned
        if (arsenalUI == null)
        {
            arsenalUI = FindObjectOfType<ArsenalUI>();
        }

        Hide();
    }

    public void Show(int kills, int waves, float time)
    {
        // Calculate IA XP earned this run (from IAExperienceManager)
        if (IAExperienceManager.Instance != null)
        {
            iaxpEarned = IAExperienceManager.Instance.XPThisRun;
        }

        gameOverPanel.SetActive(true);
        StartCoroutine(AnimateGameOver(kills, waves, time));
    }

    public void Hide()
    {
        gameOverPanel.SetActive(false);
    }

    private IEnumerator AnimateGameOver(int kills, int waves, float time)
    {
        // Set initial states
        if (gameOverText != null)
        {
            gameOverText.transform.localScale = Vector3.zero;
        }

        if (statsCanvasGroup != null)
        {
            statsCanvasGroup.alpha = 0f;
        }

        // Grow "GAME OVER" text
        if (gameOverText != null)
        {
            float elapsed = 0f;
            while (elapsed < textGrowDuration)
            {
                elapsed += Time.unscaledDeltaTime; // Use unscaled time since game is paused
                float progress = elapsed / textGrowDuration;
                float scale = Mathf.Lerp(0f, maxTextScale, EaseOutElastic(progress));
                gameOverText.transform.localScale = Vector3.one * scale;
                yield return null;
            }
            gameOverText.transform.localScale = Vector3.one * maxTextScale;
        }

        // Wait before showing stats
        yield return new WaitForSecondsRealtime(statsFadeDelay);

        // Set stat values
        if (killsText != null)
            killsText.text = $"Kills: {kills}";

        if (wavesText != null)
            wavesText.text = $"Waves Survived: {waves}";

        if (timeText != null)
        {
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            timeText.text = $"Time: {minutes:00}:{seconds:00}";
        }

        // NEW: Show IA XP earned
        if (iaxpEarnedText != null)
        {
            iaxpEarnedText.text = $"IA Experience Earned: {iaxpEarned}";
        }

        // Fade in stats
        if (statsCanvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < statsFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / statsFadeDuration;
                statsCanvasGroup.alpha = Mathf.Lerp(0f, 1f, progress);
                yield return null;
            }
            statsCanvasGroup.alpha = 1f;
        }
    }

    private void OnRestartClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
    }

    private void OnArsenalClicked()
    {
        if (arsenalUI != null)
        {
            arsenalUI.OpenArsenal();
            Debug.Log("[GameOverUI] Opened Arsenal from Game Over screen");
        }
        else
        {
            Debug.LogError("[GameOverUI] ArsenalUI reference not assigned!");
        }
    }

    // Easing function for elastic bounce
    private float EaseOutElastic(float t)
    {
        float p = 0.3f;
        return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - p / 4f) * (2f * Mathf.PI) / p) + 1f;
    }
}