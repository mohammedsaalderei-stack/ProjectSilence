using UnityEngine;
public class FloatingText : MonoBehaviour
{
    TextMesh text;
    Color color;
    float age;
    Vector3 start;
    public static void Show(Vector3 position, string value, Color color, float size = 1f)
    {
        var go = new GameObject("Floating Text " + value);
        go.transform.position = position;
        TextMesh text = go.AddComponent<TextMesh>();
        text.text = value;
        text.fontSize = 80;
        text.characterSize = 0.045f * size;
        text.anchor = TextAnchor.MiddleCenter;
        text.fontStyle = FontStyle.Bold;
        text.color = color;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        go.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
        FloatingText floating = go.AddComponent<FloatingText>();
        floating.text = text;
        floating.color = color;
        floating.start = position;
    }
    void Update()
    {
        age += Time.deltaTime;
        float t = age / 1.1f;
        float pop = t < 0.15f ? Mathf.Lerp(0.6f, 1.3f, t / 0.15f) : Mathf.Lerp(1.3f, 1f, Mathf.Clamp01((t - 0.15f) / 0.15f));
        transform.localScale = Vector3.one * pop;
        transform.position = start + Vector3.up * (0.6f * t);
        if (Camera.main != null) transform.rotation = Camera.main.transform.rotation;
        text.color = new Color(color.r, color.g, color.b, Mathf.Clamp01(1.6f - t * 1.6f));
        if (t >= 1f) Destroy(gameObject);
    }
}
