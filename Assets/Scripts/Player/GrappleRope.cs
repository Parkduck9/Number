using UnityEngine;

// DECISIONS 16 neon grapple rope, drawn in code (no asset): a thin cyan core over a wider low-alpha glow, two stretched 1x1 squares
// from the hand to the tip, on the FX sorting layer. Scaled time like SpriteFx, so it freezes with the hit-stop.
// PlayerController drives it: Shoot (out to the target; a miss snaps back by itself), Hide (release / cancel).
public class GrappleRope : MonoBehaviour
{
    static readonly Color Core = new Color(0.31f, 0.94f, 1f);          // #4FF0FF
    static readonly Color Glow = new Color(0.31f, 0.94f, 1f, 0.28f);
    const float CoreWidth = 2f / 32f, GlowWidth = 6f / 32f;          // 2 px core, 6 px glow at PPU 32

    PlayerController player;
    SpriteRenderer core, glow;
    Vector2 target;
    bool active, hit;
    float t;

    public static GrappleRope Create(PlayerController player)
    {
        var rope = new GameObject("GrappleRope").AddComponent<GrappleRope>();
        rope.player = player;
        rope.glow = rope.Part("Glow", 120);
        rope.core = rope.Part("Core", 121);
        rope.SetVisible(false);
        return rope;
    }

    SpriteRenderer Part(string name, int order)
    {
        var sr = new GameObject(name).AddComponent<SpriteRenderer>();
        sr.transform.SetParent(transform, false);
        sr.sprite = ParryFx.Square;
        sr.sortingLayerID = SortingLayer.NameToID("FX"); // 0 (Default) if missing
        sr.sortingOrder = order;
        return sr;
    }

    public void Shoot(Vector2 to, bool attached)
    {
        target = to;
        hit = attached;
        t = 0f;
        active = true;
        SetVisible(true);
        Draw();
    }

    public void Hide()
    {
        active = false;
        SetVisible(false);
    }

    void SetVisible(bool on) { core.enabled = glow.enabled = on; }

    void LateUpdate() // after the player moved this frame, so the rope leaves the hand
    {
        if (!active) return;
        if (player == null) { Destroy(gameObject); return; }
        float prevT = t;
        t += Time.deltaTime;
        if (hit && prevT < DashRules.GrappleExtendTime && t >= DashRules.GrappleExtendTime) MomentFx.GrappleHit(target); // hook just attached
        if (!hit && t >= DashRules.GrappleExtendTime + DashRules.GrappleRetractTime) { Hide(); return; }
        Draw();
    }

    void OnDestroy() { if (core != null) Destroy(core.gameObject); if (glow != null) Destroy(glow.gameObject); }

    void Draw()
    {
        Vector2 from = player.HandPosition;
        // Out over GrappleExtendTime; a miss then comes back over GrappleRetractTime.
        float k = Mathf.Clamp01(t / DashRules.GrappleExtendTime);
        if (!hit && t > DashRules.GrappleExtendTime) k = 1f - Mathf.Clamp01((t - DashRules.GrappleExtendTime) / DashRules.GrappleRetractTime);
        Vector2 tip = Vector2.Lerp(from, target, k);
        Vector2 d = tip - from;
        float len = d.magnitude;
        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        float flicker = 0.85f + 0.15f * Mathf.PerlinNoise(Time.time * 30f, 0.37f); // neon buzz
        Place(core, from, d, len, angle, CoreWidth, Core, flicker);
        Place(glow, from, d, len, angle, GlowWidth, Glow, flicker);
    }

    static void Place(SpriteRenderer sr, Vector2 from, Vector2 d, float len, float angle, float width, Color c, float flicker)
    {
        sr.transform.SetPositionAndRotation(from + d * 0.5f, Quaternion.Euler(0f, 0f, angle));
        sr.transform.localScale = new Vector3(Mathf.Max(len, 0.001f), width, 1f);
        c.a *= flicker;
        sr.color = c;
    }
}
