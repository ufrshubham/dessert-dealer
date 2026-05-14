using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class DessertIndicator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform arrowRectTransform;
    [SerializeField] private Camera mainCamera;

    [Header("Settings")]
    [SerializeField] private float spriteRotationOffset = 90f;

    public Transform dessertToLocate;
    public List<Transform> desserts = new();

    private void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        InvokeRepeating(nameof(CheckPickup), 0f, 0.01f);
    }

    void CheckPickup()
    {
        if (arrowRectTransform == null) return;
        if (dessertToLocate == null)
        {
            arrowRectTransform.gameObject.SetActive(false);
            return;
        }

        arrowRectTransform.gameObject.SetActive(true);
        // 1. Get target screen position
        Vector3 screenPos = mainCamera.WorldToScreenPoint(dessertToLocate.position);

        // 2. Handle objects behind the camera
        if (screenPos.z < 0)
        {
            screenPos *= -1;
        }

        // 3. THE FIX: Always use the screen's center as the origin point
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

        // Calculate direction from the Center of the screen to the target
        Vector2 direction = (Vector2)screenPos - screenCenter;

        // 4. Calculate angle
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // 5. Rotate the UI element safely
        arrowRectTransform.rotation = Quaternion.Euler(0, 0, angle - spriteRotationOffset);
    }
}