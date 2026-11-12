using System.Collections.Generic;
using UnityEngine;

// Elite clash duel (user design 11-10). An elite (TestEnemy.Kind.Elite) that would attack a grounded player in range starts it:
// everything else is held (GameFlow.EnemiesHeld; the elite is exempt via TestEnemy.InDuel), the player is input-locked (LMB is read
// here through PlayerInputReader.AttackPressedRaw), the camera punches in one PPU step, letterbox bars slide in.
// The head number N (3..9, never counted down) = exchanges. Exchange k: the elite strikes at S_k (ParryRing tell; LMB within
// -WindowEarly / +WindowLate = clang, one press per strike, a stray press locks presses out for Lockout), then the hero's automatic
// counter at S_k + gap/2 that the elite parries (second clang, presentation only). Clang pitch +1 semitone per exchange.
// All N parried: finisher (blink through, shatter, hit-stop, shake), a kill. Any miss: a normal hit, duel over, the elite back to its AI.
// Close-in (DECISIONS 49): over CloseTime both slide toward each other to FightGap centre to centre (each stops short of walls and
// ledges; if one can't move the other covers more). The rest of the world goes out of focus (DuelFocusFx).
// Scene object built on demand. Scaled time (a hit-stop freezes it); the bars and the camera ease on real time.
// The foe is an IDuelFoe: the elite (TestEnemy). (The boss stuck blade used it too until DECISIONS 36 made it a mash window.)
public interface IDuelFoe
{
    Transform transform { get; }       // body centre (feet + 1, like TestEnemy)
    bool isActiveAndEnabled { get; }
    bool InDuel { get; set; }
    bool DuelReady { get; }            // false mid-duel = something else took it (dice freeze, ...): the duel ends
    SpriteRenderer Visual { get; }     // nudged / shattered; null = none
    void DuelPose(int frame, bool flash);
    void EndDuel();                    // the player missed
    void DuelFinish();                 // all N parried
}

public class EliteDuel : MonoBehaviour
{
    const float StartRange = 2.5f, StartDy = 1f;        // (T) u, centre to centre
    const float FightGap = 1.1f;                         // (T) u centre to centre after the close-in (blades meet)
    const float CloseTime = 0.15f;                       // (T) s, ease-out
    const float WallSkin = 0.02f, StepUp = 0.1f;         // u: stop short of a wall; hits this close above the feet are floor seams
    const float EdgeStep = 0.05f, EdgeMargin = 0.2f;     // u: ledge probes along the slide, ahead of the body centre
    const float FirstHit = 0.9f;                         // (T) s from the start to the first elite strike
    static readonly float[] Gaps = { 0.8f, 0.75f, 0.7f, 0.65f, 0.6f, 0.56f, 0.52f }; // (T) s between elite strikes, N = 3..9
    const float WindowEarly = 0.25f, WindowLate = 0.1f;  // (T) LMB parry window around each strike
    const float Lockout = 0.25f;                         // (T) s
    const float FocusPpuMul = 1.5f;                      // PPU 32 -> 48 in one step
    const float FocusFlash = 0.06f;
    const float FinishHitStop = 0.15f, FinishHold = 0.3f, CamEaseOut = 0.35f; // (T) s
    const float ShakePx = 4f, ShakeTime = 0.3f;
    const float Px = 1f / 32f;
    const int PipSize = 3, PipStep = 6, PipBelowFeet = 5; // px
    static readonly Color PipOff = new Color32(0x3A, 0x34, 0x44, 0xFF);
    static readonly Color PipOn = new Color32(0xF2, 0xC9, 0x4C, 0xFF); // talisman yellow
    static readonly Color PipRim = new Color32(0x14, 0x0C, 0x10, 0xFF);
    static readonly Color DigitRed = new Color(1f, 0.1647f, 0.1647f); // EnemyNumber melee red

    static EliteDuel inst;
    static Material mat;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { inst = null; mat = null; } // Domain Reload is off

    IDuelFoe elite;
    PlayerController player;
    PlayerInputReader input;
    ParryRing ring;
    DuelLetterbox bars;
    DuelFocusFx focus;
    Rigidbody2D pBody, eBody;
    float pFromX, pToX, eFromX, eToX, closeAt = -1f; // closeAt = Time.time the close-in started (-1 = not sliding)
    static readonly RaycastHit2D[] castHits = new RaycastHit2D[8];
    static readonly List<Collider2D> bodyCols = new List<Collider2D>(4);
    readonly SpriteRenderer[] pipRim = new SpriteRenderer[9], pipCore = new SpriteRenderer[9];
    readonly int[] pipPopFrame = new int[9];
    int n, k, face;             // face = elite facing (toward the player)
    float gap, t, lockedUntil, ringFrom, lastStrikeAt = -10f, lastGuardAt = -10f, finishedAt = -1f;
    bool struck, parried, ownHeld, ownLock;
    ParryHeight height;         // current exchange's strike height (rolled per exchange; presentation only)

