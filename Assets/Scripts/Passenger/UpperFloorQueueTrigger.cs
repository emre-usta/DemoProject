using UnityEngine;

/// <summary>
/// Trigger zone that continuously advances the upper floor queue while player is inside
/// </summary>
public class UpperFloorQueueTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PassengerManager passengerManager;
    
    [Header("Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float checkInterval = 0.2f; // How often to check and advance queue while player is in zone
    
    private bool isPlayerInZone = false;
    private float lastCheckTime = 0f;
    
    private void Start()
    {
        // Try to find PassengerManager if not assigned
        if (passengerManager == null)
        {
            passengerManager = FindObjectOfType<PassengerManager>();
        }
        
        if (passengerManager == null)
        {
            Debug.LogError("UpperFloorQueueTrigger: No PassengerManager found!");
        }
    }
    
    private void Update()
    {
        // Continuously process queue while player is in zone
        if (isPlayerInZone && passengerManager != null)
        {
            if (Time.time - lastCheckTime >= checkInterval)
            {
                lastCheckTime = Time.time;
                passengerManager.AdvanceUpperFloorQueueIfReady();
            }
        }
    }
    
    private void OnTriggerStay(Collider other)
    {
        // Ensure player is still in zone (in case OnTriggerEnter was missed)
        if (other.CompareTag(playerTag))
        {
            isPlayerInZone = true;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerInZone = true;
            lastCheckTime = Time.time;
            if (passengerManager != null)
            {
                passengerManager.SetPlayerInUpperFloorZone(true);
            }
            Debug.Log("UpperFloorQueueTrigger: Player entered trigger zone. Queue will advance continuously.");
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerInZone = false;
            if (passengerManager != null)
            {
                passengerManager.SetPlayerInUpperFloorZone(false);
            }
            Debug.Log("UpperFloorQueueTrigger: Player exited trigger zone. Queue advancement stopped.");
        }
    }
}

