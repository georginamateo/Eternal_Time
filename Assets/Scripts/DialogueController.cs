using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class DialogueController : MonoBehaviour
{
    [System.Serializable]
    public class DialogueEntry
    {
        public Sprite characterImage;      // Character portrait
        public Sprite sceneImage;          // Scene drawing 
        public bool isOnRightSide;         // For character images
        public string characterName;
        public string dialogueText;
        public float displayTime = 3f;
        public bool changeSceneImage = false; // Flag to change scene drawing
    }

    [Header("Dialogue Sequence")]
    public List<DialogueEntry> dialogues;

    [Header("UI References")]
    public Image leftCharacterImage;       // For character on left
    public Image rightCharacterImage;      // For character on right
    public Image sceneDisplayImage;        // For scene drawings 
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI characterNameText;
    public GameObject dialoguePanel;

    [Header("Settings")]
    public float typewriterSpeed = 0.05f;
    public float fadeTime = 1f;
    public string nextSceneName = "GameScene";

    private int currentDialogueIndex = 0;
    private bool isPlaying = false;
    private bool isTyping = false;

    void Start()
    {
        // Initialize UI - hide everything initially
        leftCharacterImage.color = new Color(1, 1, 1, 0);
        rightCharacterImage.color = new Color(1, 1, 1, 0);
        sceneDisplayImage.color = new Color(1, 1, 1, 0); // Start hidden too
        dialogueText.text = "";
        characterNameText.text = "";

        StartDialogue();
    }

    public void StartDialogue()
    {
        if (dialogues == null || dialogues.Count == 0)
        {
            Debug.LogError("No dialogues assigned!");
            return;
        }

        isPlaying = true;
        currentDialogueIndex = 0;
        StartCoroutine(PlayDialogue());
    }

    private IEnumerator PlayDialogue()
    {
        while (currentDialogueIndex < dialogues.Count)
        {
            DialogueEntry currentDialogue = dialogues[currentDialogueIndex];

            // Handle scene drawing changes 
            if (currentDialogue.changeSceneImage && currentDialogue.sceneImage != null)
            {
                // When showing a scene drawing, hide characters
                yield return StartCoroutine(HideCharacter(leftCharacterImage));
                yield return StartCoroutine(HideCharacter(rightCharacterImage));
                yield return StartCoroutine(ChangeSceneImage(currentDialogue.sceneImage));
            }
            else if (currentDialogue.characterImage != null)
            {
                // When showing a character, hide the scene drawing
                yield return StartCoroutine(HideSceneImage());

                // Show the character
                if (currentDialogue.isOnRightSide)
                {
                    yield return StartCoroutine(ShowCharacter(rightCharacterImage, currentDialogue.characterImage));
                    yield return StartCoroutine(HideCharacter(leftCharacterImage));
                }
                else
                {
                    yield return StartCoroutine(ShowCharacter(leftCharacterImage, currentDialogue.characterImage));
                    yield return StartCoroutine(HideCharacter(rightCharacterImage));
                }
            }
            else
            {
                // No image specified - hide everything
                yield return StartCoroutine(HideSceneImage());
                yield return StartCoroutine(HideCharacter(leftCharacterImage));
                yield return StartCoroutine(HideCharacter(rightCharacterImage));
            }

            // Display character name and typewriter text
            characterNameText.text = currentDialogue.characterName;
            yield return StartCoroutine(TypewriterEffect(currentDialogue.dialogueText));

            isTyping = false;

            // Wait for display time or click to continue
            float timer = 0f;
            while (timer < currentDialogue.displayTime && !Input.GetMouseButtonDown(0))
            {
                timer += Time.deltaTime;
                yield return null;
            }

            // Clear text
            dialogueText.text = "";
            characterNameText.text = "";

            currentDialogueIndex++;
        }

        // End of dialogue
        yield return StartCoroutine(EndDialogue());
    }

    private IEnumerator ChangeSceneImage(Sprite newScene)
    {
        // If there's a current scene, fade it out first
        if (sceneDisplayImage.sprite != null)
        {
            yield return StartCoroutine(FadeImage(sceneDisplayImage, 1f, 0f, fadeTime));
        }

        // Change the image
        sceneDisplayImage.sprite = newScene;

        // Fade in new scene
        yield return StartCoroutine(FadeImage(sceneDisplayImage, 0f, 1f, fadeTime));
    }

    private IEnumerator HideSceneImage()
    {
        if (sceneDisplayImage.sprite != null)
        {
            yield return StartCoroutine(FadeImage(sceneDisplayImage, 1f, 0f, fadeTime));
        }
    }

    private IEnumerator ShowCharacter(Image characterSlot, Sprite characterImage)
    {
        characterSlot.sprite = characterImage;

        float elapsedTime = 0f;
        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeTime);
            characterSlot.color = new Color(1, 1, 1, alpha);
            yield return null;
        }
    }

    private IEnumerator HideCharacter(Image characterSlot)
    {
        float elapsedTime = 0f;
        Color startColor = characterSlot.color;

        while (elapsedTime < fadeTime && startColor.a > 0)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsedTime / fadeTime);
            characterSlot.color = new Color(1, 1, 1, alpha);
            yield return null;
        }

        characterSlot.color = new Color(1, 1, 1, 0);
    }

    private IEnumerator FadeImage(Image image, float fromAlpha, float toAlpha, float duration)
    {
        float elapsedTime = 0f;
        Color startColor = image.color;
        Color targetColor = new Color(1, 1, 1, toAlpha);

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            image.color = Color.Lerp(startColor, targetColor, elapsedTime / duration);
            yield return null;
        }

        image.color = targetColor;
    }

    private IEnumerator TypewriterEffect(string text)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char letter in text.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typewriterSpeed);

            if (Input.GetMouseButtonDown(0))
            {
                dialogueText.text = text;
                break;
            }
        }

        isTyping = false;
    }

    private IEnumerator EndDialogue()
    {
        // Fade out everything
        yield return StartCoroutine(HideSceneImage());
        yield return StartCoroutine(HideCharacter(leftCharacterImage));
        yield return StartCoroutine(HideCharacter(rightCharacterImage));

        // Fade out text
        float elapsedTime = 0f;
        Color startColor = dialogueText.color;

        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeTime);
            dialogueText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            characterNameText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        // Transition to next scene
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadScene(nextSceneName, "CrossFade");
        }
        else
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    void Update()
    {
        
    }
}