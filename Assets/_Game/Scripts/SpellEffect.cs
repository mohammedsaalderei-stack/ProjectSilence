using System.Collections;
using UnityEngine;

public class SpellEffect : MonoBehaviour
{
    float life;
    float age;
    float maxSize;
    Color color;
    Material material;
    Light flash;

    public static IEnumerator Fly(Unit caster, Unit target, float duration = 0.5f)
    {
        Color color = caster.spellColor;
        Vector3 to = target.Motion.BodyCentre;
        Vector3 from = caster.Motion.BodyCentre + Vector3.up * 0.3f;
        from += Flat(to - from).normalized * 0.5f;

        GameObject orb = MakeGlowSphere("Spell Orb", color, 0.35f, 0.9f);
        orb.transform.position = from;

        Light light = orb.AddComponent<Light>();
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
            position.y += Mathf.Sin(t * Mathf.PI) * 0.8f;
            orb.transform.position = position;
            orb.transform.localScale = Vector3.one * 0.35f * (1f + 0.15f * Mathf.Sin(Time.time * 40f));
            yield return null;
        }

        Destroy(orb);
        target.Motion.HitFrom(from);
        Burst(to, color);
    }

    public static IEnumerator Arrow(Unit shooter, Unit target, float duration = 0.3f)
    {
        Vector3 to = target.Motion.BodyCentre;
        Vector3 from = shooter.Motion.BodyCentre + Vector3.up * 0.2f;
        from += Flat(to - from).normalized * 0.4f;
        Color color = new Color(1f, 0.95f, 0.75f);

        GameObject arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arrow.name = "Arrow";
        Destroy(arrow.GetComponent<Collider>());
        arrow.transform.localScale = new Vector3(0.05f, 0.05f, 0.75f);
        Renderer renderer = arrow.GetComponent<Renderer>();
        Shader unlit = Shader.Find("Sprites/Default");
        if (unlit != null) renderer.material = new Material(unlit);
        renderer.material.color = color;
        TrailRenderer trail = arrow.AddComponent<TrailRenderer>();
        trail.material = renderer.material;
        trail.time = 0.15f;
        trail.startWidth = 0.08f;
        trail.endWidth = 0f;
        trail.startColor = color;
        trail.endColor = new Color(color.r, color.g, color.b, 0f);
        GameAudio.Play(GameSounds.Slots.arrow);

        Vector3 previous = from;
        arrow.transform.position = from;
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / duration);
            Vector3 position = Vector3.Lerp(from, to, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.35f);
            arrow.transform.position = position;
            if ((position - previous).sqrMagnitude > 0.000001f) arrow.transform.rotation = Quaternion.LookRotation(position - previous);
            previous = position;
            yield return null;
        }
        Destroy(arrow);
        target.Motion.HitFrom(from);
        Burst(to, color, 0.8f);
    }

    public static void Burst(Vector3 position, Color color, float size = 1.8f)
    {
        GameObject burst = MakeGlowSphere("Spell Burst", color, 0.3f, 0.8f);
        burst.transform.position = position;

        SpellEffect effect = burst.AddComponent<SpellEffect>();
        effect.life = 0.45f;
        effect.maxSize = size;
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
        float grow = 1f - (1f - t) * (1f - t);
        transform.localScale = Vector3.one * Mathf.Lerp(0.3f, maxSize, grow);
        material.color = new Color(color.r, color.g, color.b, 0.8f * (1f - t));
        flash.intensity = 8f * (1f - t);
        if (t >= 1f) Destroy(gameObject);
    }

    static GameObject MakeGlowSphere(string name, Color color, float size, float alpha)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = name;
        Destroy(sphere.GetComponent<Collider>());
        sphere.transform.localScale = Vector3.one * size;

        Renderer renderer = sphere.GetComponent<Renderer>();
        Shader unlit = Shader.Find("Sprites/Default");
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
