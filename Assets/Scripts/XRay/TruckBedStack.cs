using UnityEngine;
using System.Collections.Generic;

public class TruckBedStack : MonoBehaviour
{
    [Header("Stack Settings")]
    [SerializeField] private Transform stackParent; 
    [SerializeField] private float stackSpacing = 0.3f; 
    [SerializeField] private Vector3 stackOffset = Vector3.zero;

    private List<GameObject> stackedLuggage = new List<GameObject>();

    private void Awake()
    {
        if (stackParent == null)
        {
            stackParent = transform;
        }
    }

 
    public void AddLuggage(GameObject luggage)
    {
        if (luggage == null) return;

        luggage.transform.SetParent(stackParent);
        
        int stackIndex = stackedLuggage.Count;
        Vector3 stackPosition = stackOffset + Vector3.up * (stackIndex * stackSpacing);
        luggage.transform.localPosition = stackPosition;
        luggage.transform.localRotation = Quaternion.Euler(0, 180f, 90f);

        stackedLuggage.Add(luggage);

        Debug.Log($"Luggage added to truck bed stack. Total: {stackedLuggage.Count}");
    }
    
    public int GetStackCount()
    {
        return stackedLuggage.Count;
    }
    
    public Vector3 GetNextStackPosition()
    {
        int stackIndex = stackedLuggage.Count;
        Vector3 localPosition = stackOffset + Vector3.up * (stackIndex * stackSpacing);
        return stackParent.TransformPoint(localPosition);
    }
    
    public void ClearStack()
    {
        foreach (GameObject luggage in stackedLuggage)
        {
            if (luggage != null)
            {
                Destroy(luggage);
            }
        }
        stackedLuggage.Clear();
    }
}


