using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using ProjectAdminPrivileges.ShopSystem;
using ProjectAdminPrivileges.PlayerCharacter;
using ProjectAdminPrivileges.Dialogue;

public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        Playing,
        Paused,
        Dialogue,
        Shopping,
        GameOver
    }

    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    [SerializeField] private GameState currentState = GameState.Playing;

    [Header("Statistics")]
    private int totalKills = 0;
    private int wavesCompleted = 0;
    private float timeElapsed = 0f;

    [Header("References")]
    [SerializeField] private GameOverUI gameOverUI;
    [SerializeField] private PauseUI pauseUI;
    [SerializeField] private ProjectAdminPrivileges.PlayerCharacter.PlayerHealth playerHealth;
    [SerializeField] private QueenHealth queenHealth;

    public bool IsGameplayActive => currentState == GameState.Playing;

    // Events
    public event Action OnGameStart;
    public event Action OnGameOver;
    public event Action OnGamePause;
    public event Action OnGameResume;

    // Properties
    public GameState CurrentState => currentState;
    public int TotalKills => totalKills;
    public int WavesCompleted => wavesCompleted;
    public float TimeElapsed => timeElapsed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (playerHealth == null)
        {
            playerHealth = FindAnyObjectByType<PlayerHealth>();
        }
        if (queenHealth == null)
        {
            queenHealth = FindAnyObjectByType<QueenHealth>();
        }
    }

    private void Start()
    {
        // Subscribe to death events
        if (playerHealth != null)
        {
            playerHealth.OnDeath += OnPlayerDeath;
        }
        else
        {
            Debug.LogWarning("[GameManager] PlayerHealth reference not assigned!");
        }

        if (queenHealth != null)
        {
            queenHealth.OnDeath += OnQueenDeath;
        }
        else
        {
            Debug.LogWarning("[GameManager] QueenHealth reference not assigned!");
        }

        StartGame();
    }

    private void Update()
    {
        // Track time only when playing
        if (currentState == GameState.Playing)
        {
            timeElapsed += Time.deltaTime;
        }

        // Pause toggle - ONLY if not in dialogue or shopping
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentState == GameState.Dialogue ||
               currentState == GameState.Shopping ||
               currentState == GameState.GameOver)
                return;

            if (currentState == GameState.Playing)
            {
                SetGameState(GameState.Paused);
            }
            else if (currentState == GameState.Paused)
            {
                SetGameState(GameState.Playing);
            }
        }
    }

    public void SetGameState(GameState newState)
    {
        if (currentState == newState) return;

        currentState = newState;

        switch (currentState)
        {
            case GameState.Playing:
                Time.timeScale = 1f;
                if (pauseUI != null)
                    pauseUI.Hide();
                OnGameResume?.Invoke();
                Debug.Log("[GameManager] Game resumed via SetGameState");
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                if (pauseUI != null)
                    pauseUI.Show();
                OnGamePause?.Invoke();
                Debug.Log("[GameManager] Game paused via SetGameState");
                break;

            case GameState.Dialogue:
                Time.timeScale = 0f;
                Debug.Log("[GameManager] Entered Dialogue state via SetGameState");
                break;

            case GameState.Shopping:
                Time.timeScale = 0f;
                Debug.Log("[GameManager] Entered Shopping state via SetGameState");
                break;

            case GameState.GameOver:
                Time.timeScale = 0f;
                if (gameOverUI != null)
                {
                    gameOverUI.Show(totalKills, wavesCompleted, timeElapsed);
                }
                OnGameOver?.Invoke();

                // Calculate IA XP and save progress
                OnRunEnd();

                Debug.Log($"[GameManager] Game Over - Kills: {totalKills}, Waves: {wavesCompleted}, Time: {timeElapsed:F1}s");
                break;
        }
        Debug.Log($"[GameManager] Game state changed to {currentState}");
    }

    public void StartGame()
    {
        totalKills = 0;
        wavesCompleted = 0;
        timeElapsed = 0f;

        if (pauseUI != null)
            pauseUI.Hide();
        if (gameOverUI != null)
            gameOverUI.Hide();

        SetGameState(GameState.Playing);

        OnGameStart?.Invoke();

        Debug.Log("[GameManager] Game started");
    }

    public void RestartGame()
    {
        Debug.Log("[GameManager] Restarting game - reloading scene");
        Time.timeScale = 1f;
        currentState = GameState.Playing;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        queenHealth = FindFirstObjectByType<QueenHealth>();
    }

    public void RegisterKill()
    {
        if (!IsGameplayActive) return;
        totalKills++;
    }

    public void RegisterWaveComplete(int waveNumber)
    {
        if (!IsGameplayActive) return;
        wavesCompleted = waveNumber;
        Debug.Log($"[GameManager] Wave {waveNumber} completed");
    }

    /// <summary>
    /// Called when player dies - triggers game over
    /// </summary>
    private void OnPlayerDeath()
    {
        Debug.Log("[GameManager] PLAYER DIED - Game Over");
        SetGameState(GameState.GameOver);
    }

    /// <summary>
    /// Called when queen dies - triggers game over
    /// </summary>
    private void OnQueenDeath()
    {
        Debug.Log("[GameManager] QUEEN DIED - Game Over");
        SetGameState(GameState.GameOver);
    }

    /// <summary>
    /// Called at end of run to calculate IA XP and save progression
    /// </summary>
    private void OnRunEnd()
    {
        // Calculate bonuses
        int queenHP = queenHealth != null ? queenHealth.CurrentHealth : 0;
        int relationshipStage = RelationshipManager.Instance != null ?
            RelationshipManager.Instance.GetCurrentStage() : 1;

        // Award IA XP
        if (IAExperienceManager.Instance != null)
        {
            IAExperienceManager.Instance.OnRunEnd(queenHP, relationshipStage);
        }

        Debug.Log($"[GameManager] Run ended - IA XP awarded");
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (playerHealth != null)
        {
            playerHealth.OnDeath -= OnPlayerDeath;
        }

        if (queenHealth != null)
        {
            queenHealth.OnDeath -= OnQueenDeath;
        }
    }
}