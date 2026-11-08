using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Upgrade 8-A (docs/design/DECISIONS.md): Space = shockwave. Every enemy in a box `range` units in front of the player
// is staggered for `staggerSeconds`. Locked until SetUnlocked(true) (STG-03, the Luddite). Added at runtime by PlayerController.
[RequireComponent(typeof(PlayerController), typeof(PlayerInputReader))]
public class Shockwave : MonoBehaviour
{
    [Header("Shockwave (T)")]
    [SerializeField] float range = 3f;   // distance in front of the player's center
    [SerializeField] float height = 2f;  // box height, centered on the player
    [SerializeField] float staggerSeconds = 1f;
    [SerializeField] float cooldown = 5f;
    [SerializeField] Color fxColor = new Color(0.298f, 0.909f, 0.847f); // #4CE8D8, same as the dash afterimage

    // Runtime-only unlock (no save). Survives scene loads, resets when the game restarts.
    public static bool Unlocked { get; private set; }
    public static event Action<bool> UnlockChanged;

    public static void SetUnlocked(bool value)
    {
        if (Unlocked == value) return;
        Unlocked = value;
        UnlockChanged?.Invoke(value);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Unlocked = false; UnlockChanged = null; }

    public event Action Used; // for a HUD / SFX / camera shake to subscribe to
    public float CooldownRemaining => Mathf.Max(0f, readyAt - Time.time);
    public bool Ready => Unlocked && CooldownRemaining <= 0f;

    static readonly HashSet<TestEnemy> hitEnemies = new HashSet<TestEnemy>();
    PlayerController player;
    PlayerInputReader input;
    float readyAt;

    void Awake()
    {
        player = GetComponent<PlayerController>();
        input = GetComponent<PlayerInputReader>();
    }

    void Update()
    {
        if (Time.timeScale == 0f) return; // parry hit-stop / pause
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame)
        {
            SetUnlocked(!Unlocked);
            Debug.Log($"[Shockwave] debug unlock: {Unlocked}");
        }
#endif
        if (input.ShockwavePressed && Ready &&
            player.CurrentState != PlayerController.State.Hurt && player.CurrentState != PlayerController.State.Dead)
            Fire();
    }

    void Fire()
    {
        readyAt = Time.time + cooldown;
        Vector2 center = Center(player.Facing);
        Vector2 size = new Vector2(range, height);
        ParryFx.Swing(center, size, fxColor, null, player.Facing, 0.15f, 0f);

        hitEnemies.Clear();
        foreach (Collider2D col in Physics2D.OverlapBoxAll(center, size, 0f, player.EnemyMask))
        {
            TestEnemy enemy = col.GetComponentInParent<TestEnemy>();
            if (enemy != null && hitEnemies.Add(enemy)) enemy.ForceStagger(staggerSeconds);
        }
        Used?.Invoke();
    }

    Vector2 Center(int facing) => (Vector2)transform.position + new Vector2(facing * range * 0.5f, 0f);

    // Prototype indicator (same style as PrototypeDebug). Replace with a HUD element that reads CooldownRemaining / listens to Used.
    void OnGUI()
    {
        if (!Unlocked) return;
        float cd = CooldownRemaining;
        string text = cd <= 0f ? Loc.T("SPACE Shockwave: READY", "SPACE 충격파: 준비") : Loc.T($"SPACE Shockwave: {cd:0.0}s", $"SPACE 충격파: {cd:0.0}초");
        GUI.skin.label.fontSize = 20;
        GUI.Label(new Rect(10, Screen.height - 40, 600, 30), text);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(Center(player != null ? player.Facing : 1), new Vector3(range, height, 0f));
    }
}
