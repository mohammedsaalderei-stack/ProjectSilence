using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        yield return new WaitForSecondsRealtime(.18f);
        RealmTravel.NextSpawn=null;
        yield return SceneManager.LoadSceneAsync(kingdomScene,LoadSceneMode.Single);
    }
}
