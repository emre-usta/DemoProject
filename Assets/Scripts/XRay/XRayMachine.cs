using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages the x-ray machine: handles suitcase placement and sliding animation
/// </summary>
public class XRayMachine : MonoBehaviour
{
    [Header("X-Ray Machine Settings")]
    [SerializeField] private Transform entryPoint; // Where player places suitcases
    [SerializeField] private Transform exitPoint;  // Where suitcases exit
    [SerializeField] private float slideDuration = 2f; // Time for suitcase to slide through
    [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Queue Settings")]
    [SerializeField] private float timeBetweenPlacements = 0.5f; // Delay between placing each suitcase
    
    [Header("Lever Connection")]
    [SerializeField] private LeverController leverController; // Reference to lever that receives suitcases
    
    private Queue<GameObject> suitcaseQueue = new Queue<GameObject>();
    private bool isProcessing = false;
    private bool playerInZone = false;
    private PlayerLuggageStack playerLuggageStack;

    private void Start()
    {
        // Find player's luggage stack
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerLuggageStack = player.GetComponent<PlayerLuggageStack>();
        }
    }

    /// <summary>
    /// Called when player enters the trigger zone
    /// </summary>
    public void OnPlayerEnter()
    {
        if (playerLuggageStack == null || playerInZone) return;
        
        playerInZone = true;
        StartCoroutine(ProcessSuitcases());
    }

    /// <summary>
    /// Called when player exits the trigger zone
    /// </summary>
    public void OnPlayerExit()
    {
        playerInZone = false;
    }

    private IEnumerator ProcessSuitcases()
    {
        if (isProcessing) yield break;
        isProcessing = true;

        // Get all suitcases from player
        List<GameObject> suitcases = playerLuggageStack.GetAllLuggage();
        
        if (suitcases == null || suitcases.Count == 0)
        {
            Debug.Log("No suitcases to process.");
            isProcessing = false;
            yield break;
        }

        Debug.Log($"Processing {suitcases.Count} suitcases through x-ray machine.");

        // Process each suitcase one by one
        foreach (GameObject suitcase in suitcases)
        {
            if (suitcase == null) continue;

            // Remove from player's stack
            playerLuggageStack.RemoveLuggage(suitcase);

            // Place suitcase at entry point (with handover animation)
            yield return StartCoroutine(PlaceSuitcaseAtEntry(suitcase));

            // Wait before sliding
            yield return new WaitForSeconds(timeBetweenPlacements);

            // Slide suitcase through machine
            yield return StartCoroutine(SlideSuitcaseThrough(suitcase));

            // Wait before next suitcase
            yield return new WaitForSeconds(timeBetweenPlacements);
        }

        isProcessing = false;
        Debug.Log("All suitcases processed through x-ray machine.");
    }

    private IEnumerator PlaceSuitcaseAtEntry(GameObject suitcase)
    {
        if (suitcase == null || entryPoint == null) yield break;

        // Get player position for handover animation
        Transform playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform == null) yield break;

        Vector3 startPos = suitcase.transform.position;
        Vector3 endPos = entryPoint.position;
        float duration = 1f; // Same as passenger handover duration
        float elapsed = 0f;

        // Animate suitcase from player to entry point (curved path like passenger handover)
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float curveValue = Mathf.Sin(t * Mathf.PI); // Arc curve

            Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);
            currentPos.y += curveValue * 1.5f; // Arc height

            suitcase.transform.position = currentPos;
            suitcase.transform.Rotate(Vector3.up, 180f * Time.deltaTime);

            yield return null;
        }

        // Ensure final position 
        suitcase.transform.position = endPos;
        suitcase.transform.rotation = entryPoint.rotation * Quaternion.Euler(0f, 90f, 90f);

    }

    private IEnumerator SlideSuitcaseThrough(GameObject suitcase)
    {
        if (suitcase == null || entryPoint == null || exitPoint == null) yield break;

        Vector3 startPos = entryPoint.position;
        Vector3 endPos = exitPoint.position;
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideDuration;
            float curveValue = slideCurve.Evaluate(t);

            suitcase.transform.position = Vector3.Lerp(startPos, endPos, curveValue);

            yield return null;
        }

        // Ensure final position
        suitcase.transform.position = endPos;

        // Send suitcase to lever for further processing
        if (leverController != null)
        {
            leverController.ReceiveSuitcase(suitcase);
        }
        else
        {
            Debug.LogWarning("No LeverController assigned! Suitcase will remain at exit point.");
        }
    }
}

