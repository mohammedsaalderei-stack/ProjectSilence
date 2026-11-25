using System.Collections;
using UnityEngine;

// =====================================================================================
// UnitMotion.cs - written with AI help (Claude, 25 Nov 2026). List it in READ THIS DOCTOR.txt.
//
// Moves and animates a battle unit from code, so it works for every model -
// even the wolf, which has no animation clips at all.
//
//   RunUpTo       run to a spot just in front of the target (physical attacks)
//   LungeForward  the strike: lean back, then lunge in with a little hop
//   LungeBack     step back out of the lunge
//   ReturnHome    hop back to where the unit started the battle
//   PlayHit       get knocked back a little when hit
//   PlayDeath     heroes fall over, enemies sink into the ground and vanish
//   CastRise / CastSettle   magic: float up a little while casting, then back down
//   Update        models with no animation clips "breathe" so they don't look frozen
//
// Unit.cs adds this component by itself, so the scene needs no changes.
// To change the settings for one unit (for example, so the Maestro attacks from where
// he stands), add UnitMotion to that unit in the Inspector and change it there.
// =====================================================================================
public class UnitMotion : MonoBehaviour
{
    [Tooltip("Run up to the target for a physical attack. Untick to attack from where the unit stands.")]
    public bool runToTarget = true;

    [Tooltip("Running speed in metres per second.")]
    public float runSpeed = 9f;

    [Tooltip("Space left between this unit and its target, in metres.")]
    public float gap = 0.25f;

    Vector3 home;          // where the unit stood when the battle started
    Vector3 baseScale;     // its normal size
    bool prepared;
    bool measured;
    Vector3 halfSize = new Vector3(0.4f, 1f, 0.4f);   // half the model's size on each axis
    Vector3 centreOffset;                              // middle of the model, relative to the unit's position
    float centreHeight = 1f;                           // height of the middle of the body
    Vector3 castStart;                                 // where the unit stood before floating up to cast
    bool hasClips;         // true if the model has an Animator with clips
    bool busy;             // true while a move is playing (pauses the breathing)
    bool dead;
    Vector3 lungeStart;    // where the lunge started, so LungeBack can return there
    Vector3 hitFrom;       // where the last attacker struck from, so we flinch the right way
    float breathOffset;    // so units don't all breathe in sync

    // How far this unit's body reaches from its position in a flat direction ("where its front is").
    // A long wolf reaches far towards its head; a thin hero doesn't.
    public float ExtentToward(Vector3 direction)
    {
        if (!measured) Measure();
        direction = Flat(direction).normalized;
        float reach = Vector3.Dot(centreOffset, direction)
                    + Mathf.Abs(direction.x) * halfSize.x
                    + Mathf.Abs(direction.z) * halfSize.z;
        return Mathf.Clamp(reach, 0.2f, 3f);
    }

    // The middle of the body (where spells start and land).
    public Vector3 BodyCentre
    {
        get
        {
            if (!measured) Measure();
            return transform.position + centreOffset + Vector3.up * centreHeight;
        }
    }

    // The top of the head (where the target marker floats).
    public Vector3 HeadPoint
    {
        get
        {
            if (!measured) Measure();
            return transform.position + centreOffset + Vector3.up * (centreHeight + halfSize.y);
        }
    }

    // Spells call this so the target flinches away from the caster.
    public void HitFrom(Vector3 position)
    {
        hitFrom = position;
    }

    void Start()
    {
        Prepare();
    }

    // Remembers the starting spot and measures the model. Runs once.
    void Prepare()
    {
        if (prepared) return;
        prepared = true;

        home = transform.position;
        baseScale = transform.localScale;
        hitFrom = transform.position - transform.forward;
        breathOffset = Random.value * 10f;

        Animator animator = GetComponentInChildren<Animator>();
        hasClips = animator != null && animator.runtimeAnimatorController != null;
    }

