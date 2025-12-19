using UnityEngine;
using System.Collections;

public class AreaUnlockTrigger : MonoBehaviour
{
    [Header("Unlock Settings")]
    public GameObject part2Environment;
    public GameObject escalatorObject1;
    public GameObject escalatorObject2;
    public GameObject boardObject;
    public GameObject truck;
    public GameObject xRay;
    public GameObject plane;
    public GameObject xRayZone;
    public GameObject banks;
    public GameObject paintZone;
    public int cost = 50;
    public float waitTime = 2f;
    
    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.5f;

    [Header("Game Flow Integration")]
    [Tooltip("Bu trigger hangi GameFlow step'ine ait?")]
    public GameFlowManager.GameFlowStep assignedStep = GameFlowManager.GameFlowStep.AreaUnlock;
    
    [Tooltip("Debug logları göster?")]
    public bool showDebugLogs = true;

    private bool playerInZone = false;
    private bool isUnlocking = false;
    private bool isUnlocked = false;
    private float unlockTimer = 0f;
    private bool currencyDeducted = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isUnlocked && !isUnlocking)
        {

            if (GameFlowManager.Instance != null)
            {
                if (GameFlowManager.Instance.GetCurrentStep() != assignedStep)
                {
                    if (showDebugLogs)
                    {
                        Debug.Log($"AreaUnlock henüz aktif değil. Şu anki step: {GameFlowManager.Instance.GetCurrentStep()}");
                    }
                    return; 
                }
            }

            playerInZone = true;
            StartUnlockProcess();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = false;
            if (isUnlocking && !isUnlocked)
            {
                ResetUnlockProcess();
            }
        }
    }

    private void Update()
    {
        if (isUnlocking && playerInZone && !isUnlocked)
        {
            unlockTimer += Time.deltaTime;
            
            if (unlockTimer >= waitTime)
            {
                CompleteUnlock();
            }
        }
    }

    private void StartUnlockProcess()
    {
        if (GameManager.Instance.Currency >= cost)
        {
            GameManager.Instance.SpendCurrency(cost);
            currencyDeducted = true;
            isUnlocking = true;
            unlockTimer = 0f;
            
            if (showDebugLogs)
            {
                Debug.Log($"nlock process started. Waiting {waitTime} seconds... Currency deducted: {cost}");
            }
        }
        else
        {
            if (showDebugLogs)
            {
                Debug.Log($"Not enough currency to unlock. Need {cost}, have {GameManager.Instance.Currency}");
            }
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
            
            if (showDebugLogs)
            {
                Debug.Log("Unlock cancelled. Currency refunded.");
            }
        }
    }

    private void CompleteUnlock()
    {
        isUnlocked = true;
        isUnlocking = false;
        
        // Activate the area objects
        if (part2Environment != null)
        {
            part2Environment.SetActive(true);
            PlayUnlockAnimation(part2Environment);
        }
        
        if (escalatorObject1 != null)
        {
            escalatorObject1.SetActive(true);
            PlayUnlockAnimation(escalatorObject1);
        }
        
        if (escalatorObject2 != null)
        {
            escalatorObject2.SetActive(true);
            PlayUnlockAnimation(escalatorObject2);
        }
        
        if (boardObject != null)
        {
            boardObject.SetActive(true);
            PlayUnlockAnimation(boardObject);
        }
        if (truck != null)
        {
            truck.SetActive(true);
            PlayUnlockAnimation(truck);
        }
        if (xRay != null)
        {
            xRay.SetActive(true);
            PlayUnlockAnimation(xRay);
        }
        if (plane != null)
        {
            plane.SetActive(true);
            PlayUnlockAnimation(plane);
        }
        if (xRayZone != null)
        {
            xRayZone.SetActive(true);
            PlayUnlockAnimation(xRayZone);
        }
        if (banks != null)
        {
            banks.SetActive(true);
            PlayUnlockAnimation(banks);
        }
        if (paintZone != null)
        {
            paintZone.SetActive(true);
            PlayUnlockAnimation(paintZone);
        }
        
        if (showDebugLogs)
        {
            Debug.Log("🎉 Area unlocked! Part 2 and escalators are now active.");
        }

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
        
        // Hide the trigger zone after unlock
        gameObject.SetActive(false);
    }

    private void PlayUnlockAnimation(GameObject targetObject)
    {
        UnlockAnimation anim = targetObject.GetComponent<UnlockAnimation>();
        if (anim == null)
        {
            anim = targetObject.AddComponent<UnlockAnimation>();
        }
        
        anim.PlayUnlockAnimation();
    }
}