using UnityEngine;

/// <summary>
/// Isometric camera controller that follows the player with a fixed angle
/// Similar to the reference video's camera angle
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    
    [Header("Isometric Camera Settings")]
    [SerializeField] private Vector3 offset = new Vector3(28.5f, 20f, -30.3f); // Camera offset from player (manually found perfect angle)
    [SerializeField] private float smoothSpeed = 5f;
    
    [Header("Camera Angle (Isometric)")]
    [SerializeField] private float cameraAngleX = 36.7f; // Vertical angle (looking down) - manually found perfect angle
    [SerializeField] private float cameraAngleY = -48f; // Horizontal rotation (diagonal view) - manually found perfect angle
    [SerializeField] private bool useOrthographic = false; // Set to true for true isometric (orthographic)
    [SerializeField] private float orthographicSize = 10f; // Only used if useOrthographic is true
    
    private Camera cam;
    private Vector3 fixedRotation;
    
    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = Camera.main;
        }
        
        // Set fixed rotation for isometric view
        fixedRotation = new Vector3(cameraAngleX, cameraAngleY, 0f);
        transform.rotation = Quaternion.Euler(fixedRotation);
        
        // Set orthographic if needed
        if (cam != null)
        {
            cam.orthographic = useOrthographic;
            if (useOrthographic)
            {
                cam.orthographicSize = orthographicSize;
            }
        }
    }
    
    private void LateUpdate()
    {
        if (!target) return;
        
        // Calculate desired position: target position + offset
        Vector3 desiredPosition = target.position + offset;
        
        // Smoothly move camera to follow target
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        
        // Keep rotation fixed (isometric view doesn't rotate)
        transform.rotation = Quaternion.Euler(fixedRotation);
    }
    
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
    
    /// <summary>
    /// Set camera angle for isometric view
    /// </summary>
    public void SetCameraAngle(float angleX, float angleY)
    {
        cameraAngleX = angleX;
        cameraAngleY = angleY;
        fixedRotation = new Vector3(cameraAngleX, cameraAngleY, 0f);
        transform.rotation = Quaternion.Euler(fixedRotation);
    }
    
    /// <summary>
    /// Set camera offset
    /// </summary>
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
    }
}