using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(HealthSystem))]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]

public class MobAI : MonoBehaviour
{
    public enum MobState
    {
        Idle,
        Chasing,
        Attacking,
        Fleeing,
        Stunned,
        Dead
    }

    [Header("AI State")]
    private MobState currentState = MobState.Idle;
    private float stunEndTime = 0f;

    [Header("Movement & Combat")]
    [SerializeField] private float mobSpeed = 3.5f;
    [SerializeField] private float mobFleeSpeed = 5f;
    [SerializeField] private float mobStoppingDistance = 2f;
    [SerializeField] protected float mobAgroRadius = 10f;
    [SerializeField] private float mobAttackRate = 1f;
    [SerializeField] private float stunDuration = 1.5f;

    [Header("Peaceful Mode Settings")]
    [SerializeField] private float fleeHealthPercentage = 0.3f;
    [SerializeField] private float fleeDistance = 15f;

    private HealthSystem mobHealth;
    private Rigidbody mobRigidbody;
    private ZombieAudio zombieAudio;
    private Animator mobAnimator;
    public NavMeshAgent navMeshAgent;

    private float lastAttackTime;
    private bool wasPreviouslyAgroSoundPlayed = false;

    private Transform playerTransform;

    [Header("Animator Parameters")]
    [SerializeField] private string isWalkingBoolName = "IsWalking";
    [SerializeField] private string attackTriggerName = "Attack";
    [SerializeField] private string isFleeingBoolName = "IsFleeing";
    [SerializeField] private string stunTriggerName = "Stun";
    [SerializeField] private string deathTriggerName = "Death";

    private void Awake()
    {
        mobHealth = GetComponent<HealthSystem>();
        if (mobHealth == null) Debug.LogWarning("HealthSystem component not found on " + gameObject.name + ". Mob death and health logic will not work.", this);

        navMeshAgent = GetComponent<NavMeshAgent>();
        if (navMeshAgent == null)
        {
            Debug.LogError("NavMeshAgent component not found on " + gameObject.name + ". Mob AI will not work correctly! Script disabled.", this);
            enabled = false;
            return;
        }

        mobRigidbody = GetComponent<Rigidbody>();
        if (mobRigidbody == null) Debug.LogWarning("Rigidbody component not found on " + gameObject.name + ". Mob physics interaction might be affected, especially on death.", this);

        zombieAudio = GetComponent<ZombieAudio>();
        if (zombieAudio == null) Debug.LogWarning("ZombieAudio component not found on " + gameObject.name + ". Mob sounds will not play.", this);

        mobAnimator = GetComponent<Animator>();
        if (mobAnimator == null)
        {
            Debug.LogError("Animator component not found on " + gameObject.name + ". Mob animations will not work! Consider adding it or removing Animator references.", this);
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogError("Player GameObject (with 'Player' tag) not found. Disabling AI for: " + gameObject.name, this);
            enabled = false;
            return;
        }
        playerTransform = playerObj.transform;

        navMeshAgent.speed = mobSpeed;
        navMeshAgent.stoppingDistance = mobStoppingDistance;
        navMeshAgent.updateRotation = true;
        navMeshAgent.updatePosition = true;

        currentState = MobState.Idle;
        lastAttackTime = Time.time;
    }

    void Update()
    {
        if (playerTransform == null || currentState == MobState.Dead || !enabled) return;

        if (mobHealth != null && mobHealth.IsDead())
        {
            if (currentState != MobState.Dead) SwitchState(MobState.Dead);
            return;
        }

        if (currentState == MobState.Stunned)
        {
            if (Time.time >= stunEndTime)
            {
                SwitchState(MobState.Idle);
            }
            else
            {
                UpdateAnimator();
                return;
            }
        }

        bool peacefulMode = false;
        try
        {
            peacefulMode = PeaceModeManager.IsPeacefulModeActive;
        }
        catch (System.Exception)
        {
        }

        if (peacefulMode)
        {
            HandlePeacefulModeBehavior();
        }
        else
        {
            HandleAggressiveModeBehavior();
        }

        UpdateAnimator();
    }

