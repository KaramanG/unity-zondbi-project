using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(HealthSystem))]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]

public class BossAI : MonoBehaviour
{
    public enum BossState
    {
        Patrolling,
        Chasing,
        Attacking,
        SpecialAttack,
        Dead
    }

    public BossState CurrentState { get; private set; } = BossState.Patrolling;

    [Header("References")]
    [Tooltip("Assign the player's Transform here, or it will try to find GameObject with tag 'Player'")]
    public Transform playerTransform;
    private NavMeshAgent agent;
    private Animator animator;
    private HealthSystem healthSystem;
    private Rigidbody rb;

    [Header("Combat Stats")]
    [SerializeField] private float attackDamage = 15f;
    [SerializeField] private float attackCooldown = 2f;
    private float currentAttackCooldown = 0f;

    [Header("Special Attack")]
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private GameObject altFireballPrefab;

    [SerializeField] private Transform fireballSpawnPoint;
    [SerializeField] private float fireballDamage = 10f;
    [SerializeField] private float fireballSpeed = 15f;
    [SerializeField] private float fireballLifetime = 5f;
    [SerializeField] private List<LayerMask> fireballTargetLayers;
    [SerializeField] private float specialAttackCooldown = 30f;
    [Header("Nova Attack (All Directions)")]
    [SerializeField] private int novaFireballCount = 8;
    [Header("Shotgun Attack (Towards Player)")]
    [SerializeField] private int shotgunFireballCount = 3;
    [SerializeField] private float shotgunSpreadAngle = 15f;

    private float currentSpecialAttackCooldown = 0f;
    private bool useNovaAttack = true;

    [Header("Patrolling")]
    [Tooltip("Array of points the boss will patrol between")]
    public Transform[] patrolPoints;
    [SerializeField] private float patrolSpeed = 2f;
    private int currentPatrolIndex = 0;
    [SerializeField] private float waypointProximityThreshold = 1f;
    [SerializeField] private float attackRange = 3f;

    [Header("Chasing")]
    [SerializeField] private float chaseSpeed = 5f;

