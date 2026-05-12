using System.Xml.Serialization;
using UnityEngine;

public class MouseTrap : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Delay before the mouse trap is triggered after the player enters.")]
    private float _triggerDelay = 0.5f;

    private bool _isTriggered = false;
    private float _triggerTimer = 0f;

    private void OnTriggerEnter(Collider other)
    {
        // Check if the player has entered the trap's trigger area
        if (other.CompareTag("Player"))
        {
            Debug.Log("[MouseTrap] Player entered trap area, starting trigger timer.");
            _isTriggered = true;
            _triggerTimer = 0f;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Check if the player has exited the trap's trigger area
        if (other.CompareTag("Player"))
        {
            Debug.Log("[MouseTrap] Player exited trap area, resetting trap.");
            _isTriggered = false;
            _triggerTimer = 0f;
        }
    }

    private void Update()
    {
        // If the trap is triggered, count up the timer
        if (_isTriggered)
        {
            _triggerTimer += Time.deltaTime;

            // If the timer exceeds the trigger delay, trigger the trap
            if (_triggerTimer >= _triggerDelay)
            {
                TriggerTrap();
                _isTriggered = false;
            }
        }
    }

    private void TriggerTrap()
    {
        Debug.Log("[MouseTrap] Trap triggered!");
        GameManager.Instance.OnMouseTrapTriggered?.Invoke();
    }
}
