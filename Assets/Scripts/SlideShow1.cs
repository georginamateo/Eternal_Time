using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SlideShow1 : MonoBehaviour
{
    [Header("Slideshow Settings")]
    public List<Sprite> slides;
    public float fadeInTime = 2f;
    public float displayTime = 3f;
    public float fadeOutTime = 2f;
    public bool autoStart = true;
    public string nextSceneName = "Intro_Dialogue";

    [Header("Display Settings")]
    public Image slideDisplay;
    public Vector2 maxSlideSize = new Vector2(880, 480); // Max dimensions

    private int currentSlideIndex = 0;
    private bool isPlaying = false;

    void Start()
    {
        if (slideDisplay != null)
        {
            slideDisplay.color = new Color(1, 1, 1, 0);
            slideDisplay.gameObject.SetActive(true);
        }

        if (autoStart && slides != null && slides.Count > 0)
        {
            StartSlideshow();
        }
    }

    void Update()
    {
        if (isPlaying && (Input.GetKeyDown(KeyCode.Space)))
        {
            SkipSlideshow();
        }
    }

    public void StartSlideshow()
    {
        if (slides == null || slides.Count == 0)
        {
            Debug.LogError("No slides assigned to slideshow!");
            return;
        }

        isPlaying = true;
        currentSlideIndex = 0;
        StartCoroutine(PlaySlideshow());
    }

    private IEnumerator PlaySlideshow()
    {
        while (currentSlideIndex < slides.Count)
        {
            // Set the current slide and adjust size
            slideDisplay.sprite = slides[currentSlideIndex];
            ResizeImageToFit(slideDisplay, maxSlideSize);

            // Fade in
            yield return StartCoroutine(FadeSlide(0f, 1f, fadeInTime));

            // Display
            yield return new WaitForSeconds(displayTime);

            // Fade out (except last slide)
            if (currentSlideIndex < slides.Count - 1)
            {
                yield return StartCoroutine(FadeSlide(1f, 0f, fadeOutTime));
            }

            currentSlideIndex++;
        }

        EndSlideshow();
    }

    // Automatically resize image to fit within max dimensions while preserving aspect ratio
    private void ResizeImageToFit(Image image, Vector2 maxSize)
    {
        if (image.sprite == null) return;

        Sprite sprite = image.sprite;
        float spriteWidth = sprite.rect.width;
        float spriteHeight = sprite.rect.height;
        float aspectRatio = spriteWidth / spriteHeight;

        Vector2 newSize = maxSize;

        // Adjust size to maintain aspect ratio
        if (spriteWidth > spriteHeight)
        {
            // Landscape image
            newSize.y = maxSize.x / aspectRatio;
            if (newSize.y > maxSize.y)
            {
                newSize.y = maxSize.y;
                newSize.x = maxSize.y * aspectRatio;
            }
        }
        else
        {
            // Portrait image
            newSize.x = maxSize.y * aspectRatio;
            if (newSize.x > maxSize.x)
            {
                newSize.x = maxSize.x;
                newSize.y = maxSize.x / aspectRatio;
            }
        }

        // Apply the calculated size
        RectTransform rectTransform = image.GetComponent<RectTransform>();
        rectTransform.sizeDelta = newSize;
    }

    private IEnumerator FadeSlide(float fromAlpha, float toAlpha, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(fromAlpha, toAlpha, elapsedTime / duration);
            slideDisplay.color = new Color(1, 1, 1, currentAlpha);
            yield return null;
        }

        slideDisplay.color = new Color(1, 1, 1, toAlpha);
    }

    public void SkipSlideshow()
    {
        StopAllCoroutines();
        EndSlideshow();
    }

    private void EndSlideshow()
    {
        isPlaying = false;

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.LoadScene(nextSceneName, "CrossFade");
            }
            else
            {
                SceneManager.LoadScene(nextSceneName);
            }
        }
    }
}