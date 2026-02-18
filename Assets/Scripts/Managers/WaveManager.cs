using System.Collections;
using UnityEngine;
using ProjectAdminPrivileges.Dialogue;
using ProjectAdminPrivileges.Audio;

public class WaveManager : MonoBehaviour
{
    public enum WaveState
    {
        Idle,
        Morning,
        Shopping,
        Spawning,
        Evening,
        Night,
        Wrapup,
        Delay
    }

    [Header("Wave Settings")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private int enemiesPerWave = 5;
    [SerializeField] private float timeBetweenWaves = 3f;
    [SerializeField] private float spawnInterval = 0.5f;

    [Header("References")]
    [SerializeField] private QueenHealth queenHealth;
    [SerializeField] private GameUIManager uiManager;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform playerSpawnPoint;

    [Header("Dialogue")]
    [SerializeField] private DialogueData[] preWaveDialogues;
    [SerializeField] private DialogueData[] postWaveDialogues;
    [SerializeField] private bool showDialogueEveryWave = false;
    [SerializeField] private int[] dialogueWaves = new int[] { 1, 3, 5, 10 };

    [Header("Wave State")]
    [SerializeField] private WaveState currentWaveState = WaveState.Idle;
    private int currentWave = 0;
    private int enemiesAlive = 0;
    private float delayTimer = 0;

    private int enemiesToSpawn = 0;
    private float spawnTimer = 0f;

    [Header("Boss Settings")]
    [SerializeField] private GameObject boss1Prefab;
    [SerializeField] private GameObject boss2Prefab;
    [SerializeField] private Transform bossSpawnPoint;
    [SerializeField] private int[] bossWaves = new int[] { 10 };
    private bool isBossWave = false;

    // Fade gate for Evening state - prevents re-triggering fade/reset every frame
    private bool eveningFadeStarted = false;

    private void Start()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("[WaveManager] No enemy prefab assigned!");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[WaveManager] No spawn points assigned!");
            return;
        }

        if (queenHealth != null)
        {
            queenHealth.OnDeath += OnGameOver;
        }

