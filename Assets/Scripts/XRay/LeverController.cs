using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Controls the lever mechanism: receives suitcases from x-ray, lifts them, and sends to truck bed
/// </summary>
public class LeverController : MonoBehaviour
{
    [Header("Lever Settings")]
    [SerializeField] private Transform leverPlatform; // The platform that lifts (the lever itself)
    [SerializeField] private Transform truckBedTarget; // Where suitcases go on the truck bed (base position)
    [SerializeField] private TruckBedStack truckBedStack; // Reference to truck bed stacking system
    [SerializeField] private TruckController truckController; // Reference to truck controller
    [SerializeField] private float liftHeight = 2f; // How high the lever lifts
    [SerializeField] private float liftDuration = 1.5f; // Time to lift the lever
    [SerializeField] private float returnDuration = 1f; // Time to return lever to start position
    [SerializeField] private AnimationCurve liftCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Suitcase Settings")]
    [SerializeField] private Transform suitcaseLandingPoint; // Where suitcases land on lever (child of leverPlatform)
    [SerializeField] private float handoverDuration = 1f; // Duration for handover animations
    
    private Vector3 leverStartPosition;
    private Queue<GameObject> suitcaseQueue = new Queue<GameObject>();
    private bool isProcessing = false;
    private GameObject currentSuitcase = null;

    private void Start()
    {
        if (leverPlatform == null)
        {
            leverPlatform = transform; // Use this object if no platform assigned
        }
        
        leverStartPosition = leverPlatform.position;
        
        // Create landing point if not assigned
        if (suitcaseLandingPoint == null)
        {
            GameObject landingPoint = new GameObject("SuitcaseLandingPoint");
            landingPoint.transform.SetParent(leverPlatform);
            landingPoint.transform.localPosition = Vector3.zero;
            suitcaseLandingPoint = landingPoint.transform;
        }

        // Try to find TruckBedStack if not assigned
        if (truckBedStack == null && truckBedTarget != null)
        {
            truckBedStack = truckBedTarget.GetComponent<TruckBedStack>();
            if (truckBedStack == null)
            {
                // Try to find it in children
                truckBedStack = truckBedTarget.GetComponentInChildren<TruckBedStack>();
            }
        }

        // Try to find TruckController if not assigned
        if (truckController == null)
        {
            truckController = FindObjectOfType<TruckController>();
        }
    }

    /// <summary>
    /// Called when a suitcase arrives at the lever from x-ray machine
    /// </summary>
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

            // Step 1: Animate suitcase from x-ray exit to lever (using LuggageHandover)
            yield return StartCoroutine(HandoverToLever(currentSuitcase));

            // Step 2: Lift the lever (with suitcase on it) to truck bed height
            yield return StartCoroutine(LiftLeverWithSuitcase());

            // Step 3: Animate suitcase from lever to truck bed (using LuggageHandover)
            yield return StartCoroutine(HandoverToTruckBed(currentSuitcase));

            // Step 4: Return lever to start position
            yield return StartCoroutine(ReturnLeverToStart());

            currentSuitcase = null;
            
            // Check if we've reached the required number of suitcases to trigger truck
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
        Vector3 endPos = suitcaseLandingPoint.position; // World position of landing point
        float elapsed = 0f;

        // Use LuggageHandover-style curved animation
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

        // Ensure final position and parent to lever
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
            // Suitcase moves with lever since it's parented to suitcaseLandingPoint

            yield return null;
        }

        leverPlatform.position = endPos;
    }

    private IEnumerator HandoverToTruckBed(GameObject suitcase)
    {
        if (suitcase == null) yield break;

        // Unparent suitcase from lever
        suitcase.transform.SetParent(null);
        
        Vector3 startPos = suitcase.transform.position;
        
        // Get the next stack position from truck bed stack
        Vector3 endPos;
        Quaternion endRotation;
        
        if (truckBedStack != null)
        {
            // Use stacking system to get the next position
            endPos = truckBedStack.GetNextStackPosition();
            endRotation = truckBedTarget != null ? truckBedTarget.rotation : Quaternion.identity;
        }
        else if (truckBedTarget != null)
        {
            // Fallback to truck bed target position if no stack system
            endPos = truckBedTarget.position;
            endRotation = truckBedTarget.rotation;
        }
        else
        {
            Debug.LogWarning("LeverController: No truck bed target or stack assigned!");
            yield break;
        }
        
        float elapsed = 0f;

        // Use LuggageHandover-style curved animation
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

        // Ensure final position
        suitcase.transform.position = endPos;
        suitcase.transform.rotation = endRotation;
        
        // Add to truck bed stack (this will handle parenting and positioning)
        if (truckBedStack != null)
        {
            truckBedStack.AddLuggage(suitcase);
        }
        else if (truckBedTarget != null)
        {
            // Fallback: just parent to truck bed target
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