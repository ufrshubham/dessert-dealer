using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

/// <summary>
/// Simple AI for a patrolling cat that can detect and chase the player.
/// Uses a basic state machine with three states: Patrol, Investigate, and Chase.
/// </summary>
[RequireComponent(typeof(NavMeshAgent), typeof(FovVisualizer))]
public class CatAI : MonoBehaviour
{
    /// <summary>
    /// Defines the current behavior state of the cat.
    /// </summary>
    private enum State
    {
        /// <summary>
        /// The cat follows a predefined set of waypoints in a loop, looking for the player.
        /// </summary>
        Patrol,

        /// <summary>
        /// The cat moves to the player's last known position and waits for a short time, hoping to spot the player again.
        /// </summary>
        Investigate,

        /// <summary>
        /// The cat has spotted the player and actively chases them. If the player is lost, it transitions to Investigate state.
        /// </summary>
        Chase
    }

    [Header("Movement")]
    [SerializeField]
    [Tooltip("Speed at which the cat patrols between waypoints.")]
    private float _patrolSpeed = 2.5f;

    [SerializeField]
    [Tooltip("Speed at which the cat chases the player.")]
    private float _chaseSpeed = 6.0f;

    [SerializeField]
    [Tooltip("Acceleration of the cat when changing speed or direction.")]
    private float _acceleration = 12.0f;

    [Header("Vision")]
    [SerializeField]
    [Tooltip("Radius within which the cat can see the player.")]
    private float _viewRadius = 12.0f;

    [SerializeField, Range(0, 360)]
    [Tooltip("Angle of the cat's field of view.")]
    private float _viewAngle = 90.0f;

    [SerializeField]
    [Tooltip("Height of the cat's eyes from the ground.")]
    private float _eyeHeight = 0.5f;

    [SerializeField]
    [Tooltip("Layer mask for detecting the player.")]
    private LayerMask _targetMask;

    [SerializeField]
    [Tooltip("Layer mask for detecting obstacles.")]
    private LayerMask _obstacleMask;

    [Header("Patrol")]
    [SerializeField]
    [Tooltip("Reference to the WaypointCollector that provides the patrol waypoints.")]
    private WaypointCollector _waypointCollector;

    [SerializeField]
    [Tooltip("Distance within which the cat considers it has reached a waypoint.")]
    private float _waypointTolerance = 0.5f;

    [Header("Investigate")]
    [SerializeField]
    [Tooltip("Time the cat will spend investigating the player's last known position before giving up and returning to patrol.")]
    private float _investigateTime = 4.0f;

    [Header("Events")]
    [SerializeField]
    [Tooltip("Event triggered when the game is over.")]
    private UnityEvent _onGameOver;

    /// <summary>
    /// Public read-only properties for the cat's vision parameters, 
    /// used by the FovVisualizer to draw the field of view cone in the editor.
    /// </summary>
    public float ViewRadius => _viewRadius;

    /// <summary>
    /// Public read-only properties for the cat's vision parameters, 
    /// used by the FovVisualizer to draw the field of view cone in the editor.
    /// </summary>
    public float ViewAngle => _viewAngle;

    /// <summary>
    /// Public read-only properties for the cat's vision parameters, 
    /// used by the FovVisualizer to draw the field of view cone in the editor.
    /// </summary>
    public float ViewHeight => _eyeHeight;

