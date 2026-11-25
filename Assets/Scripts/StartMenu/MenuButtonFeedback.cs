using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public sealed class MenuButtonFeedback : MonoBehaviour,IPointerEnterHandler,ISelectHandler
{
    Button button;
    void Awake(){button=GetComponent<Button>();button.onClick.AddListener(Press);}
    public void OnPointerEnter(PointerEventData e){if(button.IsInteractable()&&MenuAudio.Instance!=null)MenuAudio.Instance.Hover();}
    public void OnSelect(BaseEventData e){if(button!=null&&button.IsInteractable()&&MenuAudio.Instance!=null)MenuAudio.Instance.Hover();}
    void Press(){if(MenuAudio.Instance!=null)MenuAudio.Instance.Press();}
}
