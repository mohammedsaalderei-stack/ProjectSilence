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
    public int magicCost = 6;
    public string returnScene;
    List<Unit> enemies;
    string chosenAction;
    Unit chosenTarget;
    bool choosingTarget;
    float timingMultiplier;
    void Start()
    {
        foreach (GameObject group in enemyGroups) group.SetActive(false);
        int groupIndex = GameFlow.InBattleFromMap && GameFlow.PendingEnemyGroup >= 0 && GameFlow.PendingEnemyGroup < enemyGroups.Length
            ? GameFlow.PendingEnemyGroup
            : Random.Range(0, enemyGroups.Length);
        GameObject chosenGroup = enemyGroups[groupIndex];
        chosenGroup.SetActive(true);
        enemies = chosenGroup.GetComponentsInChildren<Unit>().ToList();
        GameFlow.ApplyToBattle(party, enemies);
        if (GameFlow.InBattleFromMap && GameFlow.PendingIsFinal) DarkenForFinale();
        hud.ShowActions(false, false);
        hud.ShowTiming(false);
        hud.ShowResult(false, "");
        if (hud.magicButton != null) hud.magicButton.onClick.AddListener(OnMagicButton);
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
        hud.SetMessage(!GameFlow.InBattleFromMap ? "Enemies appear!"
            : GameFlow.PendingIsFinal ? "Archcantor Vale raises his staff. The Silence closes in!"
            : GameFlow.PendingIsBoss ? "The Hollow Colossus rises. Every sound around you dies..."
            : "A " + GameFlow.PendingMobName + " attacks!");
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
                    GameFlow.SaveParty(party, true);
                    PlayResultPiece(true);
                    activeHero = null;
                    hud.ShowResult(true, "Victory!");
                    yield break;
                }
                if (party.All(p => p.IsDead))
                {
                    GameFlow.SaveParty(party, false);
                    PlayResultPiece(false);
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
        activeHero = unit;
        while (true)
        {
            chosenAction = null;
            hud.ShowActions(true, unit.currentMP >= healCost, unit.currentMP >= magicCost);
            while (chosenAction == null)
            {
                hud.SetMessage(unit.unitName + "'s turn. Choose an action." + (GamepadSupport.UsingGamepad
                    ? "     D-pad: choose    " + GamepadSupport.Confirm + ": OK"
                    : "     (click a button)"));
                yield return null;
            }
            hud.ShowActions(false, false);
            GameAudio.Play(GameSounds.Slots.confirm);
            if (chosenAction != "Attack" && chosenAction != "Magic") break;
            yield return ChooseTarget(chosenAction == "Magic" ? "cast at" : "attack");
            if (chosenTarget != null) break;
        }
        activeHero = null;
        if (chosenAction == "Attack")
        {
            bool melee = unit.Motion.runToTarget;
            hud.SetMessage(melee ? unit.unitName + " charges at " + chosenTarget.unitName + "!"
                                 : unit.unitName + " takes aim at " + chosenTarget.unitName + "...");
            yield return unit.Motion.RunUpTo(chosenTarget.Motion);
            hud.SetMessage(BeatMessage("!"));
            yield return TimingPrompt();
            unit.PlayAttack();
            if (melee) yield return unit.Motion.LungeForward(chosenTarget.Motion);
            else yield return SpellEffect.Arrow(unit, chosenTarget);
            int damage = chosenTarget.TakeDamage(Mathf.RoundToInt(CalculateDamage(unit, chosenTarget) * timingMultiplier));
            hud.SetMessage(unit.unitName + " hits " + chosenTarget.unitName + " for " + damage + "!");
            if (melee) yield return unit.Motion.LungeBack();
            yield return new WaitForSeconds(0.35f);
            yield return unit.Motion.ReturnHome();
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
            hud.SetMessage(BeatMessage(" to cast!"));
            yield return TimingPrompt();
            unit.currentMP -= magicCost;
            unit.PlayCast();
            yield return unit.Motion.CastRise();
            GameAudio.Play(GameSounds.Slots.spell);
            yield return SpellEffect.Fly(unit, chosenTarget);
            unit.Motion.CastSettle();
            int damage = chosenTarget.TakeDamage(Mathf.RoundToInt(CalculateMagicDamage(unit, chosenTarget) * timingMultiplier));
            hud.SetMessage(unit.unitName + "'s spell hits " + chosenTarget.unitName + " for " + damage + "!");
        }
        yield return new WaitForSeconds(0.8f);
    }
    void PlayResultPiece(bool won)
    {
        GameMusic songs = GameMusic.Get();
        AudioClip piece = songs == null ? null : won ? songs.victory : songs.defeat;
        if (piece == null) return;
        music.Stop();
        GameAudio.PlayOnce(piece);
    }
    void DarkenForFinale()
    {
        foreach (Light light in FindObjectsByType<Light>())
            if (light.type == LightType.Directional) light.color = new Color(0.72f, 0.55f, 1f);
        RenderSettings.ambientLight = new Color(0.32f, 0.22f, 0.45f);
        RenderSettings.fogColor = new Color(0.2f, 0.1f, 0.3f);
    }
    readonly Dictionary<Unit, int> casterTurns = new Dictionary<Unit, int>();
    IEnumerator CasterTurn(Unit caster)
    {
        casterTurns.TryGetValue(caster, out int turn);
        turn++;
        casterTurns[caster] = turn;
        bool chord = caster.chordEvery > 0 && turn % caster.chordEvery == 0;
        bool warnNext = caster.chordEvery > 0 && (turn + 1) % caster.chordEvery == 0;
        List<Unit> alive = party.Where(p => !p.IsDead).ToList();
        if (chord)
        {
            hud.SetMessage(caster.unitName + " plays the Perpetual Chord! Silence crashes over everyone!");
            yield return new WaitForSeconds(0.4f);
            caster.PlayCast();
            yield return caster.Motion.CastRise();
            GameAudio.Play(GameSounds.Slots.spell);
            foreach (Unit hero in alive) StartCoroutine(SpellEffect.Fly(caster, hero, 0.6f));
            yield return new WaitForSeconds(0.65f);
            caster.Motion.CastSettle();
            foreach (Unit hero in alive) hero.TakeDamage(Mathf.Max(1, caster.magic + 4 - hero.defense / 2));
        }
        else
        {
            Unit target = alive[Random.Range(0, alive.Count)];
            hud.SetMessage(caster.unitName + " points his staff at " + target.unitName + "!");
            yield return new WaitForSeconds(0.4f);
            caster.PlayCast();
            yield return caster.Motion.CastRise();
            GameAudio.Play(GameSounds.Slots.spell);
            yield return SpellEffect.Fly(caster, target);
            caster.Motion.CastSettle();
            int damage = target.TakeDamage(Mathf.Max(1, caster.magic * 2 - target.defense / 2));
            hud.SetMessage(caster.unitName + "'s spell hits " + target.unitName + " for " + damage + "!");
        }
        yield return new WaitForSeconds(0.8f);
        if (warnNext)
        {
            hud.SetMessage(caster.unitName + " raises his staff high... the Perpetual Chord is coming. Guard!");
            GameAudio.Play(GameSounds.Slots.chordWarning);
            yield return new WaitForSeconds(1.4f);
        }
    }
    IEnumerator EnemyTurn(Unit enemy)
    {
        if (enemy.castsSpells)
        {
            yield return CasterTurn(enemy);
            yield break;
        }
        List<Unit> targets = party.Where(p => !p.IsDead).ToList();
        Unit target = targets[Random.Range(0, targets.Count)];
        hud.SetMessage(enemy.unitName + " attacks " + target.unitName + "!");
        yield return new WaitForSeconds(0.3f);
        yield return enemy.Motion.RunUpTo(target.Motion);
        enemy.PlayAttack();
        yield return enemy.Motion.LungeForward(target.Motion);
        int damage = target.TakeDamage(CalculateDamage(enemy, target));
        hud.SetMessage(enemy.unitName + " hits " + target.unitName + " for " + damage + "!");
        yield return enemy.Motion.LungeBack();
        yield return new WaitForSeconds(0.35f);
        yield return enemy.Motion.ReturnHome();
        yield return new WaitForSeconds(0.3f);
    }
    IEnumerator TimingPrompt()
    {
        float beatLength = music.BeatLength;
        double beatTime = (System.Math.Floor(music.SongTime / beatLength) + 2) * beatLength;
        string result = "MISS";
        timingMultiplier = 0.5f;
        hud.SetJudgment("");
        hud.ShowTiming(true);
        yield return null;
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
        GameAudio.Play(result == "PERFECT!" ? GameSounds.Slots.perfect : result == "GOOD" ? GameSounds.Slots.good : GameSounds.Slots.miss);
        yield return new WaitForSeconds(0.5f);
        hud.ShowTiming(false);
    }
    int CalculateDamage(Unit attacker, Unit target)
    {
        return Mathf.Max(1, attacker.attack * 2 - target.defense);
    }
    GameObject targetMarker;
    IEnumerator ChooseTarget(string verb)
    {
        List<Unit> alive = enemies.Where(e => !e.IsDead)
            .OrderBy(e => Camera.main.WorldToScreenPoint(e.transform.position).x).ToList();
        int index = 0;
        float nextMove = 0f;
        chosenTarget = null;
        choosingTarget = true;
        yield return null;
        while (chosenTarget == null)
        {
            Vector2 move = BattleInput.Navigate;
            if (Mathf.Abs(move.x) > 0.5f)
            {
                if (Time.time >= nextMove)
                {
                    index = (index + (move.x > 0f ? 1 : alive.Count - 1)) % alive.Count;
                    nextMove = Time.time + 0.3f;
                    GameAudio.Play(GameSounds.Slots.select);
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
                GameAudio.Play(GameSounds.Slots.back);
                break;
            }
            yield return null;
        }
        choosingTarget = false;
        if (targetMarker != null) targetMarker.SetActive(false);
    }
    Unit activeHero;
    GameObject turnMarker;
    void LateUpdate()
    {
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
        Destroy(crystal.GetComponent<Collider>());
        crystal.transform.localScale = Vector3.one * size;
        Renderer renderer = crystal.GetComponent<Renderer>();
        Shader unlit = Shader.Find("Sprites/Default");
        if (unlit != null) renderer.material = new Material(unlit);
        renderer.material.color = color;
        return crystal;
    }
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
    public void OnMagicButton()
    {
        chosenAction = "Magic";
    }
    public void OnContinueButton()
    {
        if (GameFlow.InBattleFromMap)
        {
            GameFlow.EndBattle();
            return;
        }
        if (!string.IsNullOrEmpty(returnScene)) SceneManager.LoadScene(returnScene);
        else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
