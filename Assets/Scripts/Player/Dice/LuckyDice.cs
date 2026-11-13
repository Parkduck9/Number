using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// DECISIONS 18 / 31: E = lucky dice (replaces the 8-A shockwave slot). 7 landed hits on enemies / the boss charge one roll (lucky number 7; parries do not) (max 1 stored).
// E throws a die (DiceRollView, ~0.7 s under GameFlow.InputLocked), the face 1..6 picks a dosa skill (SkillN.Run). Unlocked at STG-03
// (the Luddite). Boss fight (DECISIONS 31): BossSkill instead (no TestEnemies there). Added at runtime by PlayerController.
[RequireComponent(typeof(PlayerController), typeof(PlayerInputReader))]
public class LuckyDice : MonoBehaviour
{
    public const int ChargesPerRoll = 7;
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
    static void ResetStatics() { Unlocked = true; /* new story 11-12: Yudo brings his own die, usable from the first fight */ UnlockChanged = null; Charges = 0; boss = null; current = null; }

    static LuckyDice current; // the player's dice: its skills' strikes must not charge it
    static BossFight boss; // found per scene in Start
    public static bool BossActive => boss != null && boss.isActiveAndEnabled && !boss.Dead; // dead = the smash / collapse ending: field skills (they fizzle)

    public PlayerController Player;
    public PlayerHealth Health;
    public Camera Cam;

    public bool Busy => rolling || skill != null;
    public int LastFace { get; private set; }

    PlayerInputReader input;
    DiceRollView view;
    LuckyDiceHud hud;
    bool pendingPress; // E during a hit-stop: fired on the first unfrozen frame instead of being dropped
    bool rolling, lockedByRoll;
    IEnumerator skill; // running SkillN.Run enumerator (disposed on disable so its finally blocks restore flags)
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    int debugFace; // 1..6 forces the next roll (editor / dev: number keys)
#endif

    void Awake()
    {
        Player = GetComponent<PlayerController>();
        Health = GetComponent<PlayerHealth>();
        input = GetComponent<PlayerInputReader>();
        current = this;
    }

    void Start()
    {
        Cam = Camera.main;
        boss = FindFirstObjectByType<BossFight>();
        view = DiceRollView.Create();
        hud = LuckyDiceHud.Create(this);
    }

    void OnDisable()
    {
        if (skill is IDisposable d) d.Dispose(); // runs the skill's finally blocks (EnemiesHeld / InputLocked / invulnerability restore)
        skill = null;
        if (lockedByRoll) GameFlow.InputLocked = false;
        rolling = lockedByRoll = false;
    }

    void OnDestroy()
    {
        if (view != null) Destroy(view.gameObject);
        if (hud != null) Destroy(hud.gameObject);
    }

    // +1 per landed player hit (one per enemy hit; boss core / mash / arm hits). Dice-skill strikes and reflected shots do not count.
    public static void AddHitCharge()
    {
        if (!Unlocked || Charges >= ChargesPerRoll || (current != null && current.Busy)) return;
        Charges++;
    }

    // Playtest 11-13: in the field a roll charges on KILLS (TestEnemy, any player kill: cut, issen, reflected shot). The boss keeps
    // charging on hits (BossFight calls AddHitCharge), so this one ignores kills while the boss is up. Skill kills don't count (Busy).
    public static void AddKillCharge() { if (!BossActive) AddHitCharge(); }

    void Update()
    {
        if (Time.timeScale == 0f) { pendingPress |= input.DicePressed; return; } // hit-stop / pause: keep the press
        if (GameFlow.InputLocked) { pendingPress = false; return; }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        DebugKeys();
#endif
        bool pressed = input.DicePressed || pendingPress || Charges >= ChargesPerRoll; // playtest 11-13: the 7th hit rolls by itself (E kept as a no-op fallback)
        pendingPress = false;
        if (pressed && CanRoll()) StartCoroutine(Roll());
    }

    bool CanRoll() =>
        Unlocked && Charges >= ChargesPerRoll && !Busy && !GameFlow.InputLocked &&
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
        int face = forcedFace != 0 ? forcedFace : BossActive ? UnityEngine.Random.Range(1, 7) : FaceForCrowd();
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
        skill = BossActive ? BossSkill(face) : SkillFor(face);
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

    // Playtest 11-13 (user): the field roll reads the crowd instead of chance. 6+ on screen -> 6; 2+ inside the rope whirl (a bit smaller
    // than its reach, so they really get hit) -> 2; else the count on screen (1, 3, 4, 5); two apart -> 3 (lightning hits both); none -> random.
    const float WhirlCheck = 0.8f; // fraction of Skill2Rope.Radius

