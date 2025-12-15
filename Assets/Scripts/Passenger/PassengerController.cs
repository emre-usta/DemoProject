using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

public class PassengerController : MonoBehaviour
{
    public enum PassengerState
    {
        WaitingInQueue,
        MovingToPlayer,
        DeliveringLuggage,
        FollowingRoute,
        CheckingTicket,
        Boarding,
        Finished
    }

    public PassengerState CurrentState { get; private set; }

    [Header("Luggage Settings")]
    [SerializeField] private GameObject luggagePrefab; // Prefab for suitcase
    [SerializeField] private Vector3 luggageLocalOffset = new Vector3(0, 1.5f, 0.3f); // Local offset from passenger center
    [SerializeField] private float luggageDeliveryTime = 1f;

    [Header("Movement Settings")]
    private Vector3 targetQueuePosition;
    private Vector3 targetPosition;
    private float moveSpeed = 5f;
    private bool hasQueuePosition = false;
    private bool hasTarget = false;

    [Header("Route Following")]
    private List<Vector3> routeWaypoints = new List<Vector3>();
    private int currentWaypointIndex = 0;
    private float waypointReachDistance = 0.5f;
    [SerializeField] private LayerMask obstacleLayerMask = -1; // Layers to consider as obstacles (not used after simplification)
    [Header("Escalator Settings")]
    [SerializeField] private int escalatorStartWaypointIndex = 4; // inclusive
    [SerializeField] private int escalatorEndWaypointIndex = 7;   // inclusive
    private bool needsToBackAway = false;
    private float backAwayTimer = 0f;
    private float backAwayDuration = 0.5f; // Time to back away from delivery point

    private Transform playerTransform;
    private PlayerLuggageStack playerLuggageStack;
    private GameObject luggageObject;
    private LuggageHandover luggageHandover;
    private Rigidbody rb;
    private NavMeshAgent agent;
    private bool isDeliveringLuggage = false;
    private bool luggageHandedOver = false;
    private float luggageDeliveryTimer;
    private Animator animator;
    
    // Separation handling to prevent passengers from overlapping
    private static readonly List<PassengerController> AllPassengers = new List<PassengerController>();
    [SerializeField] private float separationDistance = 0.8f; // desired minimum spacing
    [SerializeField] private float separationForce = 2f;     // push strength


    private void Start()
    {
        SetState(PassengerState.WaitingInQueue);
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        playerLuggageStack = playerTransform?.GetComponent<PlayerLuggageStack>();
        animator = GetComponent<Animator>();
        
        // Get or add NavMeshAgent for movement
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
        }
        ConfigureAgent();
        
        // Keep Rigidbody kinematic (if present) to avoid physics interference
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        
        // Create luggage handover component
        luggageHandover = gameObject.AddComponent<LuggageHandover>();
        
        // Spawn suitcase for this passenger
        SpawnLuggage();
        
        // Register for separation
        if (!AllPassengers.Contains(this))
            AllPassengers.Add(this);

