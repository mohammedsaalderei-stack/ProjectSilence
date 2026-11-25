using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public sealed class MenuAudio : MonoBehaviour
{
    public static MenuAudio Instance { get; private set; }
    public AudioClip hover,press,adjust,ambience;
    AudioSource music,sounds;
    public float Master { get; private set; }
    public float Music { get; private set; }
    public float Effects { get; private set; }
    float lastAdjust=-1;
    void Awake()
    {
        if(Instance!=null){Destroy(gameObject);return;}
        Instance=this;DontDestroyOnLoad(gameObject);
        music=gameObject.AddComponent<AudioSource>();music.loop=true;music.playOnAwake=false;music.spatialBlend=0;music.clip=ambience;
        sounds=gameObject.AddComponent<AudioSource>();sounds.playOnAwake=false;sounds.spatialBlend=0;
        SetMaster(PlayerPrefs.GetFloat("Audio.Master",.8f));SetMusic(PlayerPrefs.GetFloat("Audio.Music",.35f));SetEffects(PlayerPrefs.GetFloat("Audio.Effects",.7f));
        SceneManager.sceneLoaded+=SceneLoaded;
        if(SceneManager.GetActiveScene().name=="Menu")music.Play();
    }
    void SceneLoaded(Scene s,LoadSceneMode mode){if(s.name=="Menu"){if(!music.isPlaying)music.Play();}else music.Stop();}
    public void SetMaster(float v){Master=Mathf.Clamp01(v);AudioListener.volume=Master;PlayerPrefs.SetFloat("Audio.Master",Master);}
    public void SetMusic(float v){Music=Mathf.Clamp01(v);if(music!=null)music.volume=Music;PlayerPrefs.SetFloat("Audio.Music",Music);}
    public void SetEffects(float v){Effects=Mathf.Clamp01(v);if(sounds!=null)sounds.volume=Effects;PlayerPrefs.SetFloat("Audio.Effects",Effects);}
    public void Hover(){if(hover!=null)sounds.PlayOneShot(hover,.6f);}
    public void Press(){if(press!=null)sounds.PlayOneShot(press);}
    public void Adjust(){if(Time.unscaledTime-lastAdjust<.07f)return;lastAdjust=Time.unscaledTime;if(adjust!=null)sounds.PlayOneShot(adjust,.6f);}
    void OnDestroy(){if(Instance!=this)return;SceneManager.sceneLoaded-=SceneLoaded;Instance=null;}
    void OnApplicationQuit(){PlayerPrefs.Save();}
}
