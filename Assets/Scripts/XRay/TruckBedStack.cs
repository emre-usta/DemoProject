using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages luggage stacking on the truck bed
/// </summary>
public class TruckBedStack : MonoBehaviour
{
    [Header("Stack Settings")]
    [SerializeField] private Transform stackParent; // Where suitcases will be parented (truck bed)
    [SerializeField] private float stackSpacing = 0.3f; // Vertical spacing between suitcases
    [SerializeField] private Vector3 stackOffset = Vector3.zero; // Offset from stack parent

    private List<GameObject> stackedLuggage = new List<GameObject>();

    private void Awake()
    {
        // If no stack parent assigned, use this transform
        if (stackParent == null)
        {
            stackParent = transform;
        }
    }

    /// <summary>
    /// Add a suitcase to the stack
    /// </summary>
    public void AddLuggage(GameObject luggage)
    {
        if (luggage == null) return;

        // Parent the luggage to the stack
        luggage.transform.SetParent(stackParent);
        
        // Calculate position in stack (bottom to top)
        int stackIndex = stackedLuggage.Count;
        Vector3 stackPosition = stackOffset + Vector3.up * (stackIndex * stackSpacing);
        luggage.transform.localPosition = stackPosition;
        luggage.transform.localRotation = Quaternion.Euler(0, 180f, 90f);

        // Add to list
        stackedLuggage.Add(luggage);

        Debug.Log($"Luggage added to truck bed stack. Total: {stackedLuggage.Count}");
    }

    /// <summary>
    /// Get the current stack count
    /// </summary>
    public int GetStackCount()
    {
        return stackedLuggage.Count;
    }

    /// <summary>
    /// Get the next stack position (world position) for placing a suitcase
    /// </summary>
    public Vector3 GetNextStackPosition()
    {
        int stackIndex = stackedLuggage.Count;
        Vector3 localPosition = stackOffset + Vector3.up * (stackIndex * stackSpacing);
        return stackParent.TransformPoint(localPosition);
    }

    /// <summary>
    /// Clear all luggage from stack (for testing or reset)
    /// </summary>
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

