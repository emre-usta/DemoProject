using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

/// <summary>
/// Manages passengers queuing on the upper floor (waypoints 8-13)
/// </summary>
public class UpperFloorQueueManager : MonoBehaviour
{
    [Header("Queue Settings")]
    [SerializeField] private PassengerRoute upperFloorRoute; // Route containing waypoints
    [SerializeField] private int queueStartWaypointIndex = 4; // First passenger position (array index 4 = waypoint 12)
    [SerializeField] private int queueEndWaypointIndex = 0; // Last passenger position (array index 0 = waypoint 8)
    [SerializeField] private int exitWaypointIndex = 5; // Waypoint where passengers are eliminated (array index 5 = waypoint 13)
    
    [Header("Note")]
    [Tooltip("Waypoint indices are treated as array indices (0, 1, 2...). Array index 4 = waypoint 12, array index 5 = waypoint 13.")]
    [SerializeField] private bool useArrayIndices = true; // Always true - using array indices
    
    [Header("Movement Settings")]
    [SerializeField] private float timeBetweenMovements = 0.2f; // Delay between each passenger moving forward
    
    private List<PassengerController> queuedPassengers = new List<PassengerController>();
    private Dictionary<PassengerController, int> passengerWaypointMap = new Dictionary<PassengerController, int>(); // Maps passenger to their current waypoint index (logical or array)
    private bool isProcessingQueue = false;
    private bool isPlayerInTriggerZone = false;
    private float lastExitCheckTime = 0f;
    [SerializeField] private float exitCheckInterval = 0.5f; // How often to check for passengers at exit waypoint
    
    private void Awake()
    {
        // Ensure we have a route
        if (upperFloorRoute == null)
        {
            Debug.LogError("UpperFloorQueueManager: No upper floor route assigned!");
        }
    }
    
    private void Update()
    {
        // Periodically check if any passengers at exit waypoint should be eliminated
        if (Time.time - lastExitCheckTime >= exitCheckInterval)
        {
            lastExitCheckTime = Time.time;
            CheckAndEliminatePassengersAtExit();
        }
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
                if (currentWaypoint >= exitWaypointIndex && passenger.HasReachedUpperFloorWaypoint())
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
    /// Register a passenger that has finished their route and reached the upper floor
    /// </summary>
    public void RegisterPassenger(PassengerController passenger)
    {
        if (passenger == null) return;
        
        // Check if passenger is already registered
        if (queuedPassengers.Contains(passenger))
        {
            Debug.LogWarning($"UpperFloorQueueManager: Passenger {passenger.name} is already registered!");
            return;
        }
        
        // Determine which waypoint this passenger should go to
        // First passenger goes to waypoint 12, second to 11, etc.
        int targetWaypointIndex = queueStartWaypointIndex - queuedPassengers.Count;
        
        // Check if we have space in the queue
        if (targetWaypointIndex < queueEndWaypointIndex)
        {
            Debug.LogWarning($"UpperFloorQueueManager: Queue is full! Cannot register passenger {passenger.name}. Target waypoint {targetWaypointIndex} is below end waypoint {queueEndWaypointIndex}.");
            return;
        }
        
        // Add to queue
        queuedPassengers.Add(passenger);
        passengerWaypointMap[passenger] = targetWaypointIndex;
        
        // Move passenger to their queue position
        MovePassengerToWaypoint(passenger, targetWaypointIndex);
        
        Debug.Log($"UpperFloorQueueManager: Registered passenger {passenger.name} at waypoint {targetWaypointIndex}. Total in queue: {queuedPassengers.Count}");
    }
    
    /// <summary>
    /// Move a passenger to a specific waypoint index (logical or array index)
    /// </summary>
    private void MovePassengerToWaypoint(PassengerController passenger, int waypointIndex)
    {
        if (passenger == null || upperFloorRoute == null) return;
        
        List<Vector3> waypoints = upperFloorRoute.GetWaypoints();
        if (waypoints == null || waypoints.Count == 0)
        {
            Debug.LogError($"UpperFloorQueueManager: No waypoints in route!");
            return;
        }
        
        // Convert logical waypoint number to array index if needed
        int arrayIndex = waypointIndex;
        if (!useArrayIndices)
        {
            // If using logical waypoint numbers (8-13), find the corresponding array index
            // This assumes waypoints are ordered in the route
            // For now, we'll use the waypoint index directly as array index
            // If your route has waypoints in a different order, you may need to map them
            arrayIndex = waypointIndex;
            
            // Safety check: ensure array index is valid
            if (arrayIndex < 0 || arrayIndex >= waypoints.Count)
            {
                Debug.LogError($"UpperFloorQueueManager: Waypoint index {waypointIndex} is out of range! Route has {waypoints.Count} waypoints.");
                return;
            }
        }
        else
        {
            // Using array indices directly
            if (arrayIndex < 0 || arrayIndex >= waypoints.Count)
            {
                Debug.LogError($"UpperFloorQueueManager: Array index {arrayIndex} is out of range! Route has {waypoints.Count} waypoints.");
                return;
            }
        }
        
        Vector3 targetPosition = waypoints[arrayIndex];
        
        // Validate position is on NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPosition, out hit, 5f, NavMesh.AllAreas))
        {
            targetPosition = hit.position;
        }
        
        // Set passenger to move to this waypoint (pass logical index for tracking)
        passenger.SetUpperFloorQueuePosition(targetPosition, waypointIndex);
    }
    
    /// <summary>
    /// Set whether player is in trigger zone (called by trigger)
    /// </summary>
    public void SetPlayerInZone(bool inZone)
    {
        isPlayerInTriggerZone = inZone;
    }
    
    /// <summary>
    /// Check if queue can be advanced and advance if ready
    /// Called continuously while player is in trigger zone
    /// </summary>
    public void AdvanceQueueIfReady()
    {
        if (!isPlayerInTriggerZone || isProcessingQueue || queuedPassengers.Count == 0)
        {
            return;
        }
        
        // Check if any passenger is ready to move forward
        bool hasPassengerReadyToMove = false;
        foreach (var passenger in queuedPassengers)
        {
            if (passenger == null) continue;
            
            int currentWaypoint = passengerWaypointMap[passenger];
            
            // Check if passenger has reached their current waypoint and can move forward
            // Also check if passenger is at exit waypoint and should be eliminated
            if (passenger.HasReachedUpperFloorWaypoint())
            {
                if (currentWaypoint >= exitWaypointIndex)
                {
                    // Passenger is at exit waypoint, should be eliminated
                    hasPassengerReadyToMove = true;
                    break;
                }
                else if (currentWaypoint < exitWaypointIndex)
                {
                    // Passenger can move forward
                    hasPassengerReadyToMove = true;
                    break;
                }
            }
        }
        
        if (hasPassengerReadyToMove)
        {
            StartCoroutine(AdvanceQueueSequence());
        }
    }
    
    /// <summary>
    /// Trigger queue advancement when player enters trigger zone (legacy method, kept for compatibility)
    /// </summary>
    public void AdvanceQueue()
    {
        AdvanceQueueIfReady();
    }
    
    private IEnumerator AdvanceQueueSequence()
    {
        isProcessingQueue = true;
        
        // Find the first passenger that has reached their waypoint and can move forward
        PassengerController passengerToMove = null;
        int passengerIndex = -1;
        
        for (int i = 0; i < queuedPassengers.Count; i++)
        {
            PassengerController passenger = queuedPassengers[i];
            
            if (passenger == null)
            {
                // Remove null passengers
                queuedPassengers.RemoveAt(i);
                passengerWaypointMap.Remove(passenger);
                i--; // Adjust index
                continue;
            }
            
            int currentWaypoint = passengerWaypointMap[passenger];
            
            // Check if passenger has reached their current waypoint
            if (passenger.HasReachedUpperFloorWaypoint())
            {
                int nextWaypoint = currentWaypoint + 1;
                
                // Check if passenger should be eliminated (reached exit waypoint)
                if (currentWaypoint >= exitWaypointIndex)
                {
                    // Passenger has reached exit waypoint, eliminate them sequentially
                    Debug.Log($"UpperFloorQueueManager: Passenger {passenger.name} reached exit waypoint {currentWaypoint}. Eliminating.");
                    yield return StartCoroutine(EliminatePassengerSequentially(passenger));
                    continue;
                }
                
                // This passenger can move forward
                passengerToMove = passenger;
                passengerIndex = i;
                break; // Only move one passenger at a time
            }
        }
        
        if (passengerToMove != null)
        {
            int currentWaypoint = passengerWaypointMap[passengerToMove];
            int nextWaypoint = currentWaypoint + 1;
            
            // Double-check: if passenger is already at exit waypoint, eliminate them
            if (currentWaypoint >= exitWaypointIndex)
            {
                Debug.Log($"UpperFloorQueueManager: Passenger {passengerToMove.name} is at exit waypoint {currentWaypoint}. Eliminating.");
                yield return StartCoroutine(EliminatePassengerSequentially(passengerToMove));
            }
            else
            {
                // Move passenger to next waypoint
                passengerWaypointMap[passengerToMove] = nextWaypoint;
                MovePassengerToWaypoint(passengerToMove, nextWaypoint);
                
                Debug.Log($"UpperFloorQueueManager: Moving passenger {passengerToMove.name} from waypoint {currentWaypoint} to {nextWaypoint}");
                
                // Wait before processing next movement
                yield return new WaitForSeconds(timeBetweenMovements);
            }
        }
        
        isProcessingQueue = false;
    }
    
    /// <summary>
    /// Eliminate a passenger sequentially (with a small delay for visual effect)
    /// </summary>
    private IEnumerator EliminatePassengerSequentially(PassengerController passenger)
    {
        if (passenger == null) yield break;
        
        // Small delay before elimination for sequential effect
        yield return new WaitForSeconds(0.1f);
        
        OnPassengerReachedExit(passenger);
    }
    
    /// <summary>
    /// Called when a passenger reaches the exit waypoint (13) - eliminate them
    /// </summary>
    public void OnPassengerReachedExit(PassengerController passenger)
    {
        if (passenger == null) return;
        
        Debug.Log($"UpperFloorQueueManager: Passenger {passenger.name} reached exit waypoint. Eliminating.");
        
        // Remove from queue first
        if (queuedPassengers.Contains(passenger))
        {
            queuedPassengers.Remove(passenger);
            Debug.Log($"UpperFloorQueueManager: Removed {passenger.name} from queue. Remaining passengers: {queuedPassengers.Count}");
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
    }
    
    /// <summary>
    /// Get the number of passengers currently in queue
    /// </summary>
    public int GetQueueCount()
    {
        return queuedPassengers.Count;
    }
}

