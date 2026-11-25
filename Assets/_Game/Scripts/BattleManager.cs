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
    public int magicCost = 6;   // Added with AI help: MP needed for one spell
    public string returnScene;

    List<Unit> enemies;
    string chosenAction;
    Unit chosenTarget;
    bool choosingTarget;
    float timingMultiplier;

    void Start()
    {
        foreach (GameObject group in enemyGroups) group.SetActive(false);
        // Changed with AI help: the overworld mob that caught you decides which enemy group appears.
        int groupIndex = GameFlow.InBattleFromMap && GameFlow.PendingEnemyGroup >= 0 && GameFlow.PendingEnemyGroup < enemyGroups.Length
            ? GameFlow.PendingEnemyGroup
            : Random.Range(0, enemyGroups.Length);
        GameObject chosenGroup = enemyGroups[groupIndex];
        chosenGroup.SetActive(true);
        enemies = chosenGroup.GetComponentsInChildren<Unit>().ToList();
        GameFlow.ApplyToBattle(party, enemies);   // AI help: HP/MP carry over between battles; the boss gets stronger

        hud.ShowActions(false, false);
        hud.ShowTiming(false);
        hud.ShowResult(false, "");
        if (hud.magicButton != null) hud.magicButton.onClick.AddListener(OnMagicButton);   // AI help: wire the Magic button
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
        // Added with AI help: announce who you are fighting.
        hud.SetMessage(GameFlow.InBattleFromMap && GameFlow.PendingIsBoss
            ? "The Hollow Colossus rises. Every sound around you dies..."
            : GameFlow.InBattleFromMap ? "A " + GameFlow.PendingMobName + " attacks!" : "Enemies appear!");
        yield return new WaitForSeconds(1.3f);

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
                    GameFlow.SaveParty(party, true);    // AI help: remember HP/MP for the map
                    GameAudio.Play(ProjectSilence.Synth.Victory);
                    activeHero = null;
                    hud.ShowResult(true, "Victory!");
                    yield break;
                }
                if (party.All(p => p.IsDead))
                {
                    GameFlow.SaveParty(party, false);   // AI help
                    GameAudio.Play(ProjectSilence.Synth.Defeat);
                    activeHero = null;
                    hud.ShowResult(true, "Defeat...");
                    yield break;
                }
                yield return new WaitForSeconds(0.6f);
            }
        }
    }

    IEnumerator PlayerTurn(Unit unit)
    {
        // Changed with AI help: choose an action, then (for Attack and Magic) a target.
        // B / Esc / right-click while choosing a target goes back to the actions.
        activeHero = unit;                                     // a blue crystal floats over whoever's turn it is
        while (true)
        {
            hud.SetMessage(unit.unitName + "'s turn. Choose an action.");
            chosenAction = null;
            hud.ShowActions(true, unit.currentMP >= healCost, unit.currentMP >= magicCost);   // AI help: Magic needs MP too
            while (chosenAction == null) yield return null;
            hud.ShowActions(false, false);
            GameAudio.Play(ProjectSilence.Synth.UiConfirm, 0.6f);

            if (chosenAction != "Attack" && chosenAction != "Magic") break;   // Guard and Heal need no target
            yield return ChooseTarget(chosenAction == "Magic" ? "cast at" : "attack");
            if (chosenTarget != null) break;                                   // no target = cancelled: choose again
        }
        activeHero = null;

        if (chosenAction == "Attack")
        {
            // Added with AI help: a physical attack runs up to the enemy first (see UnitMotion.cs).
            hud.SetMessage(unit.unitName + " charges at " + chosenTarget.unitName + "!");
            yield return unit.Motion.RunUpTo(chosenTarget.Motion);

            hud.SetMessage(BeatMessage("!"));
            yield return TimingPrompt();

            unit.PlayAttack();
            yield return unit.Motion.LungeForward(chosenTarget.Motion);   // AI help: the hit lands at the end of the lunge
            int damage = chosenTarget.TakeDamage(Mathf.RoundToInt(CalculateDamage(unit, chosenTarget) * timingMultiplier));
            hud.SetMessage(unit.unitName + " hits " + chosenTarget.unitName + " for " + damage + "!");
            yield return unit.Motion.LungeBack();                          // AI help
            yield return new WaitForSeconds(0.35f);                        // AI help: let the hit sink in
            yield return unit.Motion.ReturnHome();                         // AI help: hop back to the start spot
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
        else if (chosenAction == "Magic")
        {
            // Added with AI help: a spell is cast from where the hero stands (no run-up), costs MP,
            // and ignores half of the enemy's defense (see CalculateMagicDamage).
            hud.SetMessage(BeatMessage(" to cast!"));
            yield return TimingPrompt();

            unit.currentMP -= magicCost;
            unit.PlayCast();                                       // the cast_a_spell animation
            yield return unit.Motion.CastRise();                   // float up a little
            yield return SpellEffect.Fly(unit, chosenTarget);      // a glowing orb flies to the enemy
            unit.Motion.CastSettle();                              // float back down
            int damage = chosenTarget.TakeDamage(Mathf.RoundToInt(CalculateMagicDamage(unit, chosenTarget) * timingMultiplier));
            hud.SetMessage(unit.unitName + "'s spell hits " + chosenTarget.unitName + " for " + damage + "!");
        }

        yield return new WaitForSeconds(0.8f);
    }

    IEnumerator EnemyTurn(Unit enemy)
    {
        List<Unit> targets = party.Where(p => !p.IsDead).ToList();
        Unit target = targets[Random.Range(0, targets.Count)];

        hud.SetMessage(enemy.unitName + " attacks " + target.unitName + "!");
        yield return new WaitForSeconds(0.3f);                             // AI help: was 0.6 (the run-up adds time)
        yield return enemy.Motion.RunUpTo(target.Motion);                  // AI help: enemies run up to the hero too

        enemy.PlayAttack();
        yield return enemy.Motion.LungeForward(target.Motion);             // AI help
        int damage = target.TakeDamage(CalculateDamage(enemy, target));
        hud.SetMessage(enemy.unitName + " hits " + target.unitName + " for " + damage + "!");
        yield return enemy.Motion.LungeBack();                             // AI help
        yield return new WaitForSeconds(0.35f);                            // AI help
        yield return enemy.Motion.ReturnHome();                            // AI help
        yield return new WaitForSeconds(0.3f);                             // AI help: was 0.8
    }

    IEnumerator TimingPrompt()
    {
        float beatLength = music.BeatLength;
        double beatTime = (System.Math.Floor(music.SongTime / beatLength) + 2) * beatLength;
        string result = "MISS";
        timingMultiplier = 0.5f;

        hud.SetJudgment("");
        hud.ShowTiming(true);
        yield return null;   // AI help: skip the frame of the press that picked the target (A is also the beat button)

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
        // Added with AI help: a bright bell for PERFECT, a tick for GOOD, a dull thud for MISS.
        if (result == "PERFECT!") GameAudio.Play(ProjectSilence.Synth.Bell(1046.5f, 0.45f), 0.8f);
        else if (result == "GOOD") GameAudio.Play(ProjectSilence.Synth.TickAccent, 0.8f);
        else GameAudio.Play(ProjectSilence.Synth.Miss, 0.7f);
        yield return new WaitForSeconds(0.5f);
        hud.ShowTiming(false);
    }

    int CalculateDamage(Unit attacker, Unit target)
    {
        return Mathf.Max(1, attacker.attack * 2 - target.defense);
    }

    // ---- Added with AI help: choosing a target with the mouse, the keyboard or a controller ----------

    GameObject targetMarker;

    // Click an enemy, or use left/right (d-pad, stick, arrow keys) and A / Enter.
    // B / Esc / right-click cancels (then chosenTarget stays null).
    IEnumerator ChooseTarget(string verb)
    {
        List<Unit> alive = enemies.Where(e => !e.IsDead)
            .OrderBy(e => Camera.main.WorldToScreenPoint(e.transform.position).x).ToList();
        int index = 0;
        float nextMove = 0f;
        chosenTarget = null;
        choosingTarget = true;
        yield return null;                                   // ignore the press that picked the action
        while (chosenTarget == null)
        {
            Vector2 move = BattleInput.Navigate;
            if (Mathf.Abs(move.x) > 0.5f)
            {
                if (Time.time >= nextMove)
                {
                    index = (index + (move.x > 0f ? 1 : alive.Count - 1)) % alive.Count;
                    nextMove = Time.time + 0.3f;           // holding the stick keeps moving, slowly
                    GameAudio.Play(ProjectSilence.Synth.UiMove, 0.6f);
                }
            }
            else nextMove = 0f;

            ShowTargetMarker(alive[index]);
            hud.SetMessage(GamepadSupport.UsingGamepad
                ? "Who to " + verb + "?  " + alive[index].unitName + "     Left/Right: switch    " + GamepadSupport.Confirm + ": OK    " + GamepadSupport.Back + ": back"
                : "Click an enemy to " + verb + " (or arrow keys + Enter).    Esc: back");
            if (BattleInput.ConfirmPressed) chosenTarget = alive[index];
            else if (BattleInput.CancelPressed)
            {
                GameAudio.Play(ProjectSilence.Synth.UiBack, 0.6f);
                break;
            }
            yield return null;                               // a mouse click sets chosenTarget in Update()
        }
        choosingTarget = false;
        if (targetMarker != null) targetMarker.SetActive(false);
    }

    Unit activeHero;
    GameObject turnMarker;

    void LateUpdate()
    {
        // A small blue crystal above the hero whose turn it is.
        if (activeHero == null || activeHero.IsDead)
        {
            if (turnMarker != null) turnMarker.SetActive(false);
            return;
        }
        if (turnMarker == null) turnMarker = MakeCrystal("Turn Marker", new Color(0.45f, 0.8f, 1f), 0.22f);
        turnMarker.SetActive(true);
        turnMarker.transform.position = activeHero.Motion.HeadPoint + Vector3.up * (0.35f + Mathf.Sin(Time.time * 4f) * 0.07f);
        turnMarker.transform.rotation = Quaternion.Euler(45f, Time.time * 90f, 45f);
    }

    static GameObject MakeCrystal(string name, Color color, float size)
    {
        GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crystal.name = name;
        Destroy(crystal.GetComponent<Collider>());              // must not block mouse clicks
        crystal.transform.localScale = Vector3.one * size;
        Renderer renderer = crystal.GetComponent<Renderer>();
        Shader unlit = Shader.Find("Sprites/Default");
        if (unlit != null) renderer.material = new Material(unlit);
        renderer.material.color = color;
        return crystal;
    }

    // A spinning gold crystal above the enemy you are pointing at.
    void ShowTargetMarker(Unit target)
    {
        if (targetMarker == null) targetMarker = MakeCrystal("Target Marker", new Color(1f, 0.82f, 0.25f), 0.3f);
        targetMarker.SetActive(true);
        targetMarker.transform.position = target.Motion.HeadPoint + Vector3.up * (0.45f + Mathf.Sin(Time.time * 5f) * 0.1f);
        targetMarker.transform.rotation = Quaternion.Euler(45f, Time.time * 120f, 45f);
    }

    static string BeatMessage(string ending)
    {
        return "Press " + (GamepadSupport.UsingGamepad ? GamepadSupport.Confirm : "SPACE") + " on the beat" + ending;
    }

    // Added with AI help: magic ignores half of the target's defense, so it is good against armour (the golem).
    int CalculateMagicDamage(Unit caster, Unit target)
    {
        return Mathf.Max(1, caster.magic * 2 - target.defense / 2);
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

    public void OnMagicButton()   // Added with AI help
    {
        chosenAction = "Magic";
    }

    public void OnContinueButton()
    {
        // Added with AI help: a battle started from the map goes back to the map (GameFlow knows where).
        if (GameFlow.InBattleFromMap)
        {
            GameFlow.EndBattle();
            return;
        }
        if (!string.IsNullOrEmpty(returnScene)) SceneManager.LoadScene(returnScene);
        else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
