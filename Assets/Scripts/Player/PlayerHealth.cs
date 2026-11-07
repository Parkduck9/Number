using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] int maxHp = 5;
    [SerializeField] float invulnerableTime = 0.8f; // (T)

    int hp;
    float invulnerableUntil;

    public int Hp => hp;
    public int MaxHp => maxHp;
    public bool IsDead => hp <= 0;
    public bool Invulnerable => Time.time < invulnerableUntil;

    void Awake() => hp = maxHp;

    // Returns true if damage was applied. Caller checks Invulnerable first for result priority.
    public bool TakeDamage(int amount)
    {
        if (IsDead) return false;
        hp = Mathf.Max(0, hp - amount);
        invulnerableUntil = Time.time + invulnerableTime;
        return true;
    }
}