    private NavMeshAgent _agent;
    private Transform _playerTransform;
    private State _currentState;
    private int _waypointIndex;
    private Vector3 _lastKnownPosition;
    private float _investigateTimer;
    private bool _gameOver;

    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            _playerTransform = player.transform;
        }
        else
        {
            Debug.LogWarning("[CatAI] No GameObject tagged 'Player' found in scene.", this);
        }

        TransitionTo(State.Patrol);
    }

    private void Update()
    {
        if (_gameOver)
        {
            return;
        }

        switch (_currentState)
        {
            case State.Patrol:
                UpdatePatrol();
                break;
            case State.Investigate:
                UpdateInvestigate();
                break;
            case State.Chase:
                UpdateChase();
                break;
        }
    }

    /// <summary>
    /// Handles transitioning between states, setting appropriate speeds and destinations for each state.
    /// </summary>
    /// <param name="newState">The state to transition to.</param>
    private void TransitionTo(State newState)
    {
        _currentState = newState;
        _agent.isStopped = false;
        _agent.acceleration = _acceleration;

        switch (newState)
        {
            case State.Patrol:
                _agent.speed = _patrolSpeed;
                SetDestinationToWaypoint();
                break;

            case State.Investigate:
                _agent.speed = _patrolSpeed;
                _investigateTimer = _investigateTime;
                _agent.SetDestination(_lastKnownPosition);
                break;

            case State.Chase:
                _agent.speed = _chaseSpeed;
                break;
        }
    }

    /// <summary>
    /// In Patrol state, the cat moves between waypoints. If it sees the player, it transitions to Chase state.
    /// </summary>
    private void UpdatePatrol()
    {
        // Check if we've reached the current waypoint and need to advance to the next one
        if (!_agent.pathPending && _agent.remainingDistance < _waypointTolerance)
        {
            AdvanceWaypoint();
        }

        if (CanSeePlayer())
        {
            TransitionTo(State.Chase);
        }
    }

    /// <summary>
    /// In Investigate state, the cat moves to the player's last known position and waits. If it sees the player again, 
    /// it transitions back to Chase. If the timer runs out without seeing the player, it returns to Patrol.
    /// </summary>
    private void UpdateInvestigate()
    {
        if (CanSeePlayer())
        {
            TransitionTo(State.Chase);
            return;
        }

        // Count down the investigation timer only after arriving at last known pos
        if (!_agent.pathPending && _agent.remainingDistance < _waypointTolerance)
        {
            _investigateTimer -= Time.deltaTime;
            if (_investigateTimer <= 0f)
            {
                TransitionTo(State.Patrol);
            }
        }
    }

    /// <summary>
    /// In Chase state, the cat continuously updates its destination to the player's current position as long as it can see the player. 
    /// If it loses sight of the player, it transitions to Investigate state to search the last known location.
    /// </summary>
    private void UpdateChase()
    {
        if (_playerTransform == null)
        {
            return;
        }

        if (CanSeePlayer())
        {
            _agent.SetDestination(_playerTransform.position);
        }
        else
        {
            _lastKnownPosition = _playerTransform.position;
            TransitionTo(State.Investigate);
        }
    }

    /// <summary>
    /// Determines if the cat can see the player based on distance, field of view angle, and line of sight (raycast).
    /// </summary>
    /// <returns>
    /// True if the player is visible to the cat, false otherwise.
    /// </returns>
    private bool CanSeePlayer()
    {
        if (_playerTransform == null) 
        {
            return false;
        }

        // Calculate the direction and distance to the player from the cat's eye position
        // The origin point is raised by _eyeHeight to simulate the cat's eye level, improving line of sight checks.
        Vector3 origin = transform.position + Vector3.up * _eyeHeight;

        // Direction vector from the cat to the player
        Vector3 dirToPlayer = _playerTransform.position - origin;
        float distance = dirToPlayer.magnitude;

        // Check if the player is within the cat's view radius
        if (distance > _viewRadius)
        {
            return false;
        }

        // Check if the player is within the cat's field of view angle
        // Vector3.Angle returns the angle in degrees between the cat's forward direction and the direction to the player.
        // If the angle is greater than half of the view angle, the player is outside the cone of vision.
        if (Vector3.Angle(transform.forward, dirToPlayer) > _viewAngle * 0.5f)
        {
            return false;
        }

        // Perform a raycast to check for line of sight, ignoring any obstacles defined in the _obstacleMask.
        // The raycast checks if there is a clear path from the cat's eyes to the player's position. If the ray hits something, we check if it's the player.
        if (Physics.Raycast(origin, dirToPlayer.normalized, out RaycastHit hit, _viewRadius, _targetMask | _obstacleMask))
        {
            return hit.transform == _playerTransform;
        }

        return false;
    }

    /// <summary>
    /// Sets the NavMeshAgent's destination to the current patrol waypoint. This is called when entering Patrol state and when advancing to the next waypoint.
    /// </summary>
    private void SetDestinationToWaypoint()
    {
        var patrolWaypoints = _waypointCollector != null ? _waypointCollector.Waypoint : null;

        // If there are no waypoints assigned, we can't set a destination, so we simply return early.
        if (patrolWaypoints == null || patrolWaypoints.Length == 0) 
        {
            return;
        }

        // If the current waypoint index is out of bounds (which shouldn't happen due to the way we advance waypoints), we also return early.
        if (patrolWaypoints[_waypointIndex] == null)
        {
            return;
        }

        _agent.SetDestination(patrolWaypoints[_waypointIndex].position);
    }

    /// <summary>
    /// Advances to the next waypoint in the patrol route. This method is called when the cat reaches its current waypoint during Patrol state.
    /// The waypoint index is incremented and wrapped around using modulo to create a looping patrol route. After updating the index, 
    /// it calls SetDestinationToWaypoint to update the NavMeshAgent's target destination to the new waypoint.
    /// </summary>
    private void AdvanceWaypoint()
    {
        var patrolWaypoints = _waypointCollector != null ? _waypointCollector.Waypoint : null;

        if (patrolWaypoints == null || patrolWaypoints.Length == 0) 
        {
            return;
        }

        _waypointIndex = (_waypointIndex + 1) % patrolWaypoints.Length;
        SetDestinationToWaypoint();
    }

    /// <summary>
    /// Handles collision with the player. If the cat collides with the player, it triggers the game over event and stops all movement.
    /// </summary>
    /// <param name="collision">The collision data associated with the collision event.</param>
    private void OnCollisionEnter(Collision collision)
    {
        // If the game is already over, we don't want to process any more collisions, so we return early.
        if (_gameOver)
        { 
            return;
        }

        // If the collided object is not the player, we don't want to process it, so we return early.
        if (!collision.gameObject.CompareTag("Player")) 
        {
            return;
        }

        _gameOver = true;
        _agent.isStopped = true;

        Debug.Log("GAME OVER");
        _onGameOver.Invoke();
    }
}
