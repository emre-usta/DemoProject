using UnityEngine;

public class PassengerQueueTrigger : MonoBehaviour
{
    [Header("References")]
    public PassengerManager passengerManager;
    
    [Header("Game Flow Integration")]
    [Tooltip("Bu trigger hangi GameFlow step'ine ait?")]
    public GameFlowManager.GameFlowStep assignedStep = GameFlowManager.GameFlowStep.PassengerTrigger;
    
    [Tooltip("Debug logları göster?")]
    public bool showDebugLogs = true;
    
    private bool triggered = false;
    
    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        
        if (other.CompareTag("Player"))
        {
            if (GameFlowManager.Instance != null)
            {
                if (GameFlowManager.Instance.GetCurrentStep() != assignedStep)
                {
                    if (showDebugLogs)
                    {
                        Debug.Log($"PassengerTrigger step henüz aktif değil. Şu anki step: {GameFlowManager.Instance.GetCurrentStep()}");
                    }
                    return; // Bu step aktif değilse işlem yapma
                }
            }
            
            if (showDebugLogs)
            {
                Debug.Log("Passenger Trigger Zone activated by Player. Starting luggage delivery sequence.");
            }
            
            passengerManager.TriggerPassengerMovement();
            passengerManager.StartLuggageDelivery();
            triggered = true;
            
            if (GameFlowManager.Instance != null)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"GameFlow step tamamlandı: {assignedStep}");
                }
                
                GameFlowManager.Instance.CompleteCurrentStep();
            }
            else
            {
                Debug.LogError("GameFlowManager.Instance bulunamadı!");
            }
        }
    }
}