using ProjectSilence;
using UnityEngine;

// =====================================================================================
// GameAudio.cs - written with AI help (Claude, 25 Nov 2026). List it in READ THIS DOCTOR.txt.
//
// Sound effects and map music. All sounds are generated in code by Synth.cs, so no audio files
// are needed (swap in recorded sounds later by passing your own AudioClips to Play).
//   GameAudio.Play(Synth.Hit);                 a one-shot sound
//   GameAudio.PlayMapMusic(clarity);           the field song; clarity 0..1 (low = muffled by the Silence)
//   GameAudio.StopMusic();
// The music player survives scene loads, so the song keeps going between the Kingdom and the Forest.
// =====================================================================================
public static class GameAudio
{
    public static float SfxVolume = 0.8f;
    public static float MusicVolume = 0.4f;

    static AudioSource[] pool;
    static int next;
    static AudioSource music;
    static AudioLowPassFilter muffle;
    static float targetCutoff = 22000f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        pool = null;
        music = null;
        muffle = null;
        next = 0;
    }

    public static void Play(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;
        if (pool == null || pool[0] == null)
        {
            var root = new GameObject("Sound Effects");
            Object.DontDestroyOnLoad(root);
            pool = new AudioSource[10];
            for (int i = 0; i < pool.Length; i++)
            {
                pool[i] = root.AddComponent<AudioSource>();
                pool[i].playOnAwake = false;
                pool[i].ignoreListenerPause = true;
            }
        }
        AudioSource source = pool[next];
        next = (next + 1) % pool.Length;
        source.pitch = pitch;
        source.PlayOneShot(clip, volume * SfxVolume);
    }

    // The field song. clarity 1 = clear, 0 = almost swallowed by the Silence (a low-pass filter).
    public static void PlayMapMusic(float clarity)
    {
        if (music == null)
        {
            var go = new GameObject("Map Music");
            Object.DontDestroyOnLoad(go);
            music = go.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;
            muffle = go.AddComponent<AudioLowPassFilter>();
            go.AddComponent<MusicFader>();
        }
        if (music.clip == null) music.clip = Synth.FieldSong();
        music.volume = MusicVolume * Mathf.Lerp(0.55f, 1f, clarity);
        targetCutoff = Mathf.Lerp(700f, 22000f, clarity * clarity);
        if (!music.isPlaying) music.Play();
    }

    public static void StopMusic()
    {
        if (music != null) music.Stop();
    }

    // Slides the filter smoothly when the clarity changes (entering the Forest, beating the boss).
    class MusicFader : MonoBehaviour
    {
        void Update()
        {
            if (muffle == null) return;
            muffle.cutoffFrequency = Mathf.Lerp(muffle.cutoffFrequency, targetCutoff, Time.unscaledDeltaTime * 1.5f);
        }
    }
}
