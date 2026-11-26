using UnityEditor;
using UnityEngine;
public static class SoundSlots
{
    public const string AssetPath = "Assets/_Game/Audio/Resources/GameSounds.asset";
    [MenuItem("Silence/Sound Effect Slots")]
    public static void Open()
    {
        GameSounds sounds = Ensure();
        Selection.activeObject = sounds;
        EditorGUIUtility.PingObject(sounds);
    }
    public static GameSounds Ensure()
    {
        var sounds = AssetDatabase.LoadAssetAtPath<GameSounds>(AssetPath);
        if (sounds != null) return sounds;
        if (!AssetDatabase.IsValidFolder("Assets/_Game/Audio/Resources")) AssetDatabase.CreateFolder("Assets/_Game/Audio", "Resources");
        sounds = ScriptableObject.CreateInstance<GameSounds>();
        AssetDatabase.CreateAsset(sounds, AssetPath);
        AssetDatabase.SaveAssets();
        Debug.Log("SOUND slots made: " + AssetPath);
        return sounds;
    }
}
