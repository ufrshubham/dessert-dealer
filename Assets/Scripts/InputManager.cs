using UnityEngine;

public class InputManager : MonoBehaviour
{
    public InputActions inputActions;
    public static InputManager Instance;
    void Awake()
    {
        if (Instance != this && Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        inputActions = new InputActions();
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        inputActions?.Enable();
    }

    void OnDisable()
    {
        inputActions?.Disable();
    }


}
