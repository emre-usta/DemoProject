using System.Collections;
using UnityEngine;

public class PlayerEscalatorTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform targetTopPoint;

    [Header("Move Settings")]
    [SerializeField] private float moveDuration = 2f;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Tags")]
    [SerializeField] private string playerTag = "Player";

    private bool isMoving = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isMoving) return;
        if (!other.CompareTag(playerTag)) return;
        if (targetTopPoint == null)
        {
            Debug.LogWarning("PlayerEscalatorTrigger: targetTopPoint is not assigned!");
            return;
        }

        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc == null)
        {
            Debug.LogWarning("PlayerEscalatorTrigger: PlayerControl not found on Player!");
            return;
        }

        StartCoroutine(MovePlayerUp(other.transform, pc));
    }

    private IEnumerator MovePlayerUp(Transform player, PlayerController playerControl)
    {
        isMoving = true;

        playerControl.SetMovementEnabled(false);
        playerControl.ForceIdle(true);

        Rigidbody rb = player.GetComponent<Rigidbody>();

        Vector3 startPos = player.position;
        Vector3 endPos = targetTopPoint.position;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, moveDuration);
            float k = moveCurve.Evaluate(Mathf.Clamp01(t));

            player.position = Vector3.Lerp(startPos, endPos, k);
            yield return null;
        }

        // 🔥 KRİTİK SATIRLAR
        player.position = endPos;

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = endPos;   // ⭐ Rigidbody'yi senkronla
        }

        playerControl.ForceIdle(false);
        playerControl.SetMovementEnabled(true);

        isMoving = false;
    }
}