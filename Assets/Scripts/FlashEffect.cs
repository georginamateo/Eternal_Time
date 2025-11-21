using UnityEngine;
using System.Collections;

public class FlashEffect : MonoBehaviour
{
    [Header("Flash Settings")]
    public float flashDuration = 0.2f;     // How long the flash lasts
    public Color flashColor = Color.white; // Color to flash (white)

    private Material[] originalMaterials;
    private Material[] flashMaterials;
    private Renderer[] renderers;
    private bool isFlashing = false;

    void Start()
    {
        // Get all renderers in this object and its children
        renderers = GetComponentsInChildren<Renderer>();

        // Store original materials and create flash materials
        originalMaterials = new Material[renderers.Length];
        flashMaterials = new Material[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].material;

            // Create a new material for flashing (using a simple unlit shader)
            flashMaterials[i] = new Material(Shader.Find("Unlit/Color"));
            flashMaterials[i].color = flashColor;
        }
    }

    /// Trigger the flash effect
    public void Flash()
    {
        if (!isFlashing)
        {
            StartCoroutine(FlashRoutine());
        }
    }

    private IEnumerator FlashRoutine()
    {
        isFlashing = true;

        // Switch to flash materials
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].material = flashMaterials[i];
        }

        // Wait for flash duration
        yield return new WaitForSeconds(flashDuration);

        // Switch back to original materials
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && originalMaterials[i] != null)
                renderers[i].material = originalMaterials[i];
        }

        isFlashing = false;
    }

    void OnDestroy()
    {
        // Clean up created materials to prevent memory leaks
        if (flashMaterials != null)
        {
            foreach (Material mat in flashMaterials)
            {
                if (mat != null)
                    DestroyImmediate(mat);
            }
        }
    }
}