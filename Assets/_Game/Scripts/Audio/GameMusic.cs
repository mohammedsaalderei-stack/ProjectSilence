using UnityEngine;
[CreateAssetMenu(fileName = "GameMusic", menuName = "Silence/Game Music")]
public class GameMusic : ScriptableObject
{
    [System.Serializable]
    public class BattleSong
    {
        public AudioClip song;
        [Tooltip("The song's tempo in beats per minute: the timing ring closes once per beat. For a fast song use half " +
                 "its BPM (battlemusic.mp3 is 156 BPM, so 78 = a ring on every second beat).")]
        public float beatsPerMinute = 100f;
        [Tooltip("Seconds from the start of the file to its first beat (0 if the song starts right on a beat).")]
        public float firstBeatAt;
    }
    [Header("Menu and exploration (they loop)")]
    [Tooltip("The main menu. The menu plays it on its own Menu Audio object, so its Settings > MENU MUSIC slider sets the volume.")]
    public AudioClip mainMenu;
    [Tooltip("Exploring the city of Cantoria (the Kingdom map).")]
    public AudioClip city;
    [Tooltip("Exploring the Forest.")]
    public AudioClip forest;
    [Header("Battles (they loop)")]
    [Tooltip("Ordinary fights: Hollow Wolf packs and the Mute Golem.")]
    public BattleSong battle = new BattleSong();
    [Tooltip("The Hollow Colossus. Leave the song empty to use the ordinary battle song.")]
    public BattleSong bossBattle = new BattleSong();
    [Tooltip("Archcantor Vale, the final battle. Leave the song empty to use the Colossus song (or the ordinary one).")]
    public BattleSong finalBattle = new BattleSong();
    [Header("Short pieces (play once when a battle ends)")]
    [Tooltip("A battle is won. The battle song stops and this plays. Empty = the battle song keeps going.")]
    public AudioClip victory;
    [Tooltip("The whole party falls. Empty = the battle song keeps going.")]
    public AudioClip defeat;
    [Header("The end (loops)")]
    [Tooltip("The \"The Silence Breaks\" ending screen. Empty = the forest song keeps going.")]
    public AudioClip ending;
    [Header("Options")]
    [Range(0f, 1f)]
    [Tooltip("Loudness of the game's music: maps, battles, victory, defeat and ending. (The main menu has its own slider.)")]
    public float volume = 0.35f;
    [Tooltip("Story effect: the Silence swallows the forest song (it sounds muffled) until Archcantor Vale is beaten.")]
    public bool muffleForest;
    static GameMusic loaded;
    public static GameMusic Get()
    {
        if (loaded == null) loaded = Resources.Load<GameMusic>("GameMusic");
        return loaded;
    }
    public BattleSong ForBattle(bool boss, bool final)
    {
        if (final && finalBattle.song != null) return finalBattle;
        if ((boss || final) && bossBattle.song != null) return bossBattle;
        return battle;
    }
}
