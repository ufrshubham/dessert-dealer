using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RatController : MonoBehaviour
{
    public float regularMoveSpeed = 5f;
    public float heavyMoveSpeed = 2f;
    public float jumpForce = 5f;
    public float rotationSpeed = 120f;
    public float animationSmoothTime = 0.1f;

    Rigidbody rb;
    Animator animator;
    InputActions inputActions;
    Vector2 moveInput;
    float yaw;
    Vector3 movement;
    float currentMovement;
    float smoothedMovement;
    int movementId;
    DessertCollector dessertCollector;
    float currentMoveSpeed;

    void Start()
    {
        // getting references
        rb = (Rigidbody)GetComponent("Rigidbody");
        animator = (Animator)GetComponent("Animator");
        dessertCollector = (DessertCollector)GetComponent("DessertCollector");
        inputActions = InputManager.Instance.inputActions;

        // subscribing to jump action
        inputActions.Player.Jump.performed += ctx => Jump();

        // subscribing to dessert picked up action
        dessertCollector.OnDessertPickedUp += OnDessertPickedUp;

        // subscribing to dessert dropped action
        dessertCollector.OnDessertDropped += OnDessertDropped;


        // getting movement parameter hash
        movementId = Animator.StringToHash("movement");

        // initialize yaw
        yaw = transform.eulerAngles.y;

        currentMoveSpeed = regularMoveSpeed;

        Cursor.lockState = CursorLockMode.Locked;
    }

    void FixedUpdate()
    {
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();

        // update yaw
        yaw += moveInput.x * rotationSpeed * Time.fixedDeltaTime;
        yaw %= 360f;
        Quaternion flatYawRotation = Quaternion.Euler(0f, yaw, 0f);

        Vector3 targetUp = Vector3.up;
        bool foundHit = false;
        float closestDistance = float.MaxValue;
        RaycastHit slopeHit = new RaycastHit();

        // cast a ray down to find the slope normal, ignoring the rat itself
        RaycastHit[] hits = Physics.RaycastAll(transform.position + Vector3.up * 0.1f, Vector3.down, 0.5f);
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.root != transform && !hit.collider.isTrigger && hit.distance < closestDistance)
            {
                slopeHit = hit;
                closestDistance = hit.distance;
                foundHit = true;
            }
        }

        if (foundHit)
        {
            // to only apply pitch (X-axis rotation) and ignore roll (Z-axis rotation),
            // we project the slope normal onto the plane defined by our right vector.
            Vector3 flatRight = flatYawRotation * Vector3.right;
            targetUp = Vector3.ProjectOnPlane(slopeHit.normal, flatRight).normalized;
        }

        // smoothly interpolate the Up vector
        Vector3 currentUp = rb.rotation * Vector3.up;
        Vector3 smoothedUp = Vector3.Slerp(currentUp, targetUp, Time.fixedDeltaTime * 10f);

        // calculate final rotation
        Vector3 flatForward = flatYawRotation * Vector3.forward;
        Vector3 projectedForward = Vector3.ProjectOnPlane(flatForward, smoothedUp).normalized;

        if (projectedForward == Vector3.zero)
            projectedForward = flatForward;

        Quaternion finalRotation = Quaternion.LookRotation(projectedForward, smoothedUp);
        rb.MoveRotation(finalRotation);

        // move rat forward/backward using y movement input along the slope
        movement = finalRotation * Vector3.forward * moveInput.y * currentMoveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);

        // smooth movement for animation
        currentMovement = animator.GetFloat(movementId);
        smoothedMovement = Mathf.Lerp(currentMovement, moveInput.y, Time.fixedDeltaTime / animationSmoothTime);
        animator.SetFloat(movementId, smoothedMovement);

    }

    void Jump()
    {
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }
    void OnDisable()
    {
        inputActions.Player.Jump.performed -= ctx => Jump();
        dessertCollector.OnDessertPickedUp -= OnDessertPickedUp;
        dessertCollector.OnDessertDropped -= OnDessertDropped;
    }
    void OnDessertPickedUp()
    {
        currentMoveSpeed = heavyMoveSpeed;
    }
    void OnDessertDropped()
    {
        currentMoveSpeed = regularMoveSpeed;
    }


}
