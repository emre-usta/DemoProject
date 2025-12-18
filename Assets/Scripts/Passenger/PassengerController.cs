using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

/// <summary>
/// Controls individual passenger behavior: movement, luggage delivery, route following, escalator, and queue
/// </summary>
public class PassengerController : MonoBehaviour
{
    public enum PassengerState
    {
        WaitingInQueue,          // Initial state, waiting in spawn queue
        MovingToDeliveryPoint,   // Moving to luggage delivery point
        DeliveringLuggage,       // Handing over luggage to player
        FollowingRoute,          // Following the 13-waypoint route
        InQueue,                 // In upper floor queue (waypoints 7-11)
        Finished                 // Route completed
    }

    public PassengerState CurrentState { get; private set; }

    [Header("Luggage Settings")]
    [SerializeField] private GameObject luggagePrefab;
    [SerializeField] private Vector3 luggageLocalOffset = new Vector3(0, 1.5f, 0.3f);

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float waypointReachDistance = 0.5f;

    [Header("Escalator Settings")]
    [SerializeField] private int escalatorStartWaypointIndex = 3; // Waypoint where escalator starts
    [SerializeField] private int escalatorEndWaypointIndex = 5;   // Waypoint where escalator ends
    [SerializeField] private float escalatorMoveDuration = 2f;
    [SerializeField] private float escalatorInclineHeight = 1.5f;
    [SerializeField] private AnimationCurve escalatorMoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve escalatorInclineCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private string escalatorAnimationParameter = "IsOnEscalator";

    [Header("Queue Settings")]
    [SerializeField] private int queueStartWaypointIndex = 7;
    [SerializeField] private int queueEndWaypointIndex = 11;
    [SerializeField] private int exitWaypointIndex = 12;

    [Header("Money Settings")]
    [SerializeField] private GameObject moneyPickupPrefab;
    [SerializeField] private Transform moneyStartPoint;
    [SerializeField] private Transform moneyFinishPoint;
    private bool hasSpawnedMoney = false; // Track if money has been spawned for this passenger
    // Components
    private NavMeshAgent agent;
    private Animator animator;
    private Rigidbody rb;
    private PassengerManager passengerManager;
    private Transform playerTransform;
    private PlayerLuggageStack playerLuggageStack;
    private LuggageHandover luggageHandover;

    // State variables
    private List<Vector3> routeWaypoints = new List<Vector3>();
    private int currentWaypointIndex = 0;
    private bool hasReachedCurrentWaypoint = false;
    private Vector3 currentDestination = Vector3.zero;

    // Luggage
    private GameObject luggageObject;
    private bool luggageHandedOver = false;
    private bool isDeliveringLuggage = false;

    // Escalator
    private bool isMovingOnEscalator = false;
    private Coroutine escalatorCoroutine = null;
    private bool isOnEscalator = false;

    // Queue
    private bool isInQueue = false;
    private int assignedQueueWaypointIndex = -1;

    // Following
    private Vector3 followTargetPosition;
    private bool hasFollowTarget = false;

    private void Start()
    {
        InitializeComponents();
        SpawnLuggage();
        SetState(PassengerState.WaitingInQueue);
    }

    private void InitializeComponents()
    {
        // Find PassengerManager
        passengerManager = FindObjectOfType<PassengerManager>();
        if (passengerManager == null)
        {
            Debug.LogWarning($"{gameObject.name}: PassengerManager not found!");
        }

        // Find Player
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform != null)
        {
            playerLuggageStack = playerTransform.GetComponent<PlayerLuggageStack>();
        }

