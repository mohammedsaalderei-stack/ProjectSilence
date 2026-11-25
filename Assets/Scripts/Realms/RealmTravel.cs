using UnityEngine;
using UnityEngine.SceneManagement;

public static class RealmTravel
{
    public static string NextSpawn;
    public static bool Loading;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset(){NextSpawn=null;Loading=false;}
    public static void Go(string scene,string spawn)
    {
        if(Loading)return;
        Loading=true;NextSpawn=spawn;SceneManager.LoadSceneAsync(scene,LoadSceneMode.Single);
    }
}
