using UnityEngine;


public class UpperFloorQueueTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PassengerManager passengerManager;
    
    [Header("Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float checkInterval = 0.2f;

    [Header("Game Flow Integration")]
    [Tooltip("Bu trigger hangi GameFlow step'ine ait?")]
    public GameFlowManager.GameFlowStep assignedStep = GameFlowManager.GameFlowStep.UpperFloorQueue;
    
    [Tooltip("Debug logları göster?")]
    public bool showDebugLogs = true;

    private bool isStepCompleted = false;
    
    private bool isPlayerInZone = false;
    private float lastCheckTime = 0f;
    
    private void Start()
    {
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
        if (other.CompareTag(playerTag))
        {
            isPlayerInZone = true;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            if (GameFlowManager.Instance != null)
            {
                if (GameFlowManager.Instance.GetCurrentStep() != assignedStep)
                {
                    if (showDebugLogs)
                    {
                        Debug.Log($"⏸️ UpperFloorQueue step henüz aktif değil. Şu anki step: {GameFlowManager.Instance.GetCurrentStep()}");
                    }
                    return; 
                }
            }

            isPlayerInZone = true;
            lastCheckTime = Time.time;
            
            if (passengerManager != null)
            {
                passengerManager.SetPlayerInUpperFloorZone(true);
            }

            if (showDebugLogs)
            {
                Debug.Log("✅ UpperFloorQueueTrigger: Player entered trigger zone. Queue will advance continuously.");
            }

            if (!isStepCompleted)
            {
                isStepCompleted = true;

                if (GameFlowManager.Instance != null)
                {
                    if (showDebugLogs)
                    {
                        Debug.Log($"✔️ GameFlow step tamamlandı: {assignedStep}");
                    }

                    GameFlowManager.Instance.CompleteCurrentStep();
                }
                else
                {
                    Debug.LogError("❌ GameFlowManager.Instance bulunamadı!");
                }
            }
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

            if (showDebugLogs)
            {
                Debug.Log("UpperFloorQueueTrigger: Player exited trigger zone. Queue advancement stopped.");
            }
        }
    }
}