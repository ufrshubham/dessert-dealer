using UnityEngine;
using UnityEngine.InputSystem; 

public class PlayerInteractor : MonoBehaviour, @InputActions.IPlayerActions
{
    private @InputActions inputActions;

    // The trigger zone the player is currently inside (collectible or delivery)
    private IInteractable currentInteractable;

    private void Awake()
    {
        inputActions = new @InputActions();
        inputActions.Player.AddCallbacks(this);
    }

    private void OnEnable()  => inputActions.Player.Enable();
    private void OnDisable() => inputActions.Player.Disable();
    private void OnDestroy() => inputActions.Dispose();

    // Called by trigger zones when player enters/exits
    public void SetInteractable(IInteractable interactable) => currentInteractable = interactable;
    public void ClearInteractable(IInteractable interactable)
    {
        if (currentInteractable == interactable)
            currentInteractable = null;
    }

    // ── IPlayerActions callbacks ─────────────────────────────────────────────

    // Interact has a Hold interaction — performed fires after hold completes
    public void OnInteract(InputAction.CallbackContext ctx)
    {
        // Temporarily log ALL phases to diagnose
        Debug.Log($"[Interact] Phase: {ctx.phase} | performed: {ctx.performed}");
        
        if (ctx.performed)
            currentInteractable?.Interact();
    }

    // Required interface stubs (leave empty or fill in as needed)
    public void OnMove(InputAction.CallbackContext ctx) { }
    public void OnLook(InputAction.CallbackContext ctx) { }
    public void OnAttack(InputAction.CallbackContext ctx) { }
    public void OnCrouch(InputAction.CallbackContext ctx) { }
    public void OnJump(InputAction.CallbackContext ctx) { }
    public void OnPrevious(InputAction.CallbackContext ctx) { }
    public void OnNext(InputAction.CallbackContext ctx) { }
    public void OnSprint(InputAction.CallbackContext ctx) { }
}
