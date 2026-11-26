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
    GameObject dialoguePanel, pausePanel, endingPanel, controlsPanel, promptBox, toastBox;
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
        promptText.text = nearest != null ? "<color=#F1D27E>" + GamepadSupport.Words("{talk}") + "</color>   Talk to " + nearest.npcName : "";
        promptBox.SetActive(promptText.text.Length > 0);
        if (confirm) Confirm();
        if (Time.time > nextPartyRefresh)
        {
            partyText.text = GameFlow.PartySummary();
            controlsText.text = GamepadSupport.Words("{move} move    {sprint} sprint    {talk} talk    {pause} pause / controls");
            dialogueHint.text = (pad1 ? GamepadSupport.Confirm : "E / Enter") + ": next";
            if (talkingTo != null) ShowLine();
            nextPartyRefresh = Time.time + 0.25f;
        }
        toastBox.SetActive(Time.time < toastUntil);
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
        promptBox.SetActive(false);
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
        toastBox.SetActive(true);
        toastUntil = Time.time + 4.5f;
    }
    public void FlashThenLoad(string scene)
    {
        StartCoroutine(FlashRoutine(scene));
    }
    IEnumerator FlashRoutine(string scene)
    {
        promptText.text = "";
        promptBox.SetActive(false);
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
    void BuildControlsScreen(Transform root, Color gold, Color cream)
    {
        controlsPanel = Fill(root, "Controls Screen", new Color(0f, 0f, 0f, 0.7f));
        RectTransform window = Box(controlsPanel.transform, "Window", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1720f, 900f), false);
        Text title = MakeText(window, "Title", 58, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(1200f, 84f), true);
        title.text = "Controls";
        title.color = gold;
        UiKit.MakeDivider(window, new Vector2(0.5f, 1f), new Vector2(0f, -140f), 600f);
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
        float[] columnCenter = { -590f, -120f, 490f };
        float[] columnWidth = { 420f, 480f, 660f };
        for (int column = 0; column < 3; column++)
        {
            var lines = new System.Text.StringBuilder();
            for (int row = 0; row < rows.GetLength(0); row++)
                lines.AppendLine(row == 0 ? "<b>" + rows[row, column] + "</b>" : rows[row, column]);
            Text text = MakeText(window, "Column " + column, 30, TextAnchor.UpperLeft, new Vector2(0.5f, 0.5f),
                                 new Vector2(columnCenter[column], -10f), new Vector2(columnWidth[column], 560f), false);
            text.lineSpacing = 1.4f;
            text.text = lines.ToString();
            text.color = column == 0 ? gold : cream;
        }
        controlsCloseButton = MakeButton(window, "Close", new Vector2(0f, -370f), () => ShowControls(false), 360f);
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
        UiTheme theme = UiTheme.Get();
        Color gold = theme != null ? theme.headingColor : new Color(1f, 0.85f, 0.45f);
        Color cream = theme != null ? theme.textColor : Color.white;
        Color muted = theme != null ? theme.mutedColor : new Color(0.8f, 0.8f, 0.9f);
        Color ink = theme != null ? theme.inkColor : new Color(0.04f, 0.03f, 0.1f);
        RectTransform goalBox = Box(root, "Goal Box", new Vector2(0f, 1f), new Vector2(36f, -28f), new Vector2(780f, 104f), true);
        goalText = FillText(goalBox, "Goal", 27, TextAnchor.MiddleLeft, gold, 26f, 10f);
        RectTransform partyBox = Box(root, "Party Box", new Vector2(0f, 0f), new Vector2(36f, 24f), new Vector2(900f, 92f), true);
        partyText = FillText(partyBox, "Party", 24, TextAnchor.MiddleLeft, cream, 26f, 8f);
        RectTransform controlsBox = Box(root, "Controls Box", new Vector2(1f, 0f), new Vector2(-36f, 24f), new Vector2(860f, 52f), true);
        controlsText = FillText(controlsBox, "Controls", 21, TextAnchor.MiddleCenter, cream, 18f, 4f);
        RectTransform prompt = Box(root, "Prompt Box", new Vector2(0.5f, 0f), new Vector2(0f, 330f), new Vector2(620f, 72f), false);
        promptBox = prompt.gameObject;
        promptText = FillText(prompt, "Prompt", 30, TextAnchor.MiddleCenter, cream, 20f, 6f);
        promptBox.SetActive(false);
        RectTransform toast = Box(root, "Message Box", new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(960f, 84f), true);
        toastBox = toast.gameObject;
        toastText = FillText(toast, "Message", 30, TextAnchor.MiddleCenter, cream, 30f, 8f);
        toastBox.SetActive(false);
        RectTransform dialogue = Box(root, "Dialogue", new Vector2(0.5f, 0f), new Vector2(0f, 136f), new Vector2(1440f, 264f), false);
        dialoguePanel = dialogue.gameObject;
        dialogueName = MakeText(dialogue, "Name", 34, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(48f, -28f), new Vector2(1340f, 48f), true);
        dialogueName.color = gold;
        dialogueText = MakeText(dialogue, "Line", 30, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(48f, -88f), new Vector2(1340f, 140f), false);
        dialogueText.color = cream;
        dialogueHint = MakeText(dialogue, "Hint", 22, TextAnchor.LowerRight, new Vector2(1f, 0f), new Vector2(-36f, 22f), new Vector2(420f, 30f), false);
        dialogueHint.color = muted;
        dialogueHint.text = "E / Enter: next";
        dialoguePanel.SetActive(false);
        flash = Fill(root, "Battle Flash", new Color(1f, 1f, 1f, 0f)).GetComponent<Image>();
        pausePanel = Fill(root, "Pause", new Color(0f, 0f, 0f, 0.62f));
        RectTransform pauseWindow = Box(pausePanel.transform, "Window", new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(640f, 640f), false);
        Text pauseTitle = MakeText(pauseWindow, "Title", 62, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(560f, 90f), true);
        pauseTitle.text = "Paused";
        pauseTitle.color = gold;
        UiKit.MakeDivider(pauseWindow, new Vector2(0.5f, 1f), new Vector2(0f, -150f), 440f);
        resumeButton = MakeButton(pauseWindow, "Resume", new Vector2(0f, 90f), () => SetPaused(false));
        controlsButton = MakeButton(pauseWindow, "Controls", new Vector2(0f, -8f), () => ShowControls(true));
        MakeButton(pauseWindow, "Main menu", new Vector2(0f, -106f), GoToMenu);
        MakeButton(pauseWindow, "Quit", new Vector2(0f, -204f), Application.Quit);
        pausePanel.SetActive(false);
        BuildControlsScreen(root, gold, cream);
        endingPanel = Fill(root, "Ending", ink);
        if (theme != null && theme.endingArt != null)
        {
            Image art = new GameObject("Picture", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter)).GetComponent<Image>();
            art.transform.SetParent(endingPanel.transform, false);
            RectTransform artRect = art.rectTransform;
            artRect.anchorMin = new Vector2(0f, 1f);
            artRect.anchorMax = new Vector2(1f, 1f);
            artRect.pivot = new Vector2(0.5f, 1f);
            artRect.anchoredPosition = Vector2.zero;
            artRect.sizeDelta = new Vector2(0f, 640f);
            art.sprite = theme.endingArt;
            art.raycastTarget = false;
            var fit = art.GetComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            fit.aspectRatio = theme.endingArt.rect.width / theme.endingArt.rect.height;
            if (theme.fade != null)
            {
                Image fade = UiKit.MakeImage(art.transform, "Fade", theme.fade, Vector2.zero, Vector2.zero, Vector2.zero, false);
                fade.rectTransform.anchorMin = Vector2.zero;
                fade.rectTransform.anchorMax = new Vector2(1f, 0f);
                fade.rectTransform.pivot = new Vector2(0.5f, 0f);
                fade.rectTransform.sizeDelta = new Vector2(0f, 200f);
            }
        }
        Text endTitle = MakeText(endingPanel.transform, "Title", 66, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, 410f), new Vector2(1400f, 96f), true);
        endTitle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        endTitle.text = "The Silence Breaks";
        endTitle.color = gold;
        UiKit.MakeDivider(endingPanel.transform, new Vector2(0.5f, 0f), new Vector2(0f, 352f), 520f);
        Text story = MakeText(endingPanel.transform, "Story", 28, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, 255f), new Vector2(1500f, 150f), false);
        story.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        story.color = cream;
        story.lineSpacing = 1.15f;
        story.text =
            "Archcantor Vale's endless chord finally finds its last note, and every sound the Silence swallowed pours back\n" +
            "into the world. Maestro, Harmony, Forte and Lyra bring the music home.\n" +
            "<size=24><color=#" + ColorUtility.ToHtmlStringRGB(muted) + ">Thank you for playing Project Silence.</color></size>";
        endingMenuButton = MakeButton(endingPanel.transform, "Main menu", new Vector2(-220f, 0f), GoToMenu, 380f);
        Button endingQuit = MakeButton(endingPanel.transform, "Quit", new Vector2(220f, 0f), Application.Quit, 380f);
        foreach (Button b in new[] { endingMenuButton, endingQuit })
        {
            var r = (RectTransform)b.transform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0f);
            r.anchoredPosition = new Vector2(r.anchoredPosition.x, 112f);
        }
        endingPanel.SetActive(false);
    }
    Text MakeText(Transform parent, string name, int size, TextAnchor alignment, Vector2 anchor, Vector2 position, Vector2 box, bool heading)
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
        UiTheme theme = UiTheme.Get();
        text.font = theme != null ? (heading ? theme.Heading : theme.Body) : font;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        go.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.6f);
        return text;
    }
    Text FillText(RectTransform box, string name, int size, TextAnchor alignment, Color color, float padX, float padY)
    {
        Text text = MakeText(box, name, size, alignment, Vector2.zero, Vector2.zero, Vector2.zero, false);
        RectTransform rect = text.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(padX, padY);
        rect.offsetMax = new Vector2(-padX, -padY);
        text.color = color;
        return text;
    }
    RectTransform Box(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, bool soft)
    {
        UiTheme theme = UiTheme.Get();
        Image image = UiKit.MakeImage(parent, name, null, anchor, position, size, true);
        image.rectTransform.pivot = anchor;
        image.rectTransform.anchoredPosition = position;
        if (theme != null && (soft ? theme.panelSoft : theme.panel) != null) UiKit.StyleWindow(image, soft);
        else image.color = new Color(0.04f, 0.04f, 0.1f, soft ? 0.6f : 0.9f);
        return image.rectTransform;
    }
    GameObject Fill(Transform parent, string name, Color color)
    {
        Image image = UiKit.MakeImage(parent, name, null, Vector2.zero, Vector2.zero, Vector2.zero, false);
        Stretch(image.rectTransform);
        image.color = color;
        image.raycastTarget = color.a > 0f;
        return image.gameObject;
    }
    Button MakeButton(Transform parent, string label, Vector2 position, UnityAction onClick, float width = 420f)
    {
        Image image = UiKit.MakeImage(parent, label + " Button", null, new Vector2(0.5f, 0.5f), position, new Vector2(width, 84f), true);
        image.raycastTarget = true;
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        UiTheme theme = UiTheme.Get();
        if (theme != null && theme.button != null) UiKit.StyleButton(button);
        else
        {
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.15f, 0.15f, 0.28f);
            colors.highlightedColor = colors.selectedColor = new Color(0.75f, 0.58f, 0.2f);
            colors.pressedColor = new Color(0.55f, 0.42f, 0.14f);
            button.colors = colors;
        }
        button.onClick.AddListener(onClick);
        Text text = MakeText(image.transform, "Label", 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width - 30f, 70f), true);
        text.color = theme != null ? theme.textColor : Color.white;
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
