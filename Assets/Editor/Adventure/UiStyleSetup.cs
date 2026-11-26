using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
public static class UiStyleSetup
{
    const string Kit = "Assets/_Game/Art/UI/Kit/";
    const string Screens = "Assets/_Game/Art/UI/Screens/";
    const string ThemePath = "Assets/_Game/Art/UI/Resources/UiTheme.asset";
    const string MenuScene = "Assets/Scenes/_Menu.unity";
    const string BattleScene = "Assets/_Game/Scenes/Battle.unity";
    [MenuItem("Silence/Style Menus")]
    public static void StyleAll()
    {
        UiTheme theme = EnsureTheme();
        StyleMainMenu(theme);
        StyleBattle(theme);
        AssetDatabase.SaveAssets();
        Debug.Log("UI styled: main menu and battle");
    }
    [MenuItem("Silence/UI Theme")]
    public static void OpenTheme()
    {
        UiTheme theme = EnsureTheme();
        Selection.activeObject = theme;
        EditorGUIUtility.PingObject(theme);
    }
    static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
    {
        { "button_normal", new Vector4(28, 28, 28, 28) }, { "button_selected", new Vector4(28, 28, 28, 28) },
        { "button_pressed", new Vector4(28, 28, 28, 28) }, { "button_disabled", new Vector4(28, 28, 28, 28) },
        { "panel", new Vector4(32, 32, 32, 32) }, { "panel_soft", new Vector4(30, 30, 30, 30) },
        { "slider_track", new Vector4(12, 11, 12, 11) }, { "slider_fill", new Vector4(12, 11, 12, 11) },
    };
    static Sprite ImportSprite(string path, bool picture)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer == null) { Debug.LogWarning("UI: missing " + path); return null; }
        string name = System.IO.Path.GetFileNameWithoutExtension(path);
        Vector4 border = Borders.TryGetValue(name, out Vector4 b) ? b : Vector4.zero;
        bool changed = importer.textureType != TextureImporterType.Sprite || importer.spriteBorder != border
                       || importer.mipmapEnabled || importer.spriteImportMode != SpriteImportMode.Single;
        if (changed)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = border;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = picture ? 2048 : 1024;
            importer.textureCompression = picture ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    public static UiTheme EnsureTheme()
    {
        var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(ThemePath);
        if (theme == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Art/UI/Resources")) AssetDatabase.CreateFolder("Assets/_Game/Art/UI", "Resources");
            theme = ScriptableObject.CreateInstance<UiTheme>();
            AssetDatabase.CreateAsset(theme, ThemePath);
        }
        theme.mainMenuArt = theme.mainMenuArt ?? ImportSprite(Screens + "MainMenu_KeyArt.png", true);
        theme.endingArt = theme.endingArt ?? ImportSprite(Screens + "Ending_Celebration.png", true);
        theme.button = theme.button ?? ImportSprite(Kit + "button_normal.png", false);
        theme.buttonSelected = theme.buttonSelected ?? ImportSprite(Kit + "button_selected.png", false);
        theme.buttonPressed = theme.buttonPressed ?? ImportSprite(Kit + "button_pressed.png", false);
        theme.buttonDisabled = theme.buttonDisabled ?? ImportSprite(Kit + "button_disabled.png", false);
        theme.panel = theme.panel ?? ImportSprite(Kit + "panel.png", false);
        theme.panelSoft = theme.panelSoft ?? ImportSprite(Kit + "panel_soft.png", false);
        theme.divider = theme.divider ?? ImportSprite(Kit + "divider.png", false);
        theme.fade = theme.fade ?? ImportSprite(Kit + "fade.png", false);
        theme.sliderTrack = theme.sliderTrack ?? ImportSprite(Kit + "slider_track.png", false);
        theme.sliderFill = theme.sliderFill ?? ImportSprite(Kit + "slider_fill.png", false);
        theme.sliderHandle = theme.sliderHandle ?? ImportSprite(Kit + "slider_handle.png", false);
        theme.headingFont = theme.headingFont ?? AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/CinzelDecorative/CinzelDecorative-Regular.ttf");
        theme.headingFontTmp = theme.headingFontTmp ?? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/CinzelDecorative/CinzelMenu.asset");
        EditorUtility.SetDirty(theme);
        AssetDatabase.SaveAssets();
        return theme;
    }
    static void StyleMainMenu(UiTheme t)
    {
        var scene = EditorSceneManager.OpenScene(MenuScene);
        Canvas canvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include).First(c => c.isRootCanvas);
        Transform root = canvas.transform;
        Image background = root.Find("Background").GetComponent<Image>();
        background.sprite = t.mainMenuArt;
        background.color = Color.white;
        background.type = Image.Type.Simple;
        var fitter = background.GetComponent<AspectRatioFitter>() ?? background.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = t.mainMenuArt.rect.width / t.mainMenuArt.rect.height;
        Transform main = root.Find("MainPanel");
        main.Find("GameTitle").gameObject.SetActive(false);
        main.Find("GoldDivider").gameObject.SetActive(false);
        var mainRect = (RectTransform)main;
        mainRect.anchorMin = mainRect.anchorMax = new Vector2(1f, 0.5f);
        mainRect.pivot = new Vector2(1f, 0.5f);
        mainRect.anchoredPosition = new Vector2(-100f, -30f);
        mainRect.sizeDelta = new Vector2(420f, mainRect.sizeDelta.y);
        var column = main.GetComponent<VerticalLayoutGroup>();
        column.spacing = 18f;
        column.childAlignment = TextAnchor.UpperCenter;
        column.childControlWidth = true;
        column.childControlHeight = true;
        column.childForceExpandWidth = false;
        column.childForceExpandHeight = false;
        Button settingsButton = main.Find("SettingsButton").GetComponent<Button>();
        StyleMenuButton(main.Find("StartButton").GetComponent<Button>(), 420f, t);
        StyleMenuButton(settingsButton, 420f, t);
        Transform quit = main.Find("QuitButton");
        if (quit == null)
        {
            GameObject copy = Object.Instantiate(settingsButton.gameObject, main);
            copy.name = "QuitButton";
            copy.transform.SetSiblingIndex(settingsButton.transform.GetSiblingIndex() + 1);
            Button quitButton = copy.GetComponent<Button>();
            for (int i = quitButton.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                if (quitButton.onClick.GetPersistentMethodName(i) != "PlayOneShot") UnityEventTools.RemovePersistentListener(quitButton.onClick, i);
            copy.AddComponent<QuitGameButton>();
            copy.GetComponentInChildren<TMP_Text>().text = "QUIT";
            quit = copy.transform;
        }
        StyleMenuButton(quit.GetComponent<Button>(), 420f, t);
        Transform settings = root.Find("SettingsPanel");
        UiKit.StyleWindow(settings.GetComponent<Image>());
        RemoveOutline(settings.gameObject);
        ((RectTransform)settings).sizeDelta = new Vector2(820f, 700f);
        var settingsColumn = settings.GetComponent<VerticalLayoutGroup>();
        settingsColumn.padding = new RectOffset(70, 70, 54, 54);
        settingsColumn.spacing = 16f;
        settingsColumn.childAlignment = TextAnchor.UpperCenter;
        Transform title = settings.Find("SettingsTitle");
        UiKit.StyleText(title.GetComponent<TMP_Text>(), 50, t.headingColor, 6f);
        if (settings.Find("Divider") == null)
        {
            Image divider = UiKit.MakeDivider(settings, new Vector2(0.5f, 0.5f), Vector2.zero, 520f);
            divider.gameObject.AddComponent<LayoutElement>().preferredHeight = 32f;
            divider.GetComponent<LayoutElement>().preferredWidth = 520f;
            divider.transform.SetSiblingIndex(title.GetSiblingIndex() + 1);
        }
        foreach (string group in new[] { "MENU MUSIC", "BUTTON SOUNDS" })
        {
            Transform g = settings.Find(group + " Group");
            UiKit.StyleText(g.Find(group + " Label").GetComponent<TMP_Text>(), 26, t.textColor, 3f);
            UiKit.StyleSlider(g.Find(group + " Slider").GetComponent<Slider>());
        }
        StyleMenuButton(settings.Find("BackButton").GetComponent<Button>(), 360f, t);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
    static void StyleMenuButton(Button button, float width, UiTheme t)
    {
        UiKit.StyleButton(button);
        RemoveOutline(button.gameObject);
        var layout = button.GetComponent<LayoutElement>() ?? button.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = 84f;
        layout.minHeight = 84f;
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        UiKit.StyleText(label, 30, t.textColor, 4f);
        label.alignment = TextAlignmentOptions.Center;
    }
    static void RemoveOutline(GameObject go)
    {
        foreach (Outline outline in go.GetComponents<Outline>()) Object.DestroyImmediate(outline);
    }
    static void StyleBattle(UiTheme t)
    {
        var scene = EditorSceneManager.OpenScene(BattleScene);
        BattleHUD hud = Object.FindAnyObjectByType<BattleHUD>(FindObjectsInactive.Include);
        Transform root = hud.transform;
        Transform banner = root.Find("Top Banner");
        if (banner == null)
        {
            banner = UiKit.MakeImage(root, "Top Banner", t.panelSoft, new Vector2(0.5f, 1f), Vector2.zero, Vector2.one, true).transform;
            banner.SetSiblingIndex(0);
        }
        Place(banner, new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(1500f, 128f));
        UiKit.StyleWindow(banner.GetComponent<Image>(), true);
        Place(hud.turnOrderText.transform, new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(1420f, 40f));
        UiKit.StyleText(hud.turnOrderText, false, 24, t.mutedColor);
        Place(hud.messageText.transform, new Vector2(0.5f, 1f), new Vector2(0f, -98f), new Vector2(1420f, 48f));
        UiKit.StyleText(hud.messageText, false, 30, t.headingColor);
        var partyPanel = (RectTransform)hud.partyText.transform.parent;
        UiKit.StyleWindow(partyPanel.GetComponent<Image>());
        partyPanel.sizeDelta = new Vector2(720f, 200f);
        partyPanel.anchoredPosition = new Vector2(400f, 132f);
        UiKit.StyleText(hud.partyText, false, 26, t.textColor);
        var enemyPanel = (RectTransform)hud.enemyText.transform.parent;
        UiKit.StyleWindow(enemyPanel.GetComponent<Image>());
        enemyPanel.anchoredPosition = new Vector2(-300f, -230f);
        UiKit.StyleText(hud.enemyText, false, 26, t.textColor);
        var actions = (RectTransform)hud.actionPanel.transform;
        UiKit.StyleWindow(hud.actionPanel.GetComponent<Image>());
        actions.sizeDelta = new Vector2(860f, 128f);
        actions.anchoredPosition = new Vector2(-470f, 104f);
        var row = hud.actionPanel.GetComponent<HorizontalLayoutGroup>();
        row.padding = new RectOffset(20, 20, 20, 20);
        row.spacing = 12f;
        row.childAlignment = TextAnchor.MiddleCenter;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = true;
        row.childForceExpandHeight = true;
        foreach (Button button in hud.actionPanel.GetComponentsInChildren<Button>(true))
        {
            UiKit.StyleButton(button);
            UiKit.StyleText(button.GetComponentInChildren<Text>(true), true, 28, t.textColor);
        }
        UiKit.StyleText(hud.judgmentText, true, 56, t.headingColor);
        Image dim = hud.resultPanel.GetComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.55f);
        Transform window = hud.resultPanel.transform.Find("Result Window");
        if (window == null)
        {
            window = UiKit.MakeImage(hud.resultPanel.transform, "Result Window", t.panel, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one, true).transform;
            window.SetSiblingIndex(0);
        }
        Place(window, new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(820f, 400f));
        UiKit.StyleWindow(window.GetComponent<Image>());
        Place(hud.resultText.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 72f), new Vector2(760f, 130f));
        UiKit.StyleText(hud.resultText, true, 92, t.headingColor);
        if (hud.resultPanel.transform.Find("Divider") == null)
        {
            Image divider = UiKit.MakeDivider(hud.resultPanel.transform, new Vector2(0.5f, 0.5f), Vector2.zero, 520f);
            divider.transform.SetSiblingIndex(window.GetSiblingIndex() + 1);
        }
        Place(hud.resultPanel.transform.Find("Divider"), new Vector2(0.5f, 0.5f), new Vector2(0f, -4f), new Vector2(520f, 32f));
        Transform continueButton = hud.resultPanel.transform.Find("Continue Button");
        UiKit.StyleButton(continueButton.GetComponent<Button>());
        Place(continueButton, new Vector2(0.5f, 0.5f), new Vector2(0f, -104f), new Vector2(360f, 84f));
        UiKit.StyleText(continueButton.GetComponentInChildren<Text>(true), true, 30, t.textColor);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
    static void Place(Transform transform, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var rect = (RectTransform)transform;
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
