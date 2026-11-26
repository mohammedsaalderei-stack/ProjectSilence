using UnityEngine;
[CreateAssetMenu(fileName = "GameSounds", menuName = "Silence/Game Sounds")]
public class GameSounds : ScriptableObject
{
    [Header("On the map")]
    [Tooltip("An enemy notices you (the red !).")]
    public AudioClip enemyNotices;
    [Tooltip("An enemy catches you: the screen flashes and a battle starts.")]
    public AudioClip battleStart;
    [Tooltip("You start talking to someone.")]
    public AudioClip talk;
    [Tooltip("The next line of a conversation.")]
    public AudioClip nextLine;
    [Header("Menus (pause, Controls, the battle's actions and targets)")]
    [Tooltip("Opening the pause menu or the Controls screen, choosing a battle action.")]
    public AudioClip confirm;
    [Tooltip("Closing the pause menu or the Controls screen, backing out of choosing an enemy.")]
    public AudioClip back;
    [Tooltip("Moving the gold crystal to another enemy.")]
    public AudioClip select;
    [Header("Battle: hitting the beat")]
    [Tooltip("A PERFECT press.")]
    public AudioClip perfect;
    [Tooltip("A GOOD press.")]
    public AudioClip good;
    [Tooltip("A MISS.")]
    public AudioClip miss;
    [Header("Battle: actions")]
    [Tooltip("Someone takes damage.")]
    public AudioClip hit;
    [Tooltip("Someone who is guarding takes damage (half damage).")]
    public AudioClip block;
    [Tooltip("Someone is healed: Harmony's Heal, and the healers on the map (Sister Agna, Shrine Keeper Iona).")]
    public AudioClip heal;
    [Tooltip("A spell is cast: Magic, Archcantor Vale's spells and his Perpetual Chord.")]
    public AudioClip spell;
    [Tooltip("Lyra shoots an arrow.")]
    public AudioClip arrow;
    [Tooltip("Archcantor Vale warns that the Perpetual Chord is coming (\"Guard!\").")]
    public AudioClip chordWarning;
    [Header("Options")]
    [Range(0f, 1f)]
    [Tooltip("Loudness of all the sound effects.")]
    public float volume = 0.8f;
    static GameSounds loaded, none;
    public static GameSounds Slots
    {
        get
        {
            if (loaded == null) loaded = Resources.Load<GameSounds>("GameSounds");
            if (loaded != null) return loaded;
            if (none == null) none = CreateInstance<GameSounds>();
            return none;
        }
    }
}
