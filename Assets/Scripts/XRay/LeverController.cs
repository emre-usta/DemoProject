using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class LeverController : MonoBehaviour
{
    [Header("Lever Settings")]
    [SerializeField] private Transform leverPlatform; 
    [SerializeField] private Transform truckBedTarget; 
    [SerializeField] private TruckBedStack truckBedStack; 
    [SerializeField] private TruckController truckController; 
    [SerializeField] private float liftHeight = 2f; 
    [SerializeField] private float liftDuration = 1.5f; 
    [SerializeField] private float returnDuration = 1f; 
    [SerializeField] private AnimationCurve liftCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Suitcase Settings")]
    [SerializeField] private Transform suitcaseLandingPoint; 
    [SerializeField] private float handoverDuration = 1f; 
    
    private Vector3 leverStartPosition;
    private Queue<GameObject> suitcaseQueue = new Queue<GameObject>();
    private bool isProcessing = false;
    private GameObject currentSuitcase = null;

    private void Start()
    {
        if (leverPlatform == null)
        {
            leverPlatform = transform; 
        }
        
        leverStartPosition = leverPlatform.position;
        
        if (suitcaseLandingPoint == null)
        {
            GameObject landingPoint = new GameObject("SuitcaseLandingPoint");
            landingPoint.transform.SetParent(leverPlatform);
            landingPoint.transform.localPosition = Vector3.zero;
            suitcaseLandingPoint = landingPoint.transform;
        }

        if (truckBedStack == null && truckBedTarget != null)
        {
            truckBedStack = truckBedTarget.GetComponent<TruckBedStack>();
            if (truckBedStack == null)
            {
                truckBedStack = truckBedTarget.GetComponentInChildren<TruckBedStack>();
            }
        }

        if (truckController == null)
        {
            truckController = FindObjectOfType<TruckController>();
        }
    }
    
    public void ReceiveSuitcase(GameObject suitcase)
    {
        if (suitcase == null) return;
        
        suitcaseQueue.Enqueue(suitcase);
        
        if (!isProcessing)
        {
            StartCoroutine(ProcessSuitcaseQueue());
        }
    }

    private IEnumerator ProcessSuitcaseQueue()
    {
        isProcessing = true;

        while (suitcaseQueue.Count > 0)
        {
            currentSuitcase = suitcaseQueue.Dequeue();
            
            if (currentSuitcase == null) continue;

            yield return StartCoroutine(HandoverToLever(currentSuitcase));

            yield return StartCoroutine(LiftLeverWithSuitcase());

            yield return StartCoroutine(HandoverToTruckBed(currentSuitcase));

            yield return StartCoroutine(ReturnLeverToStart());

            currentSuitcase = null;
            
            if (truckBedStack != null && truckController != null && !truckController.IsMoving())
            {
                int currentCount = truckBedStack.GetStackCount();
                int requiredCount = truckController.GetRequiredSuitcaseCount();
                
                if (currentCount >= requiredCount)
                {
                    Debug.Log($"LeverController: Reached {currentCount}/{requiredCount} suitcases. Triggering truck delivery.");
                    truckController.StartDeliverySequence();
                }
            }
        }

        isProcessing = false;
    }

    private IEnumerator HandoverToLever(GameObject suitcase)
    {
        if (suitcase == null || suitcaseLandingPoint == null) yield break;

        Vector3 startPos = suitcase.transform.position;
        Vector3 endPos = suitcaseLandingPoint.position; 
        float elapsed = 0f;

        while (elapsed < handoverDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / handoverDuration;
            float arcHeight = Mathf.Sin(t * Mathf.PI) * 1.5f; 

            Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);
            currentPos.y += arcHeight;

            suitcase.transform.position = currentPos;
            suitcase.transform.Rotate(Vector3.up, 180f * Time.deltaTime);

            yield return null;
        }

        suitcase.transform.position = endPos;
        suitcase.transform.SetParent(suitcaseLandingPoint);
        suitcase.transform.localRotation = Quaternion.Euler(0f, 90f, 90f);
    }

    private IEnumerator LiftLeverWithSuitcase()
    {
        if (leverPlatform == null) yield break;

        Vector3 startPos = leverPlatform.position;
        Vector3 endPos = leverStartPosition + Vector3.up * liftHeight;
        float elapsed = 0f;

        while (elapsed < liftDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / liftDuration;
            float curveValue = liftCurve.Evaluate(t);

            leverPlatform.position = Vector3.Lerp(startPos, endPos, curveValue);

            yield return null;
        }

        leverPlatform.position = endPos;
    }

    private IEnumerator HandoverToTruckBed(GameObject suitcase)
    {
        if (suitcase == null) yield break;

        suitcase.transform.SetParent(null);
        
        Vector3 startPos = suitcase.transform.position;
        
        Vector3 endPos;
        Quaternion endRotation;
        
        if (truckBedStack != null)
        {
            endPos = truckBedStack.GetNextStackPosition();
            endRotation = truckBedTarget != null ? truckBedTarget.rotation : Quaternion.identity;
        }
        else if (truckBedTarget != null)
        {
            endPos = truckBedTarget.position;
            endRotation = truckBedTarget.rotation;
        }
        else
        {
            Debug.LogWarning("LeverController: No truck bed target or stack assigned!");
            yield break;
        }
        
        float elapsed = 0f;

        while (elapsed < handoverDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / handoverDuration;
            float arcHeight = Mathf.Sin(t * Mathf.PI) * 1.5f; // Arc curve

            Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);
            currentPos.y += arcHeight;

            suitcase.transform.position = currentPos;
            suitcase.transform.Rotate(Vector3.up, 180f * Time.deltaTime);

            yield return null;
        }

        suitcase.transform.position = endPos;
        suitcase.transform.rotation = endRotation;
        
        if (truckBedStack != null)
        {
            truckBedStack.AddLuggage(suitcase);
        }
        else if (truckBedTarget != null)
        {
            suitcase.transform.SetParent(truckBedTarget);
        }
    }

    private IEnumerator ReturnLeverToStart()
    {
        if (leverPlatform == null) yield break;

        Vector3 startPos = leverPlatform.position;
        Vector3 endPos = leverStartPosition;
        float elapsed = 0f;

        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / returnDuration;
            float curveValue = liftCurve.Evaluate(t);

            leverPlatform.position = Vector3.Lerp(startPos, endPos, curveValue);

            yield return null;
        }

        leverPlatform.position = leverStartPosition;
    }
}