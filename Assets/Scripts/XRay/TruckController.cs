using UnityEngine;
using System.Collections;

/// <summary>
/// Controls truck movement: moves forward to drop point, destroys suitcases, then returns
/// </summary>
public class TruckController : MonoBehaviour
{
    [Header("Truck Movement Settings")]
    [SerializeField] private Transform truckTransform; // The truck GameObject to move
    [SerializeField] private int requiredSuitcaseCount = 5; // Number of suitcases needed before truck moves
    [SerializeField] private float forwardDistance = 10f; // Distance to move forward on Z-axis
    [SerializeField] private float moveDuration = 3f; // Time to move forward
    [SerializeField] private float returnDuration = 3f; // Time to return to start
    [SerializeField] private float waitAtDropPoint = 1f; // Time to wait at drop point before returning
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("References")]
    [SerializeField] private TruckBedStack truckBedStack; // Reference to truck bed stack
    
    private Vector3 truckStartPosition;
    private Vector3 truckDropPosition;
    private bool isMoving = false;

    private void Start()
    {
        // If no truck transform assigned, use this transform
        if (truckTransform == null)
        {
            truckTransform = transform;
        }
        
        // Capture start position
        truckStartPosition = truckTransform.position;
        truckDropPosition = truckStartPosition + Vector3.forward * forwardDistance;
        
        // Try to find TruckBedStack if not assigned
        if (truckBedStack == null)
        {
            truckBedStack = GetComponentInChildren<TruckBedStack>();
        }
        
        Debug.Log($"TruckController: Start position set to {truckStartPosition}, Drop position: {truckDropPosition}");
    }

    /// <summary>
    /// Trigger truck to move forward, drop suitcases, and return
    /// </summary>
    public void StartDeliverySequence()
    {
        if (isMoving)
        {
            Debug.LogWarning("TruckController: Already moving! Ignoring request.");
            return;
        }
        
        StartCoroutine(DeliverySequence());
    }

    private IEnumerator DeliverySequence()
    {
        isMoving = true;
        Debug.Log("TruckController: Starting delivery sequence.");
        
        // Step 1: Move truck forward to drop point
        yield return StartCoroutine(MoveTruckForward());
        
        // Step 2: Wait at drop point
        yield return new WaitForSeconds(waitAtDropPoint);
        
        // Step 3: Destroy all suitcases on truck bed
        DestroyAllSuitcases();
        
        // Step 4: Return truck to start position
        yield return StartCoroutine(ReturnTruckToStart());
        
        isMoving = false;
        Debug.Log("TruckController: Delivery sequence complete.");
    }

    private IEnumerator MoveTruckForward()
    {
        if (truckTransform == null) yield break;
        
        Vector3 startPos = truckTransform.position;
        Vector3 endPos = truckDropPosition;
        float elapsed = 0f;
        
        Debug.Log($"TruckController: Moving forward from {startPos} to {endPos}");
        
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;
            float curveValue = moveCurve.Evaluate(t);
            
            truckTransform.position = Vector3.Lerp(startPos, endPos, curveValue);
            
            yield return null;
        }
        
        truckTransform.position = endPos;
        Debug.Log($"TruckController: Reached drop point at {endPos}");
    }

    private IEnumerator ReturnTruckToStart()
    {
        if (truckTransform == null) yield break;
        
        Vector3 startPos = truckTransform.position;
        Vector3 endPos = truckStartPosition;
        float elapsed = 0f;
        
        Debug.Log($"TruckController: Returning to start from {startPos} to {endPos}");
        
        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / returnDuration;
            float curveValue = moveCurve.Evaluate(t);
            
            truckTransform.position = Vector3.Lerp(startPos, endPos, curveValue);
            
            yield return null;
        }
        
        truckTransform.position = endPos;
        Debug.Log($"TruckController: Returned to start position {endPos}");
    }

    private void DestroyAllSuitcases()
    {
        if (truckBedStack == null)
        {
            Debug.LogWarning("TruckController: No TruckBedStack assigned! Cannot destroy suitcases.");
            return;
        }
        
        int count = truckBedStack.GetStackCount();
        Debug.Log($"TruckController: Destroying {count} suitcases at drop point.");
        
        truckBedStack.ClearStack();
        
        Debug.Log("TruckController: All suitcases destroyed.");
    }

    /// <summary>
    /// Check if truck is currently moving
    /// </summary>
    public bool IsMoving()
    {
        return isMoving;
    }

    /// <summary>
    /// Get the required number of suitcases before truck moves
    /// </summary>
    public int GetRequiredSuitcaseCount()
    {
        return requiredSuitcaseCount;
    }
}

