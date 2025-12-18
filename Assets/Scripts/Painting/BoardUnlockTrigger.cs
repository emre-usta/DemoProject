using UnityEngine;

public class BoardUnlockTrigger : MonoBehaviour
{
    [Header("Unlock Settings")]
    public int cost = 100;
    public float waitTime = 2f;

    [Header("Painting References")]
    public GameObject paintingUI;
    public Transform paintingCameraPoint;

    private bool playerInZone = false;
    private bool isUnlocking = false;
    private bool isUnlocked = false;
    private float unlockTimer = 0f;
    private bool currencyDeducted = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (isUnlocked || isUnlocking) return;

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
            Debug.Log("Not enough money to unlock painting board.");
            return;
        }

        GameManager.Instance.SpendCurrency(cost);
        currencyDeducted = true;

        unlockTimer = 0f;
        isUnlocking = true;

        Debug.Log("Board unlock started...");
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

        Debug.Log("Board unlock cancelled, money refunded.");
    }

    private void CompleteUnlock()
    {
        isUnlocked = true;
        isUnlocking = false;

        EnterPaintingMode();

        gameObject.SetActive(false);
    }

    private void EnterPaintingMode()
    {
        GameStateManager.Instance.EnterPaintingMode();

        Debug.Log("Entered Painting Mode");
    }
}
