using UnityEngine;
using System.Collections;

/// <summary>
/// Money pickup that moves from MoneyStartPoint to MoneyFinishPoint using LuggageHandover animation
/// </summary>
public class MoneyPickup : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float handoverDuration = 1f;
    [SerializeField] private float arcHeight = 2f;
    [SerializeField] private AnimationCurve arcCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Collection Settings")]
    [SerializeField] private float collectionDuration = 0.5f; // Time for scatter animation
    [SerializeField] private float scatterDistance = 2f; // How far money scatters
    [SerializeField] private int moneyValue = 20; // Amount of money this pickup gives

    private Vector3 startPosition;
    private Vector3 endPosition;
    private bool isMoving = false;
    private bool isCollected = false;
    private bool hasReachedFinishPoint = false;

    /// <summary>
    /// Start moving from start point to finish point
    /// </summary>
    public void StartMovement(Vector3 start, Vector3 finish)
    {
        if (isMoving) return;

        startPosition = start;
        endPosition = finish;
        transform.position = startPosition;
        
        // Activate GameObject when starting movement (was invisible before)
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

            // Calculate position along curved path
            Vector3 currentPos = Vector3.Lerp(startPosition, endPosition, t);

            // Add arc height (domed/curved path)
            float arcOffset = Mathf.Sin(t * Mathf.PI) * arcHeight;
            currentPos.y += arcOffset;

            transform.position = currentPos;

            // Rotate during flight for visual effect
            transform.Rotate(Vector3.up, 180f * Time.deltaTime);

            yield return null;
        }

        // Ensure final position
        transform.position = endPosition;
        isMoving = false;
        hasReachedFinishPoint = true;
        
        Debug.Log($"MoneyPickup: Reached finish point at {endPosition}. HasReachedFinishPoint: {HasReachedFinishPoint()}");
    }

    /// <summary>
    /// Check if money has reached finish point
    /// </summary>
    public bool HasReachedFinishPoint()
    {
        return hasReachedFinishPoint && !isMoving;
    }

    /// <summary>
    /// Collect this money pickup (scatter animation and add currency)
    /// </summary>
    public void Collect(System.Action onComplete = null)
    {
        if (isCollected) return;

        isCollected = true;
        StartCoroutine(CollectAnimation(onComplete));
    }

    private IEnumerator CollectAnimation(System.Action onComplete)
    {
        Vector3 scatterDirection = Random.insideUnitSphere;
        scatterDirection.y = Mathf.Abs(scatterDirection.y); // Upward scatter
        scatterDirection.Normalize();

        Vector3 targetPosition = transform.position + scatterDirection * scatterDistance;
        Vector3 startPos = transform.position;
        float elapsed = 0f;

        // Add money immediately
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddCurrency(moneyValue);
            Debug.Log($"MoneyPickup: Added {moneyValue} currency. Total: {GameManager.Instance.Currency}");
        }

        // Scatter animation
        while (elapsed < collectionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / collectionDuration;

            // Move to scatter position
            transform.position = Vector3.Lerp(startPos, targetPosition, t);

            // Fade out (scale down)
            transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);

            // Rotate faster
            transform.Rotate(Vector3.up, 360f * Time.deltaTime);

            yield return null;
        }

        // Destroy after animation
        onComplete?.Invoke();
        Destroy(gameObject);
    }

    public bool IsCollected()
    {
        return isCollected;
    }
}

