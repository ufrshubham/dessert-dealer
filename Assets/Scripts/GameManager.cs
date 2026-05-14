using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Scene Names")]
#if UNITY_EDITOR
    [SerializeField] private SceneAsset gameplaySceneAsset;
    [SerializeField] private SceneAsset dealSceneAsset;
#endif
    [HideInInspector] [SerializeField] private string gameplaySceneName = "SampleScene";
    [HideInInspector] [SerializeField] private string dealSceneName     = "DealScene";

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (gameplaySceneAsset != null) gameplaySceneName = gameplaySceneAsset.name;
        if (dealSceneAsset     != null) dealSceneName     = dealSceneAsset.name;
    }
#endif

    [Header("Day Progression")]
    [SerializeField] private int currentDay = 1;
    [SerializeField] private int maxDays = 7;

    [Header("Per-Day Collection Target")]
    [Tooltip("How many items must be collected each day before delivery is allowed")]
    [SerializeField] private int baseTargetPerDay = 1;
    [Tooltip("Each day adds this many more required items (set 0 for flat difficulty)")]
    [SerializeField] private int targetIncreasePerDay = 1;

    [Header("Game State")]
    public bool isGameOver = false;
    public bool isGameWon = false;
    public int totalScore = 0;

    // ── Runtime tracking ────────────────────────────────────────────────────────
    private int itemsCollectedToday = 0;
    private int targetForToday = 0;
    private bool deliveryReady = false;  // true once today's target is met

    [Header("Events")]
    public UnityEvent onItemCollected;
    public UnityEvent onDayTargetReached;
    public UnityEvent onDayComplete;
    public UnityEvent<int> onDayChanged;
    public UnityEvent onGameWon;
    public UnityEvent onGameOver;

    public Action OnMouseTrapTriggered;
    public Action OnCaughtByCat;

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

    private void Start()
    {
        StartDay(currentDay);
    }

    private void StartDay(int day)
    {
        currentDay = day;
        itemsCollectedToday = 0;
        deliveryReady = false;

        // Target scales up each day
        targetForToday = baseTargetPerDay + (day - 1) * targetIncreasePerDay;

        Debug.Log($"[GameManager] ── Day {currentDay} started. Collect {targetForToday} items. ──");
        onDayChanged?.Invoke(currentDay);
    }


    // ─── Public API (called by Player or Trigger scripts) ───────────────────────

    /// <summary>
    /// Call this when the player picks up a collectible.
    /// </summary>
    public void CollectItem(GameObject item)
    {
        if (isGameOver || deliveryReady) return;

        itemsCollectedToday++;
        totalScore += 10;

        item.SetActive(false);

        Debug.Log($"[GameManager] Collected {itemsCollectedToday}/{targetForToday}");
        onItemCollected?.Invoke();

        if (itemsCollectedToday >= targetForToday)
        {
            deliveryReady = true;
            Debug.Log("[GameManager] Target reached! Head to the delivery zone.");
            onDayTargetReached?.Invoke();
        }
    }

    /// <summary>
    /// Call this when the player enters a delivery zone while carrying items.
    /// </summary>
    public void DeliverItems()
    {
        // if (isGameOver || !deliveryReady) return;

        totalScore += targetForToday * 50;         // delivery bonus
        Debug.Log($"[GameManager] Day {currentDay} complete! Score: {totalScore}");
        onDayComplete?.Invoke();

        SceneManager.LoadScene(dealSceneName);
    }

    /// <summary>
    /// Called by DealCutsceneController after the cutscene finishes.
    /// Advances the day and returns to the gameplay scene.
    /// </summary>
    public void CompleteDay()
    {
        AdvanceDay();

        if (isGameWon)
        {
            SceneManager.LoadScene(gameplaySceneName);
        }
    }

    /// <summary>
    /// Returns the money the player earned this delivery (used by the cutscene UI).
    /// Safe to call before AdvanceDay resets the counter.
    /// </summary>
    public int GetDealMoneyEarned() => targetForToday * 50;

    // ─── Day Advance / Win ────────────────────────────────────────────────────────

    private void AdvanceDay()
    {
        deliveryReady = false;
        if (currentDay >= maxDays)
        {
            TriggerWin();
            return;
        }

        StartDay(currentDay + 1);
    }

    private void TriggerWin()
    {
        isGameWon = true;
        isGameOver = true;
        Debug.Log("[GameManager] All days complete — YOU WIN!");
        onGameWon?.Invoke();
    }

    public void TriggerGameOver()
    {
        isGameOver = true;
        Debug.Log("[GameManager] Game Over.");
        onGameOver?.Invoke();
    }

    // ─── Getters (use in UI scripts) ─────────────────────────────────────────────

    public int GetCurrentDay()             => currentDay;
    public int GetMaxDays()                => maxDays;
    public int GetItemsCollectedToday()    => itemsCollectedToday;
    public int GetTargetForToday()         => targetForToday;
    public int GetScore()                  => totalScore;
    public bool IsDeliveryReady()          => deliveryReady;
}