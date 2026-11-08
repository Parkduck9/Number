using UnityEngine;
using UnityEngine.InputSystem;

// Test-only (only used by Test_Camera; delete before the final build if unwanted): moves a dummy target with A/D or arrow keys so CameraFollow2D can be checked in Test_Camera.
public class CameraTestMover : MonoBehaviour
{
    [SerializeField] float speed = 8f;

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
        float y = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
        transform.position += new Vector3(x, y, 0f) * (speed * Time.deltaTime);
    }
}