    float StrikeAt(int i) => FirstHit + i * gap;
    bool HasFoe => elite is Object o && o != null; // Unity null check (an interface reference skips it)

    // TestEnemy (elite, Approach, in range): true = the duel started.
    public static bool TryBegin(TestEnemy e, PlayerController p) => TryBegin(e, e.Number.Value, p);

    // n = exchanges (the foe's number, 3..9).
    public static bool TryBegin(IDuelFoe e, int n, PlayerController p)
    {
        if ((inst != null && inst.HasFoe) || p == null || GameFlow.InputLocked || GameFlow.EnemiesHeld || PauseMenu.IsOpen) return false;
        Vector2 d = p.transform.position - e.transform.position;
        if (Mathf.Abs(d.x) > StartRange || Mathf.Abs(d.y) > StartDy || !p.Grounded) return false;
        PlayerController.State s = p.CurrentState;
        if (s != PlayerController.State.Locomotion && s != PlayerController.State.Attack && s != PlayerController.State.Parry) return false;
        var h = p.GetComponent<PlayerHealth>();
        if (h == null || h.IsDead || h.Invulnerable) return false;
        if (inst == null) inst = new GameObject("EliteDuel").AddComponent<EliteDuel>();
        inst.Begin(e, n, p);
        return true;
    }

    void Begin(IDuelFoe e, int number, PlayerController p)
    {
        elite = e;
        player = p;
        input = p.GetComponent<PlayerInputReader>();
        ring = ParryRing.For(e.transform.gameObject);
        if (bars == null) bars = DuelLetterbox.Create(transform);
        n = Mathf.Clamp(number, 3, 9);
        gap = Gaps[n - 3];
        t = ringFrom = 0f;
        k = 0;
        struck = parried = false;
        height = ParryRing.RandomHeight();
        lockedUntil = lastStrikeAt = lastGuardAt = -10f;
        finishedAt = -1f;
        if (!GameFlow.EnemiesHeld) { GameFlow.EnemiesHeld = true; ownHeld = true; }
        if (!GameFlow.InputLocked) { GameFlow.InputLocked = true; ownLock = true; }
        face = p.transform.position.x >= e.transform.position.x ? 1 : -1;
        e.InDuel = true;
        p.DuelBegin(-face);
        PlanCloseIn(e, p);
        Vector2 eAt = new Vector2(eToX, e.transform.position.y), pAt = new Vector2(pToX, p.transform.position.y);
        BuildPips(eAt);
        bars.Show(true);
        bars.Flash(FocusFlash);
        if (focus == null) focus = DuelFocusFx.Create(transform);
        focus.Begin(p.transform, e.transform, transform);
        CameraFollow2D.DuelFocus((eAt + pAt) * 0.5f + new Vector2(0f, 0.25f), FocusPpuMul);
        Sfx.Play(SfxId.LastTell);
    }

    // Split the gap beyond FightGap between the two (half each; the room one lacks goes to the other). Never pushes them apart.
    void PlanCloseIn(IDuelFoe e, PlayerController p)
    {
        pBody = p.Body;
        eBody = e.transform.GetComponent<Rigidbody2D>();
        pFromX = pToX = p.transform.position.x;
        eFromX = eToX = e.transform.position.x;
        closeAt = -1f;
        float need = Mathf.Abs(pFromX - eFromX) - FightGap;
        if (pBody == null || eBody == null || need <= 0f) return;
        int ground = LayerMask.GetMask("Ground");
        float pRoom = Room(pBody, -face, need, ground), eRoom = Room(eBody, face, need, ground);
        float pm = Mathf.Min(pRoom, need * 0.5f);
        float em = Mathf.Min(eRoom, need - pm);
        pm = Mathf.Min(pRoom, need - em);
        pToX = pFromX - face * pm; // the player is on the +face side
        eToX = eFromX + face * em;
        pBody.linearVelocity = eBody.linearVelocity = Vector2.zero;
        closeAt = Time.time;
    }