        TransitionToState(WaveState.Idle);
    }

    private void Update()
    {
        if (queenHealth != null && !queenHealth.IsAlive)
        {
            return;
        }

        switch (currentWaveState)
        {
            case WaveState.Idle:
                UpdateIdleState();
                break;
            case WaveState.Morning:
                UpdateMorningState();
                break;
            case WaveState.Shopping:
                UpdateShoppingState();
                break;
            case WaveState.Spawning:
                UpdateSpawningState();
                break;
            case WaveState.Evening:
                UpdateEveningState();
                break;
            case WaveState.Night:
                UpdateNightState();
                break;
            case WaveState.Delay:
                UpdateDelayState();
                break;
            case WaveState.Wrapup:
                break;
        }
    }

    // ─────────────────────────────────────────────
    // STATE UPDATES (polling only, no one-shot calls)
    // ─────────────────────────────────────────────

    private void UpdateIdleState()
    {
        currentWave++;

        if (uiManager != null)
        {
            uiManager.UpdateWave(currentWave);
        }

        isBossWave = IsBossWave(currentWave);

        if (ShouldShowDialogue(currentWave))
        {
            DialogueData preDialogue = GetPreWaveDialogue(currentWave);
            if (preDialogue != null)
            {
                DialogueManager.Instance.StartDialogue(preDialogue);
                TransitionToState(WaveState.Morning);
                return;
            }
        }

        TransitionToState(WaveState.Shopping);
    }

    private void UpdateMorningState()
    {
        if (DialogueManager.Instance != null &&
            !DialogueManager.Instance.IsActive)
        {
            TransitionToState(WaveState.Shopping);
        }
    }

    private void UpdateSpawningState()
    {
        if (isBossWave)
        {
            SpawnBoss();
            TransitionToState(WaveState.Evening);
        }
        else if (enemiesToSpawn > 0)
        {
            spawnTimer -= Time.deltaTime;

            if (spawnTimer <= 0f)
            {
                SpawnEnemy();
                enemiesToSpawn--;
                spawnTimer = spawnInterval;
            }
        }
        else
        {
            TransitionToState(WaveState.Evening);
        }
    }

    private void UpdateShoppingState()
    {
        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentState != GameManager.GameState.Shopping)
        {
            TransitionToState(WaveState.Spawning);
        }
    }

    private void UpdateEveningState()
    {
        if (enemiesAlive <= 0)
        {
            // Step 1: Start fade out (once)
            if (!eveningFadeStarted)
            {
                eveningFadeStarted = true;
                ScreenFader.Instance.FadeOut();
                return;
            }

            // Step 2: Wait for fade to finish
            if (ScreenFader.Instance.IsFading) return;

            // Step 3: Screen is black — do all between-wave work
            ResetPlayerPosition();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterWaveComplete(currentWave);
            }

            if (ProjectAdminPrivileges.ShopSystem.IAExperienceManager.Instance != null)
            {
                ProjectAdminPrivileges.ShopSystem.IAExperienceManager.Instance.OnDayComplete();
            }

            // Step 4: Post-wave dialogue or move on
            if (ShouldShowDialogue(currentWave))
            {
                DialogueData postDialogue = GetPostWaveDialogue(currentWave);
                if (postDialogue != null)
                {
                    DialogueManager.Instance.StartDialogue(postDialogue);
                    TransitionToState(WaveState.Night);
                    return;
                }
            }

            TransitionToState(WaveState.Delay);
        }
    }

    private void UpdateNightState()
    {
        if (DialogueManager.Instance != null &&
            !DialogueManager.Instance.IsActive)
        {
            TransitionToState(WaveState.Delay);
        }
    }

    private void UpdateDelayState()
    {
        delayTimer -= Time.deltaTime;
        if (delayTimer <= 0f)
        {
            TransitionToState(WaveState.Idle);
        }
    }

    // ─────────────────────────────────────────────
    // STATE TRANSITIONS (one-shot actions go here)
    // ─────────────────────────────────────────────

    private void TransitionToState(WaveState newState)
    {
        Debug.Log($"[WaveManager] {currentWaveState} → {newState}");
        currentWaveState = newState;

        switch (newState)
        {
            case WaveState.Morning:
                // Fade in so player sees dialogue, switch to dialogue music
                ScreenFader.Instance.FadeIn();
                SoundtrackManager.Instance?.PlayDialogueSoundtrack();
                break;

            case WaveState.Shopping:
                // Fade in (safe to call even if already visible — edge guard handles it)
                ScreenFader.Instance?.FadeIn();
                if (ProjectAdminPrivileges.ShopSystem.ShopManager.Instance != null)
                {
                    ProjectAdminPrivileges.ShopSystem.ShopManager.Instance.OpenShop();
                    Debug.Log("[WaveManager] Opening shop");
                }
                break;

            case WaveState.Spawning:
                SoundtrackManager.Instance?.PlayCombatSoundtrack();
                enemiesToSpawn = enemiesPerWave + (currentWave - 1) * 2;
                spawnTimer = 0f;
                Debug.Log($"[WaveManager] Spawning {enemiesToSpawn} enemies");
                break;

            case WaveState.Evening:
                // Reset the gate so fade logic can run fresh
                eveningFadeStarted = false;
                break;

            case WaveState.Night:
                // Post-wave dialogue — fade in so player sees it, dialogue music
                ScreenFader.Instance?.FadeIn();
                SoundtrackManager.Instance?.PlayDialogueSoundtrack();
                break;

            case WaveState.Delay:
                delayTimer = timeBetweenWaves;
                Debug.Log($"[WaveManager] Waiting {timeBetweenWaves}s before next wave");
                break;
        }
    }

    // ─────────────────────────────────────────────
    // SPAWNING
    // ─────────────────────────────────────────────

    private void SpawnEnemy()
    {
        if (spawnPoints.Length == 0) return;

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
        enemiesAlive++;

        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
        if (health != null)
        {
            int scaledHealth = 50 + (currentWave - 1) * 20;
            health.SetMaxHealth(scaledHealth);

            health.OnDeath += () =>
            {
                enemiesAlive--;

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.RegisterKill();
                }

                if (ProjectAdminPrivileges.ShopSystem.IAExperienceManager.Instance != null)
                {
                    ProjectAdminPrivileges.ShopSystem.IAExperienceManager.Instance.OnKill();
                }
            };
        }

        UnityEngine.AI.NavMeshAgent agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            float scaledSpeed = 2f + (currentWave - 1) * 0.3f;
            agent.speed = Mathf.Min(scaledSpeed, 5f);
        }
    }

    private void SpawnBoss()
    {
        if (bossSpawnPoint == null)
        {
            Debug.LogWarning("[WaveManager] No boss spawn point assigned!");
            return;
        }

        GameObject bossPrefabToSpawn = boss1Prefab;
        Transform spawnPoint = bossSpawnPoint != null ? bossSpawnPoint : spawnPoints[0];

        GameObject boss = Instantiate(bossPrefabToSpawn, spawnPoint.position, spawnPoint.rotation);
        enemiesAlive = 1;
        EnemyHealth bossHealth = boss.GetComponent<EnemyHealth>();
        if (bossHealth != null)
        {
            if (uiManager != null)
            {
                uiManager.SetBossHealthBar(true);
                uiManager.SubscribeToBossHealth(bossHealth);
            }

            bossHealth.OnDeath += () =>
            {
                enemiesAlive--;

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.RegisterKill();
                }

                if (ProjectAdminPrivileges.ShopSystem.IAExperienceManager.Instance != null)
                {
                    ProjectAdminPrivileges.ShopSystem.IAExperienceManager.Instance.OnKill();
                }

                if (uiManager != null)
                {
                    uiManager.SetBossHealthBar(false);
                    uiManager.UnsubscribeFromBossHealth(bossHealth);
                }
            };
        }

        Debug.Log($"[WaveManager] Boss spawned for wave {currentWave}!");
    }

    // HELPERS
   

    private void ResetPlayerPosition()
    {
        if (playerTransform != null && playerSpawnPoint != null)
        {
            // Disable CharacterController if present — it blocks transform.position changes
            CharacterController cc = playerTransform.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            playerTransform.position = playerSpawnPoint.position;

            if (cc != null) cc.enabled = true;
        }
    }

    private bool ShouldShowDialogue(int wave)
    {
        if (showDialogueEveryWave) return true;

        foreach (int dialogueWave in dialogueWaves)
        {
            if (wave == dialogueWave) return true;
        }

        return false;
    }

    private DialogueData GetPreWaveDialogue(int wave)
    {
        if (preWaveDialogues == null || preWaveDialogues.Length == 0)
            return null;

        for (int i = 0; i < dialogueWaves.Length; i++)
        {
            if (dialogueWaves[i] == wave)
            {
                if (i < preWaveDialogues.Length)
                    return preWaveDialogues[i];

                Debug.LogWarning($"[WaveManager] Wave {wave} in dialogueWaves but no corresponding preWaveDialogues[{i}]");
                return null;
            }
        }

        return null;
    }

    private DialogueData GetPostWaveDialogue(int wave)
    {
        if (postWaveDialogues == null || postWaveDialogues.Length == 0)
            return null;

        for (int i = 0; i < dialogueWaves.Length; i++)
        {
            if (dialogueWaves[i] == wave)
            {
                if (i < postWaveDialogues.Length)
                    return postWaveDialogues[i];

                Debug.LogWarning($"[WaveManager] Wave {wave} in dialogueWaves but no corresponding postWaveDialogues[{i}]");
                return null;
            }
        }

        return null;
    }

    private bool IsBossWave(int wave)
    {
        foreach (int bossWave in bossWaves)
        {
            if (wave == bossWave) return true;
        }
        return false;
    }

    private void OnGameOver()
    {
        StopAllCoroutines();
    }
}