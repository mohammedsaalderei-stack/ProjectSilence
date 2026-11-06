using UnityEngine;
using UnityEngine.InputSystem;

public static class BattleInput
{
    static InputActionMap map;

    static InputActionMap Map
    {
        get
        {
            if (map == null)
            {
                map = InputSystem.actions.FindActionMap("Battle", true);
                map.Enable();
            }
            return map;
        }
    }

    public static bool BeatPressed => Map["Beat"].WasPressedThisFrame();
    public static bool ClickPressed => Map["Click"].WasPressedThisFrame();
    public static Vector2 PointerPosition => Map["Point"].ReadValue<Vector2>();
}
