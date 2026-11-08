using UnityEngine;

// Keeps the camera clamped to the level segment the player is currently in.
// Works for door teleports and for respawns alike, because it only looks at the player's x.
public class SegmentCameraBounds : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] CameraFollow2D cameraFollow;
    [SerializeField] Rect[] segments; // world-space rectangles (x, y = bottom-left)

    int current = -1;

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
