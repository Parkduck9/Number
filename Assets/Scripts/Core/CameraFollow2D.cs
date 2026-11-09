using UnityEngine;
using UnityEngine.Rendering.Universal;

// Smooth 2D follow with a dead-zone, look-ahead in the facing direction and level-bounds clamp.
// Facing is derived from the target's own horizontal movement, so no dependency on PlayerController.
[RequireComponent(typeof(Camera))]
public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] Transform target;
    [Tooltip("Focus point offset from the target pivot (world units).")]
    [SerializeField] Vector2 offset = new Vector2(0f, 1f);
    [Tooltip("Half-size of the dead-zone around the camera center (world units). Target moves freely inside.")]
    [SerializeField] Vector2 deadZone = new Vector2(0.75f, 0.5f);
    [SerializeField] float smoothTime = 0.15f;
    // Look-ahead distance / smoothing: Juice.LookAhead*.
    [SerializeField] bool useBounds = true;
    [Tooltip("Level rectangle in world units (x, y = bottom-left). The camera view stays inside it.")]
    [SerializeField] Rect levelBounds = new Rect(-20f, -6f, 60f, 24f);

    Camera cam;
    PixelPerfectCamera ppc;
    float basePpu, baseOrtho;
    Vector2 velocity;
    float lookAhead, lookAheadVelocity;
    float facing = 1f;
    float lastTargetX;

    // Camera shake: decaying random offset added after the follow step, in unscaled time. Removed again at the start of the next
    // LateUpdate, so it never feeds into the follow position.
    static CameraFollow2D active;
    float shakeAmp, shakeTime, shakeLeft;
    Vector2 shakeOffset;

    // Strongest shake wins; a weaker one never cuts a stronger one short.
    // blowDir != 0: Juice.ShakeKick of the amplitude pushes the view along it (a kick that decays), the rest stays random.
    public static void Shake(float amplitude, float duration, Vector2 blowDir = default)
    {
        if (active == null || amplitude <= 0f || duration <= 0f) return;
        // Punch zoom keyed off the shake size (no PlayerController call needed): kill = big directional shake, parry = its own amplitude.
        // Decided before the shake gate: a parry right after a kill / hit still punches.
        if (blowDir.sqrMagnitude > 0.0001f && amplitude >= Juice.ShakeKillAmp - 0.0001f) active.Punch(Juice.PunchKill);
        else if (Mathf.Abs(amplitude - Juice.ShakeParryAmp) < 0.0001f) active.Punch(Juice.PunchParry);
        float currentAmp = active.shakeLeft > 0f ? active.shakeAmp * active.shakeLeft / active.shakeTime : 0f;
        if (amplitude < currentAmp) return;
        active.shakeAmp = amplitude;
        active.shakeTime = active.shakeLeft = duration;
        active.shakeDir = blowDir.sqrMagnitude > 0.0001f ? blowDir.normalized : Vector2.zero;
    }

    // Strongest wins, never stacks: a new punch only restarts if it is at least as strong as what is left.
    float punchStrength, punchLeft;
    void Punch(float strength)
    {
        if (strength <= 0f || Juice.PunchTime <= 0f) return;
        float current = punchLeft > 0f ? punchStrength * punchLeft / Juice.PunchTime : 0f;
        if (strength < current) return;
        punchStrength = strength;
        punchLeft = Juice.PunchTime;
    }
    Vector2 shakeDir;

    void OnEnable() => active = this;
    void OnDisable() { if (active == this) active = null; }

    void Awake()
    {
        cam = GetComponent<Camera>();
        ppc = GetComponent<PixelPerfectCamera>();
        baseOrtho = cam.orthographicSize;
        if (ppc != null) basePpu = ppc.assetsPPU;
    }

    void Start() => Snap();

    public void SetTarget(Transform t) { target = t; Snap(); }
    public void SetBounds(Rect bounds) { levelBounds = bounds; useBounds = true; }

    // Jump to the target immediately (scene start, teleport, respawn).
    public void Snap()
    {
        if (target == null) return;
        lastTargetX = target.position.x;
        lookAhead = 0f;
        velocity = Vector2.zero;
        lookAheadVelocity = 0f;
        shakeOffset = Vector2.zero;
        punchLeft = 0f;
        ApplyZoom(0f);
        Vector2 p = Clamp((Vector2)target.position + offset);
        transform.position = new Vector3(p.x, p.y, transform.position.z);
    }

    void LateUpdate()
    {
        transform.position -= new Vector3(shakeOffset.x, shakeOffset.y, 0f);
        float dt = Time.deltaTime;
        if (target != null && dt > 0f) Follow(dt); // dt is 0 during hit-stop / pause: hold position
        ApplyShake();
        ApplyPunch();
    }

    // Zoom in via the Pixel Perfect Camera's PPU (it owns the orthographic size); plain camera: orthographic size. Real time, so it
    // plays through the hit-stop. Bounds keep using the base view (Clamp), the zoomed view is smaller, so it stays inside.
    void ApplyPunch()
    {
        if (PauseMenu.IsOpen) return;
        punchLeft = Mathf.Max(0f, punchLeft - Time.unscaledDeltaTime);
        float k = punchLeft / Juice.PunchTime;
        ApplyZoom(punchStrength * k * k); // ease-out back to 1
    }

    void ApplyZoom(float amount)
    {
        if (ppc != null && basePpu > 0f) ppc.assetsPPU = Mathf.Max(1, Mathf.RoundToInt(basePpu * (1f + amount)));
        else if (ppc == null) cam.orthographicSize = baseOrtho / (1f + amount);
    }

    void ApplyShake()
    {
        if (!PauseMenu.IsOpen) // paused: no progress, keep the current offset
        {
            shakeLeft = Mathf.Max(0f, shakeLeft - Time.unscaledDeltaTime);
            float ppu = basePpu > 0 ? basePpu : 32f;
            float a = shakeTime > 0f ? shakeAmp * shakeLeft / shakeTime : 0f;
            float kick = shakeDir == Vector2.zero ? 0f : Juice.ShakeKick;
            float j = a * (1f - kick);
            Vector2 r = shakeDir * (a * kick) + new Vector2(Random.Range(-j, j), Random.Range(-j, j));
            shakeOffset = new Vector2(Mathf.Round(r.x * ppu), Mathf.Round(r.y * ppu)) / ppu; // pixel-snapped
        }
        transform.position += new Vector3(shakeOffset.x, shakeOffset.y, 0f);
    }

    void Follow(float dt)
    {
        float vx = (target.position.x - lastTargetX) / dt;
        lastTargetX = target.position.x;
        if (Mathf.Abs(vx) > Juice.LookAheadFlipSpeed) facing = Mathf.Sign(vx); // only a real move flips the lead (no jitter on stops / turns)
        float lead = Mathf.Abs(vx) > Juice.DashSpeedThreshold ? Juice.LookAheadDash : Juice.LookAhead;
        lookAhead = Mathf.SmoothDamp(lookAhead, facing * lead, ref lookAheadVelocity, Juice.LookAheadSmooth);

        Vector2 pos = transform.position;
        Vector2 focus = (Vector2)target.position + offset + new Vector2(lookAhead, 0f);

        // Dead-zone: the desired point only moves once the focus leaves the box.
        Vector2 desired = pos;
        float ex = focus.x - pos.x;
        if (Mathf.Abs(ex) > deadZone.x) desired.x += ex - Mathf.Sign(ex) * deadZone.x;
        float ey = focus.y - pos.y;
        if (Mathf.Abs(ey) > deadZone.y) desired.y += ey - Mathf.Sign(ey) * deadZone.y;

        Vector2 next = Vector2.SmoothDamp(pos, desired, ref velocity, smoothTime, Mathf.Infinity, dt);
        Vector2 clamped = Clamp(next);
        if (clamped.x != next.x) velocity.x = 0f; // don't build up speed against a level edge
        if (clamped.y != next.y) velocity.y = 0f;
        next = clamped;
        transform.position = new Vector3(next.x, next.y, transform.position.z);
    }

    Vector2 Clamp(Vector2 p)
    {
        if (!useBounds) return p;
        // With Pixel Perfect Camera (upscale RT + stretch fill) the view is always the reference size, whatever the window aspect.
        float halfH = ppc == null ? baseOrtho : cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        if (ppc != null && basePpu > 0)
        {
            halfW = ppc.refResolutionX * 0.5f / basePpu;
            halfH = ppc.refResolutionY * 0.5f / basePpu;
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
