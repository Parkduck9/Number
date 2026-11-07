using UnityEngine;

// STUB — full implementation follows (Task A). Signature fixed by docs/design/PROTOTYPE_ARCH.md section 1.
public class PlayerController : MonoBehaviour
{
    public int Facing { get; private set; } = 1;

    public HitResult ReceiveEnemyHit(HitInfo info)
    {
        return HitResult.Hit;
    }
}
