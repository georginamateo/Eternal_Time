using UnityEngine;
using System.Collections;
using System;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Player1Controls : MonoBehaviour
{
    // ========== PUBLIC VARIABLES (Configurable in Inspector) ==========
    [Header("Movement Settings")]
    public float moveSpeed = 5f;           // How fast the character moves
    public float rotationSpeed = 720f;     // How fast the character rotates (degrees per second)

    [Header("Animation Settings")]
    public Animator animator;              // Reference to the Animator component
    public float attackCooldown = 0.5f;    // Time between attacks (seconds)

    [Header("Combat Settings")]
    public int attackDamage = 5;           // Damage dealt to enemies per attack
    public float attackRange = 2f;         // How far the player can hit enemies
    public LayerMask enemyLayer;           // Which layers contain enemies (set in Inspector)
    public LayerMask chestLayer;           // Layer for breakable chests

    [Header("Health Settings")]
    public int maxHealth = 100;            // Player maximum health
    public int currentHealth;              // Player current health

    [Header("Special Attack Settings")]
    public int maxSpecialAttack = 20;      // Maximum special attack energy
    public int currentSpecialAttack = 0;   // Current special attack energy

    [Header("Visual Feedback")]
    public FlashEffect flashEffect;        // Reference to flash effect component

    // Event fired when health changes: current, max
    public event Action<int, int> OnHealthChanged;
    // Event fired when special attack changes: current, max
    public event Action<int, int> OnSpecialAttackChanged;

    // ========== PRIVATE VARIABLES (Internal use only) ==========
    private Rigidbody rb;                  // Reference to the Rigidbody component
    private Vector3 movement;              // Stores movement direction
    private bool canAttack = true;         // Prevents spamming attacks during cooldown

    // ========== INITIALIZATION ==========
    void Start()
    {
        // Get the Rigidbody component attached to this GameObject
        rb = GetComponent<Rigidbody>();

        // Ensure Rigidbody is configured for reliable collisions
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        // Prevent physics from rotating the player on X/Z so MoveRotation behaves predictably
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // If no Animator is assigned in Inspector, try to find one in child objects
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // Ensure the player has a Collider so physics collides with world geometry
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            // Add a CapsuleCollider as a reasonable default for humanoid characters
            var capsule = gameObject.AddComponent<CapsuleCollider>();
            capsule.height = 2f;
            capsule.radius = 0.4f;
            capsule.center = new Vector3(0f, 1f, 0f);
            Debug.LogWarning("Player1Controls: No Collider found on player — added a default CapsuleCollider. Adjust in Inspector if needed.");
        }

        // Try to get FlashEffect component if not assigned
        if (flashEffect == null)
            flashEffect = GetComponentInChildren<FlashEffect>();
        if (flashEffect == null)
            Debug.LogWarning("FlashEffect component missing from player! Add it for hit feedback.");

        // Log warning if enemy layer is not set
        if (enemyLayer == 0)
            Debug.LogWarning("Enemy Layer not set in PlayerController! Attacks won't hit enemies.");

        // Log warning if chest layer is not set
        if (chestLayer == 0)
            Debug.LogWarning("Chest Layer not set in PlayerController! Attacks won't hit chests.");

        // Initialize health and special attack
        currentHealth = maxHealth;
        currentSpecialAttack = 0;

        // Initialize events
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnSpecialAttackChanged?.Invoke(currentSpecialAttack, maxSpecialAttack);

        setRigidbodyState(true);
        setColliderState(false);

        Debug.Log($"Player initialized with {currentHealth}/{maxHealth} health and {currentSpecialAttack}/{maxSpecialAttack} special attack");
    }

    // ========== UPDATE (Runs every frame) ==========
    void Update()
    {
        // --- MOVEMENT INPUT ---
        // Get raw input from keyboard/controller (-1, 0, or 1)
        float h = Input.GetAxisRaw("Horizontal");  // A/D or Left/Right arrows
        float v = Input.GetAxisRaw("Vertical");    // W/S or Up/Down arrows

        // Create movement vector and normalize to prevent faster diagonal movement
        movement = new Vector3(h, 0f, v).normalized;

        // --- ANIMATION CONTROL ---
        // Check if character is moving (magnitude > 0 means there's movement input)
        bool isMoving = movement.magnitude > 0;

        // Update Animator parameter to transition between Idle and Move animations
        animator.SetBool("isMoving", isMoving);

        // --- ATTACK INPUT ---
        // Check if F key is pressed AND attack is not on cooldown
        if (Input.GetKeyDown(KeyCode.F) && canAttack)
        {
            // Start attack coroutine to handle animation and damage
            StartCoroutine(PerformAttack());
        }

        // --- SPECIAL ATTACK INPUT ---
        // Check if G key is pressed for special attack (you can change the key)
        if (Input.GetKeyDown(KeyCode.G) && canAttack)
        {
            TrySpecialAttack();
        }
    }

    // ========== FIXED UPDATE (Runs at fixed intervals for physics) ==========
    void FixedUpdate()
    {
        // Only move if there's significant movement input
        if (movement.magnitude >= 0.1f)
        {
            // Calculate movement vector: direction * speed * time
            Vector3 move = movement * moveSpeed * Time.fixedDeltaTime;

            // Apply movement to Rigidbody (physics-based movement)
            rb.MovePosition(rb.position + move);

            // Calculate target rotation to face movement direction
            Quaternion targetRotation = Quaternion.LookRotation(movement);

            // Smoothly rotate toward movement direction
            rb.MoveRotation(Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime
            ));
        }
    }

    // ========== ATTACK COROUTINE ==========
    /// Handles the complete attack sequence: animation, damage, and cooldown
    private System.Collections.IEnumerator PerformAttack()
    {
        // Prevent additional attacks while this one is executing
        canAttack = false;

        // Reset trigger first to ensure it can fire again
        animator.ResetTrigger("Attack");

        // Trigger the attack animation in the Animator
        animator.SetTrigger("Attack");
        Debug.Log("Player attacking!");

        // Deal damage to enemies AND chests in range
        DealDamageToEnemies();
        DamageChests();

        // Wait for the attack cooldown period before allowing another attack
        yield return new WaitForSeconds(attackCooldown);

        // Re-enable attacking after cooldown
        canAttack = true;
    }

    // ========== SPECIAL ATTACK METHODS ==========
    /// <summary>
    /// Attempt to use special attack if enough energy is available
    /// </summary>
    private void TrySpecialAttack()
    {
        int specialAttackCost = 10; // Cost to use special attack

        if (currentSpecialAttack >= specialAttackCost)
        {
            StartCoroutine(PerformSpecialAttack());
        }
        else
        {
            Debug.Log($"Not enough special attack energy! Need {specialAttackCost}, have {currentSpecialAttack}");
        }
    }

    /// <summary>
    /// Perform special attack sequence
    /// </summary>
    private System.Collections.IEnumerator PerformSpecialAttack()
    {
        canAttack = false;

        // Use special attack energy
        UseSpecialAttack(10); // Cost 10 special energy

        // Reset trigger first to ensure it can fire again
        animator.ResetTrigger("SpecialAttack");

        // Trigger special attack animation
        animator.SetTrigger("SpecialAttack");
        Debug.Log("Player using special attack!");

        // TODO: Add special attack logic here (bigger damage, area effect, etc.)
        // For now, we'll just do enhanced damage to all enemies in larger range

        Collider[] hitEnemies = Physics.OverlapSphere(transform.position, attackRange * 1.5f, enemyLayer);
        foreach (Collider enemy in hitEnemies)
        {
            BasicEnemyControls enemyAI = enemy.GetComponent<BasicEnemyControls>();
            if (enemyAI != null && !enemyAI.IsDead())
            {
                enemyAI.TakeDamage(attackDamage * 2); // Double damage for special
                Debug.Log($"Special attack hit {enemy.name} for {attackDamage * 2} damage!");
            }
        }

        yield return new WaitForSeconds(attackCooldown * 1.5f);
        canAttack = true;
    }

    // ========== COMBAT LOGIC ==========
    /// Finds all enemies in attack range and deals damage to them
    private void DealDamageToEnemies()
    {
        // Find all colliders within attack range that are on the enemy layer
        Collider[] hitEnemies = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);

        Debug.Log($"Found {hitEnemies.Length} enemies in attack range");

        foreach (Collider enemy in hitEnemies)
        {
            // Check if enemy is in front of player (within attack arc)
            if (IsEnemyInFront(enemy.transform))
            {
                // Get the EnemyAI component from the enemy
                BasicEnemyControls enemyAI = enemy.GetComponent<BasicEnemyControls>();
                if (enemyAI != null && !enemyAI.IsDead())
                {
                    // Deal damage to the enemy
                    enemyAI.TakeDamage(attackDamage);
                    Debug.Log($"Hit {enemy.name} for {attackDamage} damage!");
                }
            }
        }

        // Visual debug - show attack direction in Scene view
        Debug.DrawRay(transform.position, transform.forward * attackRange, Color.red, 1f);
    }

    /// Damages breakable chests in attack range
    private void DamageChests()
    {
        // Find all colliders within attack range that are on the chest layer
        Collider[] hitChests = Physics.OverlapSphere(transform.position, attackRange, chestLayer);

        Debug.Log($"Found {hitChests.Length} chests in attack range");

        foreach (Collider chest in hitChests)
        {
            // Check if chest is in front of player (within attack arc)
            if (IsEnemyInFront(chest.transform))
            {
                // Get the BreakableChest component from the chest
                BreakableChest breakableChest = chest.GetComponent<BreakableChest>();
                if (breakableChest != null)
                {
                    // Deal damage to the chest
                    breakableChest.TakeDamage(attackDamage);
                    Debug.Log($"Hit chest for {attackDamage} damage!");
                }
            }
        }
    }

    /// Checks if an enemy/chest is within the player's attack arc (in front of player)
    /// <param name="target">The target's transform</param>
    /// <returns>True if target is in front of player</returns>
    private bool IsEnemyInFront(Transform target)
    {
        // Calculate direction from player to target
        Vector3 directionToTarget = (target.position - transform.position).normalized;

        // Calculate angle between player's forward direction and direction to target
        float angle = Vector3.Angle(transform.forward, directionToTarget);

        // Return true if target is within 60 degrees of player's forward direction
        return angle < 60f; // 60 degrees each side = 120 degree attack arc
    }

    // ========== HEALTH AND SPECIAL ATTACK METHODS ==========
    /// Apply damage to the player
    public void TakeDamage(int damage)
    {
        if (damage <= 0 || IsDead())
            return;

        currentHealth -= damage;
        if (currentHealth < 0)
            currentHealth = 0;

        Debug.Log($"Player took {damage} damage! Health: {currentHealth}/{maxHealth}");
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Trigger flash effect
        if (flashEffect != null)
            flashEffect.Flash();
        else
            Debug.LogWarning("FlashEffect reference missing on player!");

        if (currentHealth <= 0)
            Die();
    }

    /// Heal the player
    public void Heal(int amount)
    {
        if (amount <= 0 || IsDead())
            return;

        int oldHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth != oldHealth)
        {
            Debug.Log($"Player healed {amount} HP! Health: {currentHealth}/{maxHealth}");
        }
        else
        {
            Debug.Log("Player already at max health!");
        }
    }

    /// Add special attack energy
    public void AddSpecialAttack(int amount)
    {
        if (amount <= 0) return;

        int oldSpecial = currentSpecialAttack;
        currentSpecialAttack = Mathf.Min(maxSpecialAttack, currentSpecialAttack + amount);
        OnSpecialAttackChanged?.Invoke(currentSpecialAttack, maxSpecialAttack);

        if (currentSpecialAttack != oldSpecial)
        {
            Debug.Log($"Gained {amount} special attack! Special: {currentSpecialAttack}/{maxSpecialAttack}");
        }
        else
        {
            Debug.Log("Special attack already at maximum!");
        }
    }

    /// Use special attack energy
    public bool UseSpecialAttack(int cost)
    {
        if (currentSpecialAttack >= cost)
        {
            currentSpecialAttack -= cost;
            OnSpecialAttackChanged?.Invoke(currentSpecialAttack, maxSpecialAttack);
            Debug.Log($"Used special attack! Cost: {cost}, Remaining: {currentSpecialAttack}/{maxSpecialAttack}");
            return true;
        }
        return false;
    }

    /// Returns whether the player is dead
    public bool IsDead()
    {
        return currentHealth <= 0;
    }

    private void Die()
    {
        Debug.Log("Player died.");

        this.enabled = false;
        canAttack = false;

        if (animator != null)
            animator.enabled = false;

        // Remove constraints and reset velocity
        rb.constraints = RigidbodyConstraints.None;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Disable root collider so it doesn't fight the ragdoll
        GetComponent<Collider>().enabled = false;

        setRigidbodyState(false);   // turn on rigidbody physics
        setColliderState(true);     // enable child limb colliders
    }

    // ========== DEBUG VISUALIZATION ==========
    /// Draws gizmos in the Scene view for debugging (only visible when object is selected)
    void OnDrawGizmosSelected()
    {
        // Draw attack range (green wire sphere)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Draw attack direction (red line)
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * attackRange);

        // Draw attack arc (yellow wire arc)
        Gizmos.color = Color.yellow;
        DrawWireArc(transform.position, transform.forward, 60f, attackRange);
    }

    // Helper method to draw wire arc for attack range visualization
    void DrawWireArc(Vector3 position, Vector3 dir, float anglesRange, float radius)
    {
        float srcAngles = GetAnglesFromDir(position, dir);
        Vector3 initialPos = position;
        Vector3 posA = initialPos;
        float stepAngles = anglesRange / 2 / 20;
        float angle = srcAngles - anglesRange / 2;
        for (int i = 0; i <= 20; i++)
        {
            float rad = Mathf.Deg2Rad * angle;
            Vector3 posB = initialPos;
            posB += new Vector3(radius * Mathf.Cos(rad), 0, radius * Mathf.Sin(rad));

            Gizmos.DrawLine(posA, posB);

            angle += stepAngles;
            posA = posB;
        }
        Gizmos.DrawLine(posA, initialPos);
    }

    float GetAnglesFromDir(Vector3 position, Vector3 dir)
    {
        Vector3 forward = position + dir;
        float srcAngles = Mathf.Rad2Deg * Mathf.Atan2(forward.z - position.z, forward.x - position.x);
        return srcAngles;
    }

    // ========== PUBLIC METHODS ==========
    /// Check if player can currently attack (for UI cooldown indicators)
    public bool CanAttack()
    {
        return canAttack;
    }

    /// Get the current attack cooldown progress (for UI progress bars)
    public float GetAttackCooldownProgress()
    {
        return canAttack ? 1f : 0f;
    }

    // ========== RAGDOLL METHODS ==========
    void setRigidbodyState(bool state)
    {
        Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>();

        foreach (Rigidbody rigidbody in rigidbodies)
        {
            rigidbody.isKinematic = state;
        }
    }

    void setColliderState(bool state)
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();

        foreach (Collider col in colliders)
        {
            col.enabled = state;
        }
    }
}