using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(Button))]
public class QuitGameButton : MonoBehaviour
{
    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Quit);
    }
    static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
