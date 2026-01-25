using UnityEngine;
using TMPro;

public class PauseUI : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private TextMeshProUGUI pauseText;

    private void Awake()
    {
        Hide();
    }

    public void Show()
    {
        pausePanel.SetActive(true);
    }

    public void Hide()
    {
        pausePanel.SetActive(false);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("MainMenuScene");
    }
}