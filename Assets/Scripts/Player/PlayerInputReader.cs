using UnityEngine;
using UnityEngine.InputSystem;

// Reads the Player action map once per frame (Update) and exposes the values.
[DefaultExecutionOrder(-100)]
public class PlayerInputReader : MonoBehaviour
{
    [SerializeField] InputActionAsset asset;

    InputActionMap map;
    InputAction move, jump, attack, issen, dice, grapple; // playtest 11-11: Shift (the asset's "Dash" action) = issen (DECISIONS 26), RMB = grapple rope (DECISIONS 16)

    public float MoveX { get; private set; }
    public bool DownHeld { get; private set; } // S / Down: with Space on a one-way ledge = drop through it
    public bool JumpPressed { get; private set; }
    public bool JumpHeld { get; private set; }
    public bool AttackPressed { get; private set; }
    public bool DicePressed { get; private set; }
    public bool IssenPressed { get; private set; }
    public bool GrapplePressed { get; private set; }
    public bool AttackPressedRaw => attack.WasPressedThisFrame(); // ignores GameFlow.InputLocked: EliteDuel reads LMB through its lock

    void Awake()
    {
        map = asset.FindActionMap("Player", true);
        move = map.FindAction("Move", true);
        jump = map.FindAction("Jump", true);
        attack = map.FindAction("Attack", true);
        issen = map.FindAction("Dash", true);
        dice = new InputAction("Dice", InputActionType.Button, "<Keyboard>/e"); // DECISIONS 31: E, shared with interact (Space = jump)
        dice.AddBinding("<Gamepad>/buttonEast");
        grapple = new InputAction("Grapple", InputActionType.Button, "<Mouse>/rightButton");
    }

    void OnEnable() { map.Enable(); dice.Enable(); grapple.Enable(); }
    void OnDisable() { map.Disable(); dice.Disable(); grapple.Disable(); }

    void Update()
    {
        if (GameFlow.InputLocked) // blocking dialogue: no input
        {
            MoveX = 0f;
            DownHeld = false;
            JumpPressed = JumpHeld = AttackPressed = DicePressed = IssenPressed = GrapplePressed = false;
            return;
        }
        Vector2 mv = move.ReadValue<Vector2>();
        MoveX = mv.x;
        DownHeld = mv.y < -0.5f;
        JumpPressed = jump.WasPressedThisFrame();
        JumpHeld = jump.IsPressed();
        AttackPressed = attack.WasPressedThisFrame();
        DicePressed = dice.WasPressedThisFrame() && !GameFlow.InteractNear; // E near a board / button = interact only
        IssenPressed = issen.WasPressedThisFrame();
        GrapplePressed = grapple.WasPressedThisFrame();
    }
}
