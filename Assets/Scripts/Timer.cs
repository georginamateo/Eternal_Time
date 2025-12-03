using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// Countdown timer for a level objective. Displays time in a TextMeshProUGUI
/// and, when the timer expires, checks remaining enemies and shows a popup.
///
/// Usage:
/// - Add this component to an empty GameObject in the scene (e.g. "GameManager").
/// - Assign `timerText` to a TextMeshProUGUI that displays the countdown.
/// - (Optional) Assign `popupPanel` to a UI panel GameObject (set inactive) and
///   `popupText` to a TMP text inside that panel. If not assigned, a Debug.Log is used.
/// - Call `StartTimer()` to begin the countdown, or enable `startOnAwake`.
/// </summary>
public class Timer : MonoBehaviour
{
	[Header("Timer Settings")]
	public float duration = 30f;
	public bool startOnAwake = true;

	[Header("UI References")]
	public TextMeshProUGUI timerText;
	[Tooltip("Optional popup panel to enable when time ends")]
	public GameObject popupPanel;
	public TextMeshProUGUI popupText;

	[Header("Popup Messages")]
	public string congratulationsMessage = "Congratulations! \nPress SPACE to continue";
	public string timeUpMessage = "Time's up!";

	[Tooltip("Message to show when the player dies")]
	public string youDiedMessage = "You died!";

	[Header("Level Progression")]
	[Tooltip("Name of the next scene to load when all enemies are cleared")]
	public string nextSceneName = "";
	
	[Tooltip("If empty, will load the next scene in build index")]
	public bool useNextBuildIndex = true;

	[Header("Enemy Detection")]
	[Tooltip("If true, the script will check all BasicEnemyControls in the scene and only show the congratulations message when all enemies are dead. If false, it always shows the congratulations message.")]
	public bool requireAllEnemiesDead = true;

	[Header("Failure Behavior")]
	[Tooltip("If true and the timer expires without meeting the objective, all Player1Controls instances will be disabled to stop player action.")]
	public bool disablePlayerOnFail = true;

	[Tooltip("If true, the game timeScale will be set to 0 when the popup is shown on failure (pauses physics and animations).")]
	public bool pauseGameOnFail = false;

	private float remaining;
	private Coroutine runningCoroutine;
	private bool levelComplete = false;
	private bool waitingForInput = false;

	// Optional reference to the player to monitor death
	public Player1Controls player;


	void Awake()
	{
		remaining = duration;
		if (popupPanel != null)
			popupPanel.SetActive(false);
	}

	void Start()
	{
		// Try to find player if not assigned and subscribe to health changes
		if (player == null)
			player = FindObjectOfType<Player1Controls>();

		if (player != null)
		{
			player.OnHealthChanged += OnPlayerHealthChanged;
			// If player is already dead at start, treat it as death
			if (player.IsDead())
				HandlePlayerDeath();
		}

		if (startOnAwake)
			StartTimer();
		UpdateTimerText(remaining);
	}

	void Update()
	{
		// Check for spacebar input when waiting to go to next level
		if (waitingForInput && Input.GetKeyDown(KeyCode.Space))
		{
			LoadNextLevel();
		}

		// Also check if all enemies are dead during gameplay
		if (!levelComplete && runningCoroutine != null)
		{
			CheckForLevelCompletion();
		}
	}

	public void StartTimer()
	{
		if (runningCoroutine != null)
			StopCoroutine(runningCoroutine);
		remaining = duration;
		runningCoroutine = StartCoroutine(Countdown());
	}

	public void StopTimer()
	{
		if (runningCoroutine != null)
		{
			StopCoroutine(runningCoroutine);
			runningCoroutine = null;
		}
	}

	private IEnumerator Countdown()
	{
		while (remaining > 0f)
		{
			remaining -= Time.deltaTime;
			if (remaining < 0f) remaining = 0f;
			UpdateTimerText(remaining);
			yield return null;
		}

		runningCoroutine = null;
		OnTimerExpired();
	}

	private void UpdateTimerText(float t)
	{
		if (timerText == null)
			return;

		// Format as mm:ss or ss depending on length
		int seconds = Mathf.CeilToInt(t);
		int mins = seconds / 60;
		int secs = seconds % 60;
		if (mins > 0)
			timerText.text = string.Format("Timer: {0:00}:{1:00}", mins, secs);
		else
			timerText.text = string.Format("{0:00}", secs);
	}

