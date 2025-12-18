using UnityEngine;

/// <summary>
/// Isometric camera controller that follows the player with a fixed angle
/// Similar to the reference video's camera angle
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public enum CameraMode
    {
        Player,
        Painting
    }
    
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    
    [Header("Player Camera Settings")]
    [SerializeField] private Vector3 playerOffset = new Vector3(28.5f, 20f, -30.3f);
    [SerializeField] private Vector3 playerRotation = new Vector3(36.7f, -48f, 0f);
    [Header("Fallow Settings")]
    //[SerializeField] private Vector3 offset = new Vector3(28.5f, 20f, -30.3f); // Camera offset from player (manually found perfect angle)
    [SerializeField] private float smoothSpeed = 5f;
    
    [Header("Painting Camera Settings")]
    [SerializeField] private Vector3 paintingOffset = new Vector3(0f, 10f, -8f);
    [SerializeField] private Vector3 paintingRotation = new Vector3(60f, 0f, 0f);
    [Header("Camera Angle (Isometric)")]
    //[SerializeField] private float cameraAngleX = 36.7f; // Vertical angle (looking down) - manually found perfect angle
    //[SerializeField] private float cameraAngleY = -48f; // Horizontal rotation (diagonal view) - manually found perfect angle
    //[SerializeField] private bool useOrthographic = false; // Set to true for true isometric (orthographic)
    //[SerializeField] private float orthographicSize = 10f; // Only used if useOrthographic is true
    
    private CameraMode currentMode = CameraMode.Player;
    private Camera cam;
    
    private void Awake()
    {
        cam = GetComponent<Camera>();
    }
    
    private void LateUpdate()
    {
        if (!target) return;

        Vector3 desiredPosition;
        Vector3 desiredRotation;

        if (currentMode == CameraMode.Player)
        {
            desiredPosition = target.position + playerOffset;
            desiredRotation = playerRotation;
        }
        else
        {
            desiredPosition = target.position + paintingOffset;
            desiredRotation = paintingRotation;
        }

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );

        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            Quaternion.Euler(desiredRotation),
            smoothSpeed * Time.deltaTime
        );
    }

    // 🔁 Target değiştirme (Player / Painting)
    public void SetTarget(Transform newTarget, CameraMode mode)
    {
        target = newTarget;
        currentMode = mode;
    }
    
}