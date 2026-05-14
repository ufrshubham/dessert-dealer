using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages the dialogue UI panel shown during the end-of-day deal cutscene.
/// Assign in the DealScene Canvas hierarchy.
/// </summary>
public class DialoguePanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_Text continueHintText;

    [Header("Settings")]
    [Tooltip("Text shown at the bottom of the panel prompting the player to advance")]
    [SerializeField] private string continueHint = "[ E ] to continue";

    [Tooltip("Per-speaker name colours. Any speaker not listed uses the default text colour.")]
    [SerializeField]
    private SpeakerColor[] speakerColors = new SpeakerColor[]
    {
        new() { speakerName = "Player",   color = new Color(0.4f, 0.9f, 0.4f) },
        new() { speakerName = "Boss Rat", color = new Color(1.0f, 0.6f, 0.2f) },
    };

    private void Awake()
    {
        if (continueHintText != null)
            continueHintText.text = continueHint;

        Hide();
    }

    /// <summary>
    /// Shows the panel and fills it with the given speaker name and dialogue text.
    /// </summary>
    public void ShowLine(string speaker, string text)
    {
        if (speakerNameText != null)
        {
            speakerNameText.text = speaker;
            speakerNameText.color = GetSpeakerColor(speaker);
        }
        if (dialogueText != null) dialogueText.text = text;

        if (panelRoot != null) panelRoot.SetActive(true);
    }

    private Color GetSpeakerColor(string speaker)
    {
        foreach (SpeakerColor entry in speakerColors)
        {
            if (string.Equals(entry.speakerName, speaker, StringComparison.OrdinalIgnoreCase))
                return entry.color;
        }
        return speakerNameText != null ? speakerNameText.color : Color.white;
    }

    /// <summary>
    /// Hides the dialogue panel.
    /// </summary>
    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    /// <summary>
    /// Coroutine that waits until the player presses Space or clicks the mouse.
    /// Yield this from DealCutsceneController to pause between lines.
    /// </summary>
    public IEnumerator WaitForInput()
    {
        // Skip a frame so the key that opened this line doesn't immediately close it
        yield return null;

        while (true)
        {
            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;

            // Use raw key polling (bypasses the Hold interaction on the Interact action)
            bool advance = (keyboard != null && keyboard.eKey.wasPressedThisFrame)
                        || (gamepad != null && gamepad.buttonNorth.wasPressedThisFrame);

            if (advance) break;
            yield return null;
        }
    }
}

[Serializable]
public class SpeakerColor
{
    public string speakerName;
    public Color color;
}
