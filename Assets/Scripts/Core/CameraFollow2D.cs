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
    [Tooltip("How far the camera leads in the facing direction (world units).")]
    [SerializeField] float lookAheadDistance = 2f;
    [SerializeField] float lookAheadSmoothTime = 0.35f;
    [Tooltip("Minimum horizontal speed (units/s) that changes the facing direction.")]
    [SerializeField] float facingMinSpeed = 0.5f;
    [SerializeField] bool useBounds = true;
    [Tooltip("Level rectangle in world units (x, y = bottom-left). The camera view stays inside it.")]
    [SerializeField] Rect levelBounds = new Rect(-20f, -6f, 60f, 24f);

    Camera cam;
    PixelPerfectCamera ppc;
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
    public static void Shake(float amplitude, float duration)
    {
        if (active == null || amplitude <= 0f || duration <= 0f) return;
        float currentAmp = active.shakeLeft > 0f ? active.shakeAmp * active.shakeLeft / active.shakeTime : 0f;
        if (amplitude < currentAmp) return;
        active.shakeAmp = amplitude;
        active.shakeTime = active.shakeLeft = duration;
    }

    void OnEnable() => active = this;
    void OnDisable() { if (active == this) active = null; }

    void Awake()
    {
        cam = GetComponent<Camera>();
        ppc = GetComponent<PixelPerfectCamera>();
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
        Vector2 p = Clamp((Vector2)target.position + offset);
        transform.position = new Vector3(p.x, p.y, transform.position.z);
    }

    void LateUpdate()
    {
        transform.position -= new Vector3(shakeOffset.x, shakeOffset.y, 0f);
        float dt = Time.deltaTime;
        if (target != null && dt > 0f) Follow(dt); // dt is 0 during hit-stop / pause: hold position
        ApplyShake();
    }

    void ApplyShake()
    {
        if (!PauseMenu.IsOpen) // paused: no progress, keep the current offset
        {
            shakeLeft = Mathf.Max(0f, shakeLeft - Time.unscaledDeltaTime);
            float ppu = ppc != null && ppc.assetsPPU > 0 ? ppc.assetsPPU : 32f;
            float a = shakeTime > 0f ? shakeAmp * shakeLeft / shakeTime : 0f;
            Vector2 r = new Vector2(Random.Range(-a, a), Random.Range(-a, a));
            shakeOffset = new Vector2(Mathf.Round(r.x * ppu), Mathf.Round(r.y * ppu)) / ppu; // pixel-snapped
        }
        transform.position += new Vector3(shakeOffset.x, shakeOffset.y, 0f);
    }

    void Follow(float dt)
    {
        float vx = (target.position.x - lastTargetX) / dt;
        lastTargetX = target.position.x;
        if (Mathf.Abs(vx) > facingMinSpeed) facing = Mathf.Sign(vx);
        lookAhead = Mathf.SmoothDamp(lookAhead, facing * lookAheadDistance, ref lookAheadVelocity, lookAheadSmoothTime);

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
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        if (ppc != null && ppc.assetsPPU > 0)
        {
            halfW = ppc.refResolutionX * 0.5f / ppc.assetsPPU;
            halfH = ppc.refResolutionY * 0.5f / ppc.assetsPPU;
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
