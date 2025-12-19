using UnityEngine;
using System.Collections;

public class LuggageHandover : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float handoverDuration = 1f;
    [SerializeField] private float arcHeight = 2f; 
    [SerializeField] private AnimationCurve arcCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private GameObject luggageObject;
    private Transform passengerTransform;
    private Transform playerTransform;
    private Vector3 startPosition;
    private Vector3 endPosition;
    private bool isHandingOver = false;

    public void StartHandover(GameObject luggage, Transform passenger, Transform player, System.Action onComplete)
    {
        if (isHandingOver || luggage == null) return;

        luggageObject = luggage;
        passengerTransform = passenger;
        playerTransform = player;
        startPosition = luggage.transform.position;
        endPosition = player.position + Vector3.up * 1.5f; 
        StartCoroutine(AnimateHandover(onComplete));
    }

    private IEnumerator AnimateHandover(System.Action onComplete)
    {
        isHandingOver = true;
        float elapsed = 0f;

        while (elapsed < handoverDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / handoverDuration;
            float curveValue = arcCurve.Evaluate(t);

            Vector3 currentPos = Vector3.Lerp(startPosition, endPosition, t);
            
            float arcOffset = Mathf.Sin(t * Mathf.PI) * arcHeight;
            currentPos.y += arcOffset;

            if (luggageObject != null)
            {
                luggageObject.transform.position = currentPos;
                
                luggageObject.transform.Rotate(Vector3.up, 180f * Time.deltaTime);
            }

            yield return null;
        }

        if (luggageObject != null)
        {
            luggageObject.transform.position = endPosition;
        }

        isHandingOver = false;
        onComplete?.Invoke();
    }

    public bool IsHandingOver()
    {
        return isHandingOver;
    }
}





