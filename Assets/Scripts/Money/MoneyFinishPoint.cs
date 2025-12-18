using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Trigger zone at MoneyFinishPoint that collects money pickups when player enters
/// </summary>
public class MoneyFinishPoint : MonoBehaviour
{
    public static MoneyFinishPoint Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float collectionCheckInterval = 0.2f;
    [SerializeField] private float collectionDelay = 0.1f; // Delay between collecting each money

    private List<MoneyPickup> moneyPickups = new List<MoneyPickup>();
    private bool isPlayerInZone = false;
    private bool isCollecting = false;
    private float lastCheckTime = 0f;

    private void Awake()
    {
        // Singleton pattern - ensure only one instance exists
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
            isPlayerInZone = true;
            Debug.Log("MoneyFinishPoint: Player entered zone. Starting money collection.");
            CheckAndCollectMoney();
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

    /// <summary>
    /// Register a money pickup that has reached the finish point
    /// </summary>
    public void RegisterMoneyPickup(MoneyPickup moneyPickup)
    {
        if (moneyPickup != null && !moneyPickups.Contains(moneyPickup))
        {
            moneyPickups.Add(moneyPickup);
            Debug.Log($"MoneyFinishPoint: Registered money pickup. Total: {moneyPickups.Count}");
        }
    }

    /// <summary>
    /// Check for money pickups and collect them if player is in zone
    /// </summary>
    private void CheckAndCollectMoney()
    {
        if (isCollecting) return;

        // Find all money pickups that have reached finish point
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

        Debug.Log($"MoneyFinishPoint: Checking money. Total registered: {moneyPickups.Count}, Ready to collect: {readyToCollect.Count}, Player in zone: {isPlayerInZone}");

        if (readyToCollect.Count > 0 && isPlayerInZone)
        {
            StartCoroutine(CollectAllMoney(readyToCollect));
        }
    }

    private IEnumerator CollectAllMoney(List<MoneyPickup> moneyList)
    {
        isCollecting = true;
        Debug.Log($"MoneyFinishPoint: Collecting {moneyList.Count} money pickups. Total in list: {moneyPickups.Count}");

        foreach (var money in moneyList)
        {
            if (money != null && !money.IsCollected())
            {
                Debug.Log($"MoneyFinishPoint: Collecting money pickup. HasReached: {money.HasReachedFinishPoint()}, IsCollected: {money.IsCollected()}");
                money.Collect();
                yield return new WaitForSeconds(collectionDelay);
            }
        }

        // Clean up collected/destroyed money from list (only after collection is complete)
        moneyPickups.RemoveAll(m => m == null || m.IsCollected());

        isCollecting = false;
        Debug.Log($"MoneyFinishPoint: Finished collecting money. Remaining: {moneyPickups.Count}");
    }

    /// <summary>
    /// Remove money pickup from list (called when money is destroyed)
    /// </summary>
    public void UnregisterMoneyPickup(MoneyPickup moneyPickup)
    {
        if (moneyPickups.Contains(moneyPickup))
        {
            moneyPickups.Remove(moneyPickup);
        }
    }
}

