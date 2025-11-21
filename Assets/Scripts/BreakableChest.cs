using UnityEngine;
using System.Collections;

public class BreakableChest : MonoBehaviour
{
    [Header("Chest Settings")]
    public int maxHealth = 10;
    private int currentHealth;

    [Header("Visual Feedback")]
    public float flashDuration = 0.2f;
    private Material originalMaterial;
    private Renderer chestRenderer;
    private Color originalColor;

    [Header("Loot Settings")]
    public bool dropLoot = true;
    public GameObject[] lootItems;
    public int minLoot = 1;
    public int maxLoot = 3;

    private bool isBroken = false;
    private Collider chestCollider;

    void Start()
    {
        currentHealth = maxHealth;
        chestRenderer = GetComponent<Renderer>();
        chestCollider = GetComponent<Collider>();

        // Store original material properties for flash effect
        if (chestRenderer != null)
        {
            originalMaterial = chestRenderer.material;
            originalColor = chestRenderer.material.color;
        }

        Debug.Log($"Chest ready with {currentHealth} health");
    }

    public void TakeDamage(int damage)
    {
        if (isBroken) return;

        currentHealth -= damage;
        Debug.Log($"Chest took {damage} damage! Health: {currentHealth}/{maxHealth}");

        // Flash effect
        if (chestRenderer != null)
        {
            StartCoroutine(FlashEffect());
        }

        if (currentHealth <= 0)
        {
            BreakChest();
        }
    }

    private IEnumerator FlashEffect()
    {
        if (chestRenderer != null)
        {
            chestRenderer.material.color = Color.white;
            yield return new WaitForSeconds(flashDuration);
            chestRenderer.material.color = originalColor;
        }
    }

    private void BreakChest()
    {
        isBroken = true;
        Debug.Log("Chest broke!");

        // Drop loot before despawning
        if (dropLoot && lootItems != null && lootItems.Length > 0)
        {
            DropLoot();
        }

        // Despawn chest but leave loot
        DespawnChest();
    }

    private void DropLoot()
    {
        int lootCount = Random.Range(minLoot, maxLoot + 1);
        Debug.Log($"Dropping {lootCount} loot items");

        for (int i = 0; i < lootCount; i++)
        {
            GameObject lootPrefab = lootItems[Random.Range(0, lootItems.Length)];
            if (lootPrefab != null)
            {
                Vector3 spawnPosition = transform.position + new Vector3(
                    Random.Range(-1f, 1f),
                    0.5f,
                    Random.Range(-1f, 1f)
                );

                Instantiate(lootPrefab, spawnPosition, Quaternion.identity);
            }
        }
    }

    private void DespawnChest()
    {
        // Make chest invisible and non-interactive
        if (chestRenderer != null)
            chestRenderer.enabled = false;

        if (chestCollider != null)
            chestCollider.enabled = false;

        // Destroy after a short delay
        Destroy(gameObject, 1f);
    }

    // Visual debug in Scene view
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = currentHealth > 0 ? Color.green : Color.red;
        Gizmos.DrawWireCube(transform.position + Vector3.up * 1.5f, new Vector3(1f, 0.2f, 0.2f));
    }
}