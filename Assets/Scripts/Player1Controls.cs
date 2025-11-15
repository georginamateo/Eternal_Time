using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
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

    // ========== PRIVATE VARIABLES (Internal use only) ==========
    private Rigidbody rb;                  // Reference to the Rigidbody component
    private Vector3 movement;              // Stores movement direction
    private bool canAttack = true;         // Prevents spamming attacks during cooldown

    // ========== INITIALIZATION ==========
    void Start()
    {
        // Get the Rigidbody component attached to this GameObject
        rb = GetComponent<Rigidbody>();

        // If no Animator is assigned in Inspector, try to find one in child objects
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // Log warning if enemy layer is not set
        if (enemyLayer == 0)
            Debug.LogWarning("Enemy Layer not set in PlayerController! Attacks won't hit enemies.");
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

        // Deal damage to enemies in range
        DealDamageToEnemies();

        // Wait for the attack cooldown period before allowing another attack
        yield return new WaitForSeconds(attackCooldown);

        // Re-enable attacking after cooldown
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

    /// Checks if an enemy is within the player's attack arc (in front of player)
    /// <param name="enemy">The enemy's transform</param>
    /// <returns>True if enemy is in front of player</returns>
    private bool IsEnemyInFront(Transform enemy)
    {
        // Calculate direction from player to enemy
        Vector3 directionToEnemy = (enemy.position - transform.position).normalized;

        // Calculate angle between player's forward direction and direction to enemy
        float angle = Vector3.Angle(transform.forward, directionToEnemy);

        // Return true if enemy is within 60 degrees of player's forward direction
        return angle < 60f; // 60 degrees each side = 120 degree attack arc
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
        return canAttack ? 1f : 0f; // Simple implementation - expand as needed
    }
}