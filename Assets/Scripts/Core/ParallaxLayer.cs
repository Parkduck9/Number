using UnityEngine;

// Moves this layer with the camera by `factor` of the camera's movement.
// 0 = fixed in the world (near layers), 1 = glued to the screen (sky / far layers).
[DefaultExecutionOrder(100)] // after CameraFollow2D's LateUpdate
public class ParallaxLayer : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] float factor = 0.5f;
    [SerializeField] bool affectY = false;
    [SerializeField] Transform cameraTransform; // defaults to Camera.main

    Vector3 startPos, cameraStart;

    void Start()
    {
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        if (cameraTransform == null) { enabled = false; return; }
        startPos = transform.position;
        cameraStart = cameraTransform.position;
    }

    void LateUpdate()
    {
        Vector3 d = cameraTransform.position - cameraStart;
        transform.position = new Vector3(startPos.x + d.x * factor, affectY ? startPos.y + d.y * factor : startPos.y, startPos.z);
    }
}
