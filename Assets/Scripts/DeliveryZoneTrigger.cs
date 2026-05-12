using UnityEngine;

public class DeliveryZoneTrigger : MonoBehaviour, IInteractable
{
    [Header("UI Prompt")]
    [SerializeField] private GameObject promptUI;

    private PlayerInteractor playerInteractor;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Hand")) return;

        playerInteractor = other.GetComponent<PlayerInteractor>()
                    ?? other.GetComponentInParent<PlayerInteractor>();
        playerInteractor?.SetInteractable(this);

        bool ready = GameManager.Instance.IsDeliveryReady();
        if (promptUI != null) promptUI.SetActive(ready);
        Debug.Log($"[{gameObject.name}] Hold E to collect.");

        if (!ready)
        {
            int remaining = GameManager.Instance.GetTargetForToday()
                          - GameManager.Instance.GetItemsCollectedToday();
            Debug.Log($"[DeliveryZone] Collect {remaining} more item(s) first.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Hand")) return;

        playerInteractor?.ClearInteractable(this);
        playerInteractor = null;

        if (promptUI != null) promptUI.SetActive(false);
    }

    // Update prompt visibility in case player reaches target while standing here
    private void Update()
    {
        if (playerInteractor == null || promptUI == null) return;
        promptUI.SetActive(GameManager.Instance.IsDeliveryReady());
    }

    public void Interact()
    {
        if (GameManager.Instance.IsDeliveryReady())
        {
            if (promptUI != null) promptUI.SetActive(false);
            GameManager.Instance.DeliverItems();
        }
    }

    private void OnDisable()
    {
        playerInteractor?.ClearInteractable(this);
        playerInteractor = null;
    }
}
