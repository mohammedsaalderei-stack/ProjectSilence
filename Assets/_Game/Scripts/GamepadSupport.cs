using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GamepadSupport : MonoBehaviour
{
    public static bool UsingGamepad { get; private set; }

    public static string Confirm => IsPlayStation ? "Cross" : "A";
    public static string Back => IsPlayStation ? "Circle" : "B";
    public static string Pause => IsPlayStation ? "Options" : "Start";
    static bool IsPlayStation => Gamepad.current is DualShockGamepad;

    public static string Words(string text)
    {
        if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0) return text;
        bool pad = UsingGamepad;
        return text.Replace("{move}", pad ? "Left stick" : "WASD")
                   .Replace("{sprint}", pad ? "RT" : "Shift")
                   .Replace("{talk}", pad ? Confirm : "E")
                   .Replace("{pause}", pad ? Pause : "Esc")
                   .Replace("{confirm}", pad ? Confirm : "Enter")
                   .Replace("{back}", pad ? Back : "Esc")
                   .Replace("{beat}", pad ? Confirm : "Space");
    }

    static float lastPadUse = -10f;
    static float mouseTravel;

    EventSystem fixedEventSystem;
    GameObject outlinedObject;
    Outline outline;
    float nextSelectCheck;
    Text menuHint;
    GameObject menuHintCanvas;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
        UsingGamepad = false;
        lastPadUse = -10f;
        mouseTravel = 0f;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        var go = new GameObject("Gamepad Support");
        DontDestroyOnLoad(go);
        go.AddComponent<GamepadSupport>();
        Application.targetFrameRate = 144;

        if (Gamepad.all.Count > 0) SwitchTo(true, Gamepad.all[0].displayName + " is connected");
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    static void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (device is Gamepad && (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected))
            SwitchTo(true, device.displayName + " was switched on");
    }

    static void SwitchTo(bool gamepad, string reason)
    {
        if (UsingGamepad == gamepad) return;
        UsingGamepad = gamepad;
        Debug.Log("Input: now using " + (gamepad ? "a controller" : "keyboard and mouse") + " (" + reason + ")");
    }

    void Update()
    {
        TrackLastDevice();
        ShowMenuHint();
        EventSystem events = EventSystem.current;
        if (events == null) return;
        GiveUiInput(events);
        if (UsingGamepad || Gamepad.all.Count > 0) KeepAButtonSelected(events);
        if (UsingGamepad) PressBackButtonOnB();
        OutlineSelected(UsingGamepad ? events.currentSelectedGameObject : null);
    }

    static void TrackLastDevice()
    {
        foreach (Gamepad pad in Gamepad.all)
        {
            if (!PadInUse(pad)) continue;
            lastPadUse = Time.unscaledTime;
            if (Gamepad.current != pad) pad.MakeCurrent();
            SwitchTo(true, pad.displayName + " was used");
        }

        Mouse mouse = Mouse.current;
        mouseTravel = Mathf.Max(0f, mouseTravel - 150f * Time.unscaledDeltaTime);
        if (mouse != null) mouseTravel += mouse.delta.ReadValue().magnitude;
        if (Time.unscaledTime - lastPadUse < 0.5f) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.anyKey.wasPressedThisFrame) SwitchTo(false, "a key was pressed");
        if (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame))
            SwitchTo(false, "the mouse was clicked");
        if (mouseTravel > 60f)
        {
            mouseTravel = 0f;
            SwitchTo(false, "the mouse was moved");
        }
    }

    static bool PadInUse(Gamepad pad)
    {
        return pad.leftStick.ReadValue().sqrMagnitude > 0.25f || pad.rightStick.ReadValue().sqrMagnitude > 0.25f
               || pad.dpad.ReadValue().sqrMagnitude > 0.25f
               || pad.buttonSouth.isPressed || pad.buttonEast.isPressed || pad.buttonWest.isPressed || pad.buttonNorth.isPressed
               || pad.startButton.isPressed || pad.selectButton.isPressed
               || pad.leftShoulder.isPressed || pad.rightShoulder.isPressed
               || pad.leftTrigger.isPressed || pad.rightTrigger.isPressed
               || pad.leftStickButton.isPressed || pad.rightStickButton.isPressed;
    }

    void GiveUiInput(EventSystem events)
    {
        if (fixedEventSystem == events) return;
        fixedEventSystem = events;
        InputSystemUIInputModule module = events.GetComponent<InputSystemUIInputModule>();
        if (module != null && (module.actionsAsset == null || module.move == null || module.move.action == null))
            module.AssignDefaultActions();
    }

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

    void PressBackButtonOnB()
    {
        if (SceneManager.GetActiveScene().name != GameFlow.MenuScene) return;
        bool pressed = false;
        foreach (Gamepad pad in Gamepad.all) pressed |= pad.buttonEast.wasPressedThisFrame;
        if (!pressed) return;
        foreach (Selectable candidate in Selectable.allSelectablesArray)
        {
            if (candidate is Button button && button.isActiveAndEnabled && button.IsInteractable()
                && button.name.ToLowerInvariant().Contains("back"))
            {
                button.onClick.Invoke();
                return;
            }
        }
    }

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

    void ShowMenuHint()
    {
        bool inMenu = SceneManager.GetActiveScene().name == GameFlow.MenuScene;
        if (menuHint == null)
        {
            if (!inMenu) return;
            menuHint = MakeMenuHint();
            menuHintCanvas = menuHint.transform.parent.gameObject;
        }
        if (menuHintCanvas.activeSelf != inMenu) menuHintCanvas.SetActive(inMenu);
        if (!inMenu) return;
        menuHint.text = UsingGamepad
            ? "D-pad / left stick: choose     " + Confirm + ": select     " + Back + ": back"
            : "Click with the mouse, or use the arrow keys and Enter.   A controller works too.";
    }

    Text MakeMenuHint()
    {
        var canvasObject = new GameObject("Menu Controls Hint", typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var textObject = new GameObject("Hint", typeof(RectTransform), typeof(Text), typeof(Outline));
        textObject.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = (RectTransform)textObject.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 30f);
        rect.sizeDelta = new Vector2(1600f, 40f);
        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 26;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(1f, 1f, 1f, 0.85f);
        text.raycastTarget = false;
        textObject.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.85f);
        return text;
    }
}
