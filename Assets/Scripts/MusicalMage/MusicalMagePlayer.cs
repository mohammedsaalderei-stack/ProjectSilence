using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class MusicalMagePlayer : MonoBehaviour
{
    [Header("Controls configured in Assets/Input")]
    public InputActionAsset controls;
    InputActionAsset runtimeControls;
    InputAction moveAction;
    InputAction sprintAction;
    [Header("Movement on the 3D map")]
    public float walkSpeed=2.6f;
    public float sprintSpeed=4.6f;
    public float gravity=-22f;
    public Camera movementCamera;
    public Animator spriteAnimator;
    public SpriteRenderer spriteRenderer;
    public Sprite[] standingFrames=new Sprite[8];
    public int FacingDirection { get; private set; }
    public bool IsMoving { get; private set; }
    public bool IsSprinting { get; private set; }
    public float RequestedSpeed { get; private set; }
    CharacterController capsule;
    float verticalSpeed;
    Vector2 externalInput;
    bool externalSprint;
    float externalUntil=-1;
    static readonly int Direction=Animator.StringToHash("Direction");
    static readonly int Sprint=Animator.StringToHash("Sprint");

    void Awake()
    {
        capsule=GetComponent<CharacterController>();
        if(movementCamera==null)movementCamera=Camera.main;
        if(controls!=null)
        {
            runtimeControls=Instantiate(controls);
            moveAction=runtimeControls.FindAction("Player/Move",true);
            sprintAction=runtimeControls.FindAction("Player/Sprint",true);
        }
    }
    void OnEnable(){if(runtimeControls!=null)runtimeControls.FindActionMap("Player",true).Enable();}
    void OnDisable(){if(runtimeControls!=null)runtimeControls.FindActionMap("Player",true).Disable();}
    void OnDestroy(){if(runtimeControls!=null)Destroy(runtimeControls);}
    void Update()
    {
        Vector2 input=moveAction!=null?moveAction.ReadValue<Vector2>():Vector2.zero;
        bool sprint=sprintAction!=null&&sprintAction.IsPressed();
        if(Time.time<externalUntil){input=externalInput;sprint=externalSprint;}
        input=Vector2.ClampMagnitude(input,1);
        IsMoving=input.sqrMagnitude>.015f;IsSprinting=IsMoving&&sprint;
        Vector3 forward=movementCamera!=null?Vector3.ProjectOnPlane(movementCamera.transform.forward,Vector3.up).normalized:Vector3.forward;
        Vector3 right=movementCamera!=null?Vector3.ProjectOnPlane(movementCamera.transform.right,Vector3.up).normalized:Vector3.right;
        RequestedSpeed=IsMoving?(IsSprinting?sprintSpeed:walkSpeed):0;
        if(IsMoving)
        {
            FacingDirection=((Mathf.RoundToInt(Mathf.Atan2(-input.x,-input.y)*Mathf.Rad2Deg/45)%8)+8)%8;
            spriteAnimator.enabled=true;
            spriteAnimator.SetInteger(Direction,FacingDirection);spriteAnimator.SetBool(Sprint,IsSprinting);
        }
        else
        {
            spriteAnimator.enabled=false;
            if(standingFrames.Length>FacingDirection&&standingFrames[FacingDirection]!=null)spriteRenderer.sprite=standingFrames[FacingDirection];
        }
        if(capsule.isGrounded&&verticalSpeed<0)verticalSpeed=-2;
        float dt=Mathf.Min(Time.deltaTime,.05f);verticalSpeed+=gravity*dt;
        capsule.Move(((right*input.x+forward*input.y)*RequestedSpeed+Vector3.up*verticalSpeed)*dt);
    }
    void LateUpdate()
    {
        // A camera-facing 2D sprite with a separate upright 3D capsule for collisions.
        if(movementCamera!=null&&spriteRenderer!=null)spriteRenderer.transform.rotation=movementCamera.transform.rotation;
    }
    // A short-lived input hook for scripted control and editor verification.
    public void DriveForSeconds(Vector2 direction,bool sprint,float seconds)
    {externalInput=Vector2.ClampMagnitude(direction,1);externalSprint=sprint;externalUntil=Time.time+Mathf.Max(0,seconds);}
}
