using UnityEngine;

public class BoardUnlockTrigger : MonoBehaviour
{
    [Header("Unlock Settings")]
    public int cost = 100;
    public float waitTime = 2f;

    [Header("Painting References")]
    public GameObject paintingUI;
    public Transform paintingCameraPoint;

    // ============= YENİ EKLENEN BÖLÜM =============
    [Header("Game Flow Integration")]
    [Tooltip("Bu trigger hangi GameFlow step'ine ait?")]
    public GameFlowManager.GameFlowStep assignedStep = GameFlowManager.GameFlowStep.BoardUnlock;
    
    [Tooltip("Debug logları göster?")]
    public bool showDebugLogs = true;
    // =============================================

    private bool playerInZone = false;
    private bool isUnlocking = false;
    private bool isUnlocked = false;
    private float unlockTimer = 0f;
    private bool currencyDeducted = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (isUnlocked || isUnlocking) return;

        // ============= STEP KONTROLÜ =============
        if (GameFlowManager.Instance != null)
        {
            if (GameFlowManager.Instance.GetCurrentStep() != assignedStep)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"⏸️ BoardUnlock step henüz aktif değil. Şu anki step: {GameFlowManager.Instance.GetCurrentStep()}");
                }
                return;
            }
        }
        // ========================================

        playerInZone = true;
        StartUnlockProcess();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInZone = false;

        if (isUnlocking && !isUnlocked)
        {
            ResetUnlockProcess();
        }
    }

    private void Update()
    {
        if (!isUnlocking || isUnlocked || !playerInZone) return;

        unlockTimer += Time.deltaTime;

        if (unlockTimer >= waitTime)
        {
            CompleteUnlock();
        }
    }

    private void StartUnlockProcess()
    {
        if (GameManager.Instance.Currency < cost)
        {
            if (showDebugLogs)
            {
                Debug.Log($"❌ Para yetersiz. Gereken: {cost}, Mevcut: {GameManager.Instance.Currency}");
            }
            return;
        }

        GameManager.Instance.SpendCurrency(cost);
        currencyDeducted = true;

        unlockTimer = 0f;
        isUnlocking = true;

        if (showDebugLogs)
        {
            Debug.Log($"✅ Board unlock başladı. Bekleme süresi: {waitTime}s");
        }
    }

    private void ResetUnlockProcess()
    {
        isUnlocking = false;
        unlockTimer = 0f;

        if (currencyDeducted)
        {
            GameManager.Instance.AddCurrency(cost);
            currencyDeducted = false;
        }

        if (showDebugLogs)
        {
            Debug.Log("🔄 Board unlock iptal edildi, para iade edildi.");
        }
    }

    private void CompleteUnlock()
    {
        isUnlocked = true;
        isUnlocking = false;

        if (showDebugLogs)
        {
            Debug.Log("🎉 Board unlock tamamlandı!");
        }

        EnterPaintingMode();

        // ============= STEP'İ TAMAMLA =============
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
        // ========================================

        gameObject.SetActive(false);
    }

    private void EnterPaintingMode()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.EnterPaintingMode();
            
            if (showDebugLogs)
            {
                Debug.Log("✅ Painting Mode'a girildi");
            }
        }
        else
        {
            Debug.LogError("❌ GameStateManager.Instance bulunamadı!");
        }
    }
}