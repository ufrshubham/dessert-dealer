using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public bool isGameOver = false;
    public int score = 0;
    public int Days = 0;

    [Header("Collectible Settings")]
    [SerializeField] private string collectibleTag = "Collectible";

    [Header("Delivery Zone Settings")]
    [SerializeField] private string deliveryZoneTag = "DeliveryZone";

    [Header("Events")]
    public UnityEvent onItemCollected;
    public UnityEvent onItemDelivered;
    public UnityEvent onGameOver;

    // Tracks how many items the player is currently carrying
    private int itemsCarried = 0;
    private int totalDeliveries = 0;

    // ─── Singleton Setup ────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ─── Public API (called by Player or Trigger scripts) ───────────────────────

    /// <summary>
    /// Call this when the player picks up a collectible.
    /// </summary>
    public void CollectItem(GameObject item)
    {
        if (isGameOver) return;

        itemsCarried++;
        score += 10; // optional immediate reward

        Debug.Log($"[GameManager] Item collected. Carrying: {itemsCarried}");
        onItemCollected?.Invoke();

        // Deactivate or destroy the collected object
        item.SetActive(false);
        // Destroy(item); // use this instead if you prefer
    }

    /// <summary>
    /// Call this when the player enters a delivery zone while carrying items.
    /// </summary>
    public void DeliverItems()
    {
        if (isGameOver || itemsCarried == 0) return;

        totalDeliveries += itemsCarried;
        score += itemsCarried * 50; // bigger reward on delivery

        Debug.Log($"[GameManager] Delivered {itemsCarried} item(s). Total deliveries: {totalDeliveries}. Score: {score}");

        itemsCarried = 0;
        onItemDelivered?.Invoke();

        CheckWinCondition();
    }

    // ─── Win / Lose ──────────────────────────────────────────────────────────────

    [Header("Win Condition")]
    [SerializeField] private int requiredDeliveries = 5;

    private void CheckWinCondition()
    {
        if (totalDeliveries >= requiredDeliveries)
        {
            Debug.Log("[GameManager] You Win!");
            TriggerGameOver();
        }
    }

    public void TriggerGameOver()
    {
        isGameOver = true;
        onGameOver?.Invoke();
    }

    // ─── Getters ─────────────────────────────────────────────────────────────────

    public int GetItemsCarried() => itemsCarried;
    public int GetScore() => score;
    public int GetTotalDeliveries() => totalDeliveries;
}