using UnityEngine;
public class NpcTalk : MonoBehaviour
{
    public string npcName = "Villager";
    [TextArea(2, 5)] public string[] lines = { "Hello, Maestro." };
    [Tooltip("Talking to this person restores the party's HP and MP.")]
    public bool healsParty;
    public float talkRange = 2.5f;
    Transform sprite;
    void Start()
    {
        SpriteRenderer found = GetComponentInChildren<SpriteRenderer>();
        if (found != null) sprite = found.transform;
        Collider body = GetComponent<Collider>();
        if (body != null) body.enabled = false;
        float size = transform.lossyScale.y;
        if (GroundFinder.Find(transform.position, 8f * size, 0f, 1.75f * size, out Vector3 ground)) transform.position = ground;
        else Debug.LogWarning(name + ": no walkable ground nearby - move it in the scene.");
        if (body != null) body.enabled = true;
    }
    void LateUpdate()
    {
        if (sprite != null && Camera.main != null) sprite.rotation = Camera.main.transform.rotation;
    }
}