    void HandlePeacefulModeBehavior()
    {
        if (currentState == MobState.Idle && mobHealth != null)
        {
            float currentHealth = mobHealth.GetHealth();
            float maxHealth = mobHealth.GetMaxHealth();
            if (maxHealth > 0)
            {
                float currentHealthRatio = currentHealth / maxHealth;
                if (currentHealthRatio <= fleeHealthPercentage)
                {
                    SwitchState(MobState.Fleeing);
                    return;
                }
            }
        }

        switch (currentState)
        {
            case MobState.Fleeing:
                ProcessFleeing();
                break;
            case MobState.Stunned:
                break;
            case MobState.Idle:
                ProcessIdlePeaceful();
                break;
        }
    }

    void HandleAggressiveModeBehavior()
    {
        if (currentState == MobState.Stunned || currentState == MobState.Dead) return;

        if (playerTransform == null)
        {
            if (currentState != MobState.Idle) SwitchState(MobState.Idle);
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer < mobAgroRadius)
        {
            if (!wasPreviouslyAgroSoundPlayed && zombieAudio != null)
            {
                zombieAudio.PlayAgroSound();
                wasPreviouslyAgroSoundPlayed = true;
            }

            if (navMeshAgent != null && distanceToPlayer > navMeshAgent.stoppingDistance)
            {
                if (currentState != MobState.Chasing) SwitchState(MobState.Chasing);
            }
            else
            {
                if (currentState != MobState.Attacking) SwitchState(MobState.Attacking);
            }
        }
        else
        {
            if (wasPreviouslyAgroSoundPlayed)
            {
                wasPreviouslyAgroSoundPlayed = false;
            }
            if (currentState != MobState.Idle) SwitchState(MobState.Idle);
        }

        switch (currentState)
        {
            case MobState.Chasing:
                ProcessChasing();
                break;
            case MobState.Attacking:
                ProcessAttacking();
                break;
            case MobState.Idle:
                ProcessIdleAggressive();
                break;
        }
    }

