using UnityEngine;

public static class GameAudio
{
    public static float MusicVolume
    {
        get
        {
            GameMusic songs = GameMusic.Get();
            return songs != null ? songs.volume : 0.35f;
        }
    }

    public static float SfxVolume => GameSounds.Slots.volume;

    static AudioSource[] pool;
    static int next;
    static AudioSource song, once;
    static AudioLowPassFilter muffle;
    static AudioClip wantedSong;
    static float wantedCutoff = 22000f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        pool = null;
        next = 0;
        song = null;
        once = null;
        muffle = null;
        wantedSong = null;
        wantedCutoff = 22000f;
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


    public static void PlaySong(AudioClip clip, bool muffled = false)
    {
        MakeMusicPlayers();
        wantedCutoff = muffled ? 1200f : 22000f;
        if (once.isPlaying) once.Stop();
        wantedSong = clip;
        if (clip != null && song.clip == clip && !song.isPlaying)
        {
            song.UnPause();
            if (!song.isPlaying) song.Play();
        }
    }

    public static void PauseSong()
    {
        if (song != null) song.Pause();
    }

    public static void UseMenuSong(AudioClip clip)
    {
        GameObject menuAudio = GameObject.Find("Menu Audio");
        AudioSource source = menuAudio != null ? menuAudio.GetComponent<AudioSource>() : null;
        if (source == null || source.clip == clip) return;
        source.Stop();
        source.clip = clip;
        if (clip != null) source.Play();
    }

    public static void PlayOnce(AudioClip clip)
    {
        if (clip == null) return;
        MakeMusicPlayers();
        once.Stop();
        once.clip = clip;
        once.volume = MusicVolume;
        once.Play();
    }

    static void MakeMusicPlayers()
    {
        if (song != null) return;
        var go = new GameObject("Music Player");
        Object.DontDestroyOnLoad(go);
        song = go.AddComponent<AudioSource>();
        song.loop = true;
        song.playOnAwake = false;
        song.ignoreListenerPause = true;
        muffle = go.AddComponent<AudioLowPassFilter>();
        muffle.cutoffFrequency = 22000f;

        var onceObject = new GameObject("Play Once");
        onceObject.transform.SetParent(go.transform);
        once = onceObject.AddComponent<AudioSource>();
        once.loop = false;
        once.playOnAwake = false;
        once.ignoreListenerPause = true;

        go.AddComponent<MusicFader>();
    }

    class MusicFader : MonoBehaviour
    {
        void Update()
        {
            float target = MusicVolume;
            float step = Time.unscaledDeltaTime / 0.8f * Mathf.Max(0.05f, target);
            if (song.clip != wantedSong)
            {
                song.volume = Mathf.MoveTowards(song.volume, 0f, step);
                if (song.volume <= 0.001f || !song.isPlaying)
                {
                    song.Stop();
                    song.clip = wantedSong;
                    song.volume = 0f;
                    if (wantedSong != null) song.Play();
                }
            }
            else if (song.isPlaying)
            {
                song.volume = Mathf.MoveTowards(song.volume, target, step);
            }
            muffle.cutoffFrequency = Mathf.Lerp(muffle.cutoffFrequency, wantedCutoff, Time.unscaledDeltaTime * 1.5f);
            once.volume = target;
        }
    }
}
