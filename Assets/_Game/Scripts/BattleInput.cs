using UnityEngine;
using UnityEngine.InputSystem;

// The battle's controls.
// Changed with AI help: before, this read the "Battle" map of the project-wide Input Actions asset.
// The combined project uses your friend's controls asset instead, so the battle now defines its
// own three actions here in code.
public static class BattleInput
{
    static InputActionMap map;

    static InputActionMap Map
    {
        get
        {
            if (map == null)
            {
                map = new InputActionMap("Battle");
                InputAction beat = map.AddAction("Beat", InputActionType.Button);
                beat.AddBinding("<Keyboard>/space");
                beat.AddBinding("<Gamepad>/buttonSouth");
                InputAction click = map.AddAction("Click", InputActionType.Button);
                click.AddBinding("<Mouse>/leftButton");
                map.AddAction("Point", InputActionType.Value, "<Pointer>/position", expectedControlLayout: "Vector2");

                // Added with AI help: choosing a target without the mouse (controller or keyboard).
                InputAction navigate = map.AddAction("Navigate", InputActionType.Value, expectedControlLayout: "Vector2");
                navigate.AddBinding("<Gamepad>/dpad");
                navigate.AddBinding("<Gamepad>/leftStick");
                navigate.AddCompositeBinding("2DVector")
                    .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                    .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
                InputAction confirm = map.AddAction("Confirm", InputActionType.Button);
                confirm.AddBinding("<Gamepad>/buttonSouth");
                confirm.AddBinding("<Keyboard>/enter");
                confirm.AddBinding("<Keyboard>/numpadEnter");
                InputAction cancel = map.AddAction("Cancel", InputActionType.Button);
                cancel.AddBinding("<Gamepad>/buttonEast");
                cancel.AddBinding("<Keyboard>/escape");
                cancel.AddBinding("<Keyboard>/backspace");
                cancel.AddBinding("<Mouse>/rightButton");
                map.Enable();
            }
            return map;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetMap()
    {
        map?.Dispose();
        map = null;
    }

    public static bool BeatPressed => Map["Beat"].WasPressedThisFrame();
    public static bool ClickPressed => Map["Click"].WasPressedThisFrame();
    public static Vector2 PointerPosition => Map["Point"].ReadValue<Vector2>();
    public static Vector2 Navigate => Map["Navigate"].ReadValue<Vector2>();       // AI help
    public static bool ConfirmPressed => Map["Confirm"].WasPressedThisFrame();     // AI help
    public static bool CancelPressed => Map["Cancel"].WasPressedThisFrame();       // AI help
}
