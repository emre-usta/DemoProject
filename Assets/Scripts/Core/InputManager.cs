using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    // Simple directional input for movement, to be set by UI joystick or by editor for testing
    public Vector2 MovementInput { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Call this from your UI joystick script
    public void SetMovementInput(Vector2 input)
    {
        MovementInput = input;
    }

    // Optional: Keyboard support for editor/debugging
    private void Update()
    {
#if UNITY_EDITOR
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        if (h != 0 || v != 0)
        {
            MovementInput = new Vector2(h, v).normalized;
        }
#endif
    }
}