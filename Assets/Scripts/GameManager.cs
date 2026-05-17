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
    [HideInInspector] [SerializeField] private string gameplaySceneName = "Gameplay";
    [HideInInspector] [SerializeField] private string dealSceneName     = "DealScene";

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (gameplaySceneAsset != null)
            gameplaySceneName = gameplaySceneAsset.name;
        else
            Debug.LogWarning("[GameManager] Gameplay scene not assigned!", this);

        if (dealSceneAsset != null)
            dealSceneName = dealSceneAsset.name;
        else
            Debug.LogWarning("[GameManager] Deal scene not assigned!", this);
    }
#endif

    [Header("Day Progression")]
    [SerializeField] private int currentDay = 1;
    [SerializeField] private int maxDays = 7;

    [Header("Per-Day Collection Target")]
    [Tooltip("Set exactly how many collectibles are needed each day. " +
         "Last value repeats if days exceed array length.")]
    [SerializeField] private int[] dailyTargets = { 1, 2, 3, 4, 5, 6, 7 };

    [Header("Game State")]
    public bool isGameOver = false;
    public bool isGameWon = false;
    public int totalScore = 0;

    // ── Runtime tracking ────────────────────────────────────────────────────────
    private int itemsCollectedToday = 0;
    private int targetForToday = 0;
    private bool deliveryReady = false;  // true once today's target is met

    [Header("Day Timer")]
    [Tooltip("Time in seconds for each day. Last value repeats if days exceed array length.")]
    [SerializeField] private float[] dayDurations = { 120f, 100f, 90f, 80f, 70f, 60f, 50f };
    [SerializeField] private bool timerEnabled = true;

    private float timeRemaining;
    private float dayDuration;
    private bool timerRunning = false;

    // Events
    public UnityEvent<float> onTimerTick;      // fires every frame with remaining time
    public UnityEvent onTimerExpired;  

    [Header("Events")]
    public UnityEvent onItemCollected;
    public UnityEvent onDayTargetReached;
    public UnityEvent onDayComplete;
    public UnityEvent<int> onDayChanged;
    public UnityEvent onGameWon;
    public UnityEvent onGameOver;

    public Action OnMouseTrapTriggered;
    public Action OnCaughtByCat;

    [HideInInspector] public bool isReturningFromCutscene = false;
    [HideInInspector] public bool showDayCompleteOnReturn  = false;

    public enum GameState { Menu, Playing, Won, Lost }
    public GameState currentState { get; private set; } = GameState.Menu;

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
        // StartDay(currentDay);
    }

    private void Update()
    {
        if (!timerRunning || isGameOver) return;

        timeRemaining -= Time.deltaTime;
        onTimerTick?.Invoke(timeRemaining);

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            timerRunning = false;
            Debug.Log("[GameManager] Time's up!");
            onTimerExpired?.Invoke();
            TriggerGameOver();
        }
    }

    public void StartGame()
    {
        currentState = GameState.Playing;
        isGameOver = false;
        isGameWon  = false;
        totalScore = 0;
        StartDay(1);
    }

    private void StartDay(int day)
    {
        currentDay = day;
        itemsCollectedToday = 0;
        deliveryReady = false;

        // Target scales up each day
        // Clamp to array length — last value repeats if day exceeds array
        int index = Mathf.Clamp(day - 1, 0, dailyTargets.Length - 1);
        targetForToday = dailyTargets[index];

        // Start timer
        dayDuration   = GetDurationForDay(day);
        timeRemaining = dayDuration;
        timerRunning  = timerEnabled;

        Debug.Log($"[GameManager] ── Day {currentDay} started. Collect {targetForToday} items. Time: {dayDuration}s");
        onDayChanged?.Invoke(currentDay);
    }

    private float GetDurationForDay(int day)
    {
        int index = Mathf.Clamp(day - 1, 0, dayDurations.Length - 1);
        return dayDurations[index];
    }


    // ─── Public API (called by Player or Trigger scripts) ───────────────────────

    /// <summary>
    /// Call this when the player picks up a collectible.
    /// </summary>
    public void CollectItem()
    {
        if (isGameOver || deliveryReady) return;

        itemsCollectedToday++;
        totalScore += 10;

        deliveryReady = true;
        Debug.Log($"[GameManager] Collected {itemsCollectedToday}/{targetForToday}. Go deliver!");
        onItemCollected?.Invoke();

        onDayTargetReached?.Invoke();
    }

    /// <summary>
    /// Call this when the player enters a delivery zone while carrying items.
    /// </summary>
    public void DeliverItems()
    {
        if (isGameOver || !deliveryReady) return;

        timerRunning = false;
        deliveryReady = false;

        int timeBonus  = Mathf.RoundToInt(timeRemaining) * 2;
        totalScore    += 50 + timeBonus;

        Debug.Log($"[GameManager] Delivered {itemsCollectedToday}/{targetForToday}. Score: {totalScore}");
        onDayComplete?.Invoke();

        if (itemsCollectedToday >= targetForToday)
        {
            timerRunning = false;
            Debug.Log($"[GameManager] Day {currentDay} fully complete!");
            AdvanceDay();

            SceneManager.LoadScene(dealSceneName);
        }
        else
        {
            timerRunning = true;
            Debug.Log($"[GameManager] {targetForToday - itemsCollectedToday} more to deliver. Go collect!");
        }
    }

    /// <summary>
    /// Called by DealCutsceneController after the cutscene finishes.
    /// Advances the day and returns to the gameplay scene.
    /// </summary>
    public void OnCutsceneFinished()
    {
        if (isGameWon)
        {
            SceneManager.LoadScene("WinScreen");
        }
        else
        {
            isReturningFromCutscene = true; 
            showDayCompleteOnReturn = true;
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
        currentState = GameState.Won;
        timerRunning = false;
        Debug.Log("[GameManager] All days complete — YOU WIN!");
        onGameWon?.Invoke();
    }

    public void TriggerGameOver()
    {
        isGameOver = true;
        currentState = GameState.Lost;
        timerRunning = false;
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
    public float GetTimeRemaining()  => timeRemaining;
    public float GetDayDuration()    => dayDuration;
    public float GetTimeNormalized() => dayDuration > 0 ? timeRemaining / dayDuration : 0f;
    public string GetDealSceneName() => dealSceneName;
}