using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AdventureDirector : MonoBehaviour
{
    public static AdventureDirector Instance { get; private set; }

    MusicalMagePlayer player;
    NpcTalk[] npcs;
    Font font;
    Text goalText, partyText, promptText, toastText, dialogueName, dialogueText, controlsText, dialogueHint;
    Button resumeButton, endingMenuButton, controlsButton, controlsCloseButton;
    GameObject dialoguePanel, pausePanel, endingPanel, controlsPanel;
    Image flash;
    NpcTalk talkingTo;
    int lineIndex;
    float toastUntil;
    float nextPartyRefresh;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Time.timeScale = 1f;
        if (scene.name == GameFlow.MenuScene)
        {
            GameFlow.NewGame();
            GameMusic songs = GameMusic.Get();
            GameAudio.PlaySong(null);
            if (songs != null) GameAudio.UseMenuSong(songs.mainMenu);
            return;
        }
        if (FindAnyObjectByType<MusicalMagePlayer>() == null) return;
        if (Instance != null) return;
        new GameObject("Adventure Director").AddComponent<AdventureDirector>();
    }

    void Awake()
    {
        Instance = this;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        player = FindAnyObjectByType<MusicalMagePlayer>();
        npcs = FindObjectsByType<NpcTalk>();
        if (EventSystem.current == null)
        {
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        BuildUI();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (GameFlow.PlaceAtReturnPosition && scene == GameFlow.ReturnScene) Teleport(GameFlow.ReturnPosition);
        GameFlow.PlaceAtReturnPosition = false;
        GameFlow.Busy = false;
        if (!string.IsNullOrEmpty(GameFlow.MessageOnReturn))
        {
            Toast(GameFlow.MessageOnReturn);
            GameFlow.MessageOnReturn = null;
        }
        goalText.text = Goal(scene);
        partyText.text = GameFlow.PartySummary();

        GameMusic songs = GameMusic.Get();
        if (songs != null)
        {
            bool forest = scene == "Forest";
            GameAudio.PlaySong(forest ? songs.forest : songs.city, forest && songs.muffleForest && !GameFlow.FinalDefeated);
        }

        if (GameFlow.FinalDefeated && !GameFlow.EndingShown) StartCoroutine(ShowEndingSoon());
        else if (scene == "Forest" && GameFlow.BossDefeated && !GameFlow.ValeRevealShown) StartCoroutine(ValeReveal());
        else if (scene == "Kingdom" && !GameFlow.IntroShown) StartCoroutine(OpeningNarration());
    }

    IEnumerator ValeReveal()
    {
        GameFlow.ValeRevealShown = true;
        GameFlow.Busy = true;
        yield return new WaitForSeconds(1f);
        Narrate("Project Silence",
            "The Hollow Colossus crumbles into silent stone... but the forest does not wake up.",
            "A cold voice drifts between the trees: \"So the old Maestro's student has finally come.\"",
            "Archcantor Vale, the one who tried to bind every voice into one endless chord, waits at the heart of the forest.");
    }

    IEnumerator OpeningNarration()
    {
        GameFlow.IntroShown = true;
        GameFlow.Busy = true;
        yield return new WaitForSeconds(0.8f);
        Narrate("Project Silence",
            "Cantoria was built on song. Every bell, every voice, every footstep holds the world together.",
            "Now a Silence is creeping out of the Forest. Where it passes, music dies, and so does everything it held up.",
            "You are the Maestro. With Harmony, Forte and Lyra at your side, find the source of the Silence and break it.",
            "{move} to walk, {sprint} to sprint, {talk} to talk, {pause} to pause and see all the controls. Townsfolk can help you.");
    }

    void Narrate(string speaker, params string[] lines)
    {
        NpcTalk narrator = gameObject.AddComponent<NpcTalk>();
        narrator.enabled = false;
        narrator.npcName = speaker;
        narrator.lines = lines;
        StartTalking(narrator);
    }

    System.Action afterTalk;

    public void NarrateThen(string speaker, string[] lines, System.Action after)
    {
        afterTalk = after;
        Narrate(speaker, lines);
    }

    static string Goal(string scene)
    {
        if (GameFlow.FinalDefeated) return "Goal: The Silence is broken!";
        if (GameFlow.BossDefeated) return "Goal: Archcantor Vale waits at the heart of the Forest.\nFace him and end the Silence.";
        if (scene == "Kingdom") return "Goal: The Silence is spreading from the Forest.\nLeave by the east gate and stop it.";
        if (scene == "Forest") return "Goal: Find the Hollow Colossus deep in the Forest\nand defeat it.";
        return "Goal: Stop the Silence.";
    }

    bool confirmHeld, pauseHeld, backHeld;
    float confirmReadyAt;

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        Gamepad pad = Gamepad.current;
        bool confirmDown = (keyboard != null && (keyboard.eKey.isPressed || keyboard.enterKey.isPressed || keyboard.numpadEnterKey.isPressed || keyboard.spaceKey.isPressed))
                           || (pad != null && pad.buttonSouth.isPressed);
        bool pauseDown = (keyboard != null && keyboard.escapeKey.isPressed) || (pad != null && pad.startButton.isPressed);
        bool backDown = pad != null && pad.buttonEast.isPressed;
        bool confirm = confirmDown && !confirmHeld;
        bool pausePressed = pauseDown && !pauseHeld;
        bool back = backDown && !backHeld;
        confirmHeld = confirmDown;
        pauseHeld = pauseDown;
        backHeld = backDown;

        if (endingPanel.activeSelf) return;
        if (controlsPanel.activeSelf)
        {
            if (back || pausePressed) ShowControls(false);
            return;
        }
        if (pausePressed && talkingTo == null)
        {
            SetPaused(!pausePanel.activeSelf);
            return;
        }
        if (pausePanel.activeSelf)
        {
            if (back) SetPaused(false);
            return;
        }

        bool pad1 = GamepadSupport.UsingGamepad;
        NpcTalk nearest = talkingTo == null && !GameFlow.Busy ? NearestNpc() : null;
        promptText.text = nearest != null ? GamepadSupport.Words("{talk}   Talk to ") + nearest.npcName : "";
        if (confirm) Confirm();

        if (Time.time > nextPartyRefresh)
        {
            partyText.text = GameFlow.PartySummary();
            controlsText.text = GamepadSupport.Words("{move} move    {sprint} sprint    {talk} talk    {pause} pause / controls");
            dialogueHint.text = (pad1 ? GamepadSupport.Confirm : "E / Enter") + ": next";
            if (talkingTo != null) ShowLine();
            nextPartyRefresh = Time.time + 0.25f;
        }
        toastText.gameObject.SetActive(Time.time < toastUntil);
    }

    public void Confirm()
    {
        if (Time.unscaledTime < confirmReadyAt) return;
        confirmReadyAt = Time.unscaledTime + 0.15f;
        if (talkingTo != null)
        {
            NextLine();
            return;
        }
        if (GameFlow.Busy || endingPanel.activeSelf || pausePanel.activeSelf) return;
        NpcTalk nearest = NearestNpc();
        if (nearest != null) StartTalking(nearest);
    }

    public bool IsTalking => talkingTo != null;


    NpcTalk NearestNpc()
    {
        if (player == null) return null;
        return npcs.Where(n => n != null && n.isActiveAndEnabled
                               && Vector3.Distance(Flat(n.transform.position), Flat(player.transform.position)) <= n.talkRange)
                   .OrderBy(n => Vector3.Distance(n.transform.position, player.transform.position))
                   .FirstOrDefault();
    }

    void StartTalking(NpcTalk npc)
    {
        talkingTo = npc;
        lineIndex = 0;
        GameFlow.Busy = true;
        if (player != null) player.DriveForSeconds(Vector2.zero, false, 100000f);
        promptText.text = "";
        dialoguePanel.SetActive(true);
        GameAudio.Play(GameSounds.Slots.talk);
        ShowLine();
    }

    void ShowLine()
    {
        dialogueName.text = talkingTo.npcName;
        dialogueText.text = talkingTo.lines.Length > 0 ? GamepadSupport.Words(talkingTo.lines[lineIndex]) : "...";
    }

    void NextLine()
    {
        lineIndex++;
        if (lineIndex < talkingTo.lines.Length)
        {
            GameAudio.Play(GameSounds.Slots.nextLine);
            ShowLine();
            return;
        }
        bool heal = talkingTo.healsParty;
        talkingTo = null;
        dialoguePanel.SetActive(false);
        GameFlow.Busy = false;
        if (player != null) player.DriveForSeconds(Vector2.zero, false, 0f);
        if (heal)
        {
            GameAudio.Play(GameSounds.Slots.heal);
            GameFlow.HealParty();
            partyText.text = GameFlow.PartySummary();
            Toast("Your party is fully healed.");
        }
        System.Action after = afterTalk;
        afterTalk = null;
        after?.Invoke();
    }


    public void Toast(string message)
    {
        toastText.text = message;
        toastText.gameObject.SetActive(true);
        toastUntil = Time.time + 4.5f;
    }

    public void FlashThenLoad(string scene)
    {
        StartCoroutine(FlashRoutine(scene));
    }

    IEnumerator FlashRoutine(string scene)
    {
        promptText.text = "";
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.35f)
        {
            flash.color = new Color(1f, 1f, 1f, t);
            yield return null;
        }
        SceneManager.LoadScene(scene);
    }

    void SetPaused(bool paused)
    {
        pausePanel.SetActive(paused);
        GameAudio.Play(paused ? GameSounds.Slots.confirm : GameSounds.Slots.back);
        Time.timeScale = paused ? 0f : 1f;
        GameFlow.Busy = paused;
        confirmReadyAt = Time.unscaledTime + 0.35f;
        if (paused && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
    }

    void ShowControls(bool show)
    {
        controlsPanel.SetActive(show);
        pausePanel.SetActive(!show);
        GameAudio.Play(show ? GameSounds.Slots.confirm : GameSounds.Slots.back);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(show ? controlsCloseButton.gameObject : controlsButton.gameObject);
    }

    void BuildControlsScreen(Transform root, Color gold)
    {
        controlsPanel = MakePanel(root, "Controls Screen", Vector2.zero, Vector2.zero, Vector2.zero, new Color(0.02f, 0.02f, 0.06f, 0.95f));
        Stretch((RectTransform)controlsPanel.transform);
        Text title = MakeText(controlsPanel.transform, "Title", 60, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 410f), new Vector2(1200f, 80f));
        title.text = "Controls";
        title.color = gold;

        string[,] rows =
        {
            { "",                       "Keyboard + mouse",               "Controller (Xbox / PlayStation)" },
            { "Walk",                   "WASD or arrow keys",             "Left stick" },
            { "Sprint (hold)",          "Shift",                          "RT, or L3 (press the left stick in)" },
            { "Talk / next line",       "E or Enter",                     "A / Cross" },
            { "Pause",                  "Esc",                            "Start / Options" },
            { "Battle: choose action",  "Click a button",                 "D-pad or stick, then A / Cross" },
            { "Battle: choose enemy",   "Click it (or arrows + Enter)",   "Left / Right, then A / Cross" },
            { "Battle: go back",        "Esc or right-click",             "B / Circle" },
            { "Battle: hit the beat",   "Space",                          "A / Cross" },
            { "Menus",                  "Mouse, or arrows + Enter",       "D-pad or stick, A / Cross, B / Circle" },
        };
        float[] columnCenter = { -650f, -150f, 500f };
        float[] columnWidth = { 460f, 500f, 760f };
        for (int column = 0; column < 3; column++)
        {
            var lines = new System.Text.StringBuilder();
            for (int row = 0; row < rows.GetLength(0); row++)
                lines.AppendLine(row == 0 ? "<b>" + rows[row, column] + "</b>" : rows[row, column]);
            Text text = MakeText(controlsPanel.transform, "Column " + column, 32, TextAnchor.UpperLeft, new Vector2(0.5f, 0.5f),
                                 new Vector2(columnCenter[column], 30f), new Vector2(columnWidth[column], 600f));
            text.lineSpacing = 1.35f;
            text.text = lines.ToString();
            if (column == 0) text.color = gold;
        }
        controlsCloseButton = MakeButton(controlsPanel.transform, "Close", new Vector2(0f, -400f), () => ShowControls(false));
        controlsPanel.SetActive(false);
    }

    IEnumerator ShowEndingSoon()
    {
        GameFlow.EndingShown = true;
        GameFlow.Busy = true;
        yield return new WaitForSecondsRealtime(1.2f);
        endingPanel.SetActive(true);
        GameMusic songs = GameMusic.Get();
        if (songs != null && songs.ending != null) GameAudio.PlaySong(songs.ending);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(endingMenuButton.gameObject);
    }

    static void GoToMenu()
    {
        Time.timeScale = 1f;
        GameFlow.NewGame();
        SceneManager.LoadScene(GameFlow.MenuScene);
    }

    void Teleport(Vector3 position)
    {
        if (player == null) return;
        CharacterController capsule = player.GetComponent<CharacterController>();
        if (capsule != null) capsule.enabled = false;
        Vector3 delta = position - player.transform.position;
        player.transform.position = position;
        if (capsule != null) capsule.enabled = true;
        foreach (MusicalMageFollowCamera follow in FindObjectsByType<MusicalMageFollowCamera>()) follow.SnapToTarget();
        foreach (Unity.Cinemachine.CinemachineCamera cam in FindObjectsByType<Unity.Cinemachine.CinemachineCamera>())
            cam.OnTargetObjectWarped(player.transform, delta);
    }

    static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v;
    }


    void BuildUI()
    {
        var canvasObject = new GameObject("Adventure HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        Transform root = canvasObject.transform;
        Color gold = new Color(1f, 0.85f, 0.45f);

        goalText = MakeText(root, "Goal", 30, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(40f, -30f), new Vector2(1200f, 90f));
        goalText.color = gold;
        partyText = MakeText(root, "Party", 26, TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(40f, 28f), new Vector2(1300f, 40f));
        controlsText = MakeText(root, "Controls", 22, TextAnchor.LowerRight, new Vector2(1f, 0f), new Vector2(-40f, 28f), new Vector2(760f, 34f));
        controlsText.text = "WASD move    Shift sprint    E talk    Esc pause";
        promptText = MakeText(root, "Prompt", 34, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, 330f), new Vector2(900f, 56f));
        toastText = MakeText(root, "Message", 32, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(1500f, 90f));
        toastText.gameObject.SetActive(false);

        dialoguePanel = MakePanel(root, "Dialogue", new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(1400f, 250f), new Color(0.04f, 0.04f, 0.1f, 0.9f));
        dialogueName = MakeText(dialoguePanel.transform, "Name", 32, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(40f, -22f), new Vector2(1320f, 44f));
        dialogueName.color = gold;
        dialogueText = MakeText(dialoguePanel.transform, "Line", 30, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(40f, -76f), new Vector2(1320f, 140f));
        dialogueHint = MakeText(dialoguePanel.transform, "Hint", 22, TextAnchor.LowerRight, new Vector2(1f, 0f), new Vector2(-30f, 16f), new Vector2(400f, 30f));
        dialogueHint.text = "E / Enter: next";
        dialoguePanel.SetActive(false);

        flash = MakePanel(root, "Battle Flash", Vector2.zero, Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0f)).GetComponent<Image>();
        Stretch(flash.rectTransform);
        flash.raycastTarget = false;

        pausePanel = MakePanel(root, "Pause", Vector2.zero, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.7f));
        Stretch((RectTransform)pausePanel.transform);
        MakeText(pausePanel.transform, "Title", 64, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 200f), new Vector2(900f, 90f)).text = "Paused";
        resumeButton = MakeButton(pausePanel.transform, "Resume", new Vector2(0f, 60f), () => SetPaused(false));
        controlsButton = MakeButton(pausePanel.transform, "Controls", new Vector2(0f, -40f), () => ShowControls(true));
        MakeButton(pausePanel.transform, "Main menu", new Vector2(0f, -140f), GoToMenu);
        MakeButton(pausePanel.transform, "Quit", new Vector2(0f, -240f), Application.Quit);
        pausePanel.SetActive(false);
        BuildControlsScreen(root, gold);

        endingPanel = MakePanel(root, "Ending", Vector2.zero, Vector2.zero, Vector2.zero, new Color(0.02f, 0.02f, 0.06f, 0.92f));
        Stretch((RectTransform)endingPanel.transform);
        Text endTitle = MakeText(endingPanel.transform, "Title", 72, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 290f), new Vector2(1400f, 100f));
        endTitle.text = "The Silence Breaks";
        endTitle.color = gold;
        MakeText(endingPanel.transform, "Story", 32, TextAnchor.UpperCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 190f), new Vector2(1300f, 360f)).text =
            "Archcantor Vale falls, and his endless chord finally finds its last note.\n" +
            "Every sound the Silence swallowed pours back into the world: birdsong,\n" +
            "wind in the leaves, and far away, the cathedral bells of the kingdom.\n\n" +
            "Maestro, Harmony, Forte and Lyra walk home through a forest that sings again.\n\n" +
            "Thank you for playing Project Silence.";
        endingMenuButton = MakeButton(endingPanel.transform, "Main menu", new Vector2(0f, -250f), GoToMenu);
        MakeButton(endingPanel.transform, "Quit", new Vector2(0f, -350f), Application.Quit);
        endingPanel.SetActive(false);
    }

    Text MakeText(Transform parent, string name, int size, TextAnchor alignment, Vector2 anchor, Vector2 position, Vector2 box)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
        go.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = box;
        Text text = go.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        go.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.85f);
        return text;
    }

    GameObject MakePanel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 box, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, anchor.y);
        rect.anchoredPosition = position;
        rect.sizeDelta = box;
        go.GetComponent<Image>().color = color;
        return go;
    }

    Button MakeButton(Transform parent, string label, Vector2 position, UnityAction onClick)
    {
        GameObject panel = MakePanel(parent, label + " Button", new Vector2(0.5f, 0.5f), position, new Vector2(420f, 80f), Color.white);
        Button button = panel.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.15f, 0.15f, 0.28f);
        colors.highlightedColor = new Color(0.75f, 0.58f, 0.2f);
        colors.selectedColor = new Color(0.75f, 0.58f, 0.2f);
        colors.pressedColor = new Color(0.55f, 0.42f, 0.14f);
        colors.colorMultiplier = 1f;
        button.colors = colors;
        button.onClick.AddListener(onClick);
        Text text = MakeText(panel.transform, "Label", 34, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 70f));
        text.text = label;
        return button;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
