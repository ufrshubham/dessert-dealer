using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RatController : MonoBehaviour
{
    public float moveSpeed = 5f;
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

    void Start()
    {
        // getting references
        rb = (Rigidbody)GetComponent("Rigidbody");
        animator = (Animator)GetComponent("Animator");
        inputActions = InputManager.Instance.inputActions;

        // subscribing to jump action
        inputActions.Player.Jump.performed += ctx => Jump();

        // getting movement parameter hash
        movementId = Animator.StringToHash("movement");
    }

    void FixedUpdate()
    {
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();

        // rotate rat around Y axis using x movement input
        yaw = moveInput.x * rotationSpeed * Time.fixedDeltaTime;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, yaw, 0f));

        // move rat forward/backward using y movement input
        movement = transform.forward * moveInput.y * moveSpeed * Time.fixedDeltaTime;
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
    }

}
