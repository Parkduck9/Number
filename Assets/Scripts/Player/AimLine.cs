using System.Collections.Generic;
using UnityEngine;

// DECISIONS 22 aim line: a faint dotted pixel line from the throwing hand toward the mouse so the player sees where the RMB issen
// (and the lunge) points. 2 px on / 2 px off, 1 px wide, pale #FFF1D6 at low alpha, stops at the issen range (IssenRange) or a few
// px short of the mouse crosshair (MouseCrosshair draws the cursor itself). Hidden while input is locked (dialogue, elite duel, dice),
// paused or dead. Pooled 1 px dots on the 1/32 u pixel grid, unlit (Sprites/Default) on the FX layer. Added by PlayerController.Awake.
[DefaultExecutionOrder(1000)] // after the camera follow, like MouseCrosshair
public class AimLine : MonoBehaviour
{
    const float Ppu = 32f;
    static readonly Color Dot = new Color(1f, 0.945f, 0.839f, 0.25f); // #FFF1D6
    const int On = 2, Period = 4;     // (T) 2 px on, 2 px off
    const int CursorGap = 6;          // (T) px: stop short of the mouse (crosshair half-size)
    const int MaxDots = (int)(DashRules.IssenRange * Ppu) / Period * On + On;

    static Material mat;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { mat = null; }

    PlayerController player;
    Transform root;
    readonly List<SpriteRenderer> pool = new List<SpriteRenderer>(MaxDots);
    int used;

    void Awake()
    {
        player = GetComponent<PlayerController>();
        if (mat == null) mat = new Material(Shader.Find("Sprites/Default"));
        root = new GameObject("AimLine").transform; // own object: the player moves / flips
    }

    void OnDestroy() { if (root != null) Destroy(root.gameObject); }
    void OnDisable() { used = 0; Flush(); }

    void LateUpdate()
    {
        used = 0;
        if (!GameFlow.InputLocked && !PauseMenu.IsOpen && player != null && player.CurrentState != PlayerController.State.Dead
            && !GameFlow.IsPlayerDead(player) && MouseCrosshair.MouseWorld(out Vector2 mouse))
            Draw(mouse);
        Flush();
    }

    void Draw(Vector2 mouse)
    {
        // Hand on the side the mouse is on.
        float dxm = mouse.x - player.transform.position.x;
        int face = Mathf.Abs(dxm) > 0.01f ? (dxm > 0f ? 1 : -1) : player.Facing;
        Vector2 hand = (Vector2)player.transform.position + new Vector2(face * 0.35f, 0.25f);
        Vector2 d = mouse - hand;
        float len = d.magnitude;
        if (len < 0.01f) return;
        Vector2 end = hand + d * (Mathf.Min(len - CursorGap / Ppu, DashRules.IssenRange) / len);

        // Walk the pixel grid along the major axis (Chebyshev steps), dash pattern counted from the hand so it doesn't crawl.
        int x0 = Mathf.FloorToInt(hand.x * Ppu), y0 = Mathf.FloorToInt(hand.y * Ppu);
        int x1 = Mathf.FloorToInt(end.x * Ppu), y1 = Mathf.FloorToInt(end.y * Ppu);
        int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
        if ((end - hand).sqrMagnitude < 0.0001f || Vector2.Dot(end - hand, d) <= 0f) return; // mouse inside the gap
        for (int i = 0; i <= steps; i++)
        {
            if (i % Period >= On) continue;
            float u = steps > 0 ? i / (float)steps : 0f;
            float px = Mathf.Round(x0 + (x1 - x0) * u), py = Mathf.Round(y0 + (y1 - y0) * u);
            Place(new Vector3((px + 0.5f) / Ppu, (py + 0.5f) / Ppu, 0f));
        }
    }

    void Place(Vector3 p)
    {
        if (used == pool.Count)
        {
            if (pool.Count >= MaxDots) return;
            var sr = new GameObject("Dot").AddComponent<SpriteRenderer>();
            sr.transform.SetParent(root, false);
            sr.transform.localScale = new Vector3(1f / Ppu, 1f / Ppu, 1f);
            sr.sprite = ParryFx.Square;
            sr.sharedMaterial = mat; // unlit: 2D lights must not dim it
            sr.color = Dot;
            sr.sortingLayerID = SortingLayer.NameToID("FX"); // 0 (Default) if missing
            sr.sortingOrder = 990; // under the crosshair (1000)
            pool.Add(sr);
        }
        SpriteRenderer s = pool[used++];
        s.transform.position = p;
        s.enabled = true;
    }

    void Flush()
    {
        if (root == null) return; // scene unload: the dots may already be gone
        for (int i = used; i < pool.Count; i++) if (pool[i].enabled) pool[i].enabled = false;
    }
}
