using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BattleMusic : MonoBehaviour
{
    public AudioClip music;
    public float bpm = 100f;
    public float firstBeat;

    AudioSource source;
    double startTime;

    public float BeatLength => 60f / bpm;
    public double SongTime => (music != null ? AudioSettings.dspTime : Time.timeAsDouble) - startTime - firstBeat;

    void Start()
    {
        source = GetComponent<AudioSource>();
        source.loop = true;

        GameMusic songs = GameMusic.Get();
        if (songs != null)
        {
            GameMusic.BattleSong chosen = songs.ForBattle(GameFlow.InBattleFromMap && GameFlow.PendingIsBoss,
                                                          GameFlow.InBattleFromMap && GameFlow.PendingIsFinal);
            music = chosen.song;
            bpm = Mathf.Max(30f, chosen.beatsPerMinute);
            firstBeat = chosen.firstBeatAt;
        }
        source.volume = GameAudio.MusicVolume;

        if (music != null)
        {
            source.clip = music;
            startTime = AudioSettings.dspTime + 0.1;
            source.PlayScheduled(startTime);
        }
        else
        {
            startTime = Time.timeAsDouble;
        }
    }

    public void Stop()
    {
        if (source != null) source.Stop();
    }
}
