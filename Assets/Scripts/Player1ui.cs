using UnityEngine;
using TMPro;

public class Player1ui : MonoBehaviour
{
    [Header("Health Display")]
    public TextMeshProUGUI healthText;

    [Header("Special Attack Display")]
    public TextMeshProUGUI specialAttackText; // Add this for special attack

    [Header("Player Reference")]
    public Player1Controls player;

    void Start()
    {
        if (player == null)
            player = FindObjectOfType<Player1Controls>();

        if (player != null)
        {
            // Initialize UI and subscribe to changes
            UpdateHealthText(player.currentHealth, player.maxHealth);
            UpdateSpecialAttackText(player.currentSpecialAttack, player.maxSpecialAttack);

            player.OnHealthChanged += UpdateHealthText;
            player.OnSpecialAttackChanged += UpdateSpecialAttackText; // Subscribe to new event
        }
        else
        {
            Debug.LogWarning("Player1ui: No Player1Controls found in scene.");
        }
    }

    void OnDestroy()
    {
        if (player != null)
        {
            player.OnHealthChanged -= UpdateHealthText;
            player.OnSpecialAttackChanged -= UpdateSpecialAttackText;
        }
    }

    private void UpdateHealthText(int current, int max)
    {
        if (healthText != null)
            healthText.text = $"HP: {current}/{max}";
    }

    private void UpdateSpecialAttackText(int current, int max)
    {
        if (specialAttackText != null)
            specialAttackText.text = $"Special: {current}/{max}";
    }
}