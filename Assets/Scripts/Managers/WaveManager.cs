using System.Collections;
using UnityEngine;
using ProjectAdminPrivileges.Dialogue;

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

    [Header("Dialogue")]
    [SerializeField] private DialogueData[] preWaveDialogues;  // Dialogue BEFORE wave starts
    [SerializeField] private DialogueData[] postWaveDialogues; // Dialogue AFTER wave completes
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
                //wrapup logic
                break;
        }
    }

    private void UpdateIdleState()
    {
        currentWave++;

        if (uiManager != null)
        {
            uiManager.UpdateWave(currentWave);
        }

        // Check if this is a boss wave
        isBossWave = IsBossWave(currentWave);

        // FIXED: Always check for pre-wave dialogue, but transition to Shopping regardless
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

        // If no dialogue, go straight to Shopping
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


        else if (!isBossWave && enemiesToSpawn > 0)
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
        // Wait for shopping to complete
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
            // FIXED: Register wave completion with GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterWaveComplete(currentWave);
            }

            // FIXED: Track day completion for IA XP calculation
            if (ProjectAdminPrivileges.ShopSystem.IAExperienceManager.Instance != null)
            {
                ProjectAdminPrivileges.ShopSystem.IAExperienceManager.Instance.OnDayComplete();
            }

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

    private void TransitionToState(WaveState newState)
    {
        Debug.Log($"[WaveManager] {currentWaveState} → {newState}");
        currentWaveState = newState;

        switch (newState)
        {
            case WaveState.Shopping:
                // Open shop
                if (ProjectAdminPrivileges.ShopSystem.ShopManager.Instance != null)
                {
                    ProjectAdminPrivileges.ShopSystem.ShopManager.Instance.OpenShop();
                    Debug.Log("[WaveManager] Opening shop");
                }
                break;

            case WaveState.Spawning:
                enemiesToSpawn = enemiesPerWave + (currentWave - 1) * 2;
                spawnTimer = 0f;
                Debug.Log($"[WaveManager] Spawning {enemiesToSpawn} enemies");
                break;

            case WaveState.Delay:
                delayTimer = timeBetweenWaves;
                Debug.Log($"[WaveManager] Waiting {timeBetweenWaves}s before next wave");
                break;
        }
    }

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

                // FIXED: Track kill for IA XP calculation
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

    private bool ShouldShowDialogue(int wave)
    {
        if (showDialogueEveryWave)
        {
            return true;
        }

        // Check if this wave is in the dialogueWaves array
        foreach (int dialogueWave in dialogueWaves)
        {
            if (wave == dialogueWave)
            {
                return true;
            }
        }

        return false;
    }

    private DialogueData GetPreWaveDialogue(int wave)
    {
        if (preWaveDialogues == null || preWaveDialogues.Length == 0)
            return null;

        // Find the INDEX of this wave in dialogueWaves array
        for (int i = 0; i < dialogueWaves.Length; i++)
        {
            if (dialogueWaves[i] == wave)
            {
                // Found it - use the SAME INDEX in preWaveDialogues
                if (i < preWaveDialogues.Length)
                {
                    return preWaveDialogues[i];
                }
                else
                {
                    Debug.LogWarning($"[WaveManager] Wave {wave} in dialogueWaves but no corresponding preWaveDialogues[{i}]");
                    return null;
                }
            }
        }

        // Wave not in dialogueWaves array
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
                {
                    return postWaveDialogues[i];
                }
                else
                {
                    Debug.LogWarning($"[WaveManager] Wave {wave} in dialogueWaves but no corresponding postWaveDialogues[{i}]");
                    return null;
                }
            }
        }

        return null;
    }

    private bool IsBossWave(int wave)
    {
        foreach (int bossWave in bossWaves)
        {
            if (wave == bossWave)
            {
                return true;
            }
        }
        return false;
    }

    private void SpawnBoss()
    {
        if(bossSpawnPoint == null)
        {
            Debug.LogWarning("[WaveManager] No boss spawn point assigned!");
            return;
        }

        GameObject bossPrefabToSpawn = boss1Prefab; // Default to Boss1 -> will extend the logic later
        Transform spawnPoint = bossSpawnPoint != null ? bossSpawnPoint : spawnPoints[0];

        GameObject boss = Instantiate(bossPrefabToSpawn, spawnPoint.position, spawnPoint.rotation);
        enemiesAlive = 1;
        EnemyHealth bossHealth = boss.GetComponent<EnemyHealth>();
        if (bossHealth != null)
        {
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
            };
        }

        Debug.Log($"[WaveManager] Boss spawned for wave {currentWave}!");
    }


    private void OnGameOver()
    {
        StopAllCoroutines();
    }
}