    private bool isProvokedByDamage = false;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent component not found on " + gameObject.name + ". Boss AI will not work correctly! Script disabled.", this);
            enabled = false;
            return;
        }

        animator = GetComponent<Animator>();
        if (animator == null) Debug.LogWarning("Animator component not found on " + gameObject.name + ". Boss animations will not work.", this);

        healthSystem = GetComponent<HealthSystem>();
        if (healthSystem == null) Debug.LogWarning("HealthSystem component not found on " + gameObject.name + ". Boss death and health logic will not work.", this);

        rb = GetComponent<Rigidbody>();
        if (rb == null) Debug.LogWarning("Rigidbody component not found on " + gameObject.name + ". Boss physics might not behave as expected, especially on death.", this);

        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
            else
            {
                Debug.LogError("Player GameObject (with 'Player' tag) not found for Boss: " + gameObject.name + ". AI requires a player target! Script disabled.", this);
                enabled = false;
                return;
            }
        }

        agent.speed = patrolSpeed;
        agent.stoppingDistance = attackRange;
        agent.updateRotation = true;
        agent.updatePosition = true;

        CurrentState = BossState.Patrolling;
        currentAttackCooldown = 0f;
    }

    void Start()
    {
        if (agent == null || !enabled) return;
        currentSpecialAttackCooldown = specialAttackCooldown;
        SwitchState(BossState.Patrolling);
    }

    void Update()
    {
        if (playerTransform == null || CurrentState == BossState.Dead || !enabled) return;

        if (healthSystem != null && healthSystem.IsDead())
        {
            if (CurrentState != BossState.Dead) SwitchState(BossState.Dead);
            return;
        }

        if (currentAttackCooldown > 0)
        {
            currentAttackCooldown -= Time.deltaTime;
        }

        if (isProvokedByDamage && currentSpecialAttackCooldown > 0)
        {
            currentSpecialAttackCooldown -= Time.deltaTime;
        }

        DecideState();

        switch (CurrentState)
        {
            case BossState.Patrolling:
                HandlePatrolling();
                break;
            case BossState.Chasing:
                HandleChasing();
                break;
            case BossState.Attacking:
                HandleAttacking();
                break;
            case BossState.SpecialAttack:
                break;
        }

        UpdateAnimatorParams();
    }

    void DecideState()
    {
        if (CurrentState == BossState.Dead || CurrentState == BossState.SpecialAttack) return;

        if (isProvokedByDamage)
        {
            if (currentSpecialAttackCooldown <= 0f)
            {
                SwitchState(BossState.SpecialAttack);
                return;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            if (distanceToPlayer <= attackRange)
            {
                if (CurrentState != BossState.Attacking) SwitchState(BossState.Attacking);
            }
            else
            {
                if (CurrentState != BossState.Chasing) SwitchState(BossState.Chasing);
            }
        }
        else
        {
            if (CurrentState != BossState.Patrolling)
            {
                SwitchState(BossState.Patrolling);
            }
        }
    }

    void SwitchState(BossState newState)
    {
        if (CurrentState == newState) return;

        CurrentState = newState;

        if (agent != null && agent.enabled)
        {
            switch (CurrentState)
            {
                case BossState.Patrolling:
                    agent.speed = patrolSpeed;
                    agent.isStopped = false;
                    if (patrolPoints != null && patrolPoints.Length > 0 && agent.isOnNavMesh)
                    {
                        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
                    }
                    else if (agent.isOnNavMesh)
                    {
                        agent.ResetPath();
                    }
                    break;

                case BossState.Chasing:
                    agent.speed = chaseSpeed;
                    agent.isStopped = false;
                    break;

                case BossState.Attacking:
                    if (agent.isOnNavMesh)
                    {
                        agent.isStopped = true;
                        agent.ResetPath();
                    }
                    break;

                case BossState.SpecialAttack:
                    if (agent.isOnNavMesh)
                    {
                        agent.isStopped = true;
                        agent.ResetPath();
                    }
                    PerformSpecialAttack();
                    break;

                case BossState.Dead:
                    if (agent != null && agent.isOnNavMesh)
                    {
                        agent.isStopped = true;
                        agent.enabled = false;
                    }
                    OnBossAIDeath();
                    break;
            }
        }
        else if (CurrentState != BossState.Dead)
        {
            Debug.LogWarning("Boss NavMeshAgent is null or disabled when trying to switch state to " + newState + " on " + gameObject.name);
        }
    }

    void HandlePatrolling()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh || patrolPoints == null || patrolPoints.Length == 0)
        {
            if (CurrentState == BossState.Patrolling) Debug.LogWarning("Boss " + gameObject.name + " cannot patrol: NavMeshAgent issue or no patrol points.");
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= waypointProximityThreshold)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            if (patrolPoints[currentPatrolIndex] != null)
            {
                agent.SetDestination(patrolPoints[currentPatrolIndex].position);
            }
            else
            {
                Debug.LogError("Patrol point " + currentPatrolIndex + " is null for " + gameObject.name);
            }
        }
    }

    void HandleChasing()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh || playerTransform == null)
        {
            if (CurrentState == BossState.Chasing) Debug.LogWarning("Boss " + gameObject.name + " cannot chase: NavMeshAgent issue or player is null.");
            SwitchState(BossState.Patrolling);
            return;
        }

        if (Vector3.Distance(agent.destination, playerTransform.position) > 0.1f)
        {
            agent.SetDestination(playerTransform.position);
        }
    }

    void HandleAttacking()
    {
        if (playerTransform == null)
        {
            DecideState();
            return;
        }

        Vector3 lookPos = playerTransform.position - transform.position;
        lookPos.y = 0;
        if (lookPos != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookPos);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }

        if (currentAttackCooldown <= 0f)
        {
            PerformAttack();
            currentAttackCooldown = attackCooldown;
        }
    }

    void PerformAttack()
    {
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        if (playerTransform != null)
        {
            HealthSystem playerHealth = playerTransform.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                if (Vector3.Distance(transform.position, playerTransform.position) <= attackRange * 1.1f)
                {
                    playerHealth.TakeDamage(attackDamage);
                }
            }
        }
    }

    void PerformSpecialAttack()
    {
        if (animator != null)
        {
            animator.SetTrigger("SpecialAttack");
        }
        else
        {
            OnSpecialAttackCastEvent();
            OnSpecialAttackAnimationEnd();
        }
    }

    public void OnSpecialAttackCastEvent()
    {
        if (fireballPrefab == null)
        {
            Debug.LogError("Fireball prefab is not assigned in BossAI!");
            return;
        }

        if (useNovaAttack)
        {
            // --- NOVA ATTACK LOGIC ---
            Transform spawnOrigin = (fireballSpawnPoint != null) ? fireballSpawnPoint : transform;
            float angleStep = 360f / novaFireballCount;

            for (int i = 0; i < novaFireballCount; i++)
            {
                float currentAngle = i * angleStep;
                Quaternion rotation = Quaternion.Euler(0, currentAngle, 0);
                SpawnAndLaunchFireball(spawnOrigin.position, rotation, fireballPrefab);
            }
        }
        else
        {
            // --- SHOTGUN ATTACK LOGIC ---
            if (playerTransform == null) return;
            Transform spawnOrigin = (fireballSpawnPoint != null) ? fireballSpawnPoint : transform;
            Vector3 directionToPlayer = (playerTransform.position - spawnOrigin.position).normalized;

            float startAngle = -(shotgunSpreadAngle * (shotgunFireballCount - 1) / 2f);

            for (int i = 0; i < shotgunFireballCount; i++)
            {
                float angle = startAngle + i * shotgunSpreadAngle;
                Quaternion rotation = Quaternion.LookRotation(directionToPlayer) * Quaternion.Euler(0, angle, 0);
                SpawnAndLaunchFireball(spawnOrigin.position, rotation, altFireballPrefab);
            }
        }
    }

    private void SpawnAndLaunchFireball(Vector3 position, Quaternion rotation, GameObject fireballPrefab)
    {
        GameObject spawnedFireball = Instantiate(fireballPrefab, position, rotation);

        Fireball fireballScript = spawnedFireball.GetComponent<Fireball>();
        if (fireballScript != null)
        {
            fireballScript.SetDamage(fireballDamage);
            fireballScript.SetMasks(fireballTargetLayers);
            fireballScript.SetLifetime(fireballLifetime);
            fireballScript.StartCountdown();
        }

        Rigidbody fireballRb = spawnedFireball.GetComponent<Rigidbody>();
        if (fireballRb != null)
        {
            fireballRb.AddForce(spawnedFireball.transform.forward * fireballSpeed, ForceMode.Impulse);
        }
    }

    public void OnSpecialAttackAnimationEnd()
    {
        currentSpecialAttackCooldown = specialAttackCooldown;
        useNovaAttack = !useNovaAttack; // Чередуем атаку
        CurrentState = BossState.Chasing;
        DecideState();
    }

    public void NotifyDamageTaken(float amount)
    {
        if (CurrentState == BossState.Dead) return;

        if (!isProvokedByDamage)
        {
            isProvokedByDamage = true;
            DecideState();
        }

        if (healthSystem != null && healthSystem.IsDead() && CurrentState != BossState.Dead)
        {
            SwitchState(BossState.Dead);
        }
    }

    private void OnBossAIDeath()
    {
        if (CurrentState == BossState.Dead && !enabled) return;

        Debug.Log("BossAI: " + gameObject.name + " is dead (AI cleanup)!");

        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.enabled = false;
        }

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (animator != null)
        {
            animator.SetTrigger("Death");
        }

        enabled = false;
    }

    public void DespawnBoss()
    {
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }

    void UpdateAnimatorParams()
    {
        if (animator == null) return;

        float speedForAnimator = 0f;
        if (agent != null && agent.enabled && agent.isOnNavMesh && !agent.isStopped)
        {
            speedForAnimator = agent.velocity.magnitude;
        }

        bool isWalkingParamExists = false;
        foreach (var param in animator.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Bool && param.name == "IsWalking")
            {
                isWalkingParamExists = true;
                break;
            }
        }

        if (isWalkingParamExists)
        {
            animator.SetBool("IsWalking", speedForAnimator > 0.01f);
        }
        else
        {
            bool speedParamExists = false;
            foreach (var param in animator.parameters)
            {
                if (param.type == AnimatorControllerParameterType.Float && param.name == "Speed")
                {
                    speedParamExists = true;
                    break;
                }
            }
            if (speedParamExists)
            {
                animator.SetFloat("Speed", speedForAnimator);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        if (transform != null) Gizmos.DrawWireSphere(transform.position, attackRange);

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    Gizmos.DrawSphere(patrolPoints[i].position, 0.3f);
                    if (patrolPoints.Length > 1)
                    {
                        int nextIndex = (i + 1) % patrolPoints.Length;
                        if (patrolPoints[nextIndex] != null)
                        {
                            Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[nextIndex].position);
                        }
                    }
                }
            }
        }

        if (agent != null && agent.hasPath && CurrentState != BossState.Patrolling && CurrentState != BossState.Attacking && CurrentState != BossState.Dead)
        {
            Gizmos.color = Color.yellow;
            Vector3 lastCorner = transform.position;
            foreach (var corner in agent.path.corners)
            {
                Gizmos.DrawLine(lastCorner, corner);
                lastCorner = corner;
            }
        }
    }

    public void OnBossAttackAnimationEnd()
    {
        if ((healthSystem == null || !healthSystem.IsDead()) && CurrentState == BossState.Attacking)
        {
            DecideState();
        }
    }
}