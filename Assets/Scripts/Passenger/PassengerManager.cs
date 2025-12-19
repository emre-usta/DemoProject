using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// Manages passenger spawning, initial queue movement, luggage delivery, and upper floor queue
/// </summary>
public class PassengerManager : MonoBehaviour
{
    // Event for when a passenger is eliminated
    public event Action OnPassengerEliminated;
    [Header("Spawn Settings")]
    [SerializeField] private PassengerController passengerPrefab;
    [SerializeField] private Transform passengerSpawnPointFront; // Front of the line (first passenger spawns here)
    [SerializeField] private Transform passengerSpawnPointBack;  // Back of the line (last passenger spawns here)
    [SerializeField] private int passengerCount = 5; // Total number of passengers to spawn
    [SerializeField] private float followDistance = 1.5f; // Distance between passengers when following each other

    [Header("Luggage Delivery")]
    [SerializeField] private Transform luggageDeliveryPoint; // Where passengers go to deliver luggage
    [SerializeField] private Transform playerLuggageStackPoint; // Where luggage goes on player (PlayerLuggageStack's stackParent)
    [SerializeField] private float timeBetweenDeliveries = 2f; // Time between each passenger delivering luggage

    [Header("Route Settings")]
    [Tooltip("Single route with 13 waypoints (0-12). Waypoints 3-5: escalator, 7-11: queue, 12: exit.")]
    [SerializeField] private PassengerRoute passengerRoute;

    [Header("Upper Floor Queue Settings")]
    [SerializeField] private int queueStartWaypointIndex = 7; // First queue waypoint
    [SerializeField] private int queueEndWaypointIndex = 11; // Last queue waypoint
    [SerializeField] private int exitWaypointIndex = 12; // Exit waypoint where passengers are eliminated
    [SerializeField] private float timeBetweenMovements = 0.2f; // Delay between each passenger moving forward in queue
    [SerializeField] private float exitCheckInterval = 0.5f; // How often to check for passengers at exit waypoint

    // Passenger lists
    private List<PassengerController> passengers = new List<PassengerController>();
    private List<PassengerController> queuedPassengers = new List<PassengerController>(); // Upper floor queue
    private Dictionary<PassengerController, int> passengerWaypointMap = new Dictionary<PassengerController, int>(); // Maps passenger to their current waypoint index

    // State management
    private bool isInitialMovementActive = false; // Whether passengers are following each other
    private bool isProcessingLuggage = false;
    private int processedLuggageCount = 0;
    private int currentLuggageDeliveryIndex = -1; // Index of passenger currently delivering luggage (-1 = none)
    private bool isProcessingQueue = false;
    private bool isPlayerInTriggerZone = false;
    private float lastExitCheckTime = 0f;

    private void Start()
    {
        SpawnPassengers();
    }

    private void Update()
    {
        // Update initial queue movement (passengers following each other)
        if (isInitialMovementActive)
        {
            UpdateInitialQueueMovement();
        }

        // Periodically check if any passengers at exit waypoint should be eliminated
        if (Time.time - lastExitCheckTime >= exitCheckInterval)
        {
            lastExitCheckTime = Time.time;
            CheckAndEliminatePassengersAtExit();
        }
    }

    /// <summary>
    /// Spawn all passengers at once between front and back spawn points
    /// </summary>
    private void SpawnPassengers()
    {
        if (passengerPrefab == null || passengerSpawnPointFront == null || passengerSpawnPointBack == null)
        {
            Debug.LogError("PassengerManager: Missing required spawn points or prefab!");
            return;
        }

        passengers.Clear();

        // Calculate spawn positions evenly distributed between front and back
        Vector3 frontPos = passengerSpawnPointFront.position;
        Vector3 backPos = passengerSpawnPointBack.position;
        Vector3 direction = (backPos - frontPos).normalized;
        float totalDistance = Vector3.Distance(frontPos, backPos);
        float spacing = passengerCount > 1 ? totalDistance / (passengerCount - 1) : 0f;

        // Spawn all passengers at once
        for (int i = 0; i < passengerCount; i++)
        {
            Vector3 spawnPos = frontPos + direction * (i * spacing);
            
            // Ensure spawn position is on NavMesh
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
            {
                spawnPos = hit.position;
            }

            PassengerController passenger = Instantiate(passengerPrefab, spawnPos, Quaternion.Euler(0f, -90f, 0f));
            passengers.Add(passenger);
        }

        Debug.Log($"PassengerManager: Spawned {passengers.Count} passengers.");
    }

