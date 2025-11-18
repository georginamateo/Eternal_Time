using UnityEngine;
using TMPro;

public class Player1ui : MonoBehaviour
{
	[Tooltip("TextMeshProUGUI to display health (e.g. 'HP: 100/100')")]
	public TextMeshProUGUI healthText;

	[Tooltip("Optional: slider or image fill could be wired here later")]
	public Player1Controls player;

	void Start()
	{
		if (player == null)
			player = FindObjectOfType<Player1Controls>();

		if (player != null)
		{
			// Initialize UI and subscribe to changes
			UpdateHealthText(player.currentHealth, player.maxHealth);
			player.OnHealthChanged += UpdateHealthText;
		}
		else
		{
			Debug.LogWarning("Player1ui: No Player1Controls found in scene.");
		}
	}

	void OnDestroy()
	{
		if (player != null)
			player.OnHealthChanged -= UpdateHealthText;
	}

	private void UpdateHealthText(int current, int max)
	{
		if (healthText == null)
			return;

		healthText.text = $"HP: {current}/{max}";
	}
}
