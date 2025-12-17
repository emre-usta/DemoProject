using UnityEngine;
using TMPro;

/// <summary>
/// Displays passenger count on the airplane (X/5 format)
/// Uses 3D TextMeshPro for world space display
/// </summary>
public class PassengerCounter : MonoBehaviour
{
    [Header("Text Reference")]
    [SerializeField] private TextMeshPro counterText; // 3D TextMeshPro component
    
    [Header("Settings")]
    [SerializeField] private int totalPassengers = 5;
    [SerializeField] private Vector3 textOffset = new Vector3(0, 3f, 0); // Offset above airplane
    [SerializeField] private float fontSize = 40f;
    
    private int currentCount = 0;
    private PassengerManager passengerManager;

    private void Awake()
    {
        // Auto-find TextMeshPro component if not assigned
        if (counterText == null)
        {
            counterText = GetComponentInChildren<TextMeshPro>();
            
            // If still null, create one (this will be visible in editor after play mode)
            if (counterText == null)
            {
                CreateTextObject();
            }
        }
    }

    private void Start()
    {
        // Ensure text object exists
        if (counterText == null)
        {
            counterText = GetComponentInChildren<TextMeshPro>();
            if (counterText == null)
            {
                CreateTextObject();
            }
        }

        // Find PassengerManager
        passengerManager = FindObjectOfType<PassengerManager>();
        if (passengerManager != null)
        {
            // Subscribe to passenger elimination event
            passengerManager.OnPassengerEliminated += OnPassengerEliminated;
        }
        else
        {
            Debug.LogWarning("PassengerCounter: PassengerManager not found!");
        }

        // Initialize display
        UpdateDisplay();
    }

    private void OnDestroy()
    {
        // Unsubscribe from event
        if (passengerManager != null)
        {
            passengerManager.OnPassengerEliminated -= OnPassengerEliminated;
        }
    }

    private void OnPassengerEliminated()
    {
        currentCount++;
        UpdateDisplay();
        Debug.Log($"PassengerCounter: Passenger eliminated. Count: {currentCount}/{totalPassengers}");
    }

    private void UpdateDisplay()
    {
        if (counterText != null)
        {
            counterText.text = $"{currentCount}/{totalPassengers}";
        }
    }

    /// <summary>
    /// Create the TextMeshPro GameObject (called automatically or can be called manually)
    /// </summary>
    private void CreateTextObject()
    {
        GameObject textObject = new GameObject("PassengerCounterText");
        textObject.transform.SetParent(transform);
        textObject.transform.localPosition = textOffset;
        textObject.transform.localRotation = Quaternion.Euler(-1.5f, 155f, 2f);        
        // Add TextMeshPro component (3D version)
        counterText = textObject.AddComponent<TextMeshPro>();
        counterText.text = $"0/{totalPassengers}";
        counterText.fontSize = fontSize;
        counterText.alignment = TextAlignmentOptions.Center;
        counterText.color = Color.black;
        
        // MeshRenderer is automatically added by TextMeshPro component
        Debug.Log($"PassengerCounter: Created TextMeshPro object '{textObject.name}' as child of '{gameObject.name}'");
    }

    /// <summary>
    /// Reset counter (useful for restarting game)
    /// </summary>
    public void ResetCounter()
    {
        currentCount = 0;
        UpdateDisplay();
    }
}