    /// <summary>
    /// Start initial movement where all passengers follow each other
    /// </summary>
    public void StartInitialMovement()
    {
        if (passengers.Count == 0) return;

        isInitialMovementActive = true;
        Debug.Log("PassengerManager: Starting initial queue movement.");
    }

    /// <summary>
    /// Trigger passenger movement (alias for StartInitialMovement for compatibility)
    /// </summary>
    public void TriggerPassengerMovement()
    {
        StartInitialMovement();
    }

    /// <summary>
    /// Update passengers following each other in initial queue
    /// Only one passenger at a time goes to luggage delivery point
    /// Other passengers fill the gap by moving forward in the queue
    /// </summary>
    private void UpdateInitialQueueMovement()
    {
        if (passengers.Count == 0) return;

        // Check if we need to start the first passenger
        if (currentLuggageDeliveryIndex == -1 && luggageDeliveryPoint != null)
        {
            // Start first passenger
            if (passengers[0] != null && passengers[0].CurrentState == PassengerController.PassengerState.WaitingInQueue)
            {
                currentLuggageDeliveryIndex = 0;
                passengers[0].MoveToLuggageDeliveryPoint(luggageDeliveryPoint.position);
                Debug.Log($"PassengerManager: Passenger 0 started moving to luggage delivery point.");
            }
        }

        // Check if current passenger has completed delivery and we can move to next
        if (currentLuggageDeliveryIndex >= 0 && currentLuggageDeliveryIndex < passengers.Count)
        {
            PassengerController currentPassenger = passengers[currentLuggageDeliveryIndex];
            
            // If current passenger has started route, move to next passenger
            if (currentPassenger != null && 
                (currentPassenger.CurrentState == PassengerController.PassengerState.FollowingRoute ||
                 currentPassenger.CurrentState == PassengerController.PassengerState.Finished))
            {
                // Current passenger has completed delivery, move to next
                currentLuggageDeliveryIndex++;
                
                // Start next passenger if available
                if (currentLuggageDeliveryIndex < passengers.Count)
                {
                    PassengerController nextPassenger = passengers[currentLuggageDeliveryIndex];
                    if (nextPassenger != null && 
                        nextPassenger.CurrentState == PassengerController.PassengerState.WaitingInQueue &&
                        luggageDeliveryPoint != null)
                    {
                        nextPassenger.MoveToLuggageDeliveryPoint(luggageDeliveryPoint.position);
                        Debug.Log($"PassengerManager: Passenger {currentLuggageDeliveryIndex} started moving to luggage delivery point.");
                    }
                }
            }
        }

        // Calculate queue positions for passengers behind the current delivery passenger
        // They should fill the gap by moving forward
        if (passengerSpawnPointFront != null && passengerSpawnPointBack != null)
        {
            Vector3 frontPos = passengerSpawnPointFront.position;
            Vector3 backPos = passengerSpawnPointBack.position;
            Vector3 direction = (backPos - frontPos).normalized;
            float totalDistance = Vector3.Distance(frontPos, backPos);
            float spacing = passengerCount > 1 ? totalDistance / (passengerCount - 1) : 0f;

            // Move passengers behind the current delivery passenger forward to fill gaps
            for (int i = currentLuggageDeliveryIndex + 1; i < passengers.Count; i++)
            {
                if (passengers[i] == null) continue;

                // Only move passengers that are still waiting in queue
                if (passengers[i].CurrentState == PassengerController.PassengerState.WaitingInQueue)
                {
                    // Calculate target position: passenger should be at position (i - currentLuggageDeliveryIndex - 1)
                    // This fills the gap left by passengers who have moved to delivery point
                    int queuePosition = i - currentLuggageDeliveryIndex - 1;
                    Vector3 targetPos = frontPos + direction * (queuePosition * spacing);
                    
                    // Ensure target position is on NavMesh
                    UnityEngine.AI.NavMeshHit hit;
                    if (UnityEngine.AI.NavMesh.SamplePosition(targetPos, out hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
                    {
                        targetPos = hit.position;
                    }

                    // Move passenger to fill the gap
                    passengers[i].FollowPassenger(targetPos);
                }
            }
        }
    }

    /// <summary>
    /// Start luggage delivery sequence (called when first passenger reaches delivery point)
    /// This now just starts the initial movement - actual delivery is handled in UpdateInitialQueueMovement
    /// </summary>
    public void StartLuggageDelivery()
    {
        if (isProcessingLuggage || passengers.Count == 0) return;

        isProcessingLuggage = true;
        processedLuggageCount = 0;
        currentLuggageDeliveryIndex = -1; // Reset to start from first passenger
        
        // Start monitoring luggage deliveries
        StartCoroutine(MonitorLuggageDeliveries());
    }

    /// <summary>
    /// Monitor luggage deliveries and assign routes when passengers complete delivery
    /// </summary>
    private IEnumerator MonitorLuggageDeliveries()
    {
        Debug.Log($"PassengerManager: Starting MonitorLuggageDeliveries coroutine. Total passengers: {passengers.Count}");
        
        while (processedLuggageCount < passengers.Count)
        {
            if (processedLuggageCount >= passengers.Count)
            {
                Debug.Log($"PassengerManager: All passengers processed. Exiting coroutine.");
                break;
            }

            PassengerController currentPassenger = passengers[processedLuggageCount];

            if (currentPassenger == null)
            {
                Debug.LogWarning($"PassengerManager: Passenger {processedLuggageCount} is null. Skipping.");
                processedLuggageCount++;
                continue;
            }

            Debug.Log($"PassengerManager: [START] Monitoring passenger {processedLuggageCount}. Current state: {currentPassenger.CurrentState}, IsDeliveryComplete: {currentPassenger.IsDeliveryComplete()}");

            // Wait for passenger to reach delivery point and start delivering
            float waitTimeout = 30f; // Maximum wait time (safety timeout)
            float waitStartTime = Time.time;
            
            while (currentPassenger != null && 
                   currentPassenger.CurrentState != PassengerController.PassengerState.DeliveringLuggage &&
                   !currentPassenger.IsDeliveryComplete() &&
                   (Time.time - waitStartTime) < waitTimeout)
            {
                yield return new WaitForSeconds(0.1f);
            }

            if (currentPassenger == null)
            {
                Debug.LogWarning($"PassengerManager: Passenger {processedLuggageCount} became null while waiting. Skipping.");
                processedLuggageCount++;
                continue;
            }

            if ((Time.time - waitStartTime) >= waitTimeout)
            {
                Debug.LogWarning($"PassengerManager: Timeout waiting for passenger {processedLuggageCount} to reach delivery state. Skipping.");
                processedLuggageCount++;
                continue;
            }

            Debug.Log($"PassengerManager: Passenger {processedLuggageCount} reached delivery state. Current state: {currentPassenger.CurrentState}, IsDeliveryComplete: {currentPassenger.IsDeliveryComplete()}");

            // Wait for luggage delivery to complete (luggageHandedOver = true)
            waitStartTime = Time.time;
            while (currentPassenger != null && 
                   !currentPassenger.IsDeliveryComplete() &&
                   (Time.time - waitStartTime) < waitTimeout)
            {
                yield return new WaitForSeconds(0.1f);
            }

            if (currentPassenger == null)
            {
                Debug.LogWarning($"PassengerManager: Passenger {processedLuggageCount} became null while waiting for delivery. Skipping.");
                processedLuggageCount++;
                continue;
            }

            if ((Time.time - waitStartTime) >= waitTimeout)
            {
                Debug.LogWarning($"PassengerManager: Timeout waiting for passenger {processedLuggageCount} to complete delivery. Skipping.");
                processedLuggageCount++;
                continue;
            }

            Debug.Log($"PassengerManager: Passenger {processedLuggageCount} completed delivery. IsDeliveryComplete: {currentPassenger.IsDeliveryComplete()}, Current state: {currentPassenger.CurrentState}");

            // Assign route to passenger
            if (passengerRoute != null)
            {
                var waypoints = passengerRoute.GetWaypoints();
                if (waypoints != null && waypoints.Count == 13)
                {
                    // Check current state before setting route
                    Debug.Log($"PassengerManager: [ROUTE] Assigning route to passenger {processedLuggageCount}. Current state: {currentPassenger.CurrentState}, IsDeliveryComplete: {currentPassenger.IsDeliveryComplete()}");
                    
                    // CRITICAL: Set route - SetRoute() should handle state transition
                    currentPassenger.SetRoute(waypoints);
                    
                    // Give it a moment for SetRoute to process
                    yield return new WaitForSeconds(0.2f);
                    
                    // CRITICAL: Double-check state and force if needed
                    // This is a safety net to ensure state transition happens
                    if (currentPassenger.CurrentState != PassengerController.PassengerState.FollowingRoute)
                    {
                        Debug.LogWarning($"PassengerManager: [FORCE] Passenger {processedLuggageCount} did not start route automatically. Current state: {currentPassenger.CurrentState}. Forcing state change.");
                        
                        // Force state change - SetState will handle agent resume
                        currentPassenger.SetState(PassengerController.PassengerState.FollowingRoute);
                        
                        // Ensure agent is not stopped (SetState should handle this, but double-check)
                        var agent = currentPassenger.GetComponent<UnityEngine.AI.NavMeshAgent>();
                        if (agent != null)
                        {
                            agent.isStopped = false;
                        }
                        
                        Debug.Log($"PassengerManager: [FORCE] Forced passenger {processedLuggageCount} to FollowingRoute state. New state: {currentPassenger.CurrentState}");
                    }
                    else
                    {
                        Debug.Log($"PassengerManager: [SUCCESS] Passenger {processedLuggageCount} started following route successfully. State: {currentPassenger.CurrentState}");
                    }
                }
                else
                {
                    Debug.LogWarning($"PassengerManager: Route should have 13 waypoints, but has {waypoints?.Count ?? 0}!");
                }
            }
            else
            {
                Debug.LogWarning($"PassengerManager: Cannot assign route - route is null!");
            }

            Debug.Log($"PassengerManager: [END] Finished processing passenger {processedLuggageCount}. Moving to next passenger.");
            processedLuggageCount++;
            yield return new WaitForSeconds(timeBetweenDeliveries);
        }

        isProcessingLuggage = false;
        Debug.Log($"PassengerManager: [COMPLETE] All passengers have completed luggage delivery. Processed: {processedLuggageCount}/{passengers.Count}");
    }

    #region Upper Floor Queue Management

    /// <summary>
    /// Register a passenger that reached waypoint 7 (queue start)
    /// </summary>
    public void RegisterPassengerForQueue(PassengerController passenger)
    {
        if (passenger == null) return;

        // Check if passenger is already registered
        if (queuedPassengers.Contains(passenger))
        {
            Debug.LogWarning($"PassengerManager: Passenger {passenger.name} is already registered!");
            return;
        }

        // Determine which waypoint this passenger should go to
        // First passenger goes to waypoint 11, second to 10, third to 9, fourth to 8, fifth to 7
        int targetWaypointIndex = queueEndWaypointIndex - queuedPassengers.Count;

        // Check if we have space in the queue
        if (targetWaypointIndex < queueStartWaypointIndex)
        {
            Debug.LogWarning($"PassengerManager: Queue is full! Cannot register passenger {passenger.name}.");
            return;
        }

        // Add to queue
        queuedPassengers.Add(passenger);
        passengerWaypointMap[passenger] = targetWaypointIndex;

        // Move passenger to their queue position
        passenger.SetQueueWaypoint(targetWaypointIndex);

        Debug.Log($"PassengerManager: Registered passenger {passenger.name} at waypoint {targetWaypointIndex}. Total in queue: {queuedPassengers.Count}");
    }

    /// <summary>
    /// Set whether player is in trigger zone (called by trigger)
    /// </summary>
    public void SetPlayerInTriggerZone(bool inZone)
    {
        isPlayerInTriggerZone = inZone;
        
        if (inZone)
        {
            // Start checking for queue advancement
            StartCoroutine(CheckQueueAdvancement());
        }
    }

    /// <summary>
    /// Set whether player is in upper floor trigger zone (alias for SetPlayerInTriggerZone for compatibility)
    /// </summary>
    public void SetPlayerInUpperFloorZone(bool inZone)
    {
        SetPlayerInTriggerZone(inZone);
    }

    /// <summary>
    /// Continuously check if queue can be advanced while player is in trigger zone
    /// </summary>
    private IEnumerator CheckQueueAdvancement()
    {
        while (isPlayerInTriggerZone)
        {
            if (!isProcessingQueue && queuedPassengers.Count > 0)
            {
                AdvanceQueueIfReady();
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    /// <summary>
    /// Check if queue can be advanced and advance if ready
    /// </summary>
    private void AdvanceQueueIfReady()
    {
        if (isProcessingQueue || queuedPassengers.Count == 0) return;

        // Find the first passenger that has reached their waypoint and can move forward
        for (int i = 0; i < queuedPassengers.Count; i++)
        {
            PassengerController passenger = queuedPassengers[i];
            if (passenger == null) continue;

            if (passengerWaypointMap.ContainsKey(passenger))
            {
                int currentWaypoint = passengerWaypointMap[passenger];

                // Check if passenger has reached their current waypoint
                if (passenger.HasReachedQueueWaypoint())
                {
                    // Check if passenger should be eliminated (reached exit waypoint 12)
                    if (currentWaypoint == exitWaypointIndex)
                    {
                        StartCoroutine(EliminatePassengerSequentially(passenger));
                        return;
                    }
                    // Check if passenger can move forward
                    else if (currentWaypoint < exitWaypointIndex)
                    {
                        StartCoroutine(AdvancePassengerInQueue(passenger, currentWaypoint));
                        return;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Advance upper floor queue if ready (public method for trigger compatibility)
    /// </summary>
    public void AdvanceUpperFloorQueueIfReady()
    {
        AdvanceQueueIfReady();
    }

    /// <summary>
    /// Advance a passenger to the next waypoint in queue
    /// </summary>
    private IEnumerator AdvancePassengerInQueue(PassengerController passenger, int currentWaypoint)
    {
        isProcessingQueue = true;

        int nextWaypoint = currentWaypoint + 1;
        passengerWaypointMap[passenger] = nextWaypoint;
        passenger.SetQueueWaypoint(nextWaypoint);

        Debug.Log($"PassengerManager: Moving passenger {passenger.name} from waypoint {currentWaypoint} to {nextWaypoint}");

        yield return new WaitForSeconds(timeBetweenMovements);
        isProcessingQueue = false;
    }

    /// <summary>
    /// Eliminate a passenger sequentially (with a small delay for visual effect)
    /// </summary>
    private IEnumerator EliminatePassengerSequentially(PassengerController passenger)
    {
        isProcessingQueue = true;

        if (passenger == null) yield break;

        yield return new WaitForSeconds(0.1f);

        OnPassengerReachedExit(passenger);
        isProcessingQueue = false;
    }

    /// <summary>
    /// Check for passengers at exit waypoint and eliminate them
    /// </summary>
    private void CheckAndEliminatePassengersAtExit()
    {
        if (queuedPassengers.Count == 0 || isProcessingQueue) return;

        List<PassengerController> passengersToEliminate = new List<PassengerController>();

        foreach (var passenger in queuedPassengers)
        {
            if (passenger == null) continue;

            if (passengerWaypointMap.ContainsKey(passenger))
            {
                int currentWaypoint = passengerWaypointMap[passenger];

                // If passenger is at exit waypoint and has reached it, mark for elimination
                if (currentWaypoint == exitWaypointIndex && passenger.HasReachedQueueWaypoint())
                {
                    passengersToEliminate.Add(passenger);
                }
            }
        }

        // Eliminate passengers sequentially
        if (passengersToEliminate.Count > 0)
        {
            StartCoroutine(EliminatePassengersSequentially(passengersToEliminate));
        }
    }

    /// <summary>
    /// Eliminate multiple passengers sequentially
    /// </summary>
    private IEnumerator EliminatePassengersSequentially(List<PassengerController> passengers)
    {
        isProcessingQueue = true;

        foreach (var passenger in passengers)
        {
            if (passenger != null)
            {
                yield return StartCoroutine(EliminatePassengerSequentially(passenger));
            }
        }

        isProcessingQueue = false;
    }

    /// <summary>
    /// Called when a passenger reaches the exit waypoint (12) - eliminate them
    /// </summary>
    public void OnPassengerReachedExit(PassengerController passenger)
    {
        if (passenger == null) return;

        Debug.Log($"PassengerManager: Passenger {passenger.name} reached exit waypoint. Eliminating.");

        // Remove from queue first
        if (queuedPassengers.Contains(passenger))
        {
            queuedPassengers.Remove(passenger);
        }

        if (passengerWaypointMap.ContainsKey(passenger))
        {
            passengerWaypointMap.Remove(passenger);
        }

        // Destroy passenger
        if (passenger != null && passenger.gameObject != null)
        {
            Destroy(passenger.gameObject);
        }

        // Trigger event for passenger counter
        OnPassengerEliminated?.Invoke();
    }

    /// <summary>
    /// Get the player's luggage stack point transform
    /// </summary>
    public Transform GetPlayerLuggageStackPoint()
    {
        if (playerLuggageStackPoint != null)
        {
            return playerLuggageStackPoint;
        }

        // Fallback: try to find PlayerLuggageStack component
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerLuggageStack stack = player.GetComponent<PlayerLuggageStack>();
            if (stack != null)
            {
                // Use reflection or public method to get stackParent
                // For now, return player transform
                return player.transform;
            }
        }

        return null;
    }

    #endregion
}