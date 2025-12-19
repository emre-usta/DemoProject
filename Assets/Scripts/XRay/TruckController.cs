using UnityEngine;
using System.Collections;


public class TruckController : MonoBehaviour
{
    [Header("Truck Movement Settings")]
    [SerializeField] private Transform truckTransform;
    [SerializeField] private int requiredSuitcaseCount = 5; 
    [SerializeField] private float forwardDistance = 10f; 
    [SerializeField] private float moveDuration = 3f; 
    [SerializeField] private float returnDuration = 3f; 
    [SerializeField] private float waitAtDropPoint = 1f; 
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("References")]
    [SerializeField] private TruckBedStack truckBedStack; 
    
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
        
        yield return StartCoroutine(MoveTruckForward());
        
        yield return new WaitForSeconds(waitAtDropPoint);
        
        DestroyAllSuitcases();
        
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

    
    public bool IsMoving()
    {
        return isMoving;
    }

    public int GetRequiredSuitcaseCount()
    {
        return requiredSuitcaseCount;
    }
}

