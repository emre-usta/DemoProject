using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PassengerManager : MonoBehaviour
{
    [Header("Setup")]
    public PassengerController passengerPrefab;
    public Transform queueStartPoint;     // Front of the line (first passenger)
    public Transform passengerSpawnPoint; // End of the line
    public int queueLength = 5;
    public float queueSpacing = 1.5f;
    public float spawnFrontToBackDelay = 0.15f;

    [Header("Luggage Delivery")]
    public Transform playerLuggageDeliveryPoint; // Where passengers go to deliver luggage (near player)
    public PassengerRoute passengerRoute; // Route for passengers to follow after delivering luggage
    public float timeBetweenDeliveries = 2f; // Time between each passenger delivering luggage
    [SerializeField] private float queueAdvanceDelay = 0.5f; // Delay before the queue closes the gap

    private List<PassengerController> passengers = new List<PassengerController>();
    private List<Vector3> queuePositions = new List<Vector3>();
    private bool allowMovement = false;
    private bool isProcessingLuggage = false;
    //private int currentLuggageIndex = 0;
    private int processedCount = 0;
    private Vector3 queueDir;
    private bool isAdvancingQueue = false;

    private void Update()
    {
        // Continuously keep waiting passengers tightened to the front of the queue
        if (!allowMovement) return;

        for (int j = processedCount; j < passengers.Count; j++)
        {
            var p = passengers[j];
            if (p == null) continue;

            // Only reposition passengers that are still waiting in queue
            if (p.CurrentState == PassengerController.PassengerState.WaitingInQueue)
            {
                Vector3 targetPos = queueStartPoint.position + (j - processedCount) * queueSpacing * queueDir;
                queuePositions[j] = targetPos;
                p.AssignQueuePosition(targetPos);
            }
        }
    }

    private void Start() {
        StartQueue();
    }

    public void StartQueue() {
        passengers.Clear();
        queuePositions.Clear();
        allowMovement = false;
        //currentLuggageIndex = 0;
        processedCount = 0;

        Vector3 frontPos = queueStartPoint.position;
        queueDir = (passengerSpawnPoint.position - queueStartPoint.position).normalized;

        // Precompute queue positions
        for(int i=0; i<queueLength; i++) {
            Vector3 pos = frontPos + i * queueSpacing * queueDir;
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(pos, out hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
            {
                pos = hit.position;
            }
            else
            {
                Debug.LogWarning($"Queue slot {i} not on NavMesh near {pos}. Passenger will be skipped.");
            }
            queuePositions.Add(pos);
        }

        // Spawn front-to-back so the front passenger appears first at QueueStartPoint
        StartCoroutine(SpawnFrontToBack());
    }

    private IEnumerator SpawnFrontToBack()
    {
        for (int i = 0; i < queuePositions.Count; i++)
        {
            Vector3 pos = queuePositions[i];
            PassengerController p = Instantiate(passengerPrefab, pos, Quaternion.Euler(0f, -90f, 0f));
            passengers.Add(p);
            p.AssignQueuePosition(pos);
            if (spawnFrontToBackDelay > 0f && i < queuePositions.Count - 1)
                yield return new WaitForSeconds(spawnFrontToBackDelay);
        }
    }

    public void TriggerPassengerMovement() {
        allowMovement = true;
        for(int i=0; i<passengers.Count; i++) {
            passengers[i].AssignQueuePosition(queuePositions[i]);
        }
    }

    public void StartLuggageDelivery()
    {
        if (!isProcessingLuggage && passengers.Count > 0)
        {
            processedCount = 0;
            StartCoroutine(ProcessLuggageDeliverySequence());

        }
    }

    private IEnumerator ProcessLuggageDeliverySequence()
    {
        while (processedCount < passengers.Count)
        {
            PassengerController currentPassenger = passengers[processedCount];

            if (currentPassenger != null &&
                currentPassenger.CurrentState == PassengerController.PassengerState.WaitingInQueue)
            {
                Vector3 deliveryPoint =
                    playerLuggageDeliveryPoint != null
                        ? playerLuggageDeliveryPoint.position
                        : GameObject.FindGameObjectWithTag("Player")?.transform.position ?? Vector3.zero;

                // En ÖNDEKİ yolcu hareket eder
                currentPassenger.MoveToPlayer(deliveryPoint);

                // Kuyruğun başı ilerler (with a small delay)
                processedCount++;
                if (!isAdvancingQueue)
                    StartCoroutine(AdvanceQueueAfterDelay());

                // Bu yolcunun işi bitene kadar bekle
                yield return new WaitUntil(() =>
                    currentPassenger == null ||
                    currentPassenger.IsDeliveryComplete());

                // Route ver
                if (passengerRoute != null && currentPassenger != null)
                {
                    var waypoints = passengerRoute.GetWaypoints();
                    if (waypoints != null && waypoints.Count > 0)
                        currentPassenger.SetRoute(waypoints);
                }

                yield return new WaitForSeconds(timeBetweenDeliveries);
            }
            else
            {
                yield return null;
            }
        }

        isProcessingLuggage = false;
    }


    private IEnumerator AdvanceQueueAfterDelay()
    {
        isAdvancingQueue = true;
        if (queueAdvanceDelay > 0f)
            yield return new WaitForSeconds(queueAdvanceDelay);

        // Recompute queue positions for remaining passengers so the line moves forward
        for (int j = processedCount; j < passengers.Count; j++)
        {
            if (passengers[j] == null) continue;
            Vector3 newPos = queueStartPoint.position + (j - processedCount) * queueSpacing * queueDir;
            queuePositions[j] = newPos;
            passengers[j].AssignQueuePosition(newPos);
        }
        isAdvancingQueue = false;
    }
}