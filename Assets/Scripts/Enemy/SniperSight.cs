using UnityEngine;

// Sniper laser sight (DECISIONS 15): a code-drawn red line from the muzzle. Tracking = thin and faint; locked = brighter, thicker, flashing.
// Owned by TestEnemy (Kind.Sniper), built on first use. Removal: delete this file and the Kind.Sniper branches in TestEnemy.
public class SniperSight : MonoBehaviour
{
    const float TrackAlpha = 0.45f, TrackWidth = 1f / 32f;     // (T) 1 px
    const float LockWidth = 2f / 32f, LockFlashHz = 20f;       // (T) 2 px, alternates full / LockDimAlpha
    const float LockDimAlpha = 0.55f;
    static readonly Color LaserRed = new Color(1f, 0.1647f, 0.1647f); // #FF2A2A
    static Material mat;

    LineRenderer line;

    public static SniperSight For(GameObject owner)
    {
        var s = owner.GetComponent<SniperSight>();
        return s != null ? s : owner.AddComponent<SniperSight>();
    }

    void Build()
    {
        line = GetComponentInChildren<LineRenderer>(true); // a clone made while aiming already has one
        if (line != null) return;
        if (mat == null) mat = new Material(Shader.Find("Sprites/Default"));
        var go = new GameObject("SniperLaser");
        go.transform.SetParent(transform, false);
        line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.sharedMaterial = mat;
        line.sortingOrder = 45; // over the world, under projectiles (50)
    }

    // from/to in world space. locked = the aim is fixed and the shot is coming.
    public void Show(Vector2 from, Vector2 to, bool locked)
    {
        if (line == null) Build();
        line.enabled = true;
        line.SetPosition(0, from);
        line.SetPosition(1, to);
        Color c = locked ? Color.Lerp(LaserRed, Color.white, 0.35f) : LaserRed;
        c.a = locked ? (Mathf.FloorToInt(Time.time * LockFlashHz) % 2 == 0 ? 1f : LockDimAlpha) : TrackAlpha;
        line.startColor = line.endColor = c;
        line.startWidth = line.endWidth = locked ? LockWidth : TrackWidth;
    }

    public void Hide()
    {
        if (line != null) line.enabled = false;
    }
}
