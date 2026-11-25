using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public sealed class SimpleStartMenu : MonoBehaviour
{
    public GameObject titleScreen;
    public GameObject player;
    public Transform kingdomSpawn;
    public MusicalMageFollowCamera followCamera;
    public Button startButton;
    public string kingdomSceneName="Kingdom";
    public bool HasStarted { get; private set; }

    void Awake()
    {
        HasStarted=false;
        titleScreen.SetActive(true);
        if(followCamera!=null)followCamera.enabled=false;
        if(player!=null)player.SetActive(false);
        Cursor.lockState=CursorLockMode.None;
        Cursor.visible=true;
    }

    void Start()
    {
        if(EventSystem.current!=null&&startButton!=null)
            EventSystem.current.SetSelectedGameObject(startButton.gameObject);
    }

    public void StartGame()
    {
        if(HasStarted)return;
        if(!string.IsNullOrEmpty(kingdomSceneName))
        {
            HasStarted=true;
            if(startButton!=null)startButton.interactable=false;
            RealmTravel.NextSpawn=null;
            StartCoroutine(LoadKingdom());
            return;
        }
        if(player==null||kingdomSpawn==null)return;
        HasStarted=true;
        var capsule=player.GetComponent<CharacterController>();
        if(capsule!=null)capsule.enabled=false;
        player.transform.position=kingdomSpawn.position;
        player.SetActive(true);
        if(capsule!=null)capsule.enabled=true;
        if(followCamera!=null){followCamera.enabled=true;followCamera.SnapToTarget();}
        if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);
        titleScreen.SetActive(false);
    }
    System.Collections.IEnumerator LoadKingdom()
    {
        yield return new WaitForSecondsRealtime(.18f);
        PlayerPrefs.Save();
        SceneManager.LoadSceneAsync(kingdomSceneName,LoadSceneMode.Single);
    }
}
