using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public sealed class MenuSettings : MonoBehaviour
{
    public GameObject mainPanel,settingsPanel;
    public Slider master,music,effects;
    public Text masterValue,musicValue,effectsValue;
    public Button backButton,settingsButton;
    void Start()
    {
        var a=MenuAudio.Instance;
        master.SetValueWithoutNotify(a.Master);music.SetValueWithoutNotify(a.Music);effects.SetValueWithoutNotify(a.Effects);
        UpdateLabels();master.onValueChanged.AddListener(Master);music.onValueChanged.AddListener(Music);effects.onValueChanged.AddListener(Effects);
    }
    void UpdateLabels(){masterValue.text=Mathf.RoundToInt(master.value*100)+"%";musicValue.text=Mathf.RoundToInt(music.value*100)+"%";effectsValue.text=Mathf.RoundToInt(effects.value*100)+"%";}
    public void Open(){mainPanel.SetActive(false);settingsPanel.SetActive(true);if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(master.gameObject);}
    public void Close(){PlayerPrefs.Save();settingsPanel.SetActive(false);mainPanel.SetActive(true);if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(settingsButton.gameObject);}
    void Master(float v){MenuAudio.Instance.SetMaster(v);UpdateLabels();MenuAudio.Instance.Adjust();}
    void Music(float v){MenuAudio.Instance.SetMusic(v);UpdateLabels();MenuAudio.Instance.Adjust();}
    void Effects(float v){MenuAudio.Instance.SetEffects(v);UpdateLabels();MenuAudio.Instance.Adjust();}
}
