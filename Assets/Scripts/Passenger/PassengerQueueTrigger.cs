using UnityEngine;

public class PassengerQueueTrigger : MonoBehaviour
{
    public PassengerManager passengerManager;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        if (other.CompareTag("Player")) // Ensure your player has the "Player" tag!
        {
            Debug.Log("Passenger Trigger Zone activated by Player. Starting luggage delivery sequence.");
            passengerManager.TriggerPassengerMovement();
            passengerManager.StartLuggageDelivery();
            triggered = true;
        }
    }
}