using UnityEngine;

/// <summary>
/// Trigger zone that detects when player enters near x-ray machine
/// </summary>
public class XRayTriggerZone : MonoBehaviour
{
    [Header("References")]
    public XRayMachine xRayMachine;

    // ============= YENİ EKLENEN BÖLÜM =============
    [Header("Game Flow Integration")]
    [Tooltip("Bu trigger hangi GameFlow step'ine ait?")]
    public GameFlowManager.GameFlowStep assignedStep = GameFlowManager.GameFlowStep.XRay;
    
    [Tooltip("Debug logları göster?")]
    public bool showDebugLogs = true;

    private bool isStepCompleted = false;
    // =============================================

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // ============= STEP KONTROLÜ =============
            if (GameFlowManager.Instance != null)
            {
                if (GameFlowManager.Instance.GetCurrentStep() != assignedStep)
                {
                    if (showDebugLogs)
                    {
                        Debug.Log($"⏸️ XRay step henüz aktif değil. Şu anki step: {GameFlowManager.Instance.GetCurrentStep()}");
                    }
                    return; // Bu step aktif değilse işlem yapma
                }
            }
            // ========================================

            if (showDebugLogs)
            {
                Debug.Log("✅ Player entered x-ray machine zone.");
            }

            if (xRayMachine != null)
            {
                xRayMachine.OnPlayerEnter();
            }

            // ============= STEP'İ TAMAMLA (SADECE BİR KEZ) =============
            if (!isStepCompleted)
            {
                isStepCompleted = true;

                if (showDebugLogs)
                {
                    Debug.Log("🎉 XRay zone triggered!");
                }

                // GameFlowManager'a step'in tamamlandığını bildir
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
            // =======================================================
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (showDebugLogs)
            {
                Debug.Log("Player exited x-ray machine zone.");
            }

            if (xRayMachine != null)
            {
                xRayMachine.OnPlayerExit();
            }
        }
    }
}