        // Get or add components
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
        }
        ConfigureAgent();

        animator = GetComponent<Animator>();
        luggageHandover = gameObject.AddComponent<LuggageHandover>();

        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Ensure on NavMesh
        TryWarpToNavMesh(transform.position);
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

    private void Update()
    {
        switch (CurrentState)
        {
            case PassengerState.WaitingInQueue:
                UpdateWaitingInQueue();
                break;
            case PassengerState.MovingToDeliveryPoint:
                UpdateMovingToDeliveryPoint();
                break;
            case PassengerState.DeliveringLuggage:
                UpdateDeliveringLuggage();
                break;
            case PassengerState.FollowingRoute:
                UpdateFollowingRoute();
                break;
            case PassengerState.InQueue:
                UpdateInQueue();
                break;
        }

        UpdateAnimation();
    }

    #region State Updates

    private void UpdateWaitingInQueue()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        if (hasFollowTarget)
        {
            // ✅ burada her frame set etmek yerine sadece ihtiyaç olduğunda set edelim
            if ((followTargetPosition - currentDestination).sqrMagnitude > 0.1f * 0.1f)
            {
                currentDestination = followTargetPosition;
                SetAgentDestination(followTargetPosition);
            }
        }
    }

    private void UpdateMovingToDeliveryPoint()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        // Check if reached delivery point
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.8f)
        {
            FaceDirection(playerTransform != null ? (playerTransform.position - transform.position) : Vector3.forward);
            SetState(PassengerState.DeliveringLuggage);
        }
    }

    private void UpdateDeliveringLuggage()
    {
        if (!isDeliveringLuggage)
        {
            isDeliveringLuggage = true;
            luggageHandedOver = false;

            // Start handover animation
            if (luggageObject != null && playerTransform != null && luggageHandover != null)
            {
                luggageObject.transform.SetParent(null);

                // Get stack point from manager
                Transform stackPoint = passengerManager != null ? passengerManager.GetPlayerLuggageStackPoint() : playerTransform;
                if (stackPoint == null) stackPoint = playerTransform;

                luggageHandover.StartHandover(luggageObject, transform, stackPoint, OnHandoverComplete);
            }
        }
    }


    private void UpdateFollowingRoute()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        // If moving on escalator, don't process normal route following
        if (isMovingOnEscalator) return;

        // If in queue, queue manager handles movement
        if (isInQueue)
        {
            // Check if reached current queue waypoint - stop agent to show idle animation
            if (currentWaypointIndex >= queueStartWaypointIndex && currentWaypointIndex <= queueEndWaypointIndex)
            {
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + waypointReachDistance)
                {
                    agent.isStopped = true;
                    hasReachedCurrentWaypoint = true;
                }
            }
            return;
        }

        if (routeWaypoints.Count == 0 || currentWaypointIndex >= routeWaypoints.Count)
        {
            SetState(PassengerState.Finished);
            return;
        }

        Vector3 targetWaypoint = routeWaypoints[currentWaypointIndex];
        Vector3 validatedWaypoint = ValidateWaypointOnNavMesh(targetWaypoint);

        // Set destination if needed
        if (Vector3.Distance(currentDestination, validatedWaypoint) > 0.1f || !hasReachedCurrentWaypoint)
        {
            currentDestination = validatedWaypoint;
            SetAgentDestination(validatedWaypoint);
            hasReachedCurrentWaypoint = false;
        }

        if (agent.pathPending) return;

        // Check if path is valid
        if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetWaypoint, out hit, 5f, NavMesh.AllAreas))
            {
                currentDestination = hit.position;
                SetAgentDestination(hit.position);
            }
            else
            {
                AdvanceToNextWaypoint();
            }
            return;
        }

        // Check if reached waypoint
        if (!hasReachedCurrentWaypoint && agent.remainingDistance <= agent.stoppingDistance + waypointReachDistance)
        {
            if (CurrentState != PassengerState.FollowingRoute) return;

            hasReachedCurrentWaypoint = true;
            int reachedWaypointIndex = currentWaypointIndex;

            // Special handling for escalator: when reaching waypoint 3, move diagonally to waypoint 5
            if (reachedWaypointIndex == escalatorStartWaypointIndex && !isMovingOnEscalator && !isInQueue)
            {
                if (routeWaypoints.Count > escalatorEndWaypointIndex)
                {
                    isMovingOnEscalator = true;
                    escalatorCoroutine = StartCoroutine(MoveDiagonallyOnEscalator());
                    return;
                }
            }

            // Check if reached exit waypoint (12) - eliminate passenger
            if (reachedWaypointIndex == exitWaypointIndex)
            {
                if (passengerManager != null)
                {
                    passengerManager.OnPassengerReachedExit(this);
                }
                else
                {
                    Destroy(gameObject);
                }
                return;
            }

            // Check if reached queue start waypoint (7)
            if (reachedWaypointIndex == queueStartWaypointIndex && !isInQueue)
            {
                RegisterWithQueue();
                return;
            }

            AdvanceToNextWaypoint();
        }
    }

    private void UpdateInQueue()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        if (agent.pathPending) return;

        float threshold = agent.stoppingDistance + waypointReachDistance;

        // 🔴 Eğer agent duruyorsa ama hedefe uzaksa → yeniden yürüt
        if (agent.isStopped && agent.remainingDistance > threshold)
        {
            agent.isStopped = false;
            hasReachedCurrentWaypoint = false;
            return;
        }

        if (!hasReachedCurrentWaypoint && agent.remainingDistance <= threshold)
        {
            agent.isStopped = true;
            hasReachedCurrentWaypoint = true;

            if (currentWaypointIndex == queueEndWaypointIndex && !hasSpawnedMoney)
            {
                SpawnMoneyPickup();
                hasSpawnedMoney = true;
            }
        }
    }




    #endregion

    #region Route Management

    private void AdvanceToNextWaypoint()
    {
        currentWaypointIndex++;
        hasReachedCurrentWaypoint = false;
        currentDestination = Vector3.zero;

        if (currentWaypointIndex >= routeWaypoints.Count)
        {
            SetState(PassengerState.Finished);
        }
    }

    private Vector3 ValidateWaypointOnNavMesh(Vector3 waypoint)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(waypoint, out hit, 5f, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return waypoint;
    }

    #endregion

    #region Escalator

    private IEnumerator MoveDiagonallyOnEscalator()
    {
        isMovingOnEscalator = true;

        // 🔴 NAVMESH TAM KAPAT
        agent.isStopped = true;
        agent.ResetPath();

        // 🔥 WAYPOINT 3–4–5 TAMAMEN ATLANIYOR
        currentWaypointIndex = escalatorEndWaypointIndex;

        Vector3 startPos = transform.position;
        Vector3 endPos = ValidateWaypointOnNavMesh(routeWaypoints[escalatorEndWaypointIndex]);

        float elapsed = 0f;

        while (elapsed < escalatorMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / escalatorMoveDuration);

            Vector3 pos = Vector3.Lerp(
                startPos,
                endPos,
                escalatorMoveCurve.Evaluate(t)
            );

            pos.y += escalatorInclineCurve.Evaluate(t) * escalatorInclineHeight;
            transform.position = pos;

            yield return null;
        }

        // 🔥 POZİSYON NETLE
        transform.position = endPos;

        // 🔥 AGENT'I GERİ GETİR
        TryWarpToNavMesh(endPos);
        agent.isStopped = false;

        // 🔥 BİR SONRAKİ WAYPOINT’TEN DEVAM
        currentWaypointIndex = escalatorEndWaypointIndex + 1;
        hasReachedCurrentWaypoint = false;
        currentDestination = Vector3.zero;

        isMovingOnEscalator = false;
    }


    #endregion
    
    #region Money Pickup

    private void SpawnMoneyPickup()
    {
        if (moneyPickupPrefab == null || moneyStartPoint == null || moneyFinishPoint == null)
            return;

        // Direkt spawn et
        GameObject moneyObj = Instantiate(
            moneyPickupPrefab,
            moneyStartPoint.position,
            Quaternion.identity
        );

        // Prefab'da MoneyPickup VAR varsayımı
        MoneyPickup moneyPickup = moneyObj.GetComponent<MoneyPickup>();

        // Hareketi başlat
        moneyPickup.StartMovement(
            moneyStartPoint.position,
            moneyFinishPoint.position
        );

        // Finish point zaten sahnede referanslı olmalı
        MoneyFinishPoint.Instance.RegisterMoneyPickup(moneyPickup);
    }


    #endregion

    #region Queue Management

    private void RegisterWithQueue()
    {
        // Stop escalator coroutine if running
        if (escalatorCoroutine != null)
        {
            StopCoroutine(escalatorCoroutine);
            escalatorCoroutine = null;
        }

        isMovingOnEscalator = false;
        isInQueue = true;

        if (passengerManager != null)
        {
            passengerManager.RegisterPassengerForQueue(this);
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: No PassengerManager found!");
            isInQueue = false;
        }
    }

    public void SetQueueWaypoint(int waypointIndex)
    {
        assignedQueueWaypointIndex = waypointIndex;
        currentWaypointIndex = waypointIndex;

        hasReachedCurrentWaypoint = false;

        if (agent != null)
            agent.isStopped = false;

        Vector3 targetPosition = routeWaypoints[waypointIndex];
        currentDestination = targetPosition;

        SetAgentDestination(targetPosition);

        SetState(PassengerState.InQueue);
    }



    public bool HasReachedQueueWaypoint()
    {
        if (!isInQueue || (CurrentState != PassengerState.FollowingRoute && CurrentState != PassengerState.InQueue))
        {
            return false;
        }

        if (currentWaypointIndex < queueStartWaypointIndex || currentWaypointIndex > exitWaypointIndex)
        {
            return false;
        }

        if (agent != null && agent.isOnNavMesh)
        {
            return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + waypointReachDistance;
        }

        return false;
    }

    #endregion

    #region Public Methods

    public void SetState(PassengerState newState)
    {
        CurrentState = newState;

        if (newState == PassengerState.DeliveringLuggage)
        {
            isDeliveringLuggage = false;
            luggageHandedOver = false;
            if (agent != null) agent.isStopped = true;
        }
        else if (newState == PassengerState.FollowingRoute)
        {
            currentWaypointIndex = 0;
            hasReachedCurrentWaypoint = false;
            currentDestination = Vector3.zero;
            isMovingOnEscalator = false;
            if (agent != null) agent.isStopped = false;
        }
        else if (newState == PassengerState.InQueue)
        {
            if (escalatorCoroutine != null)
            {
                StopCoroutine(escalatorCoroutine);
                escalatorCoroutine = null;
            }
            isMovingOnEscalator = false;
            if (agent != null) agent.isStopped = false;
        }
    }

    public void MoveToLuggageDeliveryPoint(Vector3 position)
    {
        SetAgentDestination(position);
        SetState(PassengerState.MovingToDeliveryPoint);
    }

    public void FollowPassenger(Vector3 position)
    {
        if (hasFollowTarget && (position - followTargetPosition).sqrMagnitude < 0.05f * 0.05f)
            return;

        followTargetPosition = position;
        hasFollowTarget = true;
        SetAgentDestination(position);
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
        hasReachedCurrentWaypoint = false;
        currentDestination = Vector3.zero;
        
        Debug.Log($"{gameObject.name}: Route set with {waypoints.Count} waypoints. Current state: {CurrentState}, Luggage handed over: {luggageHandedOver}");
        
        // Stop any running coroutines
        StopAllCoroutines();
        
        // CRITICAL: If luggage is handed over OR delivery is complete, start route immediately
        // This ensures state transition happens regardless of timing
        if (luggageHandedOver || IsDeliveryComplete())
        {
            // Luggage already handed over or delivery complete, start route immediately
            if (agent != null)
            {
                agent.isStopped = false;
            }
            SetState(PassengerState.FollowingRoute);
            Debug.Log($"{gameObject.name}: Route set and started immediately (luggage handed over or delivery complete). New state: {CurrentState}");
        }
        else if (CurrentState == PassengerState.DeliveringLuggage)
        {
            // Still delivering luggage, OnHandoverComplete will handle state change
            // Route is now set, so when handover completes, it will start the route
            Debug.Log($"{gameObject.name}: Route set, waiting for luggage handover to complete. State will change in OnHandoverComplete.");
        }
        else if (CurrentState == PassengerState.MovingToDeliveryPoint)
        {
            // Moving to delivery point, route will start after delivery
            Debug.Log($"{gameObject.name}: Route set, will start after luggage delivery.");
        }
        else
        {
            // If not delivering luggage, start route immediately
            if (agent != null)
            {
                agent.isStopped = false;
            }
            SetState(PassengerState.FollowingRoute);
            Debug.Log($"{gameObject.name}: Route set and started immediately (not delivering). New state: {CurrentState}");
        }
    }

    public bool IsDeliveryComplete()
    {
        return luggageHandedOver || CurrentState == PassengerState.FollowingRoute || CurrentState == PassengerState.Finished;
    }

    /// <summary>
    /// Stop the NavMeshAgent (used by PassengerManager to keep passengers waiting)
    /// </summary>
    public void StopAgent()
    {
        if (agent != null)
        {
            agent.isStopped = true;
        }
    }

    #endregion

    #region Luggage

    private void SpawnLuggage()
    {
        if (luggagePrefab == null) return;

        luggageObject = Instantiate(luggagePrefab, transform);
        luggageObject.transform.localPosition = luggageLocalOffset;
        luggageObject.transform.localRotation = Quaternion.identity;
        luggageObject.SetActive(true);
    }

    private void OnHandoverComplete()
    {
        if (luggageObject != null && playerLuggageStack != null)
        {
            playerLuggageStack.AddLuggage(luggageObject);
        }

        luggageHandedOver = true;
        luggageObject = null;
        
        Debug.Log($"{gameObject.name}: Luggage handover completed. Route waypoints count: {routeWaypoints.Count}, Current state: {CurrentState}");
        
        // If route is already set, start following it immediately
        if (routeWaypoints.Count > 0)
        {
            // Stop any running coroutines
            StopAllCoroutines();
            
            // Ensure agent is not stopped
            if (agent != null)
            {
                agent.isStopped = false;
            }
            
            // Start following route
            SetState(PassengerState.FollowingRoute);
            Debug.Log($"{gameObject.name}: Started following route after luggage handover. State: {CurrentState}");
        }
        else
        {
            Debug.Log($"{gameObject.name}: Route not set yet, waiting for PassengerManager to assign route.");
        }
    }

    #endregion

    #region Animation

    private void UpdateAnimation()
    {
        if (animator == null) return;

        float currentSpeed = 0f;
        bool isOnEscalator = false;

        switch (CurrentState)
        {
            case PassengerState.WaitingInQueue:
            case PassengerState.MovingToDeliveryPoint:
                currentSpeed = agent != null ? agent.velocity.magnitude : moveSpeed;
                break;

            case PassengerState.DeliveringLuggage:
                currentSpeed = 0f;
                break;

            case PassengerState.FollowingRoute:
                if (isInQueue && currentWaypointIndex >= queueStartWaypointIndex && currentWaypointIndex <= queueEndWaypointIndex)
                {
                    // In queue - idle if stopped, walk if moving
                    if (agent != null && agent.isStopped)
                    {
                        currentSpeed = 0f;
                    }
                    else
                    {
                        currentSpeed = agent != null ? agent.velocity.magnitude : 0f;
                    }
                }
                else
                {
                    isOnEscalator = isMovingOnEscalator;
                    if (isOnEscalator)
                    {
                        currentSpeed = 0f;
                    }
                    else
                    {
                        currentSpeed = agent != null ? agent.velocity.magnitude : moveSpeed;
                    }
                }
                break;

            case PassengerState.InQueue:
                if (agent != null && agent.isStopped)
                {
                    currentSpeed = 0f;
                }
                else
                {
                    currentSpeed = agent != null ? agent.velocity.magnitude : moveSpeed;
                }
                break;

            default:
                currentSpeed = 0f;
                break;
        }

        animator.SetFloat("Speed", currentSpeed);

        if (!string.IsNullOrEmpty(escalatorAnimationParameter))
        {
            try
            {
                animator.SetBool(escalatorAnimationParameter, isOnEscalator);
            }
            catch { }
        }
    }

    #endregion

    #region Helper Methods

    private void SetAgentDestination(Vector3 destination)
    {
        if (agent == null) return;
        if (!agent.isOnNavMesh)
        {
            TryWarpToNavMesh(transform.position);
            if (!agent.isOnNavMesh) return;
        }

        // ✅ ÖNCE agent’i serbest bırak
        agent.isStopped = false;
        agent.speed = moveSpeed;

        // (Opsiyonel) Aynı hedefe çok yakınsa tekrar set etme
        if (agent.hasPath && Vector3.Distance(agent.destination, destination) < 0.25f)
            return;

        agent.SetDestination(destination);
    }


    private void TryWarpToNavMesh(Vector3 position)
    {
        if (agent == null) return;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(position, out hit, 10.0f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }
    }

    private void FaceDirection(Vector3 dir)
    {
        if (dir == Vector3.zero) return;
        transform.rotation = Quaternion.LookRotation(dir.normalized);
    }

    #endregion
}