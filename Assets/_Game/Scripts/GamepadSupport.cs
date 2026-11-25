using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// =====================================================================================
// GamepadSupport.cs - written with AI help (Claude, 25 Nov 2026). List it in READ THIS DOCTOR.txt.
//
// Makes every screen usable with a controller. It adds itself once and runs in every scene:
//   - remembers whether you last used a controller or keyboard/mouse (for the on-screen hints)
//   - when you use a controller and a menu is open, makes sure a button is selected, so the
//     d-pad/stick moves between buttons and A (Cross on PlayStation) presses them
//   - draws a gold outline around the selected button so you can see where you are
//   - gives UI input (mouse, keyboard and controller) to scenes whose event system has none
// =====================================================================================
public class GamepadSupport : MonoBehaviour
{
    public static bool UsingGamepad { get; private set; }

    // Button names that match the controller in use.
    public static string Confirm => IsPlayStation ? "Cross" : "A";
    public static string Back => IsPlayStation ? "Circle" : "B";
    public static string Pause => IsPlayStation ? "Options" : "Start";
    static bool IsPlayStation => Gamepad.current is DualShockGamepad;

    EventSystem fixedEventSystem;
    GameObject outlinedObject;
    Outline outline;
    float nextSelectCheck;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        var go = new GameObject("Gamepad Support");
        DontDestroyOnLoad(go);
        go.AddComponent<GamepadSupport>();
    }

    void Update()
    {
        TrackLastDevice();
        EventSystem events = EventSystem.current;
        if (events == null) return;
        GiveUiInput(events);
        if (UsingGamepad) KeepAButtonSelected(events);
        OutlineSelected(UsingGamepad ? events.currentSelectedGameObject : null);
    }

    static void TrackLastDevice()
    {
        Gamepad pad = Gamepad.current;
        if (pad != null && (pad.leftStick.ReadValue().sqrMagnitude > 0.25f || pad.rightStick.ReadValue().sqrMagnitude > 0.25f
                            || pad.dpad.ReadValue().sqrMagnitude > 0.25f || pad.buttonSouth.isPressed || pad.buttonEast.isPressed
                            || pad.buttonWest.isPressed || pad.buttonNorth.isPressed || pad.startButton.isPressed
                            || pad.leftTrigger.isPressed || pad.rightTrigger.isPressed || pad.leftStickButton.isPressed))
            UsingGamepad = true;
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.anyKey.isPressed) UsingGamepad = false;
        Mouse mouse = Mouse.current;
        if (mouse != null && (mouse.leftButton.isPressed || mouse.delta.ReadValue().sqrMagnitude > 25f)) UsingGamepad = false;
    }

    // An event system made without input actions can't hear the controller; give it the default ones.
    void GiveUiInput(EventSystem events)
    {
        if (fixedEventSystem == events) return;
        fixedEventSystem = events;
        InputSystemUIInputModule module = events.GetComponent<InputSystemUIInputModule>();
        if (module != null && (module.actionsAsset == null || module.move == null || module.move.action == null))
            module.AssignDefaultActions();
    }

    // If nothing usable is selected, select the top-left button that is on screen (the first menu item).
    void KeepAButtonSelected(EventSystem events)
    {
        GameObject current = events.currentSelectedGameObject;
        if (current != null && current.activeInHierarchy)
        {
            Selectable selectable = current.GetComponent<Selectable>();
            if (selectable != null && selectable.IsInteractable()) return;
        }
        if (Time.unscaledTime < nextSelectCheck) return;
        nextSelectCheck = Time.unscaledTime + 0.1f;

        Selectable best = null;
        foreach (Selectable candidate in Selectable.allSelectablesArray)
        {
            if (!candidate.isActiveAndEnabled || !candidate.IsInteractable()) continue;
            if (best == null || IsHigherOrMoreLeft(candidate, best)) best = candidate;
        }
        if (best != null) events.SetSelectedGameObject(best.gameObject);
    }

    static bool IsHigherOrMoreLeft(Selectable a, Selectable b)
    {
        Vector3 pa = a.transform.position;
        Vector3 pb = b.transform.position;
        if (Mathf.Abs(pa.y - pb.y) > 10f) return pa.y > pb.y;
        return pa.x < pb.x;
    }

    // A gold outline on the selected button (only while using a controller).
    void OutlineSelected(GameObject selected)
    {
        if (selected == outlinedObject) return;
        if (outline != null) Destroy(outline);
        outline = null;
        outlinedObject = selected;
        if (selected == null || selected.GetComponent<Graphic>() == null) return;
        outline = selected.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.82f, 0.3f, 1f);
        outline.effectDistance = new Vector2(5f, -5f);
    }
}
