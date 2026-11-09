using UnityEngine;
using UnityEngine.InputSystem;

// Reads the Player action map once per frame (Update) and exposes the values.
[DefaultExecutionOrder(-100)]
public class PlayerInputReader : MonoBehaviour
{
    [SerializeField] InputActionAsset asset;

    InputActionMap map;
    InputAction move, jump, attack, dash, shockwave; // right mouse button: intentionally unbound (DECISIONS 15)

    public float MoveX { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool JumpHeld { get; private set; }
    public bool AttackPressed { get; private set; }
    public bool DashPressed { get; private set; }
    public bool ShockwavePressed { get; private set; }

    void Awake()
    {
        map = asset.FindActionMap("Player", true);
        move = map.FindAction("Move", true);
        jump = map.FindAction("Jump", true);
        attack = map.FindAction("Attack", true);
        dash = map.FindAction("Dash", true);
        shockwave = new InputAction("Shockwave", InputActionType.Button, "<Keyboard>/space");
        shockwave.AddBinding("<Gamepad>/buttonEast");
    }

    void OnEnable() { map.Enable(); shockwave.Enable(); }
    void OnDisable() { map.Disable(); shockwave.Disable(); }

    void Update()
    {
        if (GameFlow.InputLocked) // blocking dialogue: no input
        {
            MoveX = 0f;
            JumpPressed = JumpHeld = AttackPressed = DashPressed = ShockwavePressed = false;
            return;
        }
        MoveX = move.ReadValue<Vector2>().x;
        JumpPressed = jump.WasPressedThisFrame();
        JumpHeld = jump.IsPressed();
        AttackPressed = attack.WasPressedThisFrame();
        DashPressed = dash.WasPressedThisFrame();
        ShockwavePressed = shockwave.WasPressedThisFrame();
    }
}