    // Measures the model as it stands right now (first used a moment into the battle, after the
    // idle animation has posed it). Skinned models are measured from their bones, because their
    // mesh bounds still include the arms-out pose they were modelled in.
    void Measure()
    {
        measured = true;
        bool found = false;
        Bounds bounds = new Bounds(transform.position, Vector3.zero);
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            SkinnedMeshRenderer skinned = r as SkinnedMeshRenderer;
            if (skinned != null && skinned.bones != null && skinned.bones.Length > 0)
            {
                foreach (Transform bone in skinned.bones)
                {
                    if (bone == null) continue;
                    if (!found) bounds = new Bounds(bone.position, Vector3.zero);
                    else bounds.Encapsulate(bone.position);
                    found = true;
                }
            }
            else
            {
                if (!found) bounds = r.bounds;
                else bounds.Encapsulate(r.bounds);
                found = true;
            }
        }
        if (!found) return;
        bounds.Expand(0.3f);       // bones sit inside the body: add 15 cm of "flesh" on every side
        halfSize = bounds.extents;
        centreOffset = Flat(bounds.center - transform.position);
        centreHeight = bounds.center.y - transform.position.y;
    }

    // ---------------------------------------------------------------------------------
    // Physical attack: RunUpTo -> LungeForward -> (deal damage) -> LungeBack -> ReturnHome
    // ---------------------------------------------------------------------------------

    public IEnumerator RunUpTo(UnitMotion target)
    {
        Prepare();
        if (!runToTarget || target == null) yield break;

        Vector3 from = transform.position;
        Vector3 toTarget = Flat(target.transform.position - from);
        Vector3 direction = toTarget.normalized;
        // Stop when our front is "gap" metres from the target's front.
        float stopDistance = ExtentToward(direction) + target.ExtentToward(-direction) + gap;
        if (toTarget.magnitude <= stopDistance) yield break;      // already close enough

        Vector3 spot = target.transform.position - direction * stopDistance;
        spot.y = from.y;
        float distance = Vector3.Distance(from, spot);
        int strides = Mathf.Max(2, Mathf.RoundToInt(distance / 1.2f));
        float duration = Mathf.Clamp(distance / runSpeed, 0.25f, 0.9f);
        yield return Travel(spot, duration, 0.12f, strides, 12f);
    }

    public IEnumerator LungeForward(UnitMotion target)
    {
        Prepare();
        busy = true;
        lungeStart = transform.position;

        Vector3 direction = target != null ? Flat(target.transform.position - lungeStart).normalized : transform.forward;
        if (target != null) target.hitFrom = lungeStart;          // tell the target where the hit comes from

        Vector3 windUp = lungeStart - direction * 0.15f;          // lean back first...
        Vector3 strike = lungeStart + direction * 0.35f;          // ...then lunge in
        yield return Slide(lungeStart, windUp, 0.14f, 0f);
        yield return Slide(windUp, strike, 0.1f, 0.15f);
    }

    public IEnumerator LungeBack()
    {
        yield return Slide(transform.position, lungeStart, 0.2f, 0f);
        busy = false;
    }

    public IEnumerator ReturnHome()
    {
        Prepare();
        float distance = Vector3.Distance(transform.position, home);
        if (distance < 0.01f) yield break;
        float duration = Mathf.Clamp(distance / (runSpeed * 0.8f), 0.3f, 0.8f);
        yield return Travel(home, duration, 0.5f, 1, 6f);         // one big hop back, still facing the enemy
    }

    // ---------------------------------------------------------------------------------
    // Magic: CastRise -> (the spell flies) -> CastSettle
    // ---------------------------------------------------------------------------------

    public IEnumerator CastRise()
    {
        Prepare();
        busy = true;
        castStart = transform.position;
        yield return Slide(castStart, castStart + Vector3.up * 0.25f, 0.25f, 0f);
    }

    // Floats back down on its own, so the battle can carry on at the same time.
    public void CastSettle()
    {
        if (gameObject.activeInHierarchy) StartCoroutine(SettleAfterCast());
    }

    IEnumerator SettleAfterCast()
    {
        yield return Slide(transform.position, castStart, 0.3f, 0f);
        busy = false;
    }

    // ---------------------------------------------------------------------------------
    // Reactions (Unit.TakeDamage calls these)
    // ---------------------------------------------------------------------------------

    public void PlayHit()
    {
        if (gameObject.activeInHierarchy && !dead) StartCoroutine(HitReaction());
    }

    public void PlayDeath(bool isHero)
    {
        dead = true;
        if (!gameObject.activeInHierarchy)
        {
            if (!isHero) gameObject.SetActive(false);
            return;
        }
        StopAllCoroutines();
        StartCoroutine(isHero ? FallOver() : SinkAway());
    }

    IEnumerator HitReaction()
    {
        busy = true;
        Vector3 start = transform.position;
        Quaternion startRotation = transform.rotation;
        Vector3 away = Flat(start - hitFrom).normalized;          // pushed away from the attacker
        Vector3 tiltAxis = Vector3.Cross(Vector3.up, away);

        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / 0.4f);
            // Out fast (first 20% of the time), back slowly (the rest).
            float push = t < 0.2f ? t / 0.2f : 1f - Mathf.SmoothStep(0f, 1f, (t - 0.2f) / 0.8f);
            transform.position = start + away * (push * 0.3f);
            transform.rotation = Quaternion.AngleAxis(push * 10f, tiltAxis) * startRotation;
            yield return null;
        }
        transform.position = start;
        transform.rotation = startRotation;
        busy = false;
    }

    // Heroes: the same 90-degree fall your Unit.cs did before, now animated, with a small bounce.
    IEnumerator FallOver()
    {
        busy = true;
        Quaternion start = transform.rotation;
        Quaternion end = start * Quaternion.Euler(0f, 0f, 90f);

        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / 0.45f);
            transform.rotation = Quaternion.Slerp(start, end, t * t);   // t*t speeds up, like falling
            yield return null;
        }
        t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / 0.2f);
            transform.rotation = end * Quaternion.Euler(0f, 0f, -Mathf.Sin(t * Mathf.PI) * 6f);
            yield return null;
        }
        transform.rotation = end;
    }

    // Enemies: knocked back by the final blow, then sink and shrink away (before, they just vanished).
    IEnumerator SinkAway()
    {
        busy = true;
        Vector3 start = transform.position;
        Vector3 away = Flat(start - hitFrom).normalized;
        yield return Slide(start, start + away * 0.35f, 0.15f, 0.1f);
        yield return new WaitForSeconds(0.15f);

        Vector3 knocked = transform.position;
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / 0.6f);
            transform.position = knocked + Vector3.down * (t * t * 0.8f);
            transform.localScale = baseScale * (1f - 0.9f * t);
            yield return null;
        }
        gameObject.SetActive(false);                              // gone, like before
    }

    // Models with no animation clips (the wolf) would stand frozen, so they breathe a little.
    void Update()
    {
        if (hasClips || busy || dead || !prepared) return;
        float breath = 1f + Mathf.Sin(Time.time * 2.4f + breathOffset) * 0.03f;
        transform.localScale = new Vector3(baseScale.x, baseScale.y * breath, baseScale.z);
    }

    // ---------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------

    // Moves along a straight line: speeds up, then slows down, with small hops
    // (running strides) and a lean towards where it is going.
    IEnumerator Travel(Vector3 to, float duration, float hopHeight, int hops, float leanDegrees)
    {
        busy = true;
        Vector3 from = transform.position;
        Quaternion startRotation = transform.rotation;
        Vector3 leanAxis = Vector3.Cross(Vector3.up, Flat(to - from).normalized);

        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / duration);
            float eased = t * t * (3f - 2f * t);                    // "smoothstep" easing
            Vector3 position = Vector3.Lerp(from, to, eased);
            position.y += Mathf.Abs(Mathf.Sin(t * Mathf.PI * hops)) * hopHeight;
            transform.position = position;
            transform.rotation = Quaternion.AngleAxis(Mathf.Sin(t * Mathf.PI) * leanDegrees, leanAxis) * startRotation;
            yield return null;
        }
        transform.position = to;
        transform.rotation = startRotation;
        busy = false;
    }

    // A short straight move with an optional hop (used by the lunge).
    IEnumerator Slide(Vector3 from, Vector3 to, float duration, float hopHeight)
    {
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / duration);
            Vector3 position = Vector3.Lerp(from, to, t * t * (3f - 2f * t));
            position.y += Mathf.Sin(t * Mathf.PI) * hopHeight;
            transform.position = position;
            yield return null;
        }
        transform.position = to;
    }

    static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v;
    }
}
