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
}
