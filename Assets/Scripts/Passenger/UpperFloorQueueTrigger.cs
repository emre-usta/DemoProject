using UnityEngine;

/// <summary>
/// Trigger zone that continuously advances the upper floor queue while player is inside
/// </summary>
public class UpperFloorQueueTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UpperFloorQueueManager queueManager;
    
    [Header("Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float checkInterval = 0.2f; // How often to check and advance queue while player is in zone
    
    private bool isPlayerInZone = false;
    private float lastCheckTime = 0f;
    
    private void Start()
    {
        // Try to find queue manager if not assigned
        if (queueManager == null)
        {
            queueManager = FindObjectOfType<UpperFloorQueueManager>();
        }
        
        if (queueManager == null)
        {
            Debug.LogError("UpperFloorQueueTrigger: No UpperFloorQueueManager found!");
        }
    }
    
    private void Update()
    {
        // Continuously process queue while player is in zone
        if (isPlayerInZone && queueManager != null)
        {
            if (Time.time - lastCheckTime >= checkInterval)
            {
                lastCheckTime = Time.time;
                queueManager.AdvanceQueueIfReady();
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
            if (queueManager != null)
            {
                queueManager.SetPlayerInZone(true);
            }
            Debug.Log("UpperFloorQueueTrigger: Player entered trigger zone. Queue will advance continuously.");
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerInZone = false;
            if (queueManager != null)
            {
                queueManager.SetPlayerInZone(false);
            }
            Debug.Log("UpperFloorQueueTrigger: Player exited trigger zone. Queue advancement stopped.");
        }
    }
}


