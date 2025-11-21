using UnityEngine;

public class LootItem : MonoBehaviour
{
    public enum LootType { Health, SpecialAttack }

    [Header("Loot Settings")]
    public LootType lootType = LootType.Health;
    public string itemName = "Health Potion";
    public int value = 10;

    [Header("Appearance Settings")]
    public float rotationSpeed = 50f;
    public float floatHeight = 0.5f;
    public float floatSpeed = 2f;
    public Vector3 lootScale = Vector3.one;

    [Header("Collision Settings")]
    public LayerMask playerLayer = 1 << 0; // Default layer by default

    private Vector3 startPosition;
    private bool collected = false;

    void Start()
    {
        startPosition = transform.position;
        transform.localScale = lootScale;

        // Auto-assign to Loots layer if available
        if (LayerMask.NameToLayer("Loots") != -1)
        {
            gameObject.layer = LayerMask.NameToLayer("Loots");
        }

        // Ensure we have a collider
        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<SphereCollider>();
            Debug.LogWarning($"Added missing collider to {itemName}");
        }

        // Ensure collider is trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        Debug.Log($"Loot item '{itemName}' spawned on layer: {LayerMask.LayerToName(gameObject.layer)}");
    }

    void Update()
    {
        if (collected) return;

        // Floating animation
        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // Rotation animation
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected) return;

        Debug.Log($"=== LOOT TRIGGER ===");
        Debug.Log($"Loot: {itemName}");
        Debug.Log($"Triggered by: {other.gameObject.name}");
        Debug.Log($"Tag: {other.tag}");
        Debug.Log($"Layer: {LayerMask.LayerToName(other.gameObject.layer)}");
        Debug.Log($"Has Player1Controls: {other.GetComponent<Player1Controls>() != null}");

        // Check both tag AND layer for extra safety
        if (other.CompareTag("Player1") && ((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            Player1Controls player = other.GetComponent<Player1Controls>();
            if (player != null)
            {
                CollectLoot(player);
            }
            else
            {
                Debug.LogError("Player tag found but no Player1Controls component!");
            }
        }
        else
        {
            Debug.Log($"Collision ignored - Tag: '{other.tag}', Layer: '{LayerMask.LayerToName(other.gameObject.layer)}'");
        }
    }

    private void CollectLoot(Player1Controls player)
    {
        collected = true;

        switch (lootType)
        {
            case LootType.Health:
                player.Heal(value);
                Debug.Log($"Collected {itemName} and healed {value} HP!");
                break;
            case LootType.SpecialAttack:
                player.AddSpecialAttack(value);
                Debug.Log($"Collected {itemName} and gained {value} special attack!");
                break;
        }

        // Visual effect before destruction (optional)
        StartCoroutine(DestroyWithEffect());
    }

    private System.Collections.IEnumerator DestroyWithEffect()
    {
        // Optional: Add a quick flash or scale down effect
        float destroyTime = 0.2f;
        float timer = 0f;
        Vector3 originalScale = transform.localScale;

        while (timer < destroyTime)
        {
            timer += Time.deltaTime;
            transform.localScale = originalScale * (1f - timer / destroyTime);
            yield return null;
        }

        Destroy(gameObject);
    }

    void OnDrawGizmos()
    {
        // Visualize loot collection area in Scene view
        Gizmos.color = lootType == LootType.Health ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}