// ============ SCRIPT FOR MAIN MENU ============
// ======= MOVES CLOUDS ACROSS SCREEN =======

using UnityEngine;

public class CloudMovement : MonoBehaviour
{
    [Header("Cloud Movement Settings")]
    public float moveSpeed = 50f;

    private RectTransform rectTransform;
    private float cloudWidth;
    private float screenWidth = 1920f; // Your canvas width

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        cloudWidth = rectTransform.rect.width;
    }

    void Update()
    {
        // Move cloud to the RIGHT
        rectTransform.anchoredPosition += Vector2.right * moveSpeed * Time.deltaTime;

        // Check if THIS cloud has left the screen
        // Cloud is off-screen when its LEFT edge is past screen RIGHT edge
        if (rectTransform.anchoredPosition.x > screenWidth)
        {
            ResetCloudToLeft();
        }
    }

    void ResetCloudToLeft()
    {
        // Reset this cloud to left side (off-screen left)
        // Position it so its RIGHT edge is at screen LEFT edge
        float resetPositionX = -cloudWidth;
        rectTransform.anchoredPosition = new Vector2(resetPositionX, rectTransform.anchoredPosition.y);
    }
}