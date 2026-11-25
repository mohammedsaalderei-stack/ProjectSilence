using UnityEngine;

// =====================================================================================
// NpcTalk.cs - written with AI help (Claude, 25 Nov 2026). List it in READ THIS DOCTOR.txt.
//
// Someone you can talk to: walk close and press E (or Enter / gamepad A).
// AdventureDirector shows the prompt and the dialogue box. A child sprite always faces the camera.
// =====================================================================================
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

        // Stand on real ground near where the NPC was placed.
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