    int FaceForCrowd()
    {
        List<TestEnemy> on = EnemiesOnCamera();
        int n = on.Count;
        if (n == 0) return UnityEngine.Random.Range(1, 7);
        if (n >= 6) return 6;
        Vector2 o = Player.Body.position;
        float r = Skill2Rope.Radius * WhirlCheck;
        int near = 0;
        foreach (TestEnemy e in on)
        {
            Collider2D col = e.BodyCollider;
            Vector2 cp = col != null ? col.ClosestPoint(o) : (Vector2)e.transform.position; // same measure as Skill2Rope's hit test
            if ((cp - o).sqrMagnitude <= r * r) near++;
        }
        if (near >= 2) return 2;
        return n == 2 ? 3 : n;
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

    // Boss fight (DECISIONS 31 / 32): face 2 (rope whirl) plays as usual; 3 (talisman lightning) and 4 (sheath cut) play their boss
    // versions on the core; 6 dashes beside the core (BossPath, DECISIONS 44); the others need TestEnemies and would only fizzle, so their caption shows and `face` talismans fly into
    // the core instead. Then every face hurts the boss (BossFight.SkillHit, DiceDamage); 3 / 4 land with their bolt / click.
    static readonly string[] BossCapEn = { "SHRINK EARTH", "BINDING ROPE", "TALISMAN LIGHTNING", "SHEATH CUT", "FIVE TALISMANS", "DIVINATION PATH" };
    static readonly string[] BossCapKo = { "축지법", "오랏줄", "부적 낙뢰", "칼집 일섬", "부적 다섯 장", "점괘 길" };
    const float BossFly = 0.2f, BossStagger = 0.05f; // (T) s: first talisman lands / gap between talismans

    IEnumerator BossSkill(int face)
    {
        IEnumerator s = face == 2 ? SkillFor(face) : face == 3 ? Skill3Talisman.RunBoss(this, boss) : face == 4 ? Skill4Hold.RunBoss(this, boss)
            : face == 6 ? BossPath() : BossTalismans(face);
        try { while (s.MoveNext()) yield return s.Current; }
        finally { (s as IDisposable)?.Dispose(); } // disabled mid-skill: the inner finally blocks still restore their flags
        while (GameFlow.InputLocked) yield return null; // a blade duel that began while the talismans flew: a kill now would stop the fight under it
        if (DiceSkillKit.PlayerGone(this) || boss == null) yield break;
        Vector2 at = boss.CorePosition;
        if (face == 3) Skill3Talisman.Smite(new List<Vector2> { at }, Cam);
        else if (face == 4) Skill4Hold.Click(Player);
        else if (face == 6) { ParryFx.Slash(at, null, Player.Facing); Sfx.Play(SfxId.Swing); }
        boss.SkillHit(BossFight.DiceDamage(face), at);
        if (face == 3 || face == 4 || face == 6) Player.DuelHitStop(Juice.HitStopKill);
    }

    // DECISIONS 44: face 6 in the boss fight = a whoosh dash (afterimages + streak) to a clear floor spot BossPathSide u beside the core,
    // under the input lock with i-frames, facing the core; BossSkill then lands the hit. No clear spot: the old talisman volley.
    const float BossPathSide = 2.5f, BossPathTime = 0.16f, BossPathHold = 0.08f; // (T) u / s / s

    IEnumerator BossPath()
    {
        if (DiceSkillKit.PlayerGone(this) || !Player.PlanSpotBeside(boss.CorePosition, BossPathSide, out Vector2 spot))
        {
            IEnumerator volley = BossTalismans(6);
            while (volley.MoveNext()) yield return volley.Current;
            yield break;
        }
        DiceSkillKit.Caption(BossCapEn[5], BossCapKo[5], DiceSkillKit.Cyan);
        Sfx.Play(SfxId.HackOk);
        try
        {
            DiceSkillKit.Lock();
            DiceSkillKit.SetInvulnerable(this);
            Vector2 from = Player.Body.position;
            int travel = spot.x >= from.x ? 1 : -1;
            DiceSkillKit.Streak(from, spot, 0.3f);
            Player.DiceDashStep(from, travel, true);
            for (float t = 0f; t < BossPathTime; t += Time.deltaTime)
            {
                if (DiceSkillKit.PlayerGone(this)) yield break;
                float u = t / BossPathTime;
                Player.DiceDashStep(Vector2.Lerp(from, spot, 1f - (1f - u) * (1f - u)), travel, false); // ease-out
                yield return null;
            }
            if (DiceSkillKit.PlayerGone(this)) yield break;
            float dx = boss.CorePosition.x - spot.x;
            Player.DiceDashStep(spot, Mathf.Abs(dx) > 0.01f ? (dx > 0f ? 1 : -1) : travel, false); // land facing the core
            yield return DiceSkillKit.Wait(BossPathHold);
        }
        finally
        {
            DiceSkillKit.ReleaseLock();
            DiceSkillKit.ReleaseInvulnerable();
        }
    }

    IEnumerator BossTalismans(int face)
    {
        DiceSkillKit.Caption(BossCapEn[face - 1], BossCapKo[face - 1], face == 6 ? DiceSkillKit.Cyan : DiceSkillKit.Yellow);
        Sfx.Play(SfxId.HackOk);
        Vector2 hand = Player.HandPosition;
        for (int i = 0; i < face; i++)
        {
            DiceSkillKit.Fade tal = DiceSkillKit.Talisman(hand, BossFly + i * BossStagger + 0.3f);
            tal.follow = boss.CoreTransform;
            tal.flyFrom = hand;
            tal.flyTime = BossFly + i * BossStagger;
            tal.fadeAt = tal.flyTime;
        }
        yield return DiceSkillKit.Wait(BossFly + (face - 1) * BossStagger);
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