	private void CheckForLevelCompletion()
	{
		if (levelComplete) return;

		// Check if all enemies are dead
		var enemies = FindObjectsOfType<BasicEnemyControls>();
		bool allDead = true;

		foreach (var e in enemies)
		{
			if (e != null && !e.IsDead())
			{
				allDead = false;
				break;
			}
		}

		if (allDead && enemies.Length > 0)
		{
			OnLevelComplete();
		}
	}

	private void OnLevelComplete()
	{
		levelComplete = true;
		waitingForInput = true;
		StopTimer();

		// Show congratulations message with spacebar prompt
		ShowPopup(congratulationsMessage);
		
		Debug.Log("All enemies cleared! Press SPACE to go to next level.");
	}

	private void OnTimerExpired()
	{
		// Determine enemy status
		bool allDead = true;
		int remainingEnemies = 0;

		if (requireAllEnemiesDead)
		{
			// BasicEnemyControls is used in this project for enemy logic
			var enemies = FindObjectsOfType<BasicEnemyControls>();
			foreach (var e in enemies)
			{
				if (e != null && !e.IsDead())
				{
					allDead = false;
					remainingEnemies++;
				}
			}
		}


		// Decide message
		if (!requireAllEnemiesDead || allDead)
		{
			OnLevelComplete();
		}
		else
		{
			string msg = string.Format("{0} {1} enemies remain", timeUpMessage, remainingEnemies);
			ShowPopup(msg);

			// Failure behavior: disable player controls and optionally pause game
			if (disablePlayerOnFail)
			{
				var players = FindObjectsOfType<Player1Controls>();
				foreach (var p in players)
				{
					if (p != null)
						p.enabled = false;
				}
			}

			if (pauseGameOnFail)
			{
				Time.timeScale = 0f;
			}
		}
	}

	private void OnPlayerHealthChanged(int current, int max)
	{
		if (current <= 0)
		{
			HandlePlayerDeath();
		}
	}

	private void HandlePlayerDeath()
	{
		// Stop the timer if running
		StopTimer();

		// Show death popup
		ShowPopup(youDiedMessage);

		// Optionally pause the game if configured (match failure behavior)
		if (pauseGameOnFail)
		{
			Time.timeScale = 0f;
		}
	}

	void OnDestroy()
	{
		if (player != null)
		{
			player.OnHealthChanged -= OnPlayerHealthChanged;
		}
	}

	private void ShowPopup(string message)
	{
		if (popupPanel != null)
		{
			if (popupText != null)
				popupText.text = message;
			popupPanel.SetActive(true);
		}
		else
		{
			Debug.Log(message);
		}
	}

	/// <summary>
	/// Re-enable player controls and unpause game if previously paused by this timer.
	/// </summary>
	public void RestorePlayerControl()
	{
		var players = FindObjectsOfType<Player1Controls>();
		foreach (var p in players)
		{
			if (p != null)
				p.enabled = true;
		}

		if (pauseGameOnFail && Time.timeScale == 0f)
		{
			Time.timeScale = 1f;
		}
	}

	/// <summary>
	/// Add bonus time to the timer (e.g., when an enemy dies)
	/// </summary>
	/// <param name="seconds">Amount of seconds to add</param>
	public void AddTime(float seconds)
	{
		remaining += seconds;
		
		// Don't exceed initial duration (optional - remove this check if you want unlimited time stacking)
		// remaining = Mathf.Min(remaining, duration);
		
		UpdateTimerText(remaining);
		Debug.Log($"Timer: Added {seconds} seconds! New time: {remaining:F1}s");
	}

	/// <summary>
	/// Load the next level/scene
	/// </summary>
	private void LoadNextLevel()
	{
		Debug.Log("Loading next level...");
		
		// Ensure time is running normally
		Time.timeScale = 1f;
		
		if (!string.IsNullOrEmpty(nextSceneName))
		{
			// Load specific scene by name
			SceneManager.LoadScene(nextSceneName);
		}
		else if (useNextBuildIndex)
		{
			// Load next scene in build index
			int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
			int nextSceneIndex = currentSceneIndex + 1;
			
			// Check if next scene exists
			if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
			{
				SceneManager.LoadScene(nextSceneIndex);
			}
			else
			{
				Debug.LogWarning("No next scene found in build settings. Reloading current scene.");
				SceneManager.LoadScene(currentSceneIndex);
			}
		}
		else
		{
			Debug.LogError("No next scene specified! Set nextSceneName or enable useNextBuildIndex.");
		}
	}

	/// <summary>
	/// Public method to manually trigger level completion (useful for testing or alternative win conditions)
	/// </summary>
	public void TriggerLevelComplete()
	{
		OnLevelComplete();
	}
}
