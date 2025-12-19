using UnityEngine;
using System.Collections.Generic;


public class PlayerLuggageStack : MonoBehaviour
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
            GameObject stackObj = new GameObject("LuggageStack");
            stackObj.transform.SetParent(transform);
            stackObj.transform.localPosition = new Vector3(0, 1.5f, 0.5f); 
            stackParent = stackObj.transform;
        }
    }

    
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

    
    public int GetStackCount()
    {
        return stackedLuggage.Count;
    }

    
    public List<GameObject> GetAllLuggage()
    {
        return new List<GameObject>(stackedLuggage);
    }

    
    public void RemoveLuggage(GameObject luggage)
    {
        if (luggage == null || !stackedLuggage.Contains(luggage)) return;

        stackedLuggage.Remove(luggage);
        luggage.transform.SetParent(null); // Unparent from stack

        // Recalculate positions for remaining luggage
        RecalculateStackPositions();

        Debug.Log($"Luggage removed from stack. Remaining: {stackedLuggage.Count}");
    }

    
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





