using UnityEditor;
using UnityEngine;

public static class MusicSlots
{
    public const string AssetPath = "Assets/_Game/Audio/Resources/GameMusic.asset";

    [MenuItem("Silence/Music Slots")]
    public static void Open()
    {
        GameMusic music = Ensure();
        Selection.activeObject = music;
        EditorGUIUtility.PingObject(music);
    }

    public static GameMusic Ensure()
    {
        var music = AssetDatabase.LoadAssetAtPath<GameMusic>(AssetPath);
        if (music != null) return music;

        if (!AssetDatabase.IsValidFolder("Assets/_Game/Audio/Resources")) AssetDatabase.CreateFolder("Assets/_Game/Audio", "Resources");
        music = ScriptableObject.CreateInstance<GameMusic>();
        music.mainMenu = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Menu/Menu_Quiet_Realms.wav");
        music.battle.song = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/battlemusic.mp3");
        music.battle.beatsPerMinute = 78f;
        AssetDatabase.CreateAsset(music, AssetPath);
        AssetDatabase.SaveAssets();
        Debug.Log("MUSIC slots made: " + AssetPath);
        return music;
    }
}
