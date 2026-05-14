using System;
using System.ComponentModel.Design;
using UnityEngine;
using UnityEngine.InputSystem;

public class DessertCollector : MonoBehaviour
{
    [SerializeField] Transform dessertAnchor;
    PickupDetails visiblePickupDetails;
    DessertData visibleDessertData;
    PickupDetails collectedPickupDetails;
    DessertData collectedDessertData;
    Vector3 dessertOriginalPosition;
    InputActions inputActions;
    bool isPickupVisible = false;

    public Action OnDessertPickedUp;
    public Action OnDessertDropped;

    void Start()
    {
        inputActions = InputManager.Instance.inputActions;
        inputActions.Player.Interact.performed += PickupDessert;
        inputActions.Player.Drop.performed += DropDessert;
    }

    void OnEnable()
    {
        if (inputActions != null)
        {
            inputActions.Player.Interact.performed += PickupDessert;
            inputActions.Player.Drop.performed += DropDessert;
        }
    }

    void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.Player.Interact.performed -= PickupDessert;
            inputActions.Player.Drop.performed -= DropDessert;
        }
    }

    void OnTriggerEnter(Collider collider)
    {
        if (collider.CompareTag("Pickup") && !isPickupVisible)
        {
            visiblePickupDetails = collider.GetComponent<PickupDetails>();
            if (visiblePickupDetails != null)
            {
                visibleDessertData = visiblePickupDetails.dessertData;
                if (visibleDessertData != null)
                {
                    isPickupVisible = true;
                    Debug.Log("Press E to pickup : " + visibleDessertData.dessertName);
                }
            }

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
            }
        }
    }

    public void PickupDessert(InputAction.CallbackContext context)
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
            OnDessertPickedUp?.Invoke();
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
}
