using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Creates moving escalator steps by instantiating and animating step objects
/// </summary>
public class EscalatorPhysicalAnimator : MonoBehaviour
{
    [Header("Step Settings")]
    [Tooltip("Basamak prefab'ı (Stairs objesi)")]
    [SerializeField] private GameObject stepPrefab;
    
    [Tooltip("Kaç basamak oluşturulsun")]
    [SerializeField] private int numberOfSteps = 15;
    
    [Tooltip("Basamaklar arası mesafe")]
    [SerializeField] private float stepSpacing = 0.3f;
    
    [Header("Movement")]
    [Tooltip("Hareket hızı (units/second)")]
    [SerializeField] private float moveSpeed = 0.5f;
    
    [Tooltip("Hareket yönü (yukarı = Vector3.up)")]
    [SerializeField] private Vector3 moveDirection = Vector3.up;
    
    [Header("Loop Settings")]
    [Tooltip("Başlangıç noktası (alt)")]
    [SerializeField] private Transform startPoint;
    
    [Tooltip("Bitiş noktası (üst)")]
    [SerializeField] private Transform endPoint;
    
    [Tooltip("Basamak bitiş noktasına ulaşınca tekrar başa dönsün mü?")]
    [SerializeField] private bool loopSteps = true;
    
    [Header("Control")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool isPlaying = true;
    
    [Header("Delay Settings")]
    [Tooltip("Oyun başlangıcından kaç saniye sonra animasyon başlasın")]
    [SerializeField] private float startDelay = 6f;
    
    private List<GameObject> steps = new List<GameObject>();
    private float totalDistance;

    private void Start()
    {
        if (startPoint == null || endPoint == null)
        {
            CreateDefaultPoints();
        }

        totalDistance = Vector3.Distance(startPoint.position, endPoint.position);

        if (stepPrefab != null)
        {
            CreateSteps();
        }
        else
        {
            Debug.LogError("EscalatorPhysicalAnimator: Step Prefab atanmamış!");
        }

        if (playOnStart)
        {
            isPlaying = false; 
            StartCoroutine(DelayedStart());
        }
        else
        {
            isPlaying = false;
        }
    }

    private IEnumerator DelayedStart()
    {
        Debug.Log($"Animasyon {startDelay} saniye sonra başlayacak...");
        yield return new WaitForSeconds(startDelay);
        isPlaying = true;
        Debug.Log("Animasyon başladı!");
    }

    private void CreateDefaultPoints()
    {
        GameObject startObj = new GameObject("StartPoint");
        startObj.transform.SetParent(transform);
        startObj.transform.localPosition = Vector3.zero;
        startPoint = startObj.transform;

        GameObject endObj = new GameObject("EndPoint");
        endObj.transform.SetParent(transform);
        endObj.transform.localPosition = moveDirection.normalized * (numberOfSteps * stepSpacing);
        endPoint = endObj.transform;

        Debug.Log($"Otomatik start/end point oluşturuldu. Mesafe: {Vector3.Distance(startPoint.position, endPoint.position)}");
    }

    private void CreateSteps()
    {
        Vector3 direction = (endPoint.position - startPoint.position).normalized;
        
        for (int i = 0; i < numberOfSteps; i++)
        {
            Vector3 spawnPos = startPoint.position + (direction * stepSpacing * i);
            GameObject step = Instantiate(stepPrefab, spawnPos, Quaternion.identity, transform);
            step.name = $"Step_{i}";
            steps.Add(step);
        }

        Debug.Log($"{numberOfSteps} basamak oluşturuldu.");
    }

    private void Update()
    {
        if (!isPlaying || steps.Count == 0) return;

        Vector3 direction = (endPoint.position - startPoint.position).normalized;
        float moveAmount = moveSpeed * Time.deltaTime;

        for (int i = 0; i < steps.Count; i++)
        {
            GameObject step = steps[i];
            if (step == null) continue;

            step.transform.position += direction * moveAmount;

            if (loopSteps)
            {
                float sqrDistanceFromStart = (step.transform.position - startPoint.position).sqrMagnitude;
                float sqrTotalDistance = totalDistance * totalDistance;
                
                if (sqrDistanceFromStart >= sqrTotalDistance)
                {
                    step.transform.position = startPoint.position;
                }
            }
        }
    }

    public void Play()
    {
        isPlaying = true;
    }

    public void Stop()
    {
        isPlaying = false;
    }

    public void SetSpeed(float speed)
    {
        moveSpeed = speed;
    }

    public void ReverseDirection()
    {
        moveSpeed = -moveSpeed;
    }

    // Inspector test
    [ContextMenu("Play Animation")]
    private void TestPlay()
    {
        Play();
    }

    [ContextMenu("Stop Animation")]
    private void TestStop()
    {
        Stop();
    }

    [ContextMenu("Clear Steps")]
    private void ClearSteps()
    {
        foreach (var step in steps)
        {
            if (step != null)
            {
                DestroyImmediate(step);
            }
        }
        steps.Clear();
        Debug.Log("Tüm basamaklar temizlendi.");
    }

    [ContextMenu("Recreate Steps")]
    private void RecreateSteps()
    {
        ClearSteps();
        if (stepPrefab != null)
        {
            CreateSteps();
        }
    }
}