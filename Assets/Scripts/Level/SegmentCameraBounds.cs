using UnityEngine;

// Keeps the camera clamped to the level segment the player is currently in.
// Works for door teleports and for respawns alike, because it only looks at the player's x.
public class SegmentCameraBounds : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] CameraFollow2D cameraFollow;
    [SerializeField] Rect[] segments; // world-space rectangles (x, y = bottom-left)

    int current = -1;

    // x range of the segment containing worldX (StageClear: only the goal's segment counts). false = no segment there.
    public bool TryGetRangeAt(float worldX, out float xMin, out float xMax)
    {
        xMin = xMax = 0f;
        if (segments == null) return false;
        foreach (var r in segments)
            if (worldX >= r.xMin && worldX <= r.xMax) { xMin = r.xMin; xMax = r.xMax; return true; }
        return false;
    }

    void Update()
    {
        if (player == null || cameraFollow == null) return;
        float x = player.position.x;
        for (int i = 0; i < segments.Length; i++)
        {
            if (x < segments[i].xMin || x > segments[i].xMax || i == current) continue;
            current = i;
            cameraFollow.SetBounds(segments[i]);
            cameraFollow.Snap();
            return;
        }
    }
}