    void DecideStateBasedOnAggro()
    {
        if (playerTransform == null || currentState == MobState.Dead || currentState == MobState.Stunned) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer < mobAgroRadius)
        {
            if (navMeshAgent != null && distanceToPlayer > navMeshAgent.stoppingDistance)
            {
                SwitchState(MobState.Chasing);
            }
            else
            {
                SwitchState(MobState.Attacking);
            }
        }
        else
        {
            SwitchState(MobState.Idle);
        }
    }

    void SwitchState(MobState newState)
    {
        if (currentState == newState && newState != MobState.Stunned) return;

        currentState = newState;

        if (navMeshAgent != null && navMeshAgent.enabled)
        {
            switch (currentState)
            {
                case MobState.Idle:
                    navMeshAgent.isStopped = true;
                    navMeshAgent.speed = mobSpeed;
                    if (navMeshAgent.isOnNavMesh) navMeshAgent.ResetPath();
                    wasPreviouslyAgroSoundPlayed = false;
                    break;

                case MobState.Chasing:
                    navMeshAgent.isStopped = false;
                    navMeshAgent.speed = mobSpeed;
                    break;

                case MobState.Attacking:
                    if (navMeshAgent.isOnNavMesh)
                    {
                        navMeshAgent.isStopped = true;
                        navMeshAgent.ResetPath();
                    }
                    break;

                case MobState.Fleeing:
                    navMeshAgent.isStopped = false;
                    navMeshAgent.speed = mobFleeSpeed;
                    InitiateFleeing();
                    break;

                case MobState.Stunned:
                    navMeshAgent.isStopped = true;
                    if (navMeshAgent.isOnNavMesh) navMeshAgent.ResetPath();
                    stunEndTime = Time.time + stunDuration;
                    if (zombieAudio != null) zombieAudio.PlayStunSound();
                    break;

                case MobState.Dead:
                    if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
                    {
                        navMeshAgent.isStopped = true;
                        navMeshAgent.enabled = false;
                    }
                    OnMobAIDeath();
                    break;
            }
        }
        else if (currentState != MobState.Dead && navMeshAgent == null)
        {
        }
    }

    void ProcessIdlePeaceful() { }
    void ProcessIdleAggressive() { }

    void ProcessChasing()
    {
        if (playerTransform == null || navMeshAgent == null || !navMeshAgent.enabled || !navMeshAgent.isOnNavMesh || navMeshAgent.isStopped) return;

        if (Vector3.Distance(navMeshAgent.destination, playerTransform.position) > 0.1f)
        {
            navMeshAgent.SetDestination(playerTransform.position);
        }
    }

    void ProcessAttacking()
    {
        if (playerTransform == null) return;

        Vector3 lookPos = playerTransform.position - transform.position;
        lookPos.y = 0;
        if (lookPos.magnitude > 0)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookPos);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }

        if (Time.time >= lastAttackTime + (1f / mobAttackRate))
        {
            PerformAttack();
            lastAttackTime = Time.time;
        }
    }

    void InitiateFleeing()
    {
        if (playerTransform == null || navMeshAgent == null || !navMeshAgent.enabled || !navMeshAgent.isOnNavMesh)
        {
            SwitchState(MobState.Idle);
            return;
        }

        Vector3 fleeDirection = (transform.position - playerTransform.position).normalized;
        Vector3 fleeTargetPosition = transform.position + fleeDirection * fleeDistance;

        NavMeshHit hit;

        if (NavMesh.SamplePosition(fleeTargetPosition, out hit, fleeDistance * 2f, NavMesh.AllAreas))
        {
            navMeshAgent.SetDestination(hit.position);
        }
        else
        {
            Vector3 randomDir = Random.insideUnitSphere * fleeDistance;
            randomDir.y = 0;
            if (NavMesh.SamplePosition(transform.position + randomDir + transform.position, out hit, fleeDistance, NavMesh.AllAreas))
            {
                navMeshAgent.SetDestination(hit.position);
            }
            else
            {
                Debug.LogWarning(gameObject.name + " could not find valid flee path. Switching to Idle.");
                SwitchState(MobState.Idle);
            }
        }
    }

    void ProcessFleeing()
    {
        if (navMeshAgent == null || !navMeshAgent.enabled || !navMeshAgent.isOnNavMesh) return;

        bool reachedDestination = !navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance + 0.1f && (!navMeshAgent.hasPath || navMeshAgent.velocity.sqrMagnitude < 0.1f);

        if (reachedDestination)
        {
            float currentHealthRatio = (mobHealth != null && mobHealth.GetMaxHealth() > 0) ? mobHealth.GetHealth() / mobHealth.GetMaxHealth() : 1f;
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            bool peacefulMode = false;
            try { peacefulMode = PeaceModeManager.IsPeacefulModeActive; }
            catch (System.Exception) { }

            if (peacefulMode && (currentHealthRatio > fleeHealthPercentage || distanceToPlayer > fleeDistance * 1.5f))
            {
                SwitchState(MobState.Idle);
            }
            else if (peacefulMode)
            {
                InitiateFleeing();
            }
            else
            {
                DecideStateBasedOnAggro();
            }
        }
        else if (navMeshAgent.pathStatus == NavMeshPathStatus.PathInvalid || navMeshAgent.pathStatus == NavMeshPathStatus.PathPartial)
        {
            bool peacefulMode = false;
            try { peacefulMode = PeaceModeManager.IsPeacefulModeActive; }
            catch (System.Exception) { }

            if (currentState == MobState.Fleeing && peacefulMode)
            {
                InitiateFleeing();
            }
            else if (!peacefulMode)
            {
                DecideStateBasedOnAggro();
            }
        }
    }

    private void PerformAttack()
    {
        if (mobAnimator != null)
        {
            bool attackTriggerExists = false;
            foreach (var param in mobAnimator.parameters)
            {
                if (param.type == AnimatorControllerParameterType.Trigger && param.name == attackTriggerName)
                {
                    attackTriggerExists = true;
                    break;
                }
            }
            if (attackTriggerExists)
            {
                mobAnimator.SetTrigger(attackTriggerName);
            }
            else
            {
                Debug.LogWarning($"Animator trigger '{attackTriggerName}' not found for {gameObject.name}");
            }
        }

        if (zombieAudio != null) zombieAudio.PlayAttackSound();
    }

    private void OnMobAIDeath()
    {
        if (currentState == MobState.Dead && !enabled) return;

        Debug.Log("MobAI: " + gameObject.name + " is dead (AI cleanup)!");
        currentState = MobState.Dead;

        if (navMeshAgent != null && navMeshAgent.enabled)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.enabled = false;
        }

        if (mobRigidbody != null)
        {
            mobRigidbody.velocity = Vector3.zero;
            mobRigidbody.isKinematic = true;
            mobRigidbody.constraints = RigidbodyConstraints.FreezeAll;
        }

        if (zombieAudio != null) zombieAudio.PlayDeathSound();

        if (mobAnimator != null)
        {
            bool deathTriggerExists = false;
            foreach (var param in mobAnimator.parameters)
            {
                if (param.type == AnimatorControllerParameterType.Trigger && param.name == deathTriggerName)
                {
                    deathTriggerExists = true;
                    break;
                }
            }
            if (deathTriggerExists)
            {
                mobAnimator.SetTrigger(deathTriggerName);
            }
            else
            {
                Debug.LogWarning($"Animator trigger '{deathTriggerName}' not found for {gameObject.name}");
            }
        }

        enabled = false;
    }

    private void UpdateAnimator()
    {
        bool hasAnimator = (mobAnimator != null);
        bool isAgentActive = (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh);

        if (!hasAnimator) return;

        bool isMoving = isAgentActive && navMeshAgent.velocity.sqrMagnitude > 0.01f;

        if (!string.IsNullOrEmpty(isWalkingBoolName))
        {
            bool isWalkingParamExists = false;
            foreach (var param in mobAnimator.parameters)
            {
                if (param.type == AnimatorControllerParameterType.Bool && param.name == isWalkingBoolName)
                {
                    isWalkingParamExists = true;
                    break;
                }
            }
            if (isWalkingParamExists)
            {
                mobAnimator.SetBool(isWalkingBoolName, isMoving && (currentState == MobState.Chasing || currentState == MobState.Fleeing));
            }
            else
            {
            }
        }

        if (!string.IsNullOrEmpty(isFleeingBoolName))
        {
            bool isFleeingParamExists = false;
            foreach (var param in mobAnimator.parameters)
            {
                if (param.type == AnimatorControllerParameterType.Bool && param.name == isFleeingBoolName)
                {
                    isFleeingParamExists = true;
                    break;
                }
            }
            if (isFleeingParamExists)
            {
                mobAnimator.SetBool(isFleeingBoolName, currentState == MobState.Fleeing && isMoving);
            }
            else
            {
            }
        }
    }

    public void TakeStun()
    {
        if (currentState == MobState.Dead || currentState == MobState.Stunned) return;

        if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.isStopped = true;
            if (navMeshAgent.hasPath) navMeshAgent.ResetPath();
        }

        SwitchState(MobState.Stunned);
    }

    public void DespawnMob()
    {
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }

    public void OnNormalAttackAnimationEnd()
    {
        if ((mobHealth == null || !mobHealth.IsDead()) && currentState == MobState.Attacking)
        {
            bool peacefulMode = false;
            try { peacefulMode = PeaceModeManager.IsPeacefulModeActive; }
            catch (System.Exception) { }

            if (!peacefulMode)
            {
                DecideStateBasedOnAggro();
            }
            else
            {
                SwitchState(MobState.Idle);
            }
        }
    }

    public void NotifyDamageTaken(float amount)
    {
        if (currentState == MobState.Dead) return;

        bool peacefulMode = false;
        try { peacefulMode = PeaceModeManager.IsPeacefulModeActive; }
        catch (System.Exception) { }

        if (peacefulMode)
        {
            if (mobHealth != null)
            {
                float currentHealth = mobHealth.GetHealth();
                float maxHealth = mobHealth.GetMaxHealth();
                if (maxHealth > 0 && currentHealth / maxHealth <= fleeHealthPercentage && currentState != MobState.Fleeing && currentState != MobState.Dead && currentState != MobState.Stunned)
                {
                    SwitchState(MobState.Fleeing);
                }
            }
        }
        else
        {
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        if (transform != null) Gizmos.DrawWireSphere(transform.position, mobAgroRadius);

        Gizmos.color = Color.blue;
        if (transform != null) Gizmos.DrawWireSphere(transform.position, mobStoppingDistance);

        if (navMeshAgent != null && navMeshAgent.hasPath && (currentState == MobState.Chasing || currentState == MobState.Fleeing))
        {
            Gizmos.color = Color.yellow;
            Vector3 lastCorner = transform.position;
            foreach (var corner in navMeshAgent.path.corners)
            {
                Gizmos.DrawLine(lastCorner, corner);
                lastCorner = corner;
            }
            Gizmos.DrawSphere(navMeshAgent.destination, 0.3f);
        }
    }
}