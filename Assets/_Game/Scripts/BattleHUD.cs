using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
public class BattleHUD : MonoBehaviour
{
    public Text turnOrderText;
    public Text messageText;
    public Text partyText;
    public Text enemyText;
    public GameObject actionPanel;
    public Button healButton;
    public Button magicButton;
    public GameObject timingPanel;
    public RectTransform ring;
    public Text judgmentText;
    public GameObject resultPanel;
    public Text resultText;
    void Awake()
    {
        if (magicButton == null && healButton != null)
        {
            magicButton = Instantiate(healButton, healButton.transform.parent);
            magicButton.name = "Magic Button";
            magicButton.onClick = new Button.ButtonClickedEvent();
            Text label = magicButton.GetComponentInChildren<Text>();
            if (label != null) label.text = "Magic";
        }
        if (partyText != null)
        {
            partyText.resizeTextForBestFit = true;
            partyText.resizeTextMinSize = 10;
            partyText.resizeTextMaxSize = Mathf.Max(11, partyText.fontSize);
            RectTransform panel = partyText.transform.parent as RectTransform;
            if (panel != null && panel.sizeDelta.y < 200f) panel.sizeDelta = new Vector2(panel.sizeDelta.x, 200f);
        }
    }
    public void SetTurnOrder(IEnumerable<Unit> units)
    {
        turnOrderText.text = string.Join("  >  ", units.Select(u => u.unitName));
    }
    public void SetMessage(string message)
    {
        messageText.text = message;
    }
    public void UpdateStatus(List<Unit> party, List<Unit> enemies)
    {
        partyText.text = string.Join("\n", party.Select(u => u.unitName + "   HP " + u.currentHP + "/" + u.maxHP + "   MP " + u.currentMP + "/" + u.maxMP));
        enemyText.text = string.Join("\n", enemies.Where(u => !u.IsDead).Select(u => u.unitName + "   HP " + u.currentHP + "/" + u.maxHP));
    }
    public void ShowActions(bool show, bool canHeal, bool canMagic = false)
    {
        actionPanel.SetActive(show);
        healButton.interactable = canHeal;
        if (magicButton != null) magicButton.interactable = canMagic;
        FadeLabel(healButton);
        FadeLabel(magicButton);
    }
    static void FadeLabel(Button button)
    {
        if (button == null) return;
        Text label = button.GetComponentInChildren<Text>(true);
        if (label == null) return;
        Color color = label.color;
        color.a = button.interactable ? 1f : 0.45f;
        label.color = color;
    }
    public void ShowTiming(bool show)
    {
        timingPanel.SetActive(show);
    }
    public void SetRingScale(float scale)
    {
        ring.localScale = Vector3.one * Mathf.Max(0f, scale);
    }
    public void SetJudgment(string text)
    {
        judgmentText.text = text;
    }
    public void ShowResult(bool show, string text)
    {
        resultPanel.SetActive(show);
        resultText.text = text;
    }
}