    // How far (<= max) the body can slide along dir: short of a wall (ground layer, not one-way, above the feet) and of a ledge
    // (ground under the centre + EdgeMargin all the way).
    static float Room(Rigidbody2D body, int dir, float max, int mask)
    {
        body.GetAttachedColliders(bodyCols);
        bool any = false;
        Bounds b = default;
        foreach (Collider2D c in bodyCols)
        {
            if (c.isTrigger) continue;
            if (!any) { b = c.bounds; any = true; } else b.Encapsulate(c.bounds);
        }
        if (!any) return 0f;
        float feet = b.min.y, room = max;
        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(mask);
        int n = body.Cast(new Vector2(dir, 0f), filter, castHits, max + WallSkin);
        for (int i = 0; i < n; i++)
        {
            RaycastHit2D h = castHits[i];
            if (h.collider.usedByEffector || Mathf.Abs(h.normal.y) >= 0.5f || h.collider.bounds.max.y < feet + StepUp) continue; // one-way / floor / tile seam
            room = Mathf.Min(room, Mathf.Max(0f, h.distance - WallSkin));
        }
        float x0 = body.position.x;
        for (float s = 0f; ; s += EdgeStep)
        {
            float sc = Mathf.Min(s, room);
            if (Physics2D.Raycast(new Vector2(x0 + dir * (sc + EdgeMargin), feet + 0.1f), Vector2.down, 0.35f, mask).collider == null)
                return Mathf.Max(0f, sc - EdgeStep);
            if (sc >= room) return room;
        }
    }

    // The close-in slide (physics step, so the interpolated bodies move smoothly). Velocities zeroed: nothing else pushes them.
    void FixedUpdate()
    {
        if (closeAt < 0f || !HasFoe) return;
        float u = Mathf.Clamp01((Time.time - closeAt) / CloseTime);
        float w = 1f - (1f - u) * (1f - u) * (1f - u);
        Slide(pBody, Mathf.Lerp(pFromX, pToX, w));
        Slide(eBody, Mathf.Lerp(eFromX, eToX, w));
        if (u >= 1f) closeAt = -1f;
    }

    static void Slide(Rigidbody2D b, float x)
    {
        if (b == null) return;
        b.linearVelocity = Vector2.zero;
        b.MovePosition(new Vector2(x, b.position.y));
    }

    void Update()
    {
        if (!HasFoe) return;
        if (player == null || GameFlow.IsPlayerDead(player) || !elite.isActiveAndEnabled) { Release(); return; } // death / despawn: end cleanly
        t += Time.deltaTime;
        if (finishedAt >= 0f)
        {
            UpdatePips();
            if (t >= finishedAt + FinishHold) Release();
            return;
        }
        if (!elite.DuelReady) { Release(); return; } // a dice skill froze / struck it mid-duel (Fable review)
        if (k < n)
        {
            float s = StrikeAt(k);
            if (input != null && input.AttackPressedRaw)
            {
                if (t < lockedUntil) { } // locked out: ignored
                else if (!parried && t >= s - WindowEarly && t - Time.deltaTime <= s + WindowLate) { parried = true; if (struck) PlayerClang(); } // judged over the frame interval: a WebGL hitch can't eat the press
                else if (t < s - WindowEarly || t > s + WindowLate) lockedUntil = t + Lockout; // no strike in its window: anti-mash (a repeat inside a parried window is free)
            }
            if (!struck && t >= s)
            {
                struck = true;
                lastStrikeAt = t;
                ring.Flash();
                ParryFx.NudgeVisual(elite.Visual != null ? elite.Visual.transform : null, face);
                if (parried) PlayerClang();
            }
            if (struck && !parried && t > s + WindowLate) { Miss(); return; }
            if (struck && parried && t >= s + gap * 0.5f) Counter();
            else if (!struck) ring.Show(Contact(), face, s - ringFrom, s - t, t >= s - WindowEarly);
        }
        else if (t >= StrikeAt(n - 1) + gap) { Finish(); return; }
        float sinceStrike = t - lastStrikeAt;
        int frame = sinceStrike < 0.12f ? 2 : sinceStrike < 0.2f ? 3 : k < n && !struck && StrikeAt(k) - t <= 0.1f ? 1 : 0;
        elite.DuelPose(frame, t - lastGuardAt < 0.06f);
        UpdatePips();
    }

    // Where the blades meet: the player's front edge at the elite's body height + the strike height (like the normal melee rings).
    Vector2 Contact() => new Vector2(player.transform.position.x - face * 0.45f, elite.transform.position.y + ParryRing.Offset(height));

    static float Semitone(int i) => Mathf.Pow(2f, i / 12f);

    void PlayerClang()
    {
        Vector2 c = Contact();
        player.DuelClash(false, height);
        SpriteFx.Play("parry_talisman", c, face > 0, 102); // a blow from the left (elite on the left, face > 0): flipped
        Sfx.Play(SfxId.Parry, 1f, Semitone(k));
        Juice.Shake(Juice.ShakeHitAmp, Juice.ShakeHitTime);
        pipPopFrame[k] = Time.frameCount;
    }

