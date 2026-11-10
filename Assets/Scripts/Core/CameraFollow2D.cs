using UnityEngine;
using UnityEngine.Rendering.Universal;

// Smooth 2D follow with a dead-zone, look-ahead in the facing direction and level-bounds clamp.
// Facing is derived from the target's own horizontal movement. Vertical: while the target (a PlayerController, read only) is in the
// air the camera holds the last ground height, so hops don't bob the view; falls lead downward; a landing eases the view in.
// The follow runs on an unsnapped position; the transform gets that position snapped to the pixel grid plus the (pixel) shake.
[RequireComponent(typeof(Camera))]
public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] Transform target;
    [Tooltip("Focus point offset from the target pivot (world units).")]
    [SerializeField] Vector2 offset = new Vector2(0f, 1f);
    [Tooltip("Half-size of the dead-zone around the camera center (world units). Target moves freely inside.")]
    [SerializeField] Vector2 deadZone = new Vector2(0.75f, 0.5f);
    [SerializeField] float smoothTime = 0.15f;
    // Look-ahead distance / smoothing: Juice.LookAhead*. Vertical follow: Juice.Cam*.
    [SerializeField] bool useBounds = true;
    [Tooltip("Level rectangle in world units (x, y = bottom-left). The camera view stays inside it.")]
    [SerializeField] Rect levelBounds = new Rect(-20f, -6f, 60f, 24f);

    Camera cam;
    PixelPerfectCamera ppc;
    PlayerController player; // optional: target's grounded state for the vertical follow (none = plain vertical dead-zone follow)
    float basePpu, baseOrtho;
    Vector2 followPos;       // unsnapped follow state (the transform shows it pixel-snapped)
    Vector2 velocity;
    float lookAhead, lookAheadVelocity;
    float fallLead, fallLeadVelocity;
    float facing = 1f;
    float lastTargetX, lastTargetY;
    float groundY;           // target y at its last grounded frame
    bool wasGrounded = true;

    // Camera shake: decaying random offset added on top of the snapped follow position, in unscaled time.
    static CameraFollow2D active;
    float shakeAmp, shakeTime, shakeLeft;
    Vector2 shakeOffset;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => active = null; // Domain Reload is off

    // Strongest shake wins; a weaker one never cuts a stronger one short.
    // blowDir != 0: Juice.ShakeKick of the amplitude pushes the view along it (a kick that decays), the rest stays random.
    public static void Shake(float amplitude, float duration, Vector2 blowDir = default)
    {
        if (active == null || amplitude <= 0f || duration <= 0f) return;
        // Punch zoom keyed off the shake size (no PlayerController call needed): kill = big directional shake, parry = its own amplitude.
        // Decided before the shake gate: a parry right after a kill / hit still punches.
        if (blowDir.sqrMagnitude > 0.0001f && amplitude >= Juice.ShakeKillAmp - 0.0001f) active.DoPunch(Juice.PunchKill);
        else if (Mathf.Abs(amplitude - Juice.ShakeParryAmp) < 0.0001f) active.DoPunch(Juice.PunchParry);
        float currentAmp = active.shakeLeft > 0f ? active.shakeAmp * active.shakeLeft / active.shakeTime : 0f;
        if (amplitude < currentAmp) return;
        active.shakeAmp = amplitude;
        active.shakeTime = active.shakeLeft = duration;
        active.shakeDir = blowDir.sqrMagnitude > 0.0001f ? blowDir.normalized : Vector2.zero;
    }

    // Brief zoom-in (fraction of the view size, e.g. Juice.PunchKill) that eases back over Juice.PunchTime, real time.
    // Kills and parries already punch through Shake; call this for anything else (dice result, boss phase...).
    public static void Punch(float strength) { if (active != null) active.DoPunch(strength); }

    // Strongest wins, never stacks: a new punch only restarts if it is at least as strong as what is left.
    float punchStrength, punchLeft;
    void DoPunch(float strength)
    {
        if (strength <= 0f || Juice.PunchTime <= 0f) return;
        float current = punchLeft > 0f ? punchStrength * punchLeft / Juice.PunchTime : 0f;
        if (strength < current) return;
        punchStrength = strength;
        punchLeft = Juice.PunchTime;
    }
    Vector2 shakeDir;

    // Elite clash duel (EliteDuel): the view jumps in ONE PPU step to `ppuMul` x base on `point` (pixel-snapped, inside the bounds),
    // holds there, and DuelUnfocus eases position + PPU back to the follow over `ease` s, real time. The follow keeps running underneath.
    float focusW, focusEase, focusMul = 1f;
    bool focusIn;
    Vector2 focusPoint;
    public static void DuelFocus(Vector2 point, float ppuMul)
    {
        if (active == null) return;
        active.focusPoint = point;
        active.focusMul = ppuMul;
        active.focusW = 1f;
        active.focusIn = true;
    }
    public static void DuelUnfocus(float ease)
    {
        if (active == null) return;
        active.focusIn = false;
        active.focusEase = ease;
    }
    float FocusK => focusW * focusW * (3f - 2f * focusW); // smoothstep of the ease-out

    void OnEnable() => active = this;
    void OnDisable() { if (active == this) active = null; }

    void Awake()
    {
        cam = GetComponent<Camera>();
        ppc = GetComponent<PixelPerfectCamera>();
        baseOrtho = cam.orthographicSize;
        if (ppc != null) basePpu = ppc.assetsPPU;
        followPos = transform.position;
    }

    void Start() => Snap();

    public void SetTarget(Transform t) { target = t; Snap(); }
    public void SetBounds(Rect bounds) { levelBounds = bounds; useBounds = true; }

    // Jump to the target immediately (scene start, teleport, respawn).
    public void Snap()
    {
        if (target == null) return;
        player = target.GetComponent<PlayerController>();
        lastTargetX = target.position.x;
        lastTargetY = target.position.y;
        groundY = target.position.y;
        wasGrounded = true;
        lookAhead = fallLead = 0f;
        velocity = Vector2.zero;
        lookAheadVelocity = fallLeadVelocity = 0f;
        shakeOffset = Vector2.zero;
        shakeLeft = 0f;
        punchLeft = 0f;
        focusW = 0f;
        focusIn = false;
        ApplyZoom(0f);
        followPos = Clamp((Vector2)target.position + offset);
        ApplyTransform();
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (target != null && dt > 0f) Follow(dt); // dt is 0 during hit-stop / pause: hold position
        UpdateShake();
        ApplyPunch();
        ApplyTransform();
    }

    // Snapped follow position + pixel shake. Grid = the Pixel Perfect Camera's current PPU (the punch zoom changes it for a moment).
    void ApplyTransform()
    {
        float ppu = ppc != null && ppc.assetsPPU > 0 ? ppc.assetsPPU : (basePpu > 0f ? basePpu : 32f);
        Vector2 f = focusW > 0f ? Vector2.Lerp(followPos, Clamp(focusPoint, basePpu * focusMul), FocusK) : followPos;
        Vector2 p = new Vector2(Mathf.Round(f.x * ppu), Mathf.Round(f.y * ppu)) / ppu + shakeOffset;
        transform.position = new Vector3(p.x, p.y, transform.position.z);
    }

    // Zoom in via the Pixel Perfect Camera's PPU (it owns the orthographic size); plain camera: orthographic size. Real time, so it
    // plays through the hit-stop. Bounds keep using the base view (Clamp), the zoomed view is smaller, so it stays inside.
    void ApplyPunch()
    {
        if (PauseMenu.IsOpen) return;
        punchLeft = Mathf.Max(0f, punchLeft - Time.unscaledDeltaTime);
        if (!focusIn && focusW > 0f) focusW = focusEase > 0f ? Mathf.Max(0f, focusW - Time.unscaledDeltaTime / focusEase) : 0f;
        float k = punchLeft / Juice.PunchTime;
        ApplyZoom(Mathf.Max(punchStrength * k * k, (focusMul - 1f) * FocusK)); // ease-out back to 1; a duel focus wins over a smaller punch
    }

    void ApplyZoom(float amount)
    {
        if (ppc != null && basePpu > 0f) ppc.assetsPPU = Mathf.Max(1, Mathf.RoundToInt(basePpu * (1f + amount)));
        else if (ppc == null) cam.orthographicSize = baseOrtho / (1f + amount);
    }

    void UpdateShake()
    {
        if (PauseMenu.IsOpen) return; // paused: no progress, keep the current offset
        shakeLeft = Mathf.Max(0f, shakeLeft - Time.unscaledDeltaTime);
        float ppu = basePpu > 0 ? basePpu : 32f;
        float a = shakeTime > 0f ? shakeAmp * shakeLeft / shakeTime : 0f;
        float kick = shakeDir == Vector2.zero ? 0f : Juice.ShakeKick;
        float j = a * (1f - kick);
        Vector2 r = shakeDir * (a * kick) + new Vector2(Random.Range(-j, j), Random.Range(-j, j));
        shakeOffset = new Vector2(Mathf.Round(r.x * ppu), Mathf.Round(r.y * ppu)) / ppu; // pixel-snapped
    }

    void Follow(float dt)
    {
        Vector2 t = target.position;
        float vx = (t.x - lastTargetX) / dt;
        float vy = (t.y - lastTargetY) / dt;
        lastTargetX = t.x;
        lastTargetY = t.y;
        if (Mathf.Abs(vx) > Juice.LookAheadFlipSpeed) facing = Mathf.Sign(vx); // only a real move flips the lead (no jitter on stops / turns)
        float lead = Mathf.Abs(vx) > Juice.DashSpeedThreshold ? Juice.LookAheadDash : Juice.LookAhead;
        lookAhead = Mathf.SmoothDamp(lookAhead, facing * lead, ref lookAheadVelocity, Juice.LookAheadSmooth, Mathf.Infinity, dt);

        // Vertical anchor: ground height while airborne (hops stay inside it), the target itself once it rises past the hold or drops below.
        bool grounded = player == null || player.Grounded;
        float fy = t.y;
        if (grounded) groundY = t.y;
        else if (t.y >= groundY) fy = Mathf.Max(groundY, t.y - Juice.CamJumpHold);
        bool fastFall = !grounded && vy < -Juice.CamFallLeadSpeed;
        fallLead = Mathf.SmoothDamp(fallLead, fastFall ? -Juice.CamFallLead : 0f, ref fallLeadVelocity, Juice.LookAheadSmooth, Mathf.Infinity, dt);
        bool landed = grounded && !wasGrounded;
        wasGrounded = grounded;
        if (landed && velocity.y < 0f) velocity.y *= 0.5f; // touch-down: bleed the fall speed so the catch-up eases instead of overshooting

        Vector2 pos = followPos;
        Vector2 focus = new Vector2(t.x + offset.x + lookAhead, fy + offset.y + fallLead);

        // Dead-zone: the desired point only moves once the focus leaves the box.
        Vector2 desired = pos;
        float ex = focus.x - pos.x;
        if (Mathf.Abs(ex) > deadZone.x) desired.x += ex - Mathf.Sign(ex) * deadZone.x;
        float ey = focus.y - pos.y;
        if (Mathf.Abs(ey) > deadZone.y) desired.y += ey - Mathf.Sign(ey) * deadZone.y;

        Vector2 next;
        next.x = Mathf.SmoothDamp(pos.x, desired.x, ref velocity.x, smoothTime, Mathf.Infinity, dt);
        next.y = Mathf.SmoothDamp(pos.y, desired.y, ref velocity.y, grounded ? Juice.CamYSmoothLand : Juice.CamYSmoothAir, Mathf.Infinity, dt);
        Vector2 clamped = Clamp(next);
        if (clamped.x != next.x) velocity.x = 0f; // don't build up speed against a level edge
        if (clamped.y != next.y) velocity.y = 0f;
        followPos = clamped;
    }

    // ppu = 0: the base view; else the view at that PPU (duel focus, smaller).
    Vector2 Clamp(Vector2 p, float ppu = 0f)
    {
        if (!useBounds) return p;
        // With Pixel Perfect Camera (upscale RT + stretch fill) the view is always the reference size, whatever the window aspect.
        float halfH = ppc == null ? baseOrtho : cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        if (ppc != null && basePpu > 0)
        {
            if (ppu <= 0f) ppu = basePpu;
            halfW = ppc.refResolutionX * 0.5f / ppu;
            halfH = ppc.refResolutionY * 0.5f / ppu;
        }
        p.x = ClampAxis(p.x, levelBounds.xMin + halfW, levelBounds.xMax - halfW);
        p.y = ClampAxis(p.y, levelBounds.yMin + halfH, levelBounds.yMax - halfH);
        return p;
    }

    // If the level is smaller than the view on this axis, center on it.
    static float ClampAxis(float v, float min, float max) => min > max ? (min + max) * 0.5f : Mathf.Clamp(v, min, max);

    void OnDrawGizmosSelected()
    {
        if (!useBounds) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(levelBounds.center, levelBounds.size);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(deadZone.x * 2f, deadZone.y * 2f, 0f));
    }
}
