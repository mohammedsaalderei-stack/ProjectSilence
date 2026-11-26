using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class OverworldMob : MonoBehaviour
{
    [Header("Who")]
    public string mobId = "mob";
    public string displayName = "Hollow Wolf pack";
    [Tooltip("Which enemy group of the Battle scene appears (0 = two wolves, 1 = golem and wolf).")]
    public int enemyGroup;
    [Tooltip("Above 1 makes the enemies tougher (the boss uses this).")]
    public float enemyBoost = 1f;
    public bool isBoss;
    [Tooltip("The final boss: beating it shows the ending.")]
    public bool isFinalBoss;
    [Tooltip("Only appears once this other mob is defeated (empty = always there).")]
    public string appearsAfterMob;
    [Tooltip("Lines this mob says when it notices you, before the battle (empty = it just attacks).")]
    [TextArea(2, 4)] public string[] introLines;
    bool hasSpoken;

    [Header("Movement (metres and metres per second)")]
    public float wanderRadius = 3f;
    public float noticeRadius = 6f;
    public float giveUpRadius = 12f;
    public float walkSpeed = 1.2f;
    public float chaseSpeed = 3f;
    public float touchDistance = 1.2f;

    Vector3 home;
    Vector3 wanderTarget;
    Vector3 lastPosition;
    float lastSpeed;
    float stuckTime;
    float detourUntil;
    Vector3 detourDirection;
    public bool IsChasing => chasing;
    float nextWanderTime;
    float verticalSpeed;
    bool chasing;
    Transform player;
    CharacterController body;
    Transform model;
    Vector3 modelRest;
    TextMesh alert;

    void Start()
    {
        if (GameFlow.DefeatedMobs.Contains(mobId)
            || (!string.IsNullOrEmpty(appearsAfterMob) && !GameFlow.DefeatedMobs.Contains(appearsAfterMob)))
        {
            gameObject.SetActive(false);
            return;
        }
        body = GetComponent<CharacterController>();

        body.enabled = false;
        if (GroundFinder.Find(transform.position, Mathf.Max(6f, wanderRadius * 4f), body.radius, body.height, out Vector3 ground))
        {
            transform.position = ground + Vector3.up * 0.05f;
            body.enabled = true;
        }
        else
        {
            Debug.LogWarning(name + ": no walkable ground nearby - move it in the scene. Switched off for now.");
            gameObject.SetActive(false);
            return;
        }
        body.minMoveDistance = 0f;
        home = transform.position;
        wanderTarget = home;
        lastPosition = home;
        MusicalMagePlayer found = FindAnyObjectByType<MusicalMagePlayer>();
        if (found != null) player = found.transform;
        if (transform.childCount > 0)
        {
            model = transform.GetChild(0);
            modelRest = model.localPosition;
        }
        alert = MakeAlert();
    }

    void Update()
    {
        if (player == null) return;
        if (GameFlow.Busy) return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;
        bool safe = Time.time < GameFlow.SafeUntil;

        if (!hasSpoken && introLines != null && introLines.Length > 0 && !safe && distance < noticeRadius
            && AdventureDirector.Instance != null)
        {
            hasSpoken = true;
            Vector3 where = player.position;
            AdventureDirector.Instance.NarrateThen(displayName, introLines, () => GameFlow.StartBattle(this, where));
            return;
        }

        if (!chasing && !safe && distance < noticeRadius)
        {
            chasing = true;
            GameAudio.Play(GameSounds.Slots.enemyNotices);
            if (isBoss && AdventureDirector.Instance != null)
                AdventureDirector.Instance.Toast("The Hollow Colossus turns toward you. The forest falls silent...");
        }
        if (chasing && (distance > giveUpRadius || Vector3.Distance(transform.position, home) > giveUpRadius)) chasing = false;
        alert.gameObject.SetActive(chasing);

        Vector3 goal;
        float speed;
        if (chasing)
        {
            goal = player.position;
            speed = chaseSpeed;
        }
        else
        {
            if (Time.time > nextWanderTime)
            {
                Vector2 offset = Random.insideUnitCircle * wanderRadius;
                wanderTarget = home + new Vector3(offset.x, 0f, offset.y);
                nextWanderTime = Time.time + Random.Range(2f, 4.5f);
            }
            goal = wanderTarget;
            speed = walkSpeed;
        }

        Vector3 move = goal - transform.position;
        move.y = 0f;
        bool moving = move.magnitude > 0.25f;
        Vector3 direction = moving ? move.normalized : Vector3.zero;

        if (moving)
        {
            stuckTime += Time.deltaTime;
            lastSpeed += speed * Time.deltaTime;
            if (stuckTime >= 0.3f)
            {
                Vector3 walked = transform.position - lastPosition;
                walked.y = 0f;
                if (Time.time >= detourUntil && walked.magnitude < lastSpeed * 0.3f)
                {
                    detourDirection = Quaternion.Euler(0f, Random.value < 0.5f ? 75f : -75f, 0f) * direction;
                    detourUntil = Time.time + 0.8f;
                    if (!chasing) nextWanderTime = 0f;
                }
                stuckTime = 0f;
                lastSpeed = 0f;
                lastPosition = transform.position;
            }
            if (Time.time < detourUntil) direction = detourDirection;
        }
        else
        {
            stuckTime = 0f;
            lastSpeed = 0f;
            lastPosition = transform.position;
        }
        if (moving) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 8f);

        verticalSpeed = body.isGrounded ? -2f : verticalSpeed - 20f * Time.deltaTime;
        body.Move((direction * speed + Vector3.up * verticalSpeed) * Time.deltaTime);
        if (transform.position.y < home.y - 6f)
        {
            body.enabled = false;
            transform.position = home;
            body.enabled = true;
            verticalSpeed = 0f;
            chasing = false;
        }

        if (model != null)
            model.localPosition = modelRest + Vector3.up * (moving ? Mathf.Abs(Mathf.Sin(Time.time * (4f + speed * 2f))) * 0.12f : 0f);

        if (!safe && distance < touchDistance) GameFlow.StartBattle(this, player.position);
    }

    void LateUpdate()
    {
        if (Camera.main == null) return;
        if (alert != null) alert.transform.rotation = Camera.main.transform.rotation;
        if (model != null && model.GetComponent<SpriteRenderer>() != null) model.rotation = Camera.main.transform.rotation;
    }

    TextMesh MakeAlert()
    {
        var go = new GameObject("Alert !");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.up * (body.height + 0.6f);
        TextMesh text = go.AddComponent<TextMesh>();
        text.text = "!";
        text.fontSize = 64;
        text.characterSize = 0.06f;
        text.anchor = TextAnchor.MiddleCenter;
        text.color = new Color(1f, 0.25f, 0.2f);
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        go.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
        go.SetActive(false);
        return text;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(Application.isPlaying ? home : transform.position, wanderRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, noticeRadius);
    }
}
