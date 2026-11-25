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
    public Button magicButton;   // Added with AI help. Leave empty and a "Magic" button is made from the Heal button.
    public GameObject timingPanel;
    public RectTransform ring;
    public Text judgmentText;
    public GameObject resultPanel;
    public Text resultText;

    // Added with AI help: if no Magic button is set in the Inspector, copy the Heal button
    // (same look and size). The Action Panel's layout group places it next to the others.
    void Awake()
    {
        if (magicButton == null && healButton != null)
        {
            magicButton = Instantiate(healButton, healButton.transform.parent);
            magicButton.name = "Magic Button";
            magicButton.onClick = new Button.ButtonClickedEvent();   // forget the Heal click that was copied too
            Text label = magicButton.GetComponentInChildren<Text>();
            if (label != null) label.text = "Magic";
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

    public void ShowActions(bool show, bool canHeal, bool canMagic = false)   // AI help: added canMagic
    {
        actionPanel.SetActive(show);
        healButton.interactable = canHeal;
        if (magicButton != null) magicButton.interactable = canMagic;
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
