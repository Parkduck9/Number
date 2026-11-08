using UnityEngine;

// Data passed from attacker to target. See docs/design/PROTOTYPE_ARCH.md section 1.
// PlayerController: public HitResult ReceiveEnemyHit(HitInfo info)
// TestEnemy:        public bool      ReceivePlayerHit(HitInfo info)   // true = damage applied
public struct HitInfo
{
    public int Damage;
    public Vector2 SourcePosition;
    public bool IsSpecial; // last strike of the enemy chain (number 1 -> 0)
    public bool Unparryable; // parry attempts count as a hit; only a dash dodges it
}

public enum HitResult { Hit, Parried, Dodged }
