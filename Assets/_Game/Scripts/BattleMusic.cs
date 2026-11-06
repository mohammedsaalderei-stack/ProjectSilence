using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BattleMusic : MonoBehaviour
{
    public AudioClip music;
    public float bpm = 100f;

    AudioSource source;
    double startTime;

    public float BeatLength => 60f / bpm;
    public double SongTime => music != null ? AudioSettings.dspTime - startTime : Time.timeAsDouble - startTime;

    void Start()
    {
        source = GetComponent<AudioSource>();
        source.loop = true;

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
}
