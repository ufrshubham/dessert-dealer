using UnityEngine;
using UnityEngine.InputSystem;

public class DeliveryZoneTrigger : MonoBehaviour
{
    [Header("UI Prompt")]
    [SerializeField] private GameObject promptUI;

    private bool playerInZone = false;

    // Subscribe only once, in OnEnable/OnDisable
    private void OnEnable()
    {
        // InputManager may not exist yet at OnEnable during scene load
        if (InputManager.Instance != null)
            InputManager.Instance.inputActions.Player.Interact.performed += OnInteract;
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.inputActions.Player.Interact.performed -= OnInteract;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInZone = true;

        bool ready = GameManager.Instance.IsDeliveryReady();
        if (promptUI != null) promptUI.SetActive(ready);

        if (!ready)
        {
            int remaining = GameManager.Instance.GetTargetForToday()
                          - GameManager.Instance.GetItemsCollectedToday();
            Debug.Log($"[DeliveryZone] Collect {remaining} more item(s) first.");
        }
        else
        {
            Debug.Log("[DeliveryZone] Hold E to deliver.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInZone = false;

        if (promptUI != null) promptUI.SetActive(false);
    }

    // Keep prompt in sync if player reaches target while standing in zone
    private void Update()
    {
        if (!playerInZone || promptUI == null) return;
        promptUI.SetActive(GameManager.Instance.IsDeliveryReady());
    }

    private void OnInteract(InputAction.CallbackContext ctx)
    {
        // Guard: only act if player is physically in this zone
        if (!playerInZone) return;

        if (GameManager.Instance.IsDeliveryReady())
        {
            if (promptUI != null) promptUI.SetActive(false);
            GameManager.Instance.DeliverItems();
        }
    }
}