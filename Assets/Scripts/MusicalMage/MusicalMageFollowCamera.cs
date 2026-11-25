using UnityEngine;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Camera))]
public sealed class MusicalMageFollowCamera : MonoBehaviour
{
    public Transform target;
    public float zoom=5.2f;
    [Range(0,60)] public float viewingAngle=15f;
    public float distance=18f;
    public float focusHeight=.9f;
    public float followSmoothTime=.14f;
    Vector3 velocity;
    Camera view;
    void Awake(){view=GetComponent<Camera>();}
    void Start(){SnapToTarget();}
    public void SnapToTarget()
    {
        if(view==null)view=GetComponent<Camera>();
        if(target==null)return;
        view.orthographic=true;view.orthographicSize=zoom;
        transform.rotation=Quaternion.Euler(viewingAngle,0,0);
        transform.position=target.position+Vector3.up*focusHeight-transform.forward*distance;
        velocity=Vector3.zero;
    }
    void LateUpdate()
    {
        if(target==null)return;
        view.orthographic=true;view.orthographicSize=zoom;
        transform.rotation=Quaternion.Euler(viewingAngle,0,0);
        var position=target.position+Vector3.up*focusHeight-transform.forward*distance;
        transform.position=Vector3.SmoothDamp(transform.position,position,ref velocity,followSmoothTime);
    }
}
