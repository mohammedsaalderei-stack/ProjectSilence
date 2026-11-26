using System.Linq;
using UnityEngine;
public static class GroundFinder
{
    public static bool Find(Vector3 wanted, float maxDistance, float radius, float height, out Vector3 spot)
    {
        float step = Mathf.Max(0.5f, radius * 1.5f);
        for (float ring = 0f; ring <= maxDistance; ring += step)
        {
            int tries = ring == 0f ? 1 : Mathf.Max(8, Mathf.RoundToInt(2f * Mathf.PI * ring / step));
            for (int k = 0; k < tries; k++)
            {
                float angle = k * Mathf.PI * 2f / tries;
                Vector3 candidate = wanted + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * ring;
                RaycastHit[] hits = Physics.RaycastAll(new Vector3(candidate.x, wanted.y + 2.5f, candidate.z), Vector3.down, 6f, ~0, QueryTriggerInteraction.Ignore);
                foreach (RaycastHit hit in hits.Where(h => h.normal.y > 0.6f && Mathf.Abs(h.point.y - wanted.y) < 2.5f).OrderByDescending(h => h.point.y))
                {
                    if (radius > 0f)
                    {
                        float r = radius * 0.6f;
                        Vector3 bottom = hit.point + Vector3.up * (0.5f + r);
                        Vector3 top = hit.point + Vector3.up * Mathf.Max(0.5f + r + 0.05f, height - r);
                        if (Physics.CheckCapsule(bottom, top, r, ~0, QueryTriggerInteraction.Ignore)) continue;
                    }
                    spot = hit.point;
                    return true;
                }
            }
        }
        spot = wanted;
        return false;
    }
}
