using UnityEngine;
using UnityEngine.AI;

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
    private float _patrolSpeed = 1.0f;

    [SerializeField]
    [Tooltip("Speed at which the cat chases the player.")]
    private float _chaseSpeed = 4.0f;

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
    [Tooltip("How long (seconds) the cat must hold the rat in its FOV before triggering Chase. Set to 0 for instant reaction.")]
    private float _visionAlertTime = 0.6f;

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

    [Header("Proximity Sense")]
    [SerializeField]
    [Tooltip("Radius of the inner proximity zone. If the rat lingers here long enough the cat will detect it regardless of FOV.")]
    private float _proximityRadius = 3.0f;

    [SerializeField]
    [Tooltip("How long (seconds) the rat must remain inside the proximity zone before the cat reacts.")]
    private float _proximityAlertTime = 1.5f;

    [Header("Animation")]
    [SerializeField]
    [Tooltip("Animator on the Kitty child. If left empty it is auto-detected from children at Start.")]
    private Animator _animator;

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

    /// <summary>
    /// How full the proximity alert meter is (0–1). Exposed so FovVisualizer can tint the proximity ring.
    /// </summary>
    public float ProximityAlertRadius => _proximityRadius;
    public float ProximityAlertFill   => _proximityAlertTime > 0f ? _proximityTimer / _proximityAlertTime : 0f;
    public float VisionAlertFill      => _visionAlertTime    > 0f ? _visionTimer    / _visionAlertTime    : 0f;

    private NavMeshAgent _agent;
    private Transform _playerTransform;
    private State _currentState;
    private int _waypointIndex;
    private Vector3 _lastKnownPosition;
    private float _investigateTimer;
    private bool _gameOver;

    // Proximity sense — tracks how long the rat has been inside the inner zone
    private float _proximityTimer;

    // Vision sense — tracks how long the rat has been held in the FOV cone
    private float _visionTimer;

    // Animator parameter hashes — cached to avoid per-frame string lookups
    private int _animVert;
    private int _animState;

    // Smoothed animator values — interpolated at the same rate as CreatureMover (4.5f/s)
    private const float k_AnimFlow = 4.5f;
    private float _flowVert;
    private float _flowState;

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

        if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>();
        }

        _animVert  = Animator.StringToHash("Vert");
        _animState = Animator.StringToHash("State");

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

        UpdateProximitySense();
        UpdateVisionSense();
        DriveAnimator();
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
                // Set destination immediately so the agent starts moving this frame
                // and DriveAnimator sees velocity > 0 on the same tick.
                if (_playerTransform != null)
                    _agent.SetDestination(_playerTransform.position);
                break;
        }
    }

    /// <summary>
    /// Accumulates the proximity timer while the rat lingers in the inner zone.
    /// Bypasses FOV entirely — triggers Chase once the timer fills.
    /// Resets immediately when the rat leaves the zone or Chase is already active.
    /// </summary>
    private void UpdateProximitySense()
    {
        // Already chasing — no need to run proximity sense
        if (_currentState == State.Chase || _playerTransform == null)
        {
            _proximityTimer = 0f;
            return;
        }

        float distToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

        if (distToPlayer <= _proximityRadius)
        {
            _proximityTimer += Time.deltaTime;

            if (_proximityTimer >= _proximityAlertTime)
            {
                _proximityTimer = 0f;
                _lastKnownPosition = _playerTransform.position;
                TransitionTo(State.Chase);
            }
        }
        else
        {
            // Rat left the zone — decay the timer back to zero
            _proximityTimer = Mathf.Max(0f, _proximityTimer - Time.deltaTime);
        }
    }

    /// <summary>
    /// Accumulates the vision timer while the rat is held inside the FOV cone.
    /// Decays when the rat leaves the cone. Triggers Chase when the timer fills.
    /// Skipped entirely while already chasing.
    /// </summary>
    private void UpdateVisionSense()
    {
        if (_currentState == State.Chase || _playerTransform == null)
        {
            _visionTimer = 0f;
            return;
        }

        if (CanSeePlayer())
        {
            _visionTimer += Time.deltaTime;

            if (_visionTimer >= _visionAlertTime)
            {
                _visionTimer = 0f;
                _lastKnownPosition = _playerTransform.position;
                TransitionTo(State.Chase);
            }
        }
        else
        {
            // Rat left the cone — decay the timer back to zero
            _visionTimer = Mathf.Max(0f, _visionTimer - Time.deltaTime);
        }
    }

    /// <summary>
    /// In Patrol state, the cat moves between waypoints.
    /// </summary>
    private void UpdatePatrol()
    {
        if (!_agent.pathPending && _agent.remainingDistance < _waypointTolerance)
        {
            AdvanceWaypoint();
        }
    }

    /// <summary>
    /// In Investigate state, the cat moves to the player's last known position and waits.
    /// If the timer runs out without spotting the player again, it returns to Patrol.
    /// </summary>
    private void UpdateInvestigate()
    {
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
    /// In Chase state, the cat continuously updates its destination to the player's current position.
    /// The cat considers the player detected if it can see them OR they are still within proximity range.
    /// This prevents proximity-triggered chases from immediately reverting to Investigate
    /// because the rat was behind the cat and outside the FOV cone.
    /// </summary>
    private void UpdateChase()
    {
        if (_playerTransform == null) return;

        float dist = Vector3.Distance(transform.position, _playerTransform.position);
        bool canDetect = CanSeePlayer() || dist <= _proximityRadius;

        if (canDetect)
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
    /// Drives the Kitty Animator each frame.
    /// Vert=0 → idle, Vert=1+State=0 → walk, Vert=1+State=1 → run.
    /// </summary>
    private void DriveAnimator()
    {
        if (_animator == null)
        {
            return;
        }

        float targetVert  = _agent.velocity.magnitude > 0.1f ? 1f : 0f;
        float targetState = _currentState == State.Chase      ? 1f : 0f;

        // Smoothly interpolate toward target values — mirrors CreatureMover's k_InputFlow logic
        float delta = k_AnimFlow * Time.deltaTime;
        _flowVert  = Mathf.Clamp01(_flowVert  + delta * Mathf.Sign(targetVert  - _flowVert));
        _flowState = Mathf.Clamp01(_flowState + delta * Mathf.Sign(targetState - _flowState));

        // Update animator parameters with the smoothed values
        _animator.SetFloat(_animVert,  _flowVert);
        _animator.SetFloat(_animState, _flowState);
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

        // Force idle pose immediately — Update won't run DriveAnimator again
        if (_animator != null)
        {
            _animator.SetFloat(_animVert,  0f);
            _animator.SetFloat(_animState, 0f);
        }

        Debug.Log("GAME OVER");
        GameManager.Instance.OnCaughtByCat?.Invoke();
    }
}
