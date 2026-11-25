using System.Collections;
using UnityEngine;

// =====================================================================================
// SpellEffect.cs - written with AI help (Claude, 25 Nov 2026). List it in READ THIS DOCTOR.txt.
//
// The look of a spell, made from code (no art files needed):
//   Fly    a glowing orb with a trail flies in an arc from the caster to the target,
//          lighting up the ground around it
//   Burst  a flash that grows and fades where the spell hits
// The colour comes from each Unit's "Spell Color" (Inspector).
// =====================================================================================
public class SpellEffect : MonoBehaviour
{
    float life;        // how long the burst lasts, in seconds
    float age;
    float maxSize;
    Color color;
    Material material;
    Light flash;

    // Flies a spell from the caster to the target. Use "yield return": it finishes when the spell hits.
    public static IEnumerator Fly(Unit caster, Unit target, float duration = 0.5f)
    {
        Color color = caster.spellColor;
        Vector3 to = target.Motion.BodyCentre;
        Vector3 from = caster.Motion.BodyCentre + Vector3.up * 0.3f;
        from += Flat(to - from).normalized * 0.5f;                  // start just in front of the caster

        GameObject orb = MakeGlowSphere("Spell Orb", color, 0.35f, 0.9f);
        orb.transform.position = from;

        Light light = orb.AddComponent<Light>();                    // lights up the ground as it flies
        light.type = LightType.Point;
        light.color = color;
        light.range = 4f;
        light.intensity = 4f;

        TrailRenderer trail = orb.AddComponent<TrailRenderer>();
        trail.material = orb.GetComponent<Renderer>().material;
        trail.time = 0.25f;
        trail.startWidth = 0.3f;
        trail.endWidth = 0f;
        trail.startColor = color;
        trail.endColor = new Color(color.r, color.g, color.b, 0f);

        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / duration);
            Vector3 position = Vector3.Lerp(from, to, t);
            position.y += Mathf.Sin(t * Mathf.PI) * 0.8f;              // fly in an arc
            orb.transform.position = position;
            orb.transform.localScale = Vector3.one * 0.35f * (1f + 0.15f * Mathf.Sin(Time.time * 40f));   // shimmer
            yield return null;
        }

        Destroy(orb);
        target.Motion.HitFrom(from);                                // the target flinches away from the caster
        Burst(to, color);
    }

    // A flash of light that grows and fades by itself.
    public static void Burst(Vector3 position, Color color)
    {
        GameObject burst = MakeGlowSphere("Spell Burst", color, 0.3f, 0.8f);
        burst.transform.position = position;

        SpellEffect effect = burst.AddComponent<SpellEffect>();
        effect.life = 0.45f;
        effect.maxSize = 1.8f;
        effect.color = color;
        effect.material = burst.GetComponent<Renderer>().material;
        effect.flash = burst.AddComponent<Light>();
        effect.flash.type = LightType.Point;
        effect.flash.color = color;
        effect.flash.range = 6f;
    }

    void Update()
    {
        age += Time.deltaTime;
        float t = Mathf.Clamp01(age / life);
        float grow = 1f - (1f - t) * (1f - t);                      // grows fast, then slows down
        transform.localScale = Vector3.one * Mathf.Lerp(0.3f, maxSize, grow);
        material.color = new Color(color.r, color.g, color.b, 0.8f * (1f - t));   // fades out
        flash.intensity = 8f * (1f - t);
        if (t >= 1f) Destroy(gameObject);
    }

    // A sphere with an unlit, see-through material, so it glows even in a dark scene.
    static GameObject MakeGlowSphere(string name, Color color, float size, float alpha)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = name;
        Destroy(sphere.GetComponent<Collider>());                   // spells must not block mouse clicks
        sphere.transform.localScale = Vector3.one * size;

        Renderer renderer = sphere.GetComponent<Renderer>();
        Shader unlit = Shader.Find("Sprites/Default");              // always included in builds (Graphics settings)
        if (unlit != null) renderer.material = new Material(unlit);
        renderer.material.color = new Color(color.r, color.g, color.b, alpha);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return sphere;
    }

    static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v;
    }
}
