using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class MoneyFinishPoint : MonoBehaviour
{
    public static MoneyFinishPoint Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float collectionCheckInterval = 0.2f;
    [SerializeField] private float collectionDelay = 0.1f;

    [Header("Game Flow Integration")]
    [Tooltip("Bu trigger hangi GameFlow step'ine ait?")]
    public GameFlowManager.GameFlowStep assignedStep = GameFlowManager.GameFlowStep.MoneyCollect;
    
    [Tooltip("Debug logları göster?")]
    public bool showDebugLogs = true;

    private bool isStepCompleted = false;

    private List<MoneyPickup> moneyPickups = new List<MoneyPickup>();
    private bool isPlayerInZone = false;
    private bool isCollecting = false;
    private float lastCheckTime = 0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning("MoneyFinishPoint: Multiple instances found! Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (isPlayerInZone && !isCollecting)
        {
            if (Time.time - lastCheckTime >= collectionCheckInterval)
            {
                lastCheckTime = Time.time;
                CheckAndCollectMoney();
            }
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
                        Debug.Log($"⏸️ MoneyCollect step henüz aktif değil. Şu anki step: {GameFlowManager.Instance.GetCurrentStep()}");
                    }
                    return; 
                }
            }

            isPlayerInZone = true;
            
            if (showDebugLogs)
            {
                Debug.Log("✅ MoneyFinishPoint: Player entered zone. Starting money collection.");
            }
            
            CheckAndCollectMoney();

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
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerInZone = true;
        }
    }
    
    public void RegisterMoneyPickup(MoneyPickup moneyPickup)
    {
        if (moneyPickup != null && !moneyPickups.Contains(moneyPickup))
        {
            moneyPickups.Add(moneyPickup);
            
            if (showDebugLogs)
            {
                Debug.Log($"MoneyFinishPoint: Registered money pickup. Total: {moneyPickups.Count}");
            }
        }
    }
    
    private void CheckAndCollectMoney()
    {
        if (isCollecting) return;

        List<MoneyPickup> readyToCollect = new List<MoneyPickup>();
        foreach (var money in moneyPickups)
        {
            if (money != null)
            {
                bool hasReached = money.HasReachedFinishPoint();
                bool isCollected = money.IsCollected();
                
                if (hasReached && !isCollected)
                {
                    readyToCollect.Add(money);
                }
            }
        }

        if (showDebugLogs)
        {
            Debug.Log($"MoneyFinishPoint: Checking money. Total registered: {moneyPickups.Count}, Ready to collect: {readyToCollect.Count}, Player in zone: {isPlayerInZone}");
        }

        if (readyToCollect.Count > 0 && isPlayerInZone)
        {
            StartCoroutine(CollectAllMoney(readyToCollect));
        }
    }

    private IEnumerator CollectAllMoney(List<MoneyPickup> moneyList)
    {
        isCollecting = true;
        
        if (showDebugLogs)
        {
            Debug.Log($"MoneyFinishPoint: Collecting {moneyList.Count} money pickups. Total in list: {moneyPickups.Count}");
        }

        foreach (var money in moneyList)
        {
            if (money != null && !money.IsCollected())
            {
                if (showDebugLogs)
                {
                    Debug.Log($"MoneyFinishPoint: Collecting money pickup. HasReached: {money.HasReachedFinishPoint()}, IsCollected: {money.IsCollected()}");
                }
                money.Collect();
                yield return new WaitForSeconds(collectionDelay);
            }
        }

        moneyPickups.RemoveAll(m => m == null || m.IsCollected());

        isCollecting = false;
        
        if (showDebugLogs)
        {
            Debug.Log($"MoneyFinishPoint: Finished collecting money. Remaining: {moneyPickups.Count}");
        }
    }
    
    public void UnregisterMoneyPickup(MoneyPickup moneyPickup)
    {
        if (moneyPickups.Contains(moneyPickup))
        {
            moneyPickups.Remove(moneyPickup);
        }
    }
}