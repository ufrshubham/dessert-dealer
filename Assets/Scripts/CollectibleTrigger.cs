using UnityEngine;

public class CollectibleTrigger : MonoBehaviour, IInteractable
{
    [Header("UI Prompt")]
    [SerializeField] private GameObject promptUI;

    //  private PlayerInteractor playerInteractor;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Hand")) return;
        if (GameManager.Instance.IsDeliveryReady()) return;

        // playerInteractor = other.GetComponent<PlayerInteractor>()
        //             ?? other.GetComponentInParent<PlayerInteractor>();
        // playerInteractor?.SetInteractable(this);

        if (promptUI != null) promptUI.SetActive(true);
        Debug.Log($"[{gameObject.name}] Hold E to collect.");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Hand")) return;

        // playerInteractor?.ClearInteractable(this);
        // playerInteractor = null;

        if (promptUI != null) promptUI.SetActive(false);
    }

    public void Interact()
    {
        if (!GameManager.Instance.IsDeliveryReady())
        {
            // playerInteractor?.ClearInteractable(this);
            if (promptUI != null) promptUI.SetActive(false);
            GameManager.Instance.CollectItem();
        }
    }

    private void OnDisable()
    {
        // playerInteractor?.ClearInteractable(this);
        // playerInteractor = null;
    }
}