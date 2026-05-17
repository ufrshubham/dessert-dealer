using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Drives the scripted end-of-day deal cutscene.
/// Place this on a DealManager GameObject in DealScene.
/// </summary>
public class DealCutsceneController : MonoBehaviour
{
    // ── Inspector wiring ──────────────────────────────────────────────────────

    [Header("Rats")]
    [Tooltip("Animator on the Player rat in this scene")]
    [SerializeField] private Animator playerRatAnimator;
    [Tooltip("Transform of the Player rat (used for movement)")]
    [SerializeField] private Transform playerRatTransform;
    [Tooltip("Animator on the Boss rat in this scene")]
    [SerializeField] private Animator bossRatAnimator;

    [Header("Walk Target")]
    [Tooltip("Empty GameObject the player rat walks towards before dialogue begins")]
    [SerializeField] private Transform walkDestination;
    [SerializeField] private float walkSpeed = 2f;
    [Tooltip("How close (units) the player rat needs to be before stopping")]
    [SerializeField] private float arrivalThreshold = 0.15f;

    [Header("Dialogue")]
    [SerializeField] private DialoguePanel dialoguePanel;

    [Tooltip("Lines spoken during the deal. Use {items} and {money} as runtime tokens.")]
    [SerializeField] private DialogueLine[] dialogueLines = new DialogueLine[]
    {
        new DialogueLine { speakerName = "Boss Rat",  text = "Back already? Let's see what you've got." },
        new DialogueLine { speakerName = "Player",    text = "I grabbed {items} desserts tonight, boss." },
        new DialogueLine { speakerName = "Boss Rat",  text = "Not bad for a rat your size. Here's your cut \u2014 {money} coins." },
        new DialogueLine { speakerName = "Boss Rat",  text = "Now get some rest. Tomorrow the targets go up." },
    };

    [Header("Timing")]
    [Tooltip("Pause in seconds before the player rat starts walking")]
    [SerializeField] private float introDelay = 1f;
    [Tooltip("Pause in seconds between the last dialogue line and scene transition")]
    [SerializeField] private float outroDelay = 0.5f;

    // ── Animator parameter name (must match the Rat.controller) ───────────────
    private static readonly int MovementParam = Animator.StringToHash("movement");

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        StartCoroutine(RunCutscene());
    }

    private IEnumerator RunCutscene()
    {
        // Cache tokens before GameManager resets on AdvanceDay
        int itemsDelivered = GameManager.Instance.GetItemsCollectedToday();
        int moneyEarned    = GameManager.Instance.GetDealMoneyEarned();

        // Disable RatController so it doesn't overwrite the animator parameter
        if (playerRatTransform != null)
        {
            var ratController = playerRatTransform.GetComponent<RatController>();
            if (ratController != null) ratController.enabled = false;
        }

        // ── 1. Both rats idle on entry ────────────────────────────────────────
        SetMovement(playerRatAnimator, 0f);
        SetMovement(bossRatAnimator,   0f);

        yield return new WaitForSeconds(introDelay);

        // ── 2. Player rat walks toward boss ───────────────────────────────────
        if (walkDestination != null && playerRatTransform != null)
        {
            SetMovement(playerRatAnimator, 1f);

            while (Vector3.Distance(playerRatTransform.position, walkDestination.position) > arrivalThreshold)
            {
                Vector3 direction = (walkDestination.position - playerRatTransform.position).normalized;
                playerRatTransform.position += direction * walkSpeed * Time.deltaTime;

                // Rotate to face direction of travel (Y-axis only)
                if (direction != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.z));
                    playerRatTransform.rotation = Quaternion.RotateTowards(
                        playerRatTransform.rotation, targetRot, 360f * Time.deltaTime);
                }

                yield return null;
            }

            // Snap to exact position and face the boss
            playerRatTransform.position = walkDestination.position;
            FaceTarget(playerRatTransform, bossRatAnimator != null
                ? bossRatAnimator.transform
                : null);
        }

        SetMovement(playerRatAnimator, 0f);

        yield return new WaitForSeconds(0.3f);

        // ── 3. Dialogue ───────────────────────────────────────────────────────
        foreach (DialogueLine line in dialogueLines)
        {
            string resolvedText = line.text
                .Replace("{items}",  itemsDelivered.ToString())
                .Replace("{money}",  moneyEarned.ToString());

            dialoguePanel.ShowLine(line.speakerName, resolvedText);
            yield return StartCoroutine(dialoguePanel.WaitForInput());
        }

        dialoguePanel.Hide();

        // ── 4. Outro pause, then advance day ─────────────────────────────────
        yield return new WaitForSeconds(outroDelay);

        GameManager.Instance.OnCutsceneFinished();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void SetMovement(Animator animator, float value)
    {
        if (animator != null)
            animator.SetFloat(MovementParam, value);
    }

    private static void FaceTarget(Transform source, Transform target)
    {
        if (source == null || target == null) return;

        Vector3 dir = target.position - source.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            source.rotation = Quaternion.LookRotation(dir);
    }
}

// ── Data ──────────────────────────────────────────────────────────────────────

[Serializable]
public class DialogueLine
{
    public string speakerName;
    [TextArea(2, 4)]
    public string text;
}
