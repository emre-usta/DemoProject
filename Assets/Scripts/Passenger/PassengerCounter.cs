using UnityEngine;
using TMPro;

public class PassengerCounter : MonoBehaviour
{
    [Header("Text Reference")]
    [SerializeField] private TextMeshPro counterText; 
    
    [Header("Settings")]
    [SerializeField] private int totalPassengers = 5;
    [SerializeField] private Vector3 textOffset = new Vector3(0, 3f, 0); 
    [SerializeField] private float fontSize = 40f;
    
    private int currentCount = 0;
    private PassengerManager passengerManager;

    private void Awake()
    {
        if (counterText == null)
        {
            counterText = GetComponentInChildren<TextMeshPro>();
            
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

        UpdateDisplay();
    }

    private void OnDestroy()
    {
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

  
    private void CreateTextObject()
    {
        GameObject textObject = new GameObject("PassengerCounterText");
        textObject.transform.SetParent(transform);
        textObject.transform.localPosition = textOffset;
        textObject.transform.localRotation = Quaternion.Euler(-1.5f, 155f, 2f);        
        counterText = textObject.AddComponent<TextMeshPro>();
        counterText.text = $"0/{totalPassengers}";
        counterText.fontSize = fontSize;
        counterText.alignment = TextAlignmentOptions.Center;
        counterText.color = Color.black;
        
        Debug.Log($"PassengerCounter: Created TextMeshPro object '{textObject.name}' as child of '{gameObject.name}'");
    }
    
    public void ResetCounter()
    {
        currentCount = 0;
        UpdateDisplay();
    }
}

