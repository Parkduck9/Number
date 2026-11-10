using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// DECISIONS 18: Space = lucky dice (replaces the 8-A shockwave slot). 3 successful parries / Shift counters charge one roll (max 1 stored).
// Space throws a die (DiceRollView, ~0.7 s under GameFlow.InputLocked), the face 1..6 picks a dosa skill (SkillN.Run). Unlocked at STG-03
// (the Luddite). Disabled during the STG-05 boss fight. Added at runtime by PlayerController.
[RequireComponent(typeof(PlayerController), typeof(PlayerInputReader))]
public class LuckyDice : MonoBehaviour
{
    public const int ChargesPerRoll = 3;
    const float CameraMargin = 0.5f; // (T) world units around the view that still count as "on camera"

    // Runtime-only unlock (no save). Survives scene loads, resets when the game restarts (TitleScreen).
    public static bool Unlocked { get; private set; }
    public static event Action<bool> UnlockChanged;
    public static int Charges { get; set; } // 0..ChargesPerRoll; static so death / scene reloads keep it

    public static void SetUnlocked(bool value)
    {
        if (Unlocked == value) return;
        Unlocked = value;
        UnlockChanged?.Invoke(value);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Unlocked = false; UnlockChanged = null; Charges = 0; boss = null; }

    static BossFight boss; // found per scene in Start
    public static bool BossActive => boss != null && boss.isActiveAndEnabled;

    public PlayerController Player;
    public PlayerHealth Health;
    public Camera Cam;

    public bool Busy => rolling || skill != null;
    public int LastFace { get; private set; }

    PlayerInputReader input;
    DiceRollView view;
    LuckyDiceHud hud;
    bool pendingPress; // Space during a hit-stop: fired on the first unfrozen frame instead of being dropped
    bool rolling, lockedByRoll;
    IEnumerator skill; // running SkillN.Run enumerator (disposed on disable so its finally blocks restore flags)
    int lastParryCount;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    int debugFace; // 1..6 forces the next roll (editor / dev: number keys)
#endif

    void Awake()
    {
        Player = GetComponent<PlayerController>();
        Health = GetComponent<PlayerHealth>();
        input = GetComponent<PlayerInputReader>();
        lastParryCount = PlayerController.ParryCount;
    }

    void Start()
    {
        Cam = Camera.main;
        boss = FindFirstObjectByType<BossFight>();
        view = DiceRollView.Create();
        hud = LuckyDiceHud.Create(this);
    }

    void OnEnable() { if (Player != null) Player.CounterLanded += OnCounterLanded; }

    void OnDisable()
    {
        if (Player != null) Player.CounterLanded -= OnCounterLanded;
        if (skill is IDisposable d) d.Dispose(); // runs the skill's finally blocks (EnemiesHeld / InputLocked / SpeedMul restore)
        skill = null;
        if (lockedByRoll) GameFlow.InputLocked = false;
        rolling = lockedByRoll = false;
    }

    void OnDestroy()
    {
        if (view != null) Destroy(view.gameObject);
        if (hud != null) Destroy(hud.gameObject);
    }

    void OnCounterLanded() => AddCharge();

    static void AddCharge()
    {
        if (Unlocked && Charges < ChargesPerRoll) Charges++;
    }

    void Update()
    {
        // Charge: every new successful parry (PlayerController.ParryCount is a running total; a reset just resyncs).
        int parries = PlayerController.ParryCount;
        if (parries > lastParryCount) for (int i = lastParryCount; i < parries; i++) AddCharge();
        lastParryCount = parries;

        if (Time.timeScale == 0f) { pendingPress |= input.DicePressed; return; } // hit-stop / pause: keep the press
        if (GameFlow.InputLocked) { pendingPress = false; return; }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        DebugKeys();
#endif
        bool pressed = input.DicePressed || pendingPress;
        pendingPress = false;
        if (pressed && CanRoll()) StartCoroutine(Roll());
    }

