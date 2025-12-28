using ProjectAdminPrivileges.Enemies.Animation;
using UnityEngine;
using UnityEngine.AI;
using ProjectAdminPrivileges.Audio;

/// <summary>
/// Enemy AI that targets either Player or Queen (whichever is closer).
/// Re-evaluates target every 0.5 seconds to switch dynamically.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    private NavMeshAgent agent;
    private EnemyAnimationController animationController;

    [Header("Targeting")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform queenTransform;
    [SerializeField] private float targetUpdateInterval = 0.5f;
    private Transform currentTarget;
    private float targetUpdateTimer = 0f;

    [Header("Footstep Audio")]
    [SerializeField] private AudioClipData footstepSound;
    [SerializeField] private float stepFrequency = 2.0f;
    [SerializeField] private float velocityThreshold = 0.3f;
    private float timeSinceLastStep = 0f;

    public bool IsMoving { get; private set; }
    public Transform CurrentTarget => currentTarget;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        // Find player and queen by tag
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }

        if (queenTransform == null)
        {
            GameObject queenObj = GameObject.FindGameObjectWithTag("Queen");
            if (queenObj != null) queenTransform = queenObj.transform;
        }

        if (animationController == null)
        {
            animationController = GetComponent<EnemyAnimationController>();
        }

        // Initial target selection
        UpdateTarget();
    }

    private void Update()
    {
        // Re-evaluate target periodically
        targetUpdateTimer += Time.deltaTime;
        if (targetUpdateTimer >= targetUpdateInterval)
        {
            UpdateTarget();
            targetUpdateTimer = 0f;
        }

        if (currentTarget == null) return;

        agent.SetDestination(currentTarget.position);
        IsMoving = agent.velocity.magnitude > 0.1f;

        if (IsMoving && animationController != null)
        {
            animationController.SetRun(Mathf.Abs(agent.velocity.magnitude));
        }

        HandleFootsteps();
    }

    /// <summary>
    /// Pick closest target between Player and Queen.
    /// If either is dead/null, target the other.
    /// </summary>
    private void UpdateTarget()
    {
        // Check if both are alive
        bool playerAlive = playerTransform != null && IsTargetAlive(playerTransform);
        bool queenAlive = queenTransform != null && IsTargetAlive(queenTransform);

        if (!playerAlive && !queenAlive)
        {
            currentTarget = null;
            return;
        }

        if (!playerAlive)
        {
            currentTarget = queenTransform;
            return;
        }

        if (!queenAlive)
        {
            currentTarget = playerTransform;
            return;
        }

        // Both alive - pick closest
        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        float distToQueen = Vector3.Distance(transform.position, queenTransform.position);

        currentTarget = distToPlayer < distToQueen ? playerTransform : queenTransform;
    }

    private bool IsTargetAlive(Transform target)
    {
        // Check if target has health component and is alive
        var playerHealth = target.GetComponent<ProjectAdminPrivileges.PlayerCharacter.PlayerHealth>();
        if (playerHealth != null)
        {
            return playerHealth.IsAlive;
        }

        var queenHealth = target.GetComponent<QueenHealth>();
        if (queenHealth != null)
        {
            return queenHealth.IsAlive;
        }

        // No health component found - assume alive
        return true;
    }

    public void StopMovement()
    {
        agent.isStopped = true;
    }

    public void ResumeMovement()
    {
        agent.isStopped = false;
    }

    public float DistanceToTarget()
    {
        if (currentTarget == null) return float.MaxValue;
        return Vector3.Distance(transform.position, currentTarget.position);
    }

    private void HandleFootsteps()
    {
        float currentSpeed = agent.velocity.magnitude;

        if (currentSpeed > velocityThreshold && IsMoving)
        {
            timeSinceLastStep += Time.deltaTime;

            float maxSpeed = agent.speed;
            float stepInterval = 1.0f / (stepFrequency * (currentSpeed / maxSpeed));

            if (timeSinceLastStep >= stepInterval)
            {
                if (AudioManager.Instance != null && footstepSound != null)
                {
                    AudioManager.Instance.PlayAudio(footstepSound, transform.position, 0.2f);
                }
                timeSinceLastStep = 0f;
            }
        }
        else
        {
            timeSinceLastStep = 0f;
        }
    }
}