        // Ensure we are on the NavMesh (initial spawn)
        TryWarpToNavMesh(transform.position);
    }

    private void OnDestroy()
    {
        AllPassengers.Remove(this);
    }

    private void SpawnLuggage()
    {
        if (luggagePrefab == null) return;

        // Create suitcase as child of this passenger
        luggageObject = Instantiate(luggagePrefab, transform);
        
        // Position suitcase relative to this passenger using local offset
        luggageObject.transform.localPosition = luggageLocalOffset;
        luggageObject.transform.localRotation = Quaternion.identity;
        
        // Ensure suitcase is visible from the start
        luggageObject.SetActive(true);
    }

    private void Update()
    {
        switch (CurrentState)
        {
            case PassengerState.WaitingInQueue:
                UpdateQueueMovement();
                break;
            case PassengerState.MovingToPlayer:
                UpdateMoveToPlayer();
                break;
            case PassengerState.DeliveringLuggage:
                UpdateLuggageDelivery();
                break;
            case PassengerState.FollowingRoute:
                UpdateRouteFollowing();
                break;
        }
        
        // Apply separation to keep spacing between passengers
        ApplySeparation();
        
        UpdateAnimation();
    }

    private void UpdateQueueMovement()
    {
        if (agent == null || !agent.isOnNavMesh) return;
        if (hasQueuePosition)
        {
            SetAgentDestination(targetQueuePosition);
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.05f)
                hasQueuePosition = false;
        }
    }

    private void UpdateMoveToPlayer()
    {
        if (agent == null || !agent.isOnNavMesh) return;
        if (playerTransform != null && hasTarget)
        {
            SetAgentDestination(targetPosition);

            // Check if reached player (stand opposite the table)
            if (!agent.pathPending && agent.remainingDistance <= 0.8f)
            {
                FaceDirection(playerTransform.position - transform.position);
                SetState(PassengerState.DeliveringLuggage);
            }
        }
    }

    private void UpdateLuggageDelivery()
    {
        if (!isDeliveringLuggage)
        {
            isDeliveringLuggage = true;
            luggageDeliveryTimer = 0f;
            luggageHandedOver = false;
            
            // Start handover animation
            if (luggageObject != null && playerTransform != null && luggageHandover != null)
            {
                // Unparent luggage so it can move independently
                luggageObject.transform.SetParent(null);
                
                // Start curved handover animation
                luggageHandover.StartHandover(luggageObject, transform, playerTransform, OnHandoverComplete);
            }
        }

        // Wait for handover to complete
        if (luggageHandedOver)
        {
            // Back away from delivery point first to avoid collision with player
            if (!needsToBackAway)
            {
                needsToBackAway = true;
                backAwayTimer = 0f;
            }
            
            backAwayTimer += Time.deltaTime;
            
            if (backAwayTimer < backAwayDuration)
            {
                // Back away from player/delivery point
                if (playerTransform != null)
                {
                    // Temporarily stop agent and move a bit back manually
                    agent.isStopped = true;
                    Vector3 awayFromPlayer = (transform.position - playerTransform.position).normalized;
                    if (awayFromPlayer == Vector3.zero)
                        awayFromPlayer = -transform.forward; // Fallback

                    Vector3 backAwayPosition = transform.position + awayFromPlayer * moveSpeed * Time.deltaTime * 0.5f;
                    transform.position = backAwayPosition;
                }
            }
            else
            {
                // Finished backing away, start following route
                needsToBackAway = false;
                agent.isStopped = false;
                if (routeWaypoints.Count > 0)
                {
                    SetState(PassengerState.FollowingRoute);
                }
                else
                {
                    SetState(PassengerState.Finished);
                }
            }
        }
    }

    private void OnHandoverComplete()
    {
        // Add luggage to player's stack
        if (luggageObject != null && playerLuggageStack != null)
        {
            playerLuggageStack.AddLuggage(luggageObject);
        }
        
        luggageHandedOver = true;
        luggageObject = null; // Clear reference since it's now in player's stack
    }

    private void UpdateRouteFollowing()
    {
        if (agent == null || !agent.isOnNavMesh) return;
        if (routeWaypoints.Count == 0)
        {
            Debug.LogWarning($"{gameObject.name}: No waypoints in route!");
            SetState(PassengerState.Finished);
            return;
        }

        if (currentWaypointIndex >= routeWaypoints.Count)
        {
            SetState(PassengerState.Finished);
            return;
        }

        Vector3 targetWaypoint = routeWaypoints[currentWaypointIndex];
        
        // Validate waypoint is on NavMesh, if not, find nearest point
        Vector3 validatedWaypoint = ValidateWaypointOnNavMesh(targetWaypoint);
        SetAgentDestination(validatedWaypoint);

        // Check if path is valid
        if (!agent.pathPending && agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            Debug.LogWarning($"{gameObject.name}: Invalid path to waypoint {currentWaypointIndex}. Trying to find alternative.");
            // Try to find nearest valid point
            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetWaypoint, out hit, 5f, NavMesh.AllAreas))
            {
                SetAgentDestination(hit.position);
            }
            else
            {
                // Skip this waypoint if we can't find a valid path
                Debug.LogWarning($"{gameObject.name}: Skipping waypoint {currentWaypointIndex} - cannot find valid NavMesh position.");
                currentWaypointIndex++;
                if (currentWaypointIndex >= routeWaypoints.Count)
                {
                    SetState(PassengerState.Finished);
                }
            }
            return;
        }

        // Check if reached waypoint
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + waypointReachDistance)
        {
            // Reached waypoint, move to next
            currentWaypointIndex++;
            Debug.Log($"{gameObject.name}: Reached waypoint {currentWaypointIndex - 1}/{routeWaypoints.Count}");
            
            if (currentWaypointIndex >= routeWaypoints.Count)
            {
                // Finished route
                SetState(PassengerState.Finished);
                Debug.Log($"{gameObject.name}: Finished following route.");
            }
            else
            {
                // Set next waypoint immediately
                Vector3 nextWaypoint = ValidateWaypointOnNavMesh(routeWaypoints[currentWaypointIndex]);
                SetAgentDestination(nextWaypoint);
            }
        }
    }

    private Vector3 ValidateWaypointOnNavMesh(Vector3 waypoint)
    {
        NavMeshHit hit;
        // Check if waypoint is on NavMesh, if not find nearest point within 5 units
        if (NavMesh.SamplePosition(waypoint, out hit, 5f, NavMesh.AllAreas))
        {
            return hit.position;
        }
        // If can't find nearby NavMesh, return original (will be handled by path validation)
        return waypoint;
    }

    public void SetState(PassengerState newState)
    {
        CurrentState = newState;
        
        // Reset state-specific variables
        if (newState == PassengerState.DeliveringLuggage)
        {
            isDeliveringLuggage = false;
            luggageDeliveryTimer = 0f;
            luggageHandedOver = false;
            needsToBackAway = false;
            if (agent != null) agent.isStopped = true;
        }
        else if (newState == PassengerState.FollowingRoute)
        {
            currentWaypointIndex = 0;
            needsToBackAway = false;
            if (agent != null) agent.isStopped = false;
            Debug.Log($"{gameObject.name}: Starting to follow route with {routeWaypoints.Count} waypoints.");
        }
    }

    public void AssignQueuePosition(Vector3 position)
    {
        targetQueuePosition = position;
        hasQueuePosition = true;
        TryWarpToNavMesh(position);
        SetAgentDestination(targetQueuePosition);
    }

    public void MoveToPlayer(Vector3 playerPosition)
    {
        targetPosition = playerPosition;
        hasTarget = true;
        TryWarpToNavMesh(transform.position);
        SetAgentDestination(targetPosition);
        SetState(PassengerState.MovingToPlayer);
    }

    public void SetRoute(List<Vector3> waypoints)
    {
        if (waypoints == null || waypoints.Count == 0)
        {
            Debug.LogWarning($"{gameObject.name}: Attempted to set empty route!");
            routeWaypoints = new List<Vector3>();
            return;
        }
        
        routeWaypoints = new List<Vector3>(waypoints);
        currentWaypointIndex = 0;
        Debug.Log($"{gameObject.name}: Route set with {waypoints.Count} waypoints.");
    }

    private void OnLuggageDelivered()
    {
        // Notify manager that luggage was delivered
        // This can be extended with events later
        Debug.Log($"{gameObject.name} delivered luggage to player.");
    }

    public float GetMoveSpeed()
    {
        return moveSpeed;
    }

    public bool HasLuggage()
    {
        return luggageObject != null && luggageObject.activeSelf;
    }
    
    // Indicates that this passenger finished handing over luggage and started/finished the route
    public bool IsDeliveryComplete()
    {
        return luggageHandedOver || CurrentState == PassengerState.FollowingRoute || CurrentState == PassengerState.Finished;
    }
    
    private void UpdateAnimation()
    {
        if (animator == null) return;

        float currentSpeed = 0f;

        if (CurrentState == PassengerState.WaitingInQueue && hasQueuePosition)
        {
            currentSpeed = agent != null ? agent.velocity.magnitude : moveSpeed;
        }
        else if (CurrentState == PassengerState.MovingToPlayer && hasTarget)
        {
            currentSpeed = agent != null ? agent.velocity.magnitude : moveSpeed;
        }
        else if (CurrentState == PassengerState.FollowingRoute && currentWaypointIndex < routeWaypoints.Count)
        {
            bool onEscalator = currentWaypointIndex >= escalatorStartWaypointIndex &&
                               currentWaypointIndex <= escalatorEndWaypointIndex;

            // While on the escalator, force idle animation (Speed = 0) even though agent moves
            currentSpeed = onEscalator
                ? 0f
                : (agent != null ? agent.velocity.magnitude : moveSpeed);
        }
        else
            currentSpeed = 0f;

        animator.SetFloat("Speed", currentSpeed);
    }


    public GameObject GetLuggage()
    {
        return luggageObject;
    }

    private void ApplySeparation()
    {
        if (AllPassengers.Count <= 1) return;

        Vector3 push = Vector3.zero;
        int neighbors = 0;

        foreach (var other in AllPassengers)
        {
            if (other == null || other == this) continue;

            float dist = Vector3.Distance(transform.position, other.transform.position);
            if (dist > 0f && dist < separationDistance)
            {
                Vector3 away = (transform.position - other.transform.position).normalized;
                float strength = (separationDistance - dist) / separationDistance; // closer -> stronger
                push += away * strength;
                neighbors++;
            }
        }

        if (neighbors > 0 && push != Vector3.zero)
        {
            push = push.normalized * separationForce * Time.deltaTime;
            // Nudge agent position without breaking pathing
            transform.position += push;
        }
    }

    private void SetAgentDestination(Vector3 destination)
    {
        if (agent == null) return;
        if (!agent.isOnNavMesh)
        {
            TryWarpToNavMesh(transform.position);
            if (!agent.isOnNavMesh) return;
        }
        agent.speed = moveSpeed;
        agent.isStopped = false; // Ensure agent is not stopped
        agent.SetDestination(destination);
    }

    private void ConfigureAgent()
    {
        if (agent == null) return;
        agent.speed = moveSpeed;
        agent.angularSpeed = 720f;
        agent.acceleration = 12f;
        agent.stoppingDistance = 0.15f;
        agent.autoBraking = true;
        agent.updateRotation = true;
        agent.updatePosition = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
    }

    private void TryWarpToNavMesh(Vector3 position)
    {
        if (agent == null) return;
        NavMeshHit hit;
        // Sample within 10 units to find nearest point on NavMesh
        if (NavMesh.SamplePosition(position, out hit, 10.0f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: Could not find NavMesh near {position}");
        }
    }

    private void FaceDirection(Vector3 dir)
    {
        if (dir == Vector3.zero) return;
        transform.rotation = Quaternion.LookRotation(dir.normalized);
    }
}
