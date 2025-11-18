using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class BasicEnemyControls : MonoBehaviour
{
    // ========== PUBLIC VARIABLES (Configurable in Inspector) ==========
    [Header("References")]
    public Transform player;                    // Reference to the player transform
    public Animator animator;                   // Reference to the enemy animator
    private NavMeshAgent agent;                 // NavMeshAgent for pathfinding

    [Header("Detection Settings")]
    public float sightRange = 10f;              // How far the enemy can see the player
    public float attackRange = 2f;              // How close enemy needs to be to attack

    [Header("Wander Settings")]
    public float wanderRadius = 5f;             // How far from start position enemy can wander
    public float wanderTimer = 5f;              // How often enemy picks new wander destination

    [Header("Combat Settings")]
    public float attackCooldown = 2f;           // Time between attacks
    public float attackDuration = 1f;           // How long the attack animation takes
    public int attackDamage = 10;                // Damage dealt to the player when attacking

    [Header("Health Settings")]
    public int maxHealth = 15;                  // Maximum health points
    public int currentHealth;                   // Current health points

    // ========== PRIVATE VARIABLES (Internal use only) ==========
    private Vector3 startPosition;              // Remember where enemy started
    private float timer;                        // Timer for wandering
    private bool isChasing = false;             // Whether enemy is chasing player
    private bool isAttacking = false;           // Whether enemy is attacking
    private bool canAttack = true;              // Attack cooldown control
    private bool isDead = false;                // Track if enemy is dead

    // ========== ENEMY STATE SYSTEM ==========
    /// Different states the enemy can be in
    public enum EnemyState { Wandering, Chasing, Attacking }
    private EnemyState currentState = EnemyState.Wandering;

    // ========== INITIALIZATION ==========
    void Start()
    {
        // Get required components
        agent = GetComponent<NavMeshAgent>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // Store starting position for wandering
        startPosition = transform.position;

        // Set up wander timer
        timer = wanderTimer;

        // Initialize health system
        currentHealth = maxHealth;

        // If player not assigned, try to find it by tag
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        // Log error if critical components are missing
        if (agent == null)
            Debug.LogError("NavMeshAgent component missing from enemy!");
        if (animator == null)
            Debug.LogError("Animator component missing from enemy!");
        if (player == null)
            Debug.LogError("Player reference not assigned and no GameObject with 'Player' tag found!");

        Debug.Log($"Enemy spawned with {currentHealth}/{maxHealth} health");
    }

    // ========== UPDATE (Runs every frame) ==========
    void Update()
    {
        // If enemy is dead, don't process any AI behavior
        if (isDead) return;

        // If player is null or critical components missing, don't run AI
        if (player == null || agent == null || animator == null)
            return;

        // Calculate distance to player
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // State machine logic - handle different behaviors based on current state
        switch (currentState)
        {
            case EnemyState.Wandering:
                HandleWanderingState(distanceToPlayer);
                break;
            case EnemyState.Chasing:
                HandleChasingState(distanceToPlayer);
                break;
            case EnemyState.Attacking:
                HandleAttackingState(distanceToPlayer);
                break;
        }

        // Update animator parameters
        UpdateAnimator();
    }

    // ========== STATE MACHINE METHODS ==========
    /// Handles wandering behavior when enemy is not chasing or attacking
    void HandleWanderingState(float distanceToPlayer)
    {
        // Check if player is in sight range
        if (distanceToPlayer <= sightRange)
        {
            // Player spotted! Start chasing
            currentState = EnemyState.Chasing;
            isChasing = true;
            Debug.Log("Enemy spotted player - starting chase!");
            return;
        }

        // Wander behavior - pick new random destination periodically
        timer += Time.deltaTime;
        if (timer >= wanderTimer)
        {
            // Pick a new random position to wander to
            Vector3 newPos = RandomNavMeshPosition(startPosition, wanderRadius);
            if (agent.isActiveAndEnabled)
                agent.SetDestination(newPos);

            timer = 0;
            Debug.Log("Enemy picking new wander destination");
        }

        // Stop moving if we're close to destination and not already attacking
        if (!isAttacking && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            animator.SetBool("isMoving", false);
        }
    }

    /// Handles chasing behavior when enemy has spotted the player
    void HandleChasingState(float distanceToPlayer)
    {
        // Check if player left sight range
        if (distanceToPlayer > sightRange)
        {
            // Player too far, go back to wandering
            currentState = EnemyState.Wandering;
            isChasing = false;

            // Stop chasing and pick a new wander destination
            if (agent.isActiveAndEnabled)
            {
                agent.isStopped = false;
                Vector3 newPos = RandomNavMeshPosition(startPosition, wanderRadius);
                agent.SetDestination(newPos);
            }

            Debug.Log("Player lost - returning to wandering");
            return;
        }

        // Check if player is in attack range
        if (distanceToPlayer <= attackRange)
        {
            // Player in range, start attacking
            currentState = EnemyState.Attacking;
            if (agent.isActiveAndEnabled)
                agent.isStopped = true; // Stop moving to attack

            Debug.Log("Player in attack range - starting attack!");
            return;
        }

        // Continue chasing player
        if (agent.isActiveAndEnabled)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
    }

    /// Handles attacking behavior when player is in range
    void HandleAttackingState(float distanceToPlayer)
    {
        // Check if player moved out of attack range
        if (distanceToPlayer > attackRange)
        {
            // Player too far, resume chasing
            currentState = EnemyState.Chasing;
            if (agent.isActiveAndEnabled)
                agent.isStopped = false;

            isAttacking = false;
            Debug.Log("Player moved away - resuming chase");
            return;
        }

        // Face the player while attacking
        FacePlayer();

        // Perform attack if not already attacking and cooldown is ready
        if (!isAttacking && canAttack)
        {
            StartCoroutine(PerformAttack());
        }
    }

    // ========== ANIMATION AND VISUAL METHODS ==========
    /// Updates all animator parameters based on current enemy state
    void UpdateAnimator()
    {
        // Update movement animation - enemy is moving if NavAgent has velocity and not attacking
        bool isMoving = agent.velocity.magnitude > 0.1f && !isAttacking && agent.isActiveAndEnabled;
        animator.SetBool("isMoving", isMoving);

        // Update chasing state (useful for different chase animations)
        animator.SetBool("isChasing", isChasing);
    }

    /// Makes the enemy face the player during attacks
    void FacePlayer()
    {
        // Make enemy face the player during attacks
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; // Keep rotation only on Y axis

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }

    // ========== COMBAT AND HEALTH METHODS ==========
    /// Coroutine that handles the attack sequence including animation and cooldown
    private IEnumerator PerformAttack()
    {
        isAttacking = true;
        canAttack = false;

        // Reset trigger first to ensure it can fire again
        animator.ResetTrigger("Attack");

        // Trigger attack animation
        animator.SetTrigger("Attack");
        Debug.Log("Enemy attacking player!");

        // TODO: Here you would add code to actually damage the player

        // Wait for attack animation to complete (this is the moment the hit should land)
        yield return new WaitForSeconds(attackDuration);

        // Apply damage at the moment of the attack if the player is still in range
        if (player != null)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= attackRange)
            {
                var playerControls = player.GetComponent<Player1Controls>();
                if (playerControls != null)
                {
                    playerControls.TakeDamage(attackDamage);
                    Debug.Log($"Enemy dealt {attackDamage} damage to player.");
                }
                else
                {
                    // fallback: try to find player by tag
                    var pObj = GameObject.FindGameObjectWithTag("Player");
                    if (pObj != null)
                    {
                        var pc = pObj.GetComponent<Player1Controls>();
                        if (pc != null)
                        {
                            pc.TakeDamage(attackDamage);
                            Debug.Log($"Enemy dealt {attackDamage} damage to player (via tag lookup).");
                        }
                    }
                }
            }
        }

        // Wait for remaining cooldown time (ensure non-negative)
        float postWait = Mathf.Max(attackCooldown - attackDuration, 0f);
        if (postWait > 0f)
            yield return new WaitForSeconds(postWait);

        // Reset attack state
        isAttacking = false;
        canAttack = true;

        Debug.Log("Enemy attack cooldown complete");
    }

    /// Called when enemy takes damage from player
    /// <param name="damage">Amount of damage to take</param>
    public void TakeDamage(int damage)
    {
        // Don't take damage if already dead
        if (isDead) return;

        // Reduce health
        currentHealth -= damage;
        Debug.Log($"Enemy took {damage} damage! Health: {currentHealth}/{maxHealth}");

        // Check if enemy died
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Optional: Play hurt animation or sound
            Debug.Log("Enemy is hurt but still alive!");

            // Optional: Make enemy more aggressive when hurt
            // currentState = EnemyState.Chasing;
        }
    }

    /// Handles enemy death sequence
    private void Die()
    {
        isDead = true;
        Debug.Log("Enemy died!");

        // Stop all enemy behavior
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
        }

        // Disable the NavMeshAgent to prevent movement
        if (agent != null)
            agent.enabled = false;

        // Disable the EnemyAI script to stop all AI behavior
        this.enabled = false;

        // Play death animation if you have one
        // animator.SetTrigger("Die");

        // Optional: Switch to dead body layer to prevent further collisions
        gameObject.layer = LayerMask.NameToLayer("Default");

        // Destroy the enemy after a short delay
        Destroy(gameObject, 2f); // 2 second delay to see death animation

        // Optional: You could also play death sound, spawn effects, drop items, etc.
    }

    // ========== HELPER METHODS ==========
    /// Helper method to get random position on NavMesh for wandering
    Vector3 RandomNavMeshPosition(Vector3 origin, float distance)
    {
        Vector3 randomDirection = Random.insideUnitSphere * distance;
        randomDirection += origin;

        NavMeshHit navHit;
        if (NavMesh.SamplePosition(randomDirection, out navHit, distance, NavMesh.AllAreas))
        {
            return navHit.position;
        }

        return origin; // Fallback to start position
    }

    // ========== DEBUG VISUALIZATION ==========
    /// Visualize detection ranges in Scene view for debugging
    void OnDrawGizmosSelected()
    {
        // Draw sight range (yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        // Draw attack range (red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Draw wander radius (blue) - only if start position is set
        if (Application.isPlaying)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(startPosition, wanderRadius);
        }
    }

    // ========== PUBLIC METHODS (For other scripts to use) ==========
    /// Public method to get current enemy state
    public EnemyState GetCurrentState()
    {
        return currentState;
    }

    /// Public method to check if enemy is currently attacking
    public bool IsAttacking()
    {
        return isAttacking;
    }

    /// Public method to check if enemy is dead
    public bool IsDead()
    {
        return isDead;
    }

    /// Public method to get current health
    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    /// Public method to get maximum health
    public int GetMaxHealth()
    {
        return maxHealth;
    }

    /// Public method to get current health percentage (for UI)
    public float GetHealthPercentage()
    {
        return (float)currentHealth / maxHealth;
    }
}