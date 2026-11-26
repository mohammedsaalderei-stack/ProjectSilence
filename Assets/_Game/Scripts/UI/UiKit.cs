using TMPro;
using UnityEngine;
using UnityEngine.UI;
public static class UiKit
{
    static UiTheme Theme => UiTheme.Get();
    public static void StyleButton(Button button)
    {
        UiTheme t = Theme;
        if (button == null || t == null || t.button == null) return;
        Image image = button.targetGraphic as Image ?? button.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = t.button;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            button.targetGraphic = image;
        }
        button.transition = Selectable.Transition.SpriteSwap;
        button.spriteState = new SpriteState
        {
            highlightedSprite = t.buttonSelected,
            selectedSprite = t.buttonSelected,
            pressedSprite = t.buttonPressed,
            disabledSprite = t.buttonDisabled,
        };
    }
    public static void StyleWindow(Image image, bool soft = false)
    {
        UiTheme t = Theme;
        if (image == null || t == null) return;
        Sprite sprite = soft ? t.panelSoft : t.panel;
        if (sprite == null) return;
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = Color.white;
    }
    public static void StyleText(Text text, bool heading, int size, Color color)
    {
        UiTheme t = Theme;
        if (text == null) return;
        if (t != null) text.font = heading ? t.Heading : t.Body;
        text.fontSize = size;
        text.color = color;
    }
    public static void StyleText(TMP_Text text, int size, Color color, float spacing = 0f)
    {
        UiTheme t = Theme;
        if (text == null) return;
        if (t != null && t.headingFontTmp != null) text.font = t.headingFontTmp;
        text.fontSize = size;
        text.color = color;
        text.characterSpacing = spacing;
    }
    public static void StyleSlider(Slider slider)
    {
        UiTheme t = Theme;
        if (slider == null || t == null) return;
        Transform track = slider.transform.Find("Track") ?? slider.transform.Find("Background");
        if (track != null && t.sliderTrack != null) SetSliced(track.GetComponent<Image>(), t.sliderTrack, 18f);
        if (slider.fillRect != null && t.sliderFill != null)
        {
            SetSliced(slider.fillRect.GetComponent<Image>(), t.sliderFill, 0f);
            if (slider.fillRect.parent is RectTransform area) area.sizeDelta = new Vector2(area.sizeDelta.x, 18f);
        }
        if (slider.handleRect != null && t.sliderHandle != null)
        {
            Image handle = slider.handleRect.GetComponent<Image>();
            handle.sprite = t.sliderHandle;
            handle.type = Image.Type.Simple;
            handle.color = Color.white;
            handle.preserveAspect = true;
            slider.handleRect.sizeDelta = new Vector2(44f, 0f);
            if (slider.handleRect.parent is RectTransform area) area.sizeDelta = new Vector2(area.sizeDelta.x, 44f);
            slider.targetGraphic = handle;
        }
        ColorBlock colors = slider.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.92f, 0.7f);
        colors.selectedColor = new Color(1f, 0.92f, 0.7f);
        colors.pressedColor = new Color(0.9f, 0.78f, 0.5f);
        slider.colors = colors;
        slider.transition = Selectable.Transition.ColorTint;
    }
    static void SetSliced(Image image, Sprite sprite, float height)
    {
        if (image == null) return;
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        if (height > 0f) image.rectTransform.sizeDelta = new Vector2(image.rectTransform.sizeDelta.x, height);
    }
    public static Image MakeImage(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 position, Vector2 size, bool sliced)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        image.raycastTarget = false;
        return image;
    }
    public static Image MakeDivider(Transform parent, Vector2 anchor, Vector2 position, float width)
    {
        UiTheme t = Theme;
        Image divider = MakeImage(parent, "Divider", t != null ? t.divider : null, anchor, position, new Vector2(width, 32f), false);
        if (divider.sprite == null) divider.color = new Color(0.85f, 0.7f, 0.37f, 0.8f);
        return divider;
    }
}
