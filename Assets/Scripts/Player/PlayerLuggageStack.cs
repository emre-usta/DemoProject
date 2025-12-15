using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages luggage stacking on the player character
/// </summary>
public class PlayerLuggageStack : MonoBehaviour
{
    [Header("Stack Settings")]
    [SerializeField] private Transform stackParent; // Where suitcases will be parented (player's hands)
    [SerializeField] private float stackSpacing = 0.3f; // Vertical spacing between suitcases
    [SerializeField] private Vector3 stackOffset = Vector3.zero; // Offset from stack parent

    private List<GameObject> stackedLuggage = new List<GameObject>();

    private void Awake()
    {
        // If no stack parent assigned, create one
        if (stackParent == null)
        {
            GameObject stackObj = new GameObject("LuggageStack");
            stackObj.transform.SetParent(transform);
            stackObj.transform.localPosition = new Vector3(0, 1.5f, 0.5f); // Default position in front of player
            stackParent = stackObj.transform;
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

        Debug.Log($"Luggage added to stack. Total: {stackedLuggage.Count}");
    }

    /// <summary>
    /// Get the current stack count
    /// </summary>
    public int GetStackCount()
    {
        return stackedLuggage.Count;
    }

    /// <summary>
    /// Get all luggage in the stack (returns a copy of the list)
    /// </summary>
    public List<GameObject> GetAllLuggage()
    {
        return new List<GameObject>(stackedLuggage);
    }

    /// <summary>
    /// Remove a specific luggage from the stack
    /// </summary>
    public void RemoveLuggage(GameObject luggage)
    {
        if (luggage == null || !stackedLuggage.Contains(luggage)) return;

        stackedLuggage.Remove(luggage);
        luggage.transform.SetParent(null); // Unparent from stack

        // Recalculate positions for remaining luggage
        RecalculateStackPositions();

        Debug.Log($"Luggage removed from stack. Remaining: {stackedLuggage.Count}");
    }

    /// <summary>
    /// Recalculate positions of remaining luggage in stack
    /// </summary>
    private void RecalculateStackPositions()
    {
        for (int i = 0; i < stackedLuggage.Count; i++)
        {
            if (stackedLuggage[i] != null)
            {
                Vector3 stackPosition = stackOffset + Vector3.up * (i * stackSpacing);
                stackedLuggage[i].transform.localPosition = stackPosition;
            }
        }
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





