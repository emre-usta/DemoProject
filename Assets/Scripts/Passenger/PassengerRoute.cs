using UnityEngine;
using System.Collections.Generic;


public class PassengerRoute : MonoBehaviour
{
    [Header("Route Settings")]
    [SerializeField] private List<Transform> waypoints = new List<Transform>();
    [SerializeField] private bool loopRoute = false;

    public List<Vector3> GetWaypoints()
    {
        List<Vector3> positions = new List<Vector3>();
        foreach (Transform waypoint in waypoints)
        {
            if (waypoint != null)
            {
                positions.Add(waypoint.position);
            }
        }
        return positions;
    }

    public bool IsLooping()
    {
        return loopRoute;
    }

    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count < 2) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            if (waypoints[i] != null && waypoints[i + 1] != null)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }
        }

        if (loopRoute && waypoints.Count > 2)
        {
            if (waypoints[0] != null && waypoints[waypoints.Count - 1] != null)
            {
                Gizmos.DrawLine(waypoints[waypoints.Count - 1].position, waypoints[0].position);
            }
        }
    }
}






