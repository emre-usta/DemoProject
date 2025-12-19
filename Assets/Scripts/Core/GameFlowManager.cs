using UnityEngine;
using System.Collections.Generic;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance;
    
    public enum GameFlowStep
    {
        AreaUnlock,
        PassengerTrigger,
        XRay,
        EscalatorBottom,
        UpperFloorQueue,
        MoneyCollect,
        BoardUnlock
    }

    [System.Serializable]
    public class FlowItem
    {
        public GameFlowStep step;
        public Collider triggerCollider;
        
        [Header("Visual Guide")]
        [Tooltip("Bu step'in Waiting Marks objesi (opsiyonel)")]
        public GameObject waitingMarks;
        
        [HideInInspector] public bool isCompleted = false;
    }

    [Header("Flow Order")]
    public List<FlowItem> flowItems = new List<FlowItem>();
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    
    private int currentStepIndex = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        ValidateFlowItems();
        UpdateTriggers();
        
        if (showDebugLogs)
        {
            Debug.Log($"🎮 Game Flow başlatıldı. İlk step: {GetCurrentStep()}");
        }
    }

    private void ValidateFlowItems()
    {
        for (int i = 0; i < flowItems.Count; i++)
        {
            if (flowItems[i].triggerCollider == null)
            {
                Debug.LogError($"❌ Flow item {i} ({flowItems[i].step}) collider atanmamış!");
            }
            else if (!flowItems[i].triggerCollider.isTrigger)
            {
                Debug.LogWarning($"⚠️ Flow item {i} ({flowItems[i].step}) collider'ı 'Is Trigger' değil! Düzeltiliyor...");
                flowItems[i].triggerCollider.isTrigger = true;
            }

            // Waiting Marks kontrolü
            if (flowItems[i].waitingMarks == null)
            {
                if (showDebugLogs)
                {
                    Debug.LogWarning($"⚠️ Flow item {i} ({flowItems[i].step}) waiting marks atanmamış!");
                }
            }
        }
    }

    private void UpdateTriggers()
    {
        for (int i = 0; i < flowItems.Count; i++)
        {
            bool shouldBeActive = (i == currentStepIndex);
            
            // Trigger collider'ı aktif/pasif et
            if (flowItems[i].triggerCollider != null)
            {
                flowItems[i].triggerCollider.enabled = shouldBeActive;
            }

            // ============= YENİ: WAITING MARKS KONTROLÜ =============
            // Waiting Marks'ı aktif/pasif et
            if (flowItems[i].waitingMarks != null)
            {
                flowItems[i].waitingMarks.SetActive(shouldBeActive);
                
                if (showDebugLogs)
                {
                    string markStatus = shouldBeActive ? "👁️ GÖRÜNÜR" : "👻 GİZLİ";
                    Debug.Log($"  └─ Waiting Marks: {markStatus}");
                }
            }
            // =======================================================
            
            if (showDebugLogs)
            {
                string status = shouldBeActive ? "✅ AKTİF" : "⭕ PASİF";
                Debug.Log($"Step {i} - {flowItems[i].step}: {status}");
            }
        }
    }

    public void CompleteCurrentStep()
    {
        if (currentStepIndex >= flowItems.Count)
        {
            Debug.LogWarning("⚠️ Zaten son step'tesiniz!");
            return;
        }

        flowItems[currentStepIndex].isCompleted = true;
        
        if (showDebugLogs)
        {
            Debug.Log($"✔️ Step tamamlandı: {flowItems[currentStepIndex].step} (Index: {currentStepIndex})");
        }

        currentStepIndex++;

        if (currentStepIndex >= flowItems.Count)
        {
            Debug.Log("🎉 GAME FLOW TAMAMLANDI!");
            OnGameFlowCompleted();
            return;
        }

        if (showDebugLogs)
        {
            Debug.Log($"➡️ Yeni step aktif: {GetCurrentStep()} (Index: {currentStepIndex})");
        }

        UpdateTriggers();
    }

    public GameFlowStep GetCurrentStep()
    {
        if (currentStepIndex >= flowItems.Count)
        {
            return flowItems[flowItems.Count - 1].step;
        }
        return flowItems[currentStepIndex].step;
    }

    public int GetCurrentStepIndex()
    {
        return currentStepIndex;
    }

    public bool IsStepCompleted(GameFlowStep step)
    {
        foreach (var item in flowItems)
        {
            if (item.step == step)
            {
                return item.isCompleted;
            }
        }
        return false;
    }

    private void OnGameFlowCompleted()
    {
        // Oyun tamamlandığında yapılacak işlemler
        // Tüm Waiting Marks'ları gizle
        foreach (var item in flowItems)
        {
            if (item.waitingMarks != null)
            {
                item.waitingMarks.SetActive(false);
            }
        }

        if (showDebugLogs)
        {
            Debug.Log("🎊 Tüm yön göstergeleri gizlendi!");
        }
    }

    // Test amaçlı (Inspector'dan çağırılabilir)
    [ContextMenu("Force Next Step")]
    public void ForceNextStep()
    {
        CompleteCurrentStep();
    }

    [ContextMenu("Reset Flow")]
    public void ResetFlow()
    {
        currentStepIndex = 0;
        foreach (var item in flowItems)
        {
            item.isCompleted = false;
        }
        UpdateTriggers();
        Debug.Log("🔄 Flow sıfırlandı!");
    }

    [ContextMenu("Hide All Waiting Marks")]
    public void HideAllWaitingMarks()
    {
        foreach (var item in flowItems)
        {
            if (item.waitingMarks != null)
            {
                item.waitingMarks.SetActive(false);
            }
        }
        Debug.Log("👻 Tüm Waiting Marks gizlendi!");
    }

    [ContextMenu("Show All Waiting Marks")]
    public void ShowAllWaitingMarks()
    {
        foreach (var item in flowItems)
        {
            if (item.waitingMarks != null)
            {
                item.waitingMarks.SetActive(true);
            }
        }
        Debug.Log("👁️ Tüm Waiting Marks gösterildi!");
    }
}