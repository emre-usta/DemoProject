using UnityEngine;
using System.Collections;


public class MoneyPickup : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float handoverDuration = 1f;
    [SerializeField] private float arcHeight = 2f;
    [SerializeField] private AnimationCurve arcCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Collection Settings")]
    [SerializeField] private float collectionDuration = 0.5f; 
    [SerializeField] private float scatterDistance = 2f; 
    [SerializeField] private int moneyValue = 20;

    private Vector3 startPosition;
    private Vector3 endPosition;
    private bool isMoving = false;
    private bool isCollected = false;
    private bool hasReachedFinishPoint = false;

   
    public void StartMovement(Vector3 start, Vector3 finish)
    {
        if (isMoving) return;

        startPosition = start;
        endPosition = finish;
        transform.position = startPosition;
        
        gameObject.SetActive(true);
        
        isMoving = true;
        hasReachedFinishPoint = false;

        StartCoroutine(MoveToFinishPoint());
    }

    private IEnumerator MoveToFinishPoint()
    {
        float elapsed = 0f;

        while (elapsed < handoverDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / handoverDuration;
            float curveValue = arcCurve.Evaluate(t);

            Vector3 currentPos = Vector3.Lerp(startPosition, endPosition, t);

            float arcOffset = Mathf.Sin(t * Mathf.PI) * arcHeight;
            currentPos.y += arcOffset;

            transform.position = currentPos;

            transform.Rotate(Vector3.up, 180f * Time.deltaTime);

            yield return null;
        }

        transform.position = endPosition;
        isMoving = false;
        hasReachedFinishPoint = true;
        
        Debug.Log($"MoneyPickup: Reached finish point at {endPosition}. HasReachedFinishPoint: {HasReachedFinishPoint()}");
    }
    
    public bool HasReachedFinishPoint()
    {
        return hasReachedFinishPoint && !isMoving;
    }
    
    public void Collect(System.Action onComplete = null)
    {
        if (isCollected) return;

        isCollected = true;
        StartCoroutine(CollectAnimation(onComplete));
    }

    private IEnumerator CollectAnimation(System.Action onComplete)
    {
        Vector3 scatterDirection = Random.insideUnitSphere;
        scatterDirection.y = Mathf.Abs(scatterDirection.y); 
        scatterDirection.Normalize();

        Vector3 targetPosition = transform.position + scatterDirection * scatterDistance;
        Vector3 startPos = transform.position;
        float elapsed = 0f;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddCurrency(moneyValue);
            Debug.Log($"MoneyPickup: Added {moneyValue} currency. Total: {GameManager.Instance.Currency}");
        }

        while (elapsed < collectionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / collectionDuration;

            transform.position = Vector3.Lerp(startPos, targetPosition, t);

            transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);

            transform.Rotate(Vector3.up, 360f * Time.deltaTime);

            yield return null;
        }

        onComplete?.Invoke();
        Destroy(gameObject);
    }

    public bool IsCollected()
    {
        return isCollected;
    }
}

