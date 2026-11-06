using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleManager : MonoBehaviour
{
    public List<Unit> party;
    public GameObject[] enemyGroups;
    public BattleHUD hud;
    public BattleMusic music;
    public int healCost = 8;
    public int healAmount = 40;
    public string returnScene;

    List<Unit> enemies;
    string chosenAction;
    Unit chosenTarget;
    bool choosingTarget;
    float timingMultiplier;

    void Start()
    {
        foreach (GameObject group in enemyGroups) group.SetActive(false);
        GameObject chosenGroup = enemyGroups[Random.Range(0, enemyGroups.Length)];
        chosenGroup.SetActive(true);
        enemies = chosenGroup.GetComponentsInChildren<Unit>().ToList();

        hud.ShowActions(false, false);
        hud.ShowTiming(false);
        hud.ShowResult(false, "");
        StartCoroutine(BattleLoop());
    }

    void Update()
    {
        if (!choosingTarget || !BattleInput.ClickPressed) return;

        Ray ray = Camera.main.ScreenPointToRay(BattleInput.PointerPosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Unit unit = hit.collider.GetComponentInParent<Unit>();
            if (unit != null && !unit.isPlayer && !unit.IsDead) chosenTarget = unit;
        }
    }

    IEnumerator BattleLoop()
    {
        yield return new WaitForSeconds(0.5f);

        while (true)
        {
            List<Unit> turnOrder = party.Concat(enemies).Where(u => !u.IsDead).OrderByDescending(u => u.speed).ToList();

            for (int i = 0; i < turnOrder.Count; i++)
            {
                Unit unit = turnOrder[i];
                if (unit.IsDead) continue;

                unit.isGuarding = false;
                hud.SetTurnOrder(turnOrder.Skip(i).Where(u => !u.IsDead));
                hud.UpdateStatus(party, enemies);

                if (unit.isPlayer) yield return PlayerTurn(unit);
                else yield return EnemyTurn(unit);

                hud.UpdateStatus(party, enemies);
                if (enemies.All(e => e.IsDead))
                {
                    hud.ShowResult(true, "Victory!");
                    yield break;
                }
                if (party.All(p => p.IsDead))
                {
                    hud.ShowResult(true, "Defeat...");
                    yield break;
                }
                yield return new WaitForSeconds(0.6f);
            }
        }
    }

    IEnumerator PlayerTurn(Unit unit)
    {
        hud.SetMessage(unit.unitName + "'s turn. Choose an action.");
        chosenAction = null;
        hud.ShowActions(true, unit.currentMP >= healCost);
        while (chosenAction == null) yield return null;
        hud.ShowActions(false, false);

        if (chosenAction == "Attack")
        {
            hud.SetMessage("Click an enemy to attack.");
            chosenTarget = null;
            choosingTarget = true;
            while (chosenTarget == null) yield return null;
            choosingTarget = false;

            hud.SetMessage("Press SPACE on the beat!");
            yield return TimingPrompt();

            unit.PlayAttack();
            int damage = chosenTarget.TakeDamage(Mathf.RoundToInt(CalculateDamage(unit, chosenTarget) * timingMultiplier));
            hud.SetMessage(unit.unitName + " hits " + chosenTarget.unitName + " for " + damage + "!");
        }
        else if (chosenAction == "Guard")
        {
            unit.isGuarding = true;
            hud.SetMessage(unit.unitName + " is guarding.");
        }
        else if (chosenAction == "Heal")
        {
            Unit target = party.Where(p => !p.IsDead).OrderBy(p => (float)p.currentHP / p.maxHP).First();
            unit.currentMP -= healCost;
            unit.PlayAttack();
            int healed = target.Heal(healAmount);
            hud.SetMessage(unit.unitName + " heals " + target.unitName + " for " + healed + " HP.");
        }

        yield return new WaitForSeconds(0.8f);
    }

    IEnumerator EnemyTurn(Unit enemy)
    {
        List<Unit> targets = party.Where(p => !p.IsDead).ToList();
        Unit target = targets[Random.Range(0, targets.Count)];

        hud.SetMessage(enemy.unitName + " attacks " + target.unitName + "!");
        yield return new WaitForSeconds(0.6f);

        enemy.PlayAttack();
        int damage = target.TakeDamage(CalculateDamage(enemy, target));
        hud.SetMessage(enemy.unitName + " hits " + target.unitName + " for " + damage + "!");
        yield return new WaitForSeconds(0.8f);
    }

    IEnumerator TimingPrompt()
    {
        float beatLength = music.BeatLength;
        double beatTime = (System.Math.Floor(music.SongTime / beatLength) + 2) * beatLength;
        string result = "MISS";
        timingMultiplier = 0.5f;

        hud.SetJudgment("");
        hud.ShowTiming(true);

        while (true)
        {
            double timeLeft = beatTime - music.SongTime;
            hud.SetRingScale(1f + (float)(timeLeft / beatLength));

            if (BattleInput.BeatPressed)
            {
                double error = System.Math.Abs(timeLeft);
                if (error <= 0.08)
                {
                    result = "PERFECT!";
                    timingMultiplier = 1.25f;
                }
                else if (error <= 0.16)
                {
                    result = "GOOD";
                    timingMultiplier = 1f;
                }
                break;
            }

            if (timeLeft < -0.16) break;
            yield return null;
        }

        hud.SetJudgment(result);
        yield return new WaitForSeconds(0.5f);
        hud.ShowTiming(false);
    }

    int CalculateDamage(Unit attacker, Unit target)
    {
        return Mathf.Max(1, attacker.attack * 2 - target.defense);
    }

    public void OnAttackButton()
    {
        chosenAction = "Attack";
    }

    public void OnGuardButton()
    {
        chosenAction = "Guard";
    }

    public void OnHealButton()
    {
        chosenAction = "Heal";
    }

    public void OnContinueButton()
    {
        if (!string.IsNullOrEmpty(returnScene)) SceneManager.LoadScene(returnScene);
        else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
