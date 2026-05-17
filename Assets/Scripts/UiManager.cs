using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // ── Screen panels ────────────────────────────────────────────────────────

    [Header("Screens")]
    [SerializeField] private GameObject startMenuScreen;
    [SerializeField] private GameObject hudScreen;
    [SerializeField] private GameObject winScreen;
    [SerializeField] private GameObject loseScreen;
    [SerializeField] private GameObject dayCompleteScreen;
    

    // ── Start Menu ───────────────────────────────────────────────────────────

    [Header("Start Menu")]
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI gameTitleText;

    // ── HUD ──────────────────────────────────────────────────────────────────

    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI targetText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Image timerFillBar;
    [SerializeField] private Color timerNormalColor  = Color.white;
    [SerializeField] private Color timerUrgentColor  = Color.red;
    [SerializeField] private float timerUrgentThreshold = 20f;
    [SerializeField] private TextMeshProUGUI collectedText;  // "2 / 4 collected"

    [Header("Day Complete Screen")]
    [SerializeField] private TextMeshProUGUI dayCompleteTitle;
    [SerializeField] private TextMeshProUGUI dayCompleteScoreText;
    [SerializeField] private TextMeshProUGUI dayCompleteBonusText;
    [SerializeField] private TextMeshProUGUI dayCompleteTimeText;
    [SerializeField] private TextMeshProUGUI dayCompleteNextDayText;
    [SerializeField] private Button continueButton;

    [SerializeField] private Button dayMainMenuButton;

    // ── Win Screen ───────────────────────────────────────────────────────────

    [Header("Win Screen")]
    [SerializeField] private TextMeshProUGUI winTitleText;
    [SerializeField] private TextMeshProUGUI winScoreText;
    [SerializeField] private TextMeshProUGUI winDaysText;
    [SerializeField] private Button winRestartButton;
    [SerializeField] private Button winMainMenuButton;

    // ── Lose Screen ──────────────────────────────────────────────────────────

    [Header("Lose Screen")]
    [SerializeField] private TextMeshProUGUI loseTitleText;
    [SerializeField] private TextMeshProUGUI loseReasonText;
    [SerializeField] private TextMeshProUGUI loseScoreText;
    [SerializeField] private Button loseRestartButton;
    [SerializeField] private Button loseMainMenuButton;

    // ── Runtime ──────────────────────────────────────────────────────────────

    private GameManager gm;

    // ── Singleton ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        SetCursor(locked: false);
    }

    private void Start()
    {
        InputManager.Instance.inputActions.Player.Disable();
        gm = GameManager.Instance;

        // Wire buttons
        startButton?.onClick.AddListener(OnStartPressed);
        continueButton?.onClick.AddListener(OnContinuePressed);
        dayMainMenuButton?.onClick.AddListener(OnMainMenuPressed);
        winRestartButton?.onClick.AddListener(OnRestartPressed);
        winMainMenuButton?.onClick.AddListener(OnMainMenuPressed);
        loseRestartButton?.onClick.AddListener(OnRestartPressed);
        loseMainMenuButton?.onClick.AddListener(OnMainMenuPressed);

        // Wire GameManager events
        gm.onDayChanged.AddListener(OnDayChanged);
        gm.onDayComplete.AddListener(ShowDeliveryFeedback);
        gm.onTimerTick.AddListener(OnTimerTick);
        gm.onGameWon.AddListener(ShowWinScreen);
        gm.onGameOver.AddListener(ShowLoseScreen);
        gm.onItemCollected.AddListener(UpdateCollectedText);
        gm.onDayTargetReached.AddListener(UpdateCollectedText);

        if (gm.isReturningFromCutscene)
        {
            gm.isReturningFromCutscene = false;

            if (gm.showDayCompleteOnReturn)
            {
                gm.showDayCompleteOnReturn = false;
                ShowDayCompleteScreen();
            }
            else
            {
                GoToHUD();
            }
        }
        else
        {
            ShowScreen(startMenuScreen);
            SetCursor(locked: false);
        }
    }

    private void RefreshHUD()
    {
        if (dayText      != null) dayText.text      = $"Day {gm.GetCurrentDay()} / {gm.GetMaxDays()}";
        if (targetText   != null) targetText.text   = $"Deliver: {gm.GetTargetForToday()}";
        if (scoreText    != null) scoreText.text    = $"Score: {gm.GetScore()}";
        if (collectedText != null) collectedText.text = $"0 / {gm.GetTargetForToday()} collected";

        float remaining = gm.GetTimeRemaining();
        int minutes = Mathf.FloorToInt(remaining / 60f);
        int seconds  = Mathf.FloorToInt(remaining % 60f);
        if (timerText    != null) timerText.text    = $"{minutes:00}:{seconds:00}";
        if (timerFillBar != null) timerFillBar.fillAmount = gm.GetTimeNormalized();
    }

    private void OnDestroy()
    {
        if (gm == null) return;
        gm.onDayChanged.RemoveListener(OnDayChanged);
        gm.onTimerTick.RemoveListener(OnTimerTick);
        gm.onGameWon.RemoveListener(ShowWinScreen);
        gm.onGameOver.RemoveListener(ShowLoseScreen);
        gm.onDayComplete.RemoveListener(ShowDayCompleteScreen);
        gm.onItemCollected.RemoveListener(UpdateCollectedText);
        gm.onDayTargetReached.RemoveListener(UpdateCollectedText);
    }

    // ── Screen management ────────────────────────────────────────────────────

    private void ShowScreen(GameObject screen)
    {
        startMenuScreen?.SetActive(false);
        hudScreen?.SetActive(false);
        winScreen?.SetActive(false);
        loseScreen?.SetActive(false);
        dayCompleteScreen?.SetActive(false);   // ← add this line

        screen?.SetActive(true);
    }

    // ── Start Menu ───────────────────────────────────────────────────────────

    private void OnStartPressed()
    {
        ShowScreen(hudScreen);
        SetCursor(locked: true);
        InputManager.Instance.inputActions.Player.Enable();
        gm.StartGame();   // see GameManager addition below
    }

    // ── HUD updates ──────────────────────────────────────────────────────────

    private void OnDayChanged(int day)
    {
        if (dayText != null)
            dayText.text = $"Day {day} / {gm.GetMaxDays()}";

        if (targetText != null)
            targetText.text = $"Deliver: {gm.GetTargetForToday()}";

        UpdateCollectedText();
        UpdateScoreText();
    }

    private void UpdateCollectedText()
    {
        if (collectedText != null)
            collectedText.text = $"{gm.GetItemsCollectedToday()} / {gm.GetTargetForToday()} collected";
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {gm.GetScore()}";
    }

    private void OnTimerTick(float remaining)
    {
        // Update score live
        UpdateScoreText();

        // Timer text MM:SS
        int minutes = Mathf.FloorToInt(remaining / 60f);
        int seconds = Mathf.FloorToInt(remaining % 60f);

        if (timerText != null)
        {
            timerText.text  = $"{minutes:00}:{seconds:00}";
            timerText.color = remaining <= timerUrgentThreshold
                            ? timerUrgentColor
                            : timerNormalColor;
        }

        // Fill bar
        if (timerFillBar != null)
            timerFillBar.fillAmount = gm.GetTimeNormalized();
    }

    private void ShowDeliveryFeedback()
    {
        if (gm.GetItemsCollectedToday() >= gm.GetTargetForToday())
            return;

        UpdateCollectedText();
        UpdateScoreText();

        Debug.Log($"[UI] Delivery {gm.GetItemsCollectedToday()}/{gm.GetTargetForToday()} done.");
    }

    private void ShowDayCompleteScreen()
    {
        InputManager.Instance.inputActions.Player.Disable();
        ShowScreen(dayCompleteScreen);
        SetCursor(locked: false);

        int completedDay = gm.GetCurrentDay() - 1; // day was already advanced
        int timeBonus    = Mathf.RoundToInt(gm.GetTimeRemaining()) * 2;
        int nextDay      = gm.GetCurrentDay();

        if (dayCompleteTitle    != null)
            dayCompleteTitle.text    = $"Day {completedDay} Complete!";

        if (dayCompleteScoreText != null)
            dayCompleteScoreText.text = $"Total Score: {gm.GetScore()}";

        if (dayCompleteBonusText != null)
            dayCompleteBonusText.text = $"Time Bonus: +{timeBonus}";

        if (dayCompleteTimeText  != null)
        {
            float remaining = gm.GetTimeRemaining();
            int minutes = Mathf.FloorToInt(remaining / 60f);
            int seconds  = Mathf.FloorToInt(remaining % 60f);
            dayCompleteTimeText.text = $"Time Left: {minutes:00}:{seconds:00}";
        }

        if (dayCompleteNextDayText != null)
        {
            if (gm.isGameWon)
                dayCompleteNextDayText.text = "Final Day Done!";
            else
                dayCompleteNextDayText.text = $"Next: Day {nextDay} of {gm.GetMaxDays()}";
        }
    }

    // ── Win Screen ───────────────────────────────────────────────────────────

    private void ShowWinScreen()
    {
        InputManager.Instance.inputActions.Player.Disable();
        ShowScreen(winScreen);
        SetCursor(locked: false); 

        if (winTitleText  != null) winTitleText.text  = "All Days Complete!";
        if (winScoreText  != null) winScoreText.text  = $"Final Score: {gm.GetScore()}";
        if (winDaysText   != null) winDaysText.text   = $"Days Completed: {gm.GetMaxDays()}";
    }


    // ── Lose Screen ──────────────────────────────────────────────────────────

    private void ShowLoseScreen()
    {
        InputManager.Instance.inputActions.Player.Disable();
        ShowScreen(loseScreen);
        SetCursor(locked: false); 

        if (loseTitleText  != null) loseTitleText.text  = "Time's Up!";
        if (loseReasonText != null) loseReasonText.text = $"You ran out of time on Day {gm.GetCurrentDay()}";
        if (loseScoreText  != null) loseScoreText.text  = $"Score: {gm.GetScore()}";
    }

    // ── Restart / Main Menu ───────────────────────────────────────────────────

    private void OnContinuePressed()
    {
        GoToHUD();
    }

    private void GoToHUD()
    {
        ShowScreen(hudScreen);
        SetCursor(locked: true);
        InputManager.Instance.inputActions.Player.Enable();
        RefreshHUD();
    }
    private void OnRestartPressed()
    {
        SetCursor(locked: false);
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    private void OnMainMenuPressed()
    {
        SetCursor(locked: false);
        UnityEngine.SceneManagement.SceneManager.LoadScene(0); // scene index 0 = main menu
    }

    private void SetCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible   = !locked;
    }
}