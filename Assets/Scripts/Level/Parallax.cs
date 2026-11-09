using UnityEngine;

// Background layer that follows the camera by a fraction of its movement: factor 0 = fixed in the world, 1 = pinned to the screen.
// cameraOrigin = camera position (world) at which the layer sits exactly where the builder placed it. Positions snap to the 1/32 u pixel grid.
// Runs after CameraFollow2D (execution order), so a layer never lags the camera by a frame.
[DefaultExecutionOrder(500)]
public class Parallax : MonoBehaviour
{
    [SerializeField] Vector2 factor;
    [SerializeField] Vector2 cameraOrigin;
    [SerializeField] Vector2 cameraMax; // the camera range of the layer's own segment: in another segment the layer stops instead of drifting into its view

    Vector3 basePos;
    Transform cam;

    void Awake() => basePos = transform.position;

    void LateUpdate()
    {
        if (cam == null)
        {
            Camera mc = Camera.main;
            if (mc == null) return;
            cam = mc.transform;
        }
        Vector2 c = Vector2.Min(Vector2.Max((Vector2)cam.position, cameraOrigin), cameraMax);
        Vector2 d = (c - cameraOrigin) * factor;
        transform.position = new Vector3(Snap(basePos.x + d.x), Snap(basePos.y + d.y), basePos.z);
    }

    static float Snap(float v) => Mathf.Round(v * 32f) / 32f;
}