    bool CanRoll() =>
        Unlocked && Charges >= ChargesPerRoll && !Busy && !BossActive && !GameFlow.InputLocked &&
        !GameFlow.IsPlayerDead(Player) && Player.CurrentState != PlayerController.State.Hurt;

    // Test hook (Sandbox scene): charges the dice and rolls `face` (1..6) right now through the normal Roll path. False if a roll is not allowed.
    public bool ForceRoll(int face)
    {
        if (face < 1 || face > 6) return false;
        int before = Charges;
        Charges = ChargesPerRoll;
        if (!CanRoll()) { Charges = before; return false; } // refused (busy...): no free charge, the HUD must not light up "ready"
        StartCoroutine(Roll(face));
        return true;
    }

    IEnumerator Roll(int forcedFace = 0)
    {
        rolling = true;
        Charges = 0;
        int face = forcedFace != 0 ? forcedFace : UnityEngine.Random.Range(1, 7);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (debugFace != 0) { face = debugFace; debugFace = 0; }
#endif
        LastFace = face;
        GameFlow.InputLocked = true;
        lockedByRoll = true;
        try { yield return view.Play(face); }
        finally // an exception in the roll view must not leave the game input-locked
        {
            if (lockedByRoll && GameFlow.InputLocked) GameFlow.InputLocked = false;
            lockedByRoll = false;
            rolling = false;
        }
        if (GameFlow.IsPlayerDead(Player)) yield break;
        skill = SkillFor(face);
        while (skill != null)
        {
            bool next;
            try { next = skill.MoveNext(); }
            catch (Exception ex) { Debug.LogException(ex); next = false; }
            if (!next) break;
            yield return skill.Current;
        }
        skill = null;
    }

    IEnumerator SkillFor(int face)
    {
        switch (face)
        {
            case 1: return Skill1Blink.Run(this);
            case 2: return Skill2Rope.Run(this);
            case 3: return Skill3Talisman.Run(this);
            case 4: return Skill4Hold.Run(this);
            case 5: return Skill5Marks.Run(this);
            default: return Skill6Path.Run(this);
        }
    }

    // Active, standing, non-boss-wave enemies whose body center is inside the camera view (+ margin).
    public List<TestEnemy> EnemiesOnCamera()
    {
        var list = new List<TestEnemy>();
        if (Cam == null) Cam = Camera.main;
        if (Cam == null) return list;
        float h = Cam.orthographicSize + CameraMargin;
        float w = Cam.orthographicSize * Cam.aspect + CameraMargin;
        Vector2 c = Cam.transform.position;
        foreach (TestEnemy e in TestEnemy.All)
        {
            if (e == null || e.IsDown || e.IsBossWave) continue;
            Vector2 p = e.BodyCollider != null ? (Vector2)e.BodyCollider.bounds.center : (Vector2)e.transform.position;
            if (Mathf.Abs(p.x - c.x) <= w && Mathf.Abs(p.y - c.y) <= h) list.Add(e);
        }
        return list;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // F9 = toggle unlock, F10 = fill charges, hold F10 + 1..6 = force the next roll's face (digits alone belong to the NumberDial).
    static readonly Key[] DebugDigits = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6 };

    void DebugKeys()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.f9Key.wasPressedThisFrame) { SetUnlocked(!Unlocked); Debug.Log($"[LuckyDice] debug unlock: {Unlocked}"); }
        if (kb.f10Key.wasPressedThisFrame && Unlocked) Charges = ChargesPerRoll;
        if (!kb.f10Key.isPressed || !Unlocked) return;
        for (int i = 0; i < DebugDigits.Length; i++)
            if (kb[DebugDigits[i]].wasPressedThisFrame)
            {
                debugFace = i + 1;
                Charges = ChargesPerRoll;
                Debug.Log($"[LuckyDice] debug: next roll = {debugFace}");
            }
    }
#endif
}
