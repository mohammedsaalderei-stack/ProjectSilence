using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// The menu layout, panels, controls, and sound events live in the Unity scene.
public sealed class MenuSceneNavigation : MonoBehaviour
{
    public string kingdomScene="Kingdom";
    bool loading;
    public void StartGame()
    {
        if(loading)return;
        loading=true;StartCoroutine(LoadKingdom());
    }
    IEnumerator LoadKingdom()
    {
        // Let the button's native AudioSource event finish its short click.
        yield return new WaitForSecondsRealtime(.18f);
        RealmTravel.NextSpawn=null;
        yield return SceneManager.LoadSceneAsync(kingdomScene,LoadSceneMode.Single);
    }
}
