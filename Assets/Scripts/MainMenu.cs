// ============= FOR MAIN MENU =============
// ========= PLAYER PRESSES 'SPACE' BUTTON TO START GAME =============

using UnityEngine;
using TMPro;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    [Header("Text Settings")]
    public float flashSpeed = 1.5f;
    public string cutsceneSceneName = "Intro_Cutscene"; // Name of scene it's going to transition to
    public float fadeOutTime = 1f;
    public string transitionName = "CrossFade"; // Name of transition

    private TMP_Text pressSpaceText;
    private bool isFlashing = true;
    private float timer;
    private bool transitionStarted = false;

    void Start()
    {
        // Get the TMP_Text component
        pressSpaceText = GetComponent<TMP_Text>();

        if (pressSpaceText == null)
        {
            Debug.LogError("No TMP_Text component found on " + gameObject.name);
            return;
        }

        pressSpaceText.text = "press 'space'";
        timer = 0f;

        Debug.Log("MainMenu started successfully");
    }

    void Update()
    {
        if (transitionStarted || pressSpaceText == null) return;

        // Flashing effect
        if (isFlashing)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.PingPong(timer * flashSpeed, 1f);
            Color newColor = pressSpaceText.color;
            newColor.a = alpha;
            pressSpaceText.color = newColor;
        }

        // Spacebar detection
        if (Input.GetKeyDown(KeyCode.Space) && isFlashing)
        {
            Debug.Log("Spacebar pressed Transitioning to cutscene...");
            StartCoroutine(TransitionToCutscene());
        }
    }

    IEnumerator TransitionToCutscene()
    {
        transitionStarted = true;
        Debug.Log("Starting transition to cutscene...");

        // Stop flashing and make text solid
        isFlashing = false;
        pressSpaceText.color = new Color(pressSpaceText.color.r, pressSpaceText.color.g, pressSpaceText.color.b, 1f);

        // Fade out text
        float elapsedTime = 0f;
        Color startColor = pressSpaceText.color;

        while (elapsedTime < fadeOutTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutTime);
            pressSpaceText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        Debug.Log("Loading scene: " + cutsceneSceneName + " with transition: " + transitionName);

        // Use LevelManager instead of direct SceneManager call
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadScene(cutsceneSceneName, transitionName);
        }
        else
        {
            Debug.LogError("LevelManager Instance is null! Falling back to direct load.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(cutsceneSceneName);
        }
    }
}