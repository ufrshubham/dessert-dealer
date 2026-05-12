using UnityEngine;

/// <summary>
/// Collects child transforms as waypoints for AI navigation.
/// </summary>
public class WaypointCollector : MonoBehaviour
{

    private Transform[] _waypoints;

    /// <summary>
    /// Public property to access the collected waypoints. 
    /// This allows other scripts (like CatAI) to retrieve the waypoints for patrolling or navigation purposes.
    /// </summary>
    public Transform[] Waypoint => _waypoints;

    private void Awake()
    {
        // Collect all child transforms as waypoints, excluding the parent itself
        _waypoints = new Transform[transform.childCount];

        for (int i = 0; i < transform.childCount; i++)
        {
            _waypoints[i] = transform.GetChild(i);
        }
    }

    private void OnDrawGizmos()
    {
        // Draw spheres at each waypoint position for better visibility when the object is selected
        Transform[] waypointsInEditor = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            waypointsInEditor[i] = transform.GetChild(i);
        }

        if (waypointsInEditor != null)
        {
            Gizmos.color = Color.red;
            foreach (Transform waypoint in waypointsInEditor)
            {
                if (waypoint != null)
                {
                    Gizmos.DrawSphere(waypoint.position, 0.2f);
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw lines between waypoints for visualization in the editor
        // Indicate the direction by marking an arrow from the current waypoint to the next
        Transform[] waypointsInEditor = new Transform[transform.childCount];
        
        for (int i = 0; i < transform.childCount; i++)
        {
            waypointsInEditor[i] = transform.GetChild(i);
        }

        if (waypointsInEditor != null && waypointsInEditor.Length > 1)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < waypointsInEditor.Length; i++)
            {
                Transform currentWaypoint = waypointsInEditor[i];

                // Loop back to the first waypoint
                Transform nextWaypoint = waypointsInEditor[(i + 1) % waypointsInEditor.Length];

                if (currentWaypoint != null && nextWaypoint != null)
                {
                    Gizmos.DrawLine(currentWaypoint.position, nextWaypoint.position);
                }
            }
        }
    }
}