    // The hero's automatic counter swing, parried by the elite. Ends exchange k.
    void Counter()
    {
        Vector2 c = Contact();
        player.DuelClash(true, height);
        ParryFx.Slash(c, null, -face);
        SpriteFx.Play("parry_talisman", c, face < 0, 102); // the hero's blow, met from the elite's side
        Sfx.Play(SfxId.Parry, 0.8f, Semitone(k));
        lastGuardAt = t;
        k++;
        struck = parried = false;
        height = ParryRing.RandomHeight();
        ringFrom = t;
    }

    void Miss()
    {
        IDuelFoe e = elite;
        Release();
        e.EndDuel();
        player.ReceiveEnemyHit(new HitInfo { Damage = 1, SourcePosition = e.transform.position, Unparryable = true }); // the parry already failed here
    }

    void Finish()
    {
        finishedAt = t;
        Vector2 ep = elite.transform.position;
        int dir = -face; // the hero's swing: toward the elite
        Vector2 blow = new Vector2(dir, 0f);
        if (elite is TestEnemy te) player.TryBlinkBehind(te); // the hero dashes through and ends behind it, facing back
        ParryFx.Slash(ep, null, dir);
        ShatterFx.Play(elite.Visual, blow, ep + new Vector2(face * 0.95f, 1.05f), DigitRed);
        SpriteFx.Play("kill-burst", ep, dir < 0, 102);
        elite.DuelFinish(); // the kill (normal down state); number collapses
        ring.Hide();
        Sfx.Play(SfxId.Hit);
        Sfx.Play(SfxId.Parry, 1f, Semitone(n));
        player.DuelHitStop(FinishHitStop);
        CameraFollow2D.Shake(ShakePx * Px, ShakeTime, blow);
    }

    // Flags back (only those this duel set), camera out, bars out, pips and rings gone. Safe to call twice.
    void Release()
    {
        if (ownHeld) { ownHeld = false; GameFlow.EnemiesHeld = false; }
        if (ownLock) { ownLock = false; GameFlow.InputLocked = false; }
        closeAt = -1f;
        CameraFollow2D.DuelUnfocus(CamEaseOut);
        if (bars != null) bars.Show(false);
        if (focus != null) focus.End();
        for (int i = 0; i < pipRim.Length; i++) if (pipRim[i] != null) pipRim[i].enabled = pipCore[i].enabled = false;
        if (ring != null) ring.Hide();
        if (HasFoe) elite.InDuel = false;
        elite = null;
    }

    void OnDestroy()
    {
        Release(); // scene unload mid-duel: never leave the game held / locked
        if (inst == this) inst = null;
    }

    // N pips under the elite's feet, centred, on the pixel grid (odd sizes: centres on texel centres).
    void BuildPips(Vector2 at)
    {
        if (mat == null) mat = new Material(Shader.Find("Sprites/Default"));
        float x0 = at.x - (n - 1) * PipStep * Px * 0.5f, y = at.y - 1f - PipBelowFeet * Px;
        for (int i = 0; i < pipRim.Length; i++)
        {
            if (pipRim[i] == null)
            {
                pipRim[i] = MakePip(PipSize + 2, 61);
                pipCore[i] = MakePip(PipSize, 62);
            }
            bool on = i < n;
            pipRim[i].enabled = pipCore[i].enabled = on;
            pipPopFrame[i] = -1;
            if (!on) continue;
            Vector3 p = new Vector3((Mathf.Floor((x0 + i * PipStep * Px) * 32f) + 0.5f) * Px, (Mathf.Floor(y * 32f) + 0.5f) * Px, 0f);
            pipRim[i].transform.position = pipCore[i].transform.position = p;
            pipRim[i].color = PipRim;
            pipCore[i].color = PipOff;
        }
    }

    SpriteRenderer MakePip(int px, int order)
    {
        var go = new GameObject("DuelPip");
        go.transform.SetParent(transform, false);
        go.transform.localScale = new Vector3(px * Px, px * Px, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ParryFx.Square;
        sr.sharedMaterial = mat;
        sr.sortingLayerName = "FX";
        sr.sortingOrder = order;
        return sr;
    }

    // Lit = talisman yellow, the frame it lights = white pop.
    void UpdatePips()
    {
        for (int i = 0; i < n; i++)
            pipCore[i].color = pipPopFrame[i] < 0 ? PipOff : pipPopFrame[i] == Time.frameCount ? Color.white : PipOn;
    }
}
