using UnityEngine;

[DefaultExecutionOrder(-500)]
public sealed class RealmPlayerSpawn : MonoBehaviour
{
    void Awake()
    {
        RealmTravel.Loading=false;
        if(string.IsNullOrEmpty(RealmTravel.NextSpawn))return;
        var marker=GameObject.Find(RealmTravel.NextSpawn);RealmTravel.NextSpawn=null;
        if(marker==null)return;
        var capsule=GetComponent<CharacterController>();if(capsule!=null)capsule.enabled=false;
        transform.position=marker.transform.position;
        if(capsule!=null)capsule.enabled=true;
    }
}
