using System;
using System.ComponentModel.Design;
using UnityEngine;
using UnityEngine.InputSystem;

public class DessertCollector : MonoBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private Transform dessertAnchor;
    [SerializeField] private GameObject pickupPromptUI;

    [Header("Delivery Settings")]
    [SerializeField] private GameObject deliveryPromptUI;

    PickupDetails visiblePickupDetails;
    DessertData visibleDessertData;
    PickupDetails collectedPickupDetails;
    DessertData collectedDessertData;
    Vector3 dessertOriginalPosition;
    InputActions inputActions;
    bool isPickupVisible = false;
    private bool isInDeliveryZone = false;

    public Action OnDessertPickedUp;
    public Action OnDessertDropped;
    public Action OnDessertDelivered;

    void Start()
    {
        inputActions = InputManager.Instance.inputActions;
        inputActions.Player.Interact.performed += OnInteract;
        // if (isInDeliveryZone && GameManager.Instance.IsDeliveryReady() && collectedPickupDetails != null)
        // {
        //     inputActions.Player.Interact.performed += DeliverDessert;
        //     return;
        // }
        inputActions.Player.Drop.performed += DropDessert;
    }

    void OnEnable()
    {
        if (inputActions != null)
        {
            inputActions.Player.Interact.performed += OnInteract;
            inputActions.Player.Drop.performed += DropDessert;
        }
    }

    void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.Player.Interact.performed -= OnInteract;
            inputActions.Player.Drop.performed -= DropDessert;
        }
    }

    void OnTriggerEnter(Collider collider)
    {
        if (collider.CompareTag("Pickup") && !isPickupVisible)
        {
            visiblePickupDetails = collider.GetComponent<PickupDetails>();
            if (GameManager.Instance.IsDeliveryReady()) return;
            if (visiblePickupDetails != null)
            {
                visibleDessertData = visiblePickupDetails.dessertData;
                if (visibleDessertData != null)
                {
                    isPickupVisible = true;
                    if (pickupPromptUI != null) pickupPromptUI.SetActive(true);
                    Debug.Log("Press E to pickup : " + visibleDessertData.dessertName);
                }
            }

        }

        // Delivery zone
        if (collider.CompareTag("DeliveryZone"))
        {
            isInDeliveryZone = true;
            UpdateDeliveryPrompt();
            Debug.Log("[Collector] Entered delivery zone.");
        }
    }

    void OnTriggerExit(Collider collider)
    {
        if (collider.CompareTag("Pickup") && isPickupVisible)
        {
            if (visiblePickupDetails != null && collider.gameObject == visiblePickupDetails.gameObject)
            {
                visiblePickupDetails = null;
                visibleDessertData = null;
                isPickupVisible = false;
                if (pickupPromptUI != null) pickupPromptUI.SetActive(false);
            }
        }

        // Delivery zone
        if (collider.CompareTag("DeliveryZone"))
        {
            isInDeliveryZone = false;
            if (deliveryPromptUI != null) deliveryPromptUI.SetActive(false);
            Debug.Log("[Collector] Left delivery zone.");
        }
    }

    private void OnInteract(InputAction.CallbackContext ctx)
    {
        // Delivery takes priority if player is in zone and ready
        if (isInDeliveryZone && GameManager.Instance.IsDeliveryReady() && collectedPickupDetails != null)
        {
            DeliverDessert();
            return;
        }

        // Otherwise try pickup
        PickupDessert();
    }


    public void PickupDessert()
    {
        if (visiblePickupDetails != null && isPickupVisible && collectedPickupDetails == null)
        {
            visibleDessertData = visiblePickupDetails.dessertData;
            Debug.Log("Picked up dessert : " + visibleDessertData.dessertName);
            collectedPickupDetails = visiblePickupDetails;
            collectedDessertData = visibleDessertData;
            dessertOriginalPosition = collectedPickupDetails.transform.position;
            collectedPickupDetails.transform.SetParent(dessertAnchor);
            collectedPickupDetails.transform.localPosition = Vector3.zero;
            collectedPickupDetails.transform.localRotation = Quaternion.identity;
            visiblePickupDetails = null;
            visibleDessertData = null;
            isPickupVisible = false;
            if (!GameManager.Instance.IsDeliveryReady())
            {
                if (pickupPromptUI != null) pickupPromptUI.SetActive(false);
                GameManager.Instance.CollectItem();
            }
            OnDessertPickedUp?.Invoke();
            if (isInDeliveryZone) UpdateDeliveryPrompt();
        }
    }

    public void DropDessert(InputAction.CallbackContext context)
    {
        if (collectedPickupDetails != null)
        {
            collectedPickupDetails.transform.position = dessertOriginalPosition;
            collectedPickupDetails.transform.SetParent(null);
            collectedPickupDetails = null;
            collectedDessertData = null;
            OnDessertDropped?.Invoke();
        }
    }

    private void DeliverDessert()
    {
        Debug.Log($"[Collector] Delivered: {collectedDessertData?.dessertName}");

        collectedPickupDetails.transform.position = dessertOriginalPosition;
        collectedPickupDetails.transform.SetParent(null);
        collectedPickupDetails.gameObject.SetActive(false);
        
        collectedPickupDetails = null;
        collectedDessertData   = null;

        if (deliveryPromptUI != null) deliveryPromptUI.SetActive(false);

        GameManager.Instance.DeliverItems();
        OnDessertDelivered?.Invoke();
    }

    private void UpdateDeliveryPrompt()
    {
        if (deliveryPromptUI == null) return;
        // Show only when delivery is ready AND player is holding something
        bool show = GameManager.Instance.IsDeliveryReady() && collectedPickupDetails != null;
        Debug.Log("[Collector] deliveryPromptUI ");
        deliveryPromptUI.SetActive(show);
    }
}
