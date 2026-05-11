using UnityEngine;

public class DeliveryZoneTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.DeliverItems();
        }
    }
}
