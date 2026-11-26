using TMPro;
using UnityEngine;
[CreateAssetMenu(fileName = "UiTheme", menuName = "Silence/UI Theme")]
public class UiTheme : ScriptableObject
{
    [Header("Screens")]
    [Tooltip("The picture behind the main menu (it is scaled to cover the screen).")]
    public Sprite mainMenuArt;
    [Tooltip("The picture on the ending screen (shown across the full width).")]
    public Sprite endingArt;
    [Header("Buttons (9-slice sprites)")]
    public Sprite button;
    [Tooltip("Shown while the button is selected (controller/keyboard) or under the mouse.")]
    public Sprite buttonSelected;
    public Sprite buttonPressed;
    public Sprite buttonDisabled;
    [Header("Windows")]
    [Tooltip("A framed window: menus, dialogue, battle panels.")]
    public Sprite panel;
    [Tooltip("A see-through box for things shown during play (goal, party, hints).")]
    public Sprite panelSoft;
    [Tooltip("The gold line with a diamond, under titles.")]
    public Sprite divider;
    [Tooltip("A fade from clear to dark, to soften the edge of a picture.")]
    public Sprite fade;
    [Header("Sliders")]
    public Sprite sliderTrack;
    public Sprite sliderFill;
    public Sprite sliderHandle;
    [Header("Fonts")]
    [Tooltip("Titles and button labels (Cinzel Decorative).")]
    public Font headingFont;
    [Tooltip("The same font for TextMesh Pro texts (the main menu uses those).")]
    public TMP_FontAsset headingFontTmp;
    [Tooltip("Everything else. Empty = Unity's built-in font.")]
    public Font bodyFont;
    [Header("Colours")]
    public Color textColor = new Color32(0xF4, 0xEA, 0xD0, 0xFF);
    public Color headingColor = new Color32(0xF1, 0xD2, 0x7E, 0xFF);
    public Color mutedColor = new Color32(0xB8, 0xB0, 0xD8, 0xFF);
    [Tooltip("The dark background colour of full-screen menus.")]
    public Color inkColor = new Color32(0x0B, 0x08, 0x18, 0xFF);
    static UiTheme loaded;
    public static UiTheme Get()
    {
        if (loaded == null) loaded = Resources.Load<UiTheme>("UiTheme");
        return loaded;
    }
    public Font Heading => headingFont != null ? headingFont : Body;
    public Font Body => bodyFont != null ? bodyFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
}
