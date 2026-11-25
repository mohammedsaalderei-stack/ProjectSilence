using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public sealed class RealmSceneExit : MonoBehaviour
{
    public string destinationScene;
    public string arrivalMarker;
    void OnTriggerEnter(Collider other)
    {
        if(other.GetComponentInParent<MusicalMagePlayer>()!=null)
            RealmTravel.Go(destinationScene,arrivalMarker);
    }
}
