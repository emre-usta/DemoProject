using UnityEngine;
using System.Collections;

public class AreaUnlockTrigger : MonoBehaviour
{
    [Header("Unlock Settings")]
    public GameObject part2Environment;     // Assign Environment->Part 2
    public GameObject escalatorObject1;      // Assign escalators GameObject(s)
    public GameObject escalatorObject2;
    public GameObject boardObject;
    public GameObject truck;
    public int cost = 50;
    public float waitTime = 3f;
    
    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 1f;

    private bool playerInZone = false;
    private bool isUnlocking = false;
    private bool isUnlocked = false;
    private float unlockTimer = 0f;
    private bool currencyDeducted = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isUnlocked && !isUnlocking)
        {
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
                // Player left before completing unlock - reset
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
            // Deduct currency immediately when player enters
            GameManager.Instance.SpendCurrency(cost);
            currencyDeducted = true;
            isUnlocking = true;
            unlockTimer = 0f;
            Debug.Log($"Unlock process started. Waiting {waitTime} seconds... Currency deducted: {cost}");
        }
        else
        {
            Debug.Log($"Not enough currency to unlock. Need {cost}, have {GameManager.Instance.Currency}");
        }
    }

    private void ResetUnlockProcess()
    {
        isUnlocking = false;
        unlockTimer = 0f;
        
        // Refund currency if it was deducted
        if (currencyDeducted)
        {
            GameManager.Instance.AddCurrency(cost);
            currencyDeducted = false;
            Debug.Log("Unlock cancelled. Currency refunded.");
        }
    }

    private void CompleteUnlock()
    {
        isUnlocked = true;
        isUnlocking = false;
        
        // Activate the area objects first (they'll be at scale 0 initially)
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
        
        Debug.Log("Area unlocked! Part 2 and escalators are now active.");
        
        // Hide/disappear the trigger zone after unlock is complete
        gameObject.SetActive(false);
    }

    private void PlayUnlockAnimation(GameObject targetObject)
    {
        // Get or add UnlockAnimation component
        UnlockAnimation anim = targetObject.GetComponent<UnlockAnimation>();
        if (anim == null)
        {
            anim = targetObject.AddComponent<UnlockAnimation>();
        }
        
        // Start the animation
        anim.PlayUnlockAnimation();
    }
}

