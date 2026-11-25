using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// =====================================================================================
// GameFlow.cs - written with AI help (Claude, 25 Nov 2026). List it in READ THIS DOCTOR.txt.
//
// The adventure's memory between scenes. It is "static", so it survives scene loads:
//   - which overworld mob started the battle, and where to send the player back to
//   - which mobs are already defeated (they stay gone)
//   - the party's HP and MP between battles
//   - whether the boss is beaten (then the map shows the ending)
// =====================================================================================
public static class GameFlow
{
    public const string BattleScene = "Battle";
    public const string MenuScene = "_Menu";

    public struct HeroState
    {
        public int hp, maxHp, mp, maxMp;
    }

    public static readonly Dictionary<string, HeroState> Party = new Dictionary<string, HeroState>();
    public static readonly HashSet<string> DefeatedMobs = new HashSet<string>();
    public static bool BossDefeated;
    public static bool EndingShown;
    public static bool IntroShown;           // the opening narration has played
    public static bool Busy;                 // talking, paused or changing scene: mobs stand still

    // The battle being fought right now (set by the mob that caught the player).
    public static bool InBattleFromMap;
    public static string PendingMobId;
    public static string PendingMobName;
    public static int PendingEnemyGroup = -1;
    public static float PendingEnemyBoost = 1f;
    public static bool PendingIsBoss;
    public static string ReturnScene;
    public static Vector3 ReturnPosition;
    public static bool LastBattleWon;

    // What the map does when it loads again after a battle.
    public static bool PlaceAtReturnPosition;
    public static string MessageOnReturn;
    public static float SafeUntil;           // mobs ignore the player until this time (no instant re-fight)

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void NewGame()
    {
        Party.Clear();
        DefeatedMobs.Clear();
        BossDefeated = false;
        EndingShown = false;
        IntroShown = false;
        Busy = false;
        InBattleFromMap = false;
        PendingMobId = null;
        PendingMobName = null;
        PendingEnemyGroup = -1;
        PendingEnemyBoost = 1f;
        PendingIsBoss = false;
        ReturnScene = null;
        PlaceAtReturnPosition = false;
        MessageOnReturn = null;
        SafeUntil = 0f;
    }

    // An OverworldMob calls this when it touches the player.
    public static void StartBattle(OverworldMob mob, Vector3 playerPosition)
    {
        if (Busy) return;
        Busy = true;
        InBattleFromMap = true;
        PendingMobId = mob.mobId;
        PendingMobName = mob.displayName;
        PendingEnemyGroup = mob.enemyGroup;
        PendingEnemyBoost = mob.enemyBoost;
        PendingIsBoss = mob.isBoss;
        ReturnScene = SceneManager.GetActiveScene().name;
        ReturnPosition = playerPosition;
        GameAudio.StopMusic();
        GameAudio.Play(ProjectSilence.Synth.BattleStart, 0.9f);

        if (AdventureDirector.Instance != null) AdventureDirector.Instance.FlashThenLoad(BattleScene);
        else SceneManager.LoadScene(BattleScene);
    }

    // BattleManager calls this when a battle starts: HP and MP carry over, and the boss gets stronger.
    public static void ApplyToBattle(List<Unit> party, List<Unit> enemies)
    {
        foreach (Unit hero in party)
        {
            if (!Party.TryGetValue(hero.unitName, out HeroState saved)) continue;
            hero.currentHP = Mathf.Clamp(saved.hp, 1, hero.maxHP);
            hero.currentMP = Mathf.Clamp(saved.mp, 0, hero.maxMP);
        }

        if (!InBattleFromMap) return;
        if (PendingIsBoss)
        {
            // The boss fights alone: keep the toughest enemy of the group, send the others away.
            Unit boss = enemies.OrderByDescending(e => e.maxHP).First();
            foreach (Unit other in enemies.Where(e => e != boss).ToList())
            {
                other.gameObject.SetActive(false);
                enemies.Remove(other);
            }
            boss.unitName = "Hollow Colossus";
            boss.transform.localScale *= 1.25f;
        }
        if (PendingEnemyBoost != 1f)
        {
            foreach (Unit enemy in enemies)
            {
                enemy.maxHP = Mathf.RoundToInt(enemy.maxHP * PendingEnemyBoost);
                enemy.currentHP = enemy.maxHP;
                enemy.attack = Mathf.RoundToInt(enemy.attack * (1f + (PendingEnemyBoost - 1f) * 0.2f));
            }
        }
    }

    // BattleManager calls this when the battle is won or lost.
    public static void SaveParty(List<Unit> party, bool won)
    {
        LastBattleWon = won;
        foreach (Unit hero in party)
        {
            Party[hero.unitName] = new HeroState
            {
                hp = won ? Mathf.Max(1, hero.currentHP) : hero.maxHP,   // fallen heroes get back up with 1 HP
                maxHp = hero.maxHP,
                // A win gives back a quarter of the MP; a defeat sends you back fully healed.
                mp = won ? Mathf.Min(hero.maxMP, hero.currentMP + Mathf.CeilToInt(hero.maxMP * 0.25f)) : hero.maxMP,
                maxMp = hero.maxMP
            };
        }
    }

    // The Continue button after a battle.
    public static void EndBattle()
    {
        InBattleFromMap = false;
        if (LastBattleWon)
        {
            if (!string.IsNullOrEmpty(PendingMobId)) DefeatedMobs.Add(PendingMobId);
            if (PendingIsBoss) BossDefeated = true;
            PlaceAtReturnPosition = true;
            MessageOnReturn = PendingIsBoss ? null : "Victory! The " + PendingMobName + " is gone.";
        }
        else
        {
            PlaceAtReturnPosition = false;   // back to where you entered this area
            MessageOnReturn = "Your party fell... You wake at the edge of the area, fully healed.";
        }
        SafeUntil = Time.time + 2.5f;
        Busy = false;
        RealmTravel.NextSpawn = null;
        SceneManager.LoadScene(string.IsNullOrEmpty(ReturnScene) ? MenuScene : ReturnScene);
    }

    public static void HealParty()
    {
        foreach (string name in Party.Keys.ToList())
        {
            HeroState s = Party[name];
            s.hp = s.maxHp;
            s.mp = s.maxMp;
            Party[name] = s;
        }
    }

    public static string PartySummary()
    {
        if (Party.Count == 0) return "Party: Maestro and Harmony, ready";
        return string.Join("     ", Party.Select(p => p.Key + "  HP " + p.Value.hp + "/" + p.Value.maxHp + "  MP " + p.Value.mp + "/" + p.Value.maxMp));
    }
}
