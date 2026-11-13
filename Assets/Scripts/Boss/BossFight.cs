using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// STG-05 final boss v2 (DECISIONS 23 / 21, docs/design/impl/boss-v2.md): a huge fused machine fills the room background; only its
// parts attack. Director (coroutine): hologram shows attack icon + N, the part glows (tell) -> attack -> its opening -> rest -> next.
//   A bug arm:  N swarm walls sweep the room left -> right, each with one gap (floor / jump height); dash goes through. The arm stays
//               lit and conducts (sways) until the last wall is gone, while the chest hatch fires telegraphed parryable bug shots
//               (DECISIONS 36). Then the arm slumps on the floor SlumpTime: LMB swings on it hurt the boss.
//   B blade arm: red danger zone at the player, N slams with the ParryRing tell (LMB in the window = clang, no damage); the last one
//               sticks in the bridge: mash window (DECISIONS 36), the arm jerks to pull free while LMB swings on the steel hurt
//               (MashDamage each, up to MashCap); then it rips out and reattaches. Every unparried slam sends two low ground
//               shockwaves left / right to the edges (DECISIONS 42): jump, a roof chunk or a hook clears them.
//   B' blade sweep (DECISIONS 29 / 36 / 45): the arm leaves the screen, then N horizontal sweeps that carry the whole arm (lying flat,
//               8 u thick) past the far edge. LOW: up onto a wall perch / down into the under-bridge lane; HIGH: stay on the deck / the lane;
//               a dash through the contact passes. Red warning band (the arm's full height) first. Then the arm flies back and clamps on.
//   C chest hatch: hatch opens, yellow aim dots, N volleys of small shots in a fan (phase 3: fan / spiral arc alternate).
//               A parried shot flies back; reaching the hatch hurts.
//   D bug rain (DECISIONS 38): N columns of bugs dive from the ceiling at the player's x (red floor mark first), land and scatter into a
//               short floor carpet; then the bug arm slumps like A. Phase 3: A's walls also drop a column each.
//   Rope (DECISIONS 38, PlayerController.IGrappleTarget): two ceiling hooks to hang from (over jump-gap walls / bug carpets, not
//               floor-gap walls; both sweeps hit a hanger: swing on to a wall perch, DECISIONS 45), and the open hatch with the core still inside (cyan corners): RMB pulls the hero up
//               for one HatchHookDamage hit. Bug attacks are weighted up in the director.
//   Core (DECISIONS 31): in the rest after every attack the hatch opens and a glowing weak-point core drops on a cable to the bridge
//               (pulses gold while it can be hit): LMB swings, RMB issen (PlayerController.IssenAlt) and the dice hurt it. Then it pulls back.
// HP is never shown: phase 2 / 3 darken + crack the body. Phase 3 overlaps C with A / B. HP 0: the hologram shatters, the body stops,
// the hero smashes the machine props (LMB), everything collapses into white -> END-01 lines -> title. Dice work here (DECISIONS 31):
// LuckyDice.BossSkill -> SkillHit (DiceDamage by face); no parry / reflect charge; melee hits (OnSwing, IssenHit) call LuckyDice.AddHitCharge.
// Dying reloads the scene (RespawnManager): the fight restarts at the start of the phase reached (phase checkpoints, playtest 11-12). Timers use scaled time and stop while input is locked (dice skills, dialogue).
public class BossFight : MonoBehaviour, PlayerController.IIssenTarget, PlayerController.IGrappleTarget
{
    [Header("Art overrides (empty = temporary code-drawn BossArt)")]
    [SerializeField] Sprite bodyArt;      // pivot bottom-left, placed at BodyAt (24 x 14 u)
    [SerializeField] Sprite bugArmArt;    // pivot bottom-centre = the palm
    [SerializeField] Sprite bladeArmArt;  // pivot bottom-centre = the blade tip
    [SerializeField] Sprite hatchArt;     // left half, centre pivot (the right half is the same sprite flipped)
    [SerializeField] Sprite debrisArt;    // falling roof chunk (DECISIONS 40), pivot bottom-centre; empty = a code-drawn slab
    [Header("Ending")]
    [SerializeField] TextAsset ending;    // END-01 lines (talk mode)
    [SerializeField] TMP_FontAsset font;  // "smash the machines" hint

    // ---- numbers (T) ----
    const int MaxHp = 60;                                   // phase 1 = 60..41, 2 = 40..21, 3 = 20..1 (45 before, DECISIONS 33: harder)
    const int ArmHitDamage = 1, ReflectDamage = 1;
    const float StartX = 6.5f, StartAfter = 3f;             // fight starts when the player walks past x or after s
    // weak-point core (DECISIONS 31), replaces the 0.9 s rest between attacks: drop, open (hittable), pull back
    const int CoreHitDamage = 1, CoreIssenDamage = 2;
    const float CoreDrop = 0.3f, CoreOpen = 1.2f, CoreRetract = 0.3f; // CoreOpen 1.6 -> 1.2 (DECISIONS 33)
    const float CoreHitEvery = 0.1f, CoreHalf = 0.75f, CoreIssenPast = 1.6f; // s between LMB hits (safety only: Swung fires once per swing, swings are >= attackDuration 0.3 s apart minus one frame); u half size; u issen lands past it
    public static int DiceDamage(int face) => (face + 1) / 2; // faces 1..6 -> 1, 1, 2, 2, 3, 3
    const float WaveSpeed = 10.5f, WaveEvery = 1.1f, // was 9 / 1.3 (DECISIONS 33)
 WaveHalfW = 0.6f, WaveTop = 16.5f /* playtest 11-13: was 9.5, the walls now reach the ceiling (the gap is the way through) */, SlumpTime = 1.5f;
    // Wall gaps (playtest 11-13, 2nd: a hanger on a hook kept getting hit at the top; the jump from the floor must still work):
    //   low  2.0..5.6  stand on the bridge (a hook hanger's head, 6.0, is above it: drop off)
    //   mid  3.6..8.4  a jump from the bridge (feet over 3.6 as it passes) or hanging from a hook (feet 4.35, head 6.0, with swing room)
    //   high 10.3..ceiling a wall perch / a roof chunk up there (rope up); open to the ceiling (playtest 11-13, 3rd: the rope's
    //        swing threw the hero over a 3.6 u gap into the bugs above it)
    // Picked by weight (mid most often); never high twice in a row.
    static readonly float[] GapLows = { 2f, 3.6f, PerchTop - 0.2f }, GapHs = { 3.6f, 4.8f, WaveTop - (PerchTop - 0.2f) };
    static readonly int[] GapPick = { 0, 0, 1, 1, 1, 2 };
    const float ZoneHalf = 1.5f, FirstSlam = 0.5f;
    // Stuck blade = mash window (DECISIONS 36, replaces the EliteDuel exchange): StuckTime s, each LMB swing on the steel = MashDamage;
    // MashCap hits (= the old duel's 6 damage) rip it out early. The arm tugs every TugEvery s (shorter as time runs out).
    const int MashDamage = 1, MashCap = 6;
    const float StuckTime = 2.5f, TugEvery = 0.4f, TugEveryEnd = 0.2f;
    const float WindowEarly = 0.25f, WindowLate = 0.1f, Lockout = 0.12f; // same LMB window as EliteDuel; Lockout 0.25 -> 0.12 (DECISIONS 43: swallowed the right press)
    // B tracks (DECISIONS 29): after each slam the blade re-aims at the player's x, slides there in SlamShift, waits SlamAim, slams.
    // DECISIONS 33: while it waits it keeps following the player's x (TrackSpeed u/s) and locks only when the ring appears.
    const float SlamShift = 0.15f, RingLead = 0.6f, TrackSpeed = 9f; // ring always 0.6 s (DECISIONS 28), starts after the slide
    static readonly float[] SlamAim = { 0.75f, 0.7f, 0.65f }; // playtest 11-13 (slam a bit faster): was 0.85 / 0.8 / 0.75
    const float SlamDrop = 0.08f, SlamHitHalf = 1.1f; // s blade fall (was 0.12); u half width of the slam hit (was the steel's 0.69: the slam missed people inside the 1.5 red zone)    // by phase: s from the slide's end to the slam (>= RingLead); tracks for SlamAim - RingLead
    // B' sweep: warning band SweepWarn, then the slash crosses the room in SweepTime. DECISIONS 45: LOW = up (perch) / down (lane),
    // HIGH = stay on the deck / the lane, dash through either. The escapes need moving, so the warning is longer than DECISIONS 33's 0.5.
    static readonly float[] SweepWarn = { 1f, 0.9f, 0.85f }; // by phase (T): rope to a perch ~0.45 s, perch -> deck drop ~0.65 s
    static readonly float[] SweepTime = { 0.7f, 0.6f, 0.5f };   // by phase: s per RoomW - 1 u (the old edge-to-edge run; was 0.8 / 0.7 / 0.6, DECISIONS 33)
    // DECISIONS 36: the whole arm leaves the screen. boss_blade_arm is 9 x 18 u with the pivot on the tip, so lying flat it trails
    // BladeArmLen behind the tip. BladeOff: hanging, just off the right edge (where it leaves and comes back from).
    const float BladeArmLen = 18f, ReattachTime = 0.45f, ReattachOver = 0.4f;
    static readonly Vector2 BladeOff = new Vector2(RoomW + 5.5f, 10.5f), BladeWrist = new Vector2(0f, 8.5f); // clamp spark: the wrist at rest
    // Blade hitbox (feedback 11-11): the steel of boss_blade_arm(_lit), 44 x 202 px up from the pivot (= the tip). Slams and sweeps hurt only here.
    const float BladeLen = 6.3125f, BladeHalfW = 0.6875f;
    // Sweeps (DECISIONS 43): the WHOLE drawn arm hurts. ArmSlice[u] = the sprite's opaque x range (u, px from the centre line) in the
    // 1-u slice u..u+1 up from the tip, measured from boss_blade_arm(_lit).png (288 x 576, both the same).
    static readonly Vector2[] ArmSlice = {
        new Vector2(-20, 20), new Vector2(-20, 24), new Vector2(-20, 24), new Vector2(-20, 24), new Vector2(-20, 24), new Vector2(-20, 24),
        new Vector2(-40, 40), new Vector2(-66, 104), new Vector2(-68, 120), new Vector2(-64, 124), new Vector2(-36, 120), new Vector2(-46, 108),
        new Vector2(-44, 96), new Vector2(-62, 82), new Vector2(-102, 56), new Vector2(-118, 48), new Vector2(-128, 16), new Vector2(-132, -12) };
    const float SweepStep = 0.3f; // u between swept hit tests (no tunnelling on a long frame)
    // DECISIONS 45: the arm lies flat (rot +-90, mirrored for left -> right so both directions are the same shape). Over its whole
    // length it covers ArmDown below the tip line (max ArmSlice.y) to ArmUp above it (-min ArmSlice.x): an 8 u wall. SweepY = tip line:
    //   (hit band trimmed by SweepInset since 11-12: LOW 3.475..8.975, HIGH 5.275..10.775)
    //   LOW  6.1 -> arm 2.225..10.225: above the lane (head 2.05), the deck and every jump inside it; clear = a perch (feet 10.5) / the lane.
    //   HIGH 7.9 -> arm 4.025..12.025: over a standing head (3.8) and the lane; any hop, a hook, a perch, a roof chunk under 12 are inside it.
    const float ArmDown = 124f * Px, ArmUp = 132f * Px;
    // playtest 11-12 "the sweep reach is too big": the hit band is trimmed SweepInset off both long edges of the arm (8 u -> 6.5 u),
    // the warning band with it. The thin blade part is untouched (only the band's outer edges are trimmed).
    const float SweepInset = 1.25f; // playtest 11-12 (2nd, "smaller still"): 0.75 -> 1.25, band 6.5 u -> 5.5 u
    static readonly float[] SweepY = { 6.1f, 7.9f }; // LOW, HIGH
    // Under-bridge lane (DECISIONS 45, built by Stg05Builder from these): the deck over LaneX1..LaneX2 is a one-way grate (S + Space drops
    // through, a jump comes back up), the lane floor is LaneY (head 2.05 < the LOW arm). Wall perches: one-way ledges PerchW wide at the
    // walls, top PerchTop (over the LOW arm, inside the HIGH one); reached by rope from the deck (x <= ~8.5 / >= ~21.5) or from a hook.
    public const float LaneX1 = 12f, LaneX2 = 18f, LaneY = 0.25f, PerchTop = 10.5f, PerchW = 3.5f;
    // B slam shockwave (DECISIONS 42): each unparried slam sends a low ground wave left and right from the tip to the room edges.
    // ShockH tall (a jump, a roof chunk or a hook clears it), 1 unparryable hit per wave. Two slots per pair; a wave lives up to
    // ~2.9 s and slams come ~0.9 s apart, so three pairs can be live. The waves leave ShockDelay after the impact (the floor flash
    // is the tell): someone standing right beside the blade gets time to jump, and a late parry cancels them before they show.
    const int ShockSlots = 6, ShockCols = 7;
    const float ShockH = 1.5f, ShockColW = 6f * Px, ShockHitBack = 0.9f, // playtest 11-12 "too easy to dodge": 0.9 / 4 px / 0.5 -> 1.5 / 6 px / 0.9
                ShockDustEvery = 2.5f, SlamFlashTime = 0.15f, ShockDelay = SlamFlashTime;
    static readonly float[] ShockSpeed = { 10f, 11f, 12f }; // by phase, u/s
    static readonly float[] ShockProfile = { 0.55f, 0.9f, 1f, 0.75f, 0.5f, 0.3f, 0.15f }; // column heights x ShockH, front to tail
    // A: while walls fly, the chest hatch glows and cracks open with the aim dots for BugShotTell, then fires one bug bullet at the
    // player (parryable; parried = back into the hatch like C's shots). Skipped while a side barrage owns the hatch. DECISIONS 34 / 36.
    // The bug arm conducts meanwhile: figure-8 sway ConductAmp around its rest spot.
    const float BugShotEvery = 1f, BugShotTell = 0.35f, BugShotSpeed = 8f, BugShotCrack = 0.35f, ConductSpeed = 7f;
    static readonly Vector2 ConductAmp = new Vector2(0.35f, 0.2f);
    // C: volleys of small shots; fan aimed at the player, phase 3 alternates with a spiral arc over the lower half
    const float ShotSpeed = 7.5f, ShotReflectLife = 2.5f, HatchOpenAfter = 1f, BulletScale = 0.5f /* user 11-11: bigger, readable; visual now = the 0.5 u hitbox */, FanSpread = 50f, SpiralStep = 0.045f;
    const int FanShots = 5, SideFanShots = 3, SpiralShots = 9, MaxShots = 40; // MaxShots: live cap (WebGL)
    static readonly float[] VolleyEvery = { 0.7f, 0.6f, 0.5f }; // by phase (was 0.8 / 0.7 / 0.6, DECISIONS 33)
    // D bug rain (DECISIONS 38): a column warns RainWarn (red floor mark + faint lane), falls at RainSpeed as a RainLen clump, then
    // scatters into a floor carpet CarpetHalf wide, CarpetH high for CarpetTime. One hit per column, unparryable: step aside / dash / rope.
    const int RainCols = 6, BugsPerCol = 10;
    const float RainWarn = 0.6f, RainSpeed = 20f, RainLen = 3f, RainHalfW = 0.5f, RainTop = 18f, CarpetHalf = 1.6f, CarpetH = 0.6f, CarpetTime = 0.9f;
    static readonly float[] RainEvery = { 0.6f, 0.5f, 0.42f }; // by phase: s between columns
    // Rope targets (DECISIONS 38). Hanging body centre = hook - HangDrop = y 5.2: feet 4.35 clear a jump-gap wall's gap (3.8..6.8) and
    // the bug carpet; top 6.0 is inside a floor-gap wall's bugs (gap 2..5) and both sweeps (DECISIONS 45: swing on to a wall perch).
    static readonly Vector2[] Hooks = { new Vector2(8.5f, 9.25f), new Vector2(21.5f, 9.25f) }; // rope reach (9 u) covers the whole bridge
    const float HangDrop = 4.05f, HookAim = 1.5f; // u hook -> hanging centre; u mouse radius around a hook / the hatch
    // Hatch: open (or cracked) with the core still up inside -> rope latch, pulled to HatchStrikeBelow under it, HatchHookDamage, once per
    // HatchHookEvery. Windows: VentTime at the start of every CoreWindow, C's open hatch, A's bug shot tells.
    const int HatchHookDamage = 2;
    const float HatchHookEvery = 2f, HatchStrikeBelow = 1.1f, HatchHookHalf = 1.1f, VentTime = 0.8f;

    // ---- layout (room 30 x 16.875, floor top y 2) ----
    const float Floor = 2f, RoomW = 30f, Px = 1f / 32f;
    static readonly Vector2 BodyAt = new Vector2(3f, 2f), HoloAt = new Vector2(15f, 11.75f), HatchAt = new Vector2(15f, 7.5f);
    static readonly Vector2 CoreLow = new Vector2(15f, Floor + 1.3f); // core centre when dropped: player body height on the bridge
    static readonly Vector2 BugRest = new Vector2(5.5f, 6.5f), BugSlump = new Vector2(6.5f, 1.75f), BladeRest = new Vector2(24.5f, 6.5f);
    const float BladeHigh = 7.5f, BladeStuckY = 1.4f;
    static readonly float[] PropX = { 2f, 7f, 11f, 19f, 23f, 27.8f };
    static readonly int[] PropKind = { 0, 1, 2, 0, 1, 0 };   // BossArt.Props: rack, screen, cables

    static readonly Color[] PhaseTint = { Color.white, new Color(0.8f, 0.8f, 0.86f), new Color(0.62f, 0.6f, 0.68f) };
    static readonly Color GoldShot = new Color32(0xF2, 0xC9, 0x4C, 0xFF), Cyan = new Color32(0x4C, 0xE0, 0xFF, 0xFF);
    // Unlit Sprites/Default. Pooled renderers share one material per texture (squares, bugs); every other renderer owns one:
    // the 2D renderer drew neighbouring same-material runtime sprites with ONE texture (the hologram showed the body).
    static Material squareMat, bugMat;
    static Sprite[] goldDigits; // skill damage number (DECISIONS 32), Resources/DraftHud/holo_digits_gold
    readonly System.Collections.Generic.List<Material> ownMats = new System.Collections.Generic.List<Material>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { squareMat = bugMat = null; goldDigits = null; Held = false; resumeHp = 0; } // Domain Reload is off

    // Phase checkpoints (playtest 11-12): reaching phase 2 / 3 saves its starting HP; a death reload resumes there. New game clears it.
    static int resumeHp;
    public static void ResetRun() => resumeHp = 0;

    sealed class Part
    {
        public SpriteRenderer Sr;
        public Sprite[] V;        // BossArt.Normal / Lit / Flash (override art: the same sprite three times, glow = tint blink)
        public Vector2 Pos;
        public bool Glow;
        public float FlashUntil = -1f;
    }

    PlayerController player;
    PlayerInputReader input;
    Part body, bugArm, blade, hatchL, hatchR, core;
    SpriteRenderer crack1, crack2, hatchInside, holo, coreCable;
    float hatchOpen, sink, coreHitAt, mashHitAt, stuckX, conductT;
    bool coreOpen;
    int hp = MaxHp, shownPhase = 1, mashHits;
    bool started, dead, breakable, armVulnerable, sideRunning, ownLock; // breakable: the ending finish (strike the core) is on
    const int FinishHits = 5;
    int finishHits;
    float finishHitAt;
    int playerMask, groundMask;
    ParryRing ring;

    public bool BladeStuck { get; private set; }
    public Transform CoreTransform => core.Sr.transform; // dice talismans fly here
    public bool CoreExposed => coreOpen;                 // dropped and hittable right now
    public bool Started => started;                      // the fight is on (falling roof chunks spawn, DECISIONS 40)
    public bool Dead => dead;                            // ending (smash the machines): LuckyDice plays the field skills, not the boss ones
    public Vector2 CorePosition => core.Pos;
    int Phase => hp * 3 > MaxHp * 2 ? 1 : hp * 3 > MaxHp ? 2 : 3;
    // Dice face 4 (Skill4Hold.RunBoss, DECISIONS 32): the boss stops dead (timers and drawing). Set / cleared by that skill (finally), cleared on disable.
    public static bool Held;
    static float Dt => GameFlow.InputLocked || Held ? 0f : Time.deltaTime;

    // swarm pool: Waves walls x BugsPerWave bugs
    const int Waves = 3, BugsPerWave = 130; // 56 for the old 7.5 u wall; same density over the full height
    readonly float[] waveX = new float[Waves], gapLo = new float[Waves], gapH = new float[Waves];
    readonly bool[] waveOn = new bool[Waves], waveHit = new bool[Waves];
    readonly SpriteRenderer[] bugs = new SpriteRenderer[Waves * BugsPerWave];
    readonly Vector2[] bugOff = new Vector2[Waves * BugsPerWave];
    // bug rain pool (DECISIONS 38)
    readonly float[] rainX = new float[RainCols], rainT = new float[RainCols], rainG = new float[RainCols]; // rainG: landing height
    readonly int[] rainStage = new int[RainCols]; // 0 off, 1 warning, 2 falling, 3 floor carpet
    readonly bool[] rainHit = new bool[RainCols];
    readonly SpriteRenderer[] rainBugs = new SpriteRenderer[RainCols * BugsPerCol], rainMark = new SpriteRenderer[RainCols], rainLane = new SpriteRenderer[RainCols];
    readonly Vector2[] rainOff = new Vector2[RainCols * BugsPerCol];
    // rope targets (DECISIONS 38)
    readonly SpriteRenderer[] hookGlow = new SpriteRenderer[Hooks.Length];
    bool hatchHook, hatchLatched, wasHookable; // hatchHook: an attack holds the hatch open for the rope; latched: the rope is on its way
    float hookCd, hookOpenedAt;

    // slam shockwaves (DECISIONS 42)
    readonly float[] shockX = new float[ShockSlots], shockDir = new float[ShockSlots], shockAge = new float[ShockSlots], shockDust = new float[ShockSlots];
    readonly bool[] shockOn = new bool[ShockSlots], shockHit = new bool[ShockSlots];
    readonly SpriteRenderer[] shockBody = new SpriteRenderer[ShockSlots * ShockCols], shockCap = new SpriteRenderer[ShockSlots * ShockCols];
    SpriteRenderer slamFlash;
    float slamFlashT, shockSpeed;
    int shockA = -1, shockB = -1; // the pair the latest slam launched (a late parry cancels it)

    // E storm slam (playtest 11-13): the blade charges red lightning over the centre, slams, and the whole floor stays live for
    // StormFloorTime (one hit for feet on it). Safe: a wall perch, a hook (rope), a roof chunk, or a dash at the right moment.
    static readonly float[] StormCharge = { 1.6f, 1.4f, 1.25f }; // by phase (T): s of crackle before the slam (plus the tell)
    const float StormX = 15f, StormFloorTime = 2f /* < GrappleHangMax 2.5: a rope caught late in the charge holds through it */, StormFloorH = 0.4f, StormFlick = 0.06f;
    static readonly Color StormRed = new Color32(0xFF, 0x2A, 0x2A, 0xFF), StormCore = new Color32(0xFF, 0xD6, 0xCC, 0xFF);
    DiceSkillKit.Segs stormSegs;
    SpriteRenderer stormFloor, stormGlow; // glow: a tall red haze over the live floor
    float stormCharge = -1f, stormLeft, stormFlickT;
    bool stormHit, stormDrawn;

    SpriteRenderer zoneFloor, zoneLane, white, sweepFill; // DECISIONS 40: no red lines (sweep centre line, danger lane side lines)
    float sweepY = -1f; // band centre while the B' warning shows, < 0 = off
    BossDebris debris; // DECISIONS 40: falling roof chunks to ride / rope
    static readonly int FlashId = Shader.PropertyToID("_Flash");
    readonly SpriteRenderer[] aimDots = new SpriteRenderer[14];
    readonly SpriteRenderer[] coreBracket = new SpriteRenderer[8]; // gold lock-on corners around the open core (DECISIONS 32)
    float coreOpenedAt;
    bool aimOn, bugAim; // aim dots: hatch barrage / bug shot tell
    readonly SpriteRenderer[] props = new SpriteRenderer[6];
    readonly bool[] propBroken = new bool[6];
    int propsLeft;
    TextMeshPro hint;

    // ------------------------------------------------------------------ build (runtime, so the scene only holds this component)

    void Awake()
    {
        BossArt.Ensure();
        BossArt.DrawHolo(-1, -1);
        if (squareMat == null) { squareMat = new Material(Shader.Find("Sprites/Default")); bugMat = new Material(squareMat); }
        // empty slot -> boss-v2 draft parts in Resources/BossArt (Codex), else the code-drawn BossArt
        if (bodyArt == null) bodyArt = Res("boss_body");
        if (hatchArt == null) hatchArt = Res("boss_hatch_half");
        if (bugArmArt == null) bugArmArt = Res("boss_bug_arm");
        if (bladeArmArt == null) bladeArmArt = Res("boss_blade_arm");
        Sprite hatchCore = Res("boss_hatch_core");
        body = MakePart("Body", new[] { BossArt.Body, BossArt.Body, BossArt.BodyFlash }, bodyArt, -107, BodyAt);
        crack1 = Make("Cracks1", BossArt.Crack1, "Background", -106, BodyAt);
        crack2 = Make("Cracks2", BossArt.Crack2, "Background", -106, BodyAt);
        crack1.enabled = crack2.enabled = false;
        hatchInside = Make("HatchInside", hatchCore != null ? hatchCore : BossArt.HatchInside, "Background", -105, HatchAt);
        hatchL = MakePart("HatchL", BossArt.Hatch, hatchArt, -104, HatchAt);
        hatchR = MakePart("HatchR", BossArt.Hatch, hatchArt, -104, HatchAt);
        hatchR.Sr.flipX = true;
        holo = Make("Hologram", BossArt.Holo, "Background", -103, HoloAt);
        bugArm = MakePart("BugArm", BossArt.BugArm, bugArmArt, -102, BugRest, Res("boss_bug_arm_lit"));
        blade = MakePart("BladeArm", BossArt.BladeArm, bladeArmArt, -101, BladeRest, Res("boss_blade_arm_lit"));
        Sprite coreArt = hatchCore != null ? hatchCore : BossArt.HatchInside;
        core = MakePart("WeakCore", new[] { coreArt, coreArt, coreArt }, coreArt, -99, HatchAt); // hidden while inside the hatch
        coreCable = Make("CoreCable", ParryFx.Square, "Background", -100, HatchAt);
        core.Sr.enabled = coreCable.enabled = false;

        for (int i = 0; i < bugs.Length; i++)
        {
            bugs[i] = Make("Bug", BossArt.Bug, "FX", 40, Vector2.zero);
            bugs[i].transform.localScale = new Vector3(2f, 2f, 1f); // FX: 1-px bugs read as rain on the big boss; 2x pixels
            bugs[i].enabled = false;
        }
        for (int i = 0; i < rainBugs.Length; i++)
        {
            rainBugs[i] = Make("RainBug", BossArt.Bug, "FX", 40, Vector2.zero);
            rainBugs[i].transform.localScale = new Vector3(2f, 2f, 1f); // same 2x pixels as the walls
            rainBugs[i].enabled = false;
        }
        for (int c = 0; c < RainCols; c++)
        {
            rainLane[c] = Make("RainLane", ParryFx.Square, "World", 4, Vector2.zero);
            rainLane[c].transform.localScale = new Vector3(RainHalfW * 2f + 0.4f, 16f, 1f);
            rainMark[c] = Make("RainMark", ParryFx.Square, "World", 5, Vector2.zero);
            rainMark[c].transform.localScale = new Vector3(RainHalfW * 2f + 0.4f, 0.25f, 1f);
            rainLane[c].enabled = rainMark[c].enabled = false;
        }
        for (int i = 0; i < Hooks.Length; i++) // ceiling hooks: a thin cable from the top, a neon ring (diamond) with a soft pulsing glow
        {
            float top = 17f, y0 = Hooks[i].y + 0.22f;
            SpriteRenderer cable = Make("HookCable", ParryFx.Square, "World", -4, new Vector2(Hooks[i].x, (top + y0) * 0.5f));
            cable.transform.localScale = new Vector3(2f * Px, top - y0, 1f);
            cable.color = new Color(0.55f, 0.6f, 0.7f, 0.6f);
            for (int k = 0; k < 3; k++)
            {
                SpriteRenderer r = Make(k == 0 ? "HookGlow" : "Hook", ParryFx.Square, "World", -3 + k, Hooks[i]);
                float size = k == 0 ? 0.9f : k == 1 ? 0.44f : 0.26f;
                r.transform.localScale = new Vector3(size, size, 1f);
                r.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
                r.color = k == 1 ? DiceSkillKit.Cyan : k == 2 ? (Color)new Color32(0x0B, 0x0F, 0x1E, 0xFF) : DiceSkillKit.A(DiceSkillKit.Cyan, 0.2f);
                if (k == 0) hookGlow[i] = r;
            }
        }
        zoneLane = Make("DangerLane", ParryFx.Square, "World", 4, Vector2.zero);
        zoneFloor = Make("DangerZone", ParryFx.Square, "World", 5, Vector2.zero);
        zoneLane.transform.localScale = new Vector3(ZoneHalf * 2f, 8f, 1f);
        zoneFloor.transform.localScale = new Vector3(ZoneHalf * 2f, 0.25f, 1f);
        zoneLane.color = new Color(1f, 0.23f, 0.23f, 0.12f);
        zoneLane.enabled = zoneFloor.enabled = false;
        stormFloor = Make("StormFloor", ParryFx.Square, "World", 5, new Vector2(RoomW * 0.5f, Floor + 0.125f));
        stormFloor.transform.localScale = new Vector3(RoomW, 0.25f, 1f);
        stormFloor.enabled = false;
        stormGlow = Make("StormGlow", ParryFx.Square, "World", 4, new Vector2(RoomW * 0.5f, Floor + 0.9f));
        stormGlow.transform.localScale = new Vector3(RoomW, 1.8f, 1f);
        stormGlow.enabled = false;
        stormSegs = DiceSkillKit.Segs.Create("StormBolts");
        stormSegs.transform.SetParent(transform, false);
        sweepFill = Make("SweepBand", ParryFx.Square, "World", 4, Vector2.zero);
        sweepFill.enabled = false;
        for (int i = 0; i < shockBody.Length; i++)
        {
            shockBody[i] = Make("ShockWave", ParryFx.Square, "FX", 34, Vector2.zero);
            shockCap[i] = Make("ShockCrest", ParryFx.Square, "FX", 35, Vector2.zero);
            shockBody[i].enabled = shockCap[i].enabled = false;
        }
        slamFlash = Make("SlamFlash", ParryFx.Square, "FX", 33, Vector2.zero);
        slamFlash.transform.localScale = new Vector3(3.5f, 6f * Px, 1f);
        slamFlash.enabled = false;
        debris = gameObject.AddComponent<BossDebris>();
        debris.Setup(this, debrisArt);
        for (int i = 0; i < aimDots.Length; i++)
        {
            aimDots[i] = Make("AimDot", ParryFx.Square, "FX", 30, Vector2.zero);
            aimDots[i].transform.localScale = new Vector3(2f * Px, 2f * Px, 1f);
            aimDots[i].enabled = false;
        }
        for (int i = 0; i < coreBracket.Length; i++)
        {
            coreBracket[i] = Make("CoreBracket", ParryFx.Square, "FX", 44, Vector2.zero);
            coreBracket[i].enabled = false;
        }
        for (int i = 0; i < props.Length; i++) props[i] = Make("MachineProp", BossArt.Props[PropKind[i]], "World", -5, new Vector2(PropX[i], Floor));
        propsLeft = props.Length;
        white = Make("WhiteOut", ParryFx.Square, "FX", 900, new Vector2(15f, 8.4375f));
        white.transform.localScale = new Vector3(40f, 24f, 1f);
        white.enabled = false;

        var go = new GameObject("EndingHint");
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(15f, 12.5f, 0f);
        hint = go.AddComponent<TextMeshPro>();
        if (font != null) hint.font = font;
        hint.fontSize = 4f;
        hint.alignment = TextAlignmentOptions.Center;
        hint.color = GoldShot;
        hint.rectTransform.sizeDelta = new Vector2(24f, 2f);
        var mr = go.GetComponent<MeshRenderer>();
        mr.sortingLayerName = "FX";
        mr.sortingOrder = 200;
        go.SetActive(false);
    }

    SpriteRenderer Make(string name, Sprite s, string layer, int order, Vector2 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        Material m = s == ParryFx.Square ? squareMat : s == BossArt.Bug ? bugMat : null;
        if (m == null) ownMats.Add(m = new Material(squareMat));
        sr.sharedMaterial = m;
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        return sr;
    }

    static Sprite Res(string name) => Resources.Load<Sprite>("BossArt/" + name);

    // override art: Normal / Lit (= art when no lit sprite: glow becomes a tint blink) / Flash (white tint, see Show)
    Part MakePart(string name, Sprite[] v, Sprite art, int order, Vector2 pos, Sprite artLit = null)
    {
        var p = new Part { V = art != null ? new[] { art, artLit != null ? artLit : art, art } : v, Pos = pos };
        p.Sr = Make(name, p.V[BossArt.Normal], "Background", order, pos);
        Shader flash = art != null ? Shader.Find("Number/SpriteSolidFlash") : null; // override art: solid white hit flash
        if (flash != null) { var m = new Material(flash); ownMats.Add(m); p.Sr.sharedMaterial = m; }
        return p;
    }

    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        if (player == null) { Debug.LogWarning("[BossFight] No player in the scene."); return; }
        input = player.GetComponent<PlayerInputReader>();
        playerMask = 1 << player.gameObject.layer;
        groundMask = LayerMask.GetMask("Ground");
        ring = ParryRing.For(gameObject);
        if (resumeHp > 0 && resumeHp < hp) // death reload mid-fight: start at the saved phase
        {
            hp = resumeHp;
            shownPhase = Phase;
            crack1.enabled = shownPhase >= 2;
            crack2.enabled = shownPhase >= 3;
        }
        StartCoroutine(Director());
    }

    void OnEnable() { PlayerController.Swung += OnSwing; PlayerController.IssenAlt = this; PlayerController.GrappleAlt = this; }
    void OnDisable()
    {
        Held = false;
        PlayerController.Swung -= OnSwing;
        if (ReferenceEquals(PlayerController.IssenAlt, this)) PlayerController.IssenAlt = null;
        if (ReferenceEquals(PlayerController.GrappleAlt, this)) PlayerController.GrappleAlt = null;
    }

    void OnDestroy()
    {
        foreach (Material m in ownMats) Destroy(m);
        if (ownLock) GameFlow.InputLocked = false; // scene unload mid-collapse: never leave the game locked
    }

    void Update()
    {
        if (player == null || Held) return; // dice face 4: frozen in place, nothing redrawn
        // Playtest 11-12: the player once fell out of the room (cause not reproduced). Safety net: below the lane floor -> back on the bridge.
        if (player.transform.position.y < LaneY - 2f)
        {
            var body = player.GetComponent<Rigidbody2D>();
            Vector3 back = new Vector3(4f, Floor + 1.02f, 0f); // the player's start spot, centre = feet + 1
            if (body != null) { body.linearVelocity = Vector2.zero; body.position = back; }
            player.transform.position = back;
            Debug.LogWarning("[BossFight] Player fell out of the room; put back on the bridge.");
        }
        UpdateWaves(Dt);
        UpdateRain(Dt);
        UpdateShock(Dt);
        UpdateStorm(Dt);
        hookCd -= Dt;
        bool hk = HatchHookable;
        if (hk && !wasHookable) // DECISIONS 38 cue: a cyan ring off the hatch + a soft chime; the cyan corners (DrawBracket) snap on
        {
            hookOpenedAt = Time.time;
            DiceSkillKit.Ring(HatchAt, 0.5f, 2f * Px, DiceSkillKit.Cyan, 0.3f, 2.5f, 97);
            Sfx.Play(SfxId.HackOk, 0.35f, 1.6f);
        }
        wasHookable = hk;
        if (!dead) CheckReflectedShots();
        Draw();
    }

    // ------------------------------------------------------------------ director

    IEnumerator Director()
    {
        for (float t = 0f; player.transform.position.x < StartX && t < StartAfter; t += Dt) yield return null;
        started = true;
        Bgm.Play(BgmId.Boss);
        Sfx.Play(SfxId.LastTell);
        Juice.Shake(Juice.ShakeKillAmp, 0.4f);
        if (StageRadio.FirstTime("STG05-boss")) // dialogue B01 (Codex concept-only proposal 11-13): a blocking talk, then phase 1
        {
            Say("@mode talk\nbroadcast | 오늘의 행운의 숫자는 0. 이동을 중지하십시오. | Today's lucky number is 0. Cease movement.\n" +
                "yudo | 여기까지 걸어온 게 아깝잖아. | After I walked all this way?\n");
            yield return null; // let the talk box lock input
            while (GameFlow.InputLocked) yield return null;
        }
        yield return Wait(1f);
        int last = -1;
        while (true)
        {
            if (GameFlow.IsPlayerDead(player)) yield break; // respawn reload follows
            int ph = Phase;
            if (ph != shownPhase) yield return PhaseShift(ph);
            // 0 A, 1 B, 2 C, 3 B' sweep, 4 D bug rain, 5 E storm slam (playtest 11-13). Phase 1 has no C; later phases weight the bug attacks double (DECISIONS 38).
            // No repeats, and never two blade attacks in a row.
            int atk;
            int[] pool = ph == 1 ? PhaseOnePool : LaterPool;
            do atk = pool[Random.Range(0, pool.Length)];
            while (last >= 0 && (atk == last || (IsBlade(atk) && IsBlade(last))));
            last = atk;
            int n = ph == 1 ? Random.Range(4, 6) : ph == 2 ? Random.Range(5, 8) : Random.Range(6, 9); // +1 each (DECISIONS 33)
            float tell = ph == 1 ? 0.8f : 0.65f; // was 1 / 0.8
            BossArt.DrawHolo(atk == 3 || atk == 5 ? BossArt.IconBlade : atk == 4 ? BossArt.IconBug : atk, n); // icon index == attack index (sweep / rain reuse blade / bug)
            Sfx.Play(SfxId.EnemyTell);
            // playtest 11-13 (user: three patterns at once is too hard): no side barrage over A (it already fires bug shots) or the storm
            if (atk != 2 && atk != 0 && atk != 5 && (ph == 3 ? atk == 1 || Random.value < 0.5f : ph == 2 && Random.value < 0.35f)) // overlap (phase 2 too, DECISIONS 33)
                StartCoroutine(AttackC(Mathf.Max(3, n - 2), tell, true));
            yield return atk == 5 ? AttackStorm(tell) : atk == 0 ? AttackA(n, tell) : atk == 1 ? AttackB(n, tell) : atk == 3 ? AttackSweep(n, tell) : atk == 4 ? AttackRain(n, tell) : AttackC(n, tell, false);
            BossArt.DrawHolo(-1, -1);
            while (sideRunning) yield return null;
            yield return CoreWindow();
        }
    }

    static void Say(string script) // boss dialogue (B01-B05), same format as the dialogue files
    {
        if (DialoguePlayer.Instance != null) DialoguePlayer.Instance.Play(new TextAsset(script));
    }

    static readonly int[] PhaseOnePool = { 0, 4, 1, 3, 5 }, LaterPool = { 0, 0, 4, 4, 1, 2, 3, 5 };
    static bool IsBlade(int atk) => atk == 1 || atk == 3 || atk == 5;

    IEnumerator PhaseShift(int ph)
    {
        // DECISIONS 32: the core / chest bursts first (white flash, burst, gold debris, shake), then the cracks show
        Vector2 c = core.Sr.enabled ? core.Pos : HatchAt;
        // Playtest 11-13 (user: make the phase change scarier, pixelfx): phase 2 = "override rupture" (red shockwave + glitch tear),
        // phase 3 = "crimson crown" (red pillar + forked lightning). Both charge first and burst ~0.4 s in, so the old burst waits for it.
        SpriteFx.Play(ph >= 3 ? "boss-phase-crown" : "boss-phase-rupture", c, false, 103);
        Sfx.Play(SfxId.LastTell);
        yield return Wait(ph >= 3 ? 0.36f : 0.42f);
        body.FlashUntil = Time.time + 0.2f;
        DiceSkillKit.Overlay.Get().Flash(Color.white, 0.7f, 0.25f);
        DiceSkillKit.Overlay.Get().Glitch(0.35f);
        SpriteFx.Play("kill-burst", c, false, 102, 2f);
        CoreHitFx(3, c);
        Sfx.Play(SfxId.Hit, 1f, 0.6f);
        Juice.Shake(0.25f, 0.5f);
        yield return Wait(0.35f);
        shownPhase = ph;
        if (ph == 2) Say("@mode radio\nbroadcast | 저항 수치를 다시 계산합니다. | Recalculating resistance. | 3\n"); // B02
        else if (ph == 3) Say("@mode radio\nbroadcast | 허용되는 결과는 0입니다. | The permitted outcome is 0. | 3\n"); // B03
        resumeHp = ph == 2 ? MaxHp * 2 / 3 : ph == 3 ? MaxHp / 3 : 0; // checkpoint: the full phase, not the HP left at the shift
        crack1.enabled = ph >= 2;
        crack2.enabled = ph >= 3;
        body.FlashUntil = Time.time + 0.12f;
        for (int i = 0; i < 4; i++) // parts burst off the body
            SpriteFx.Play("kill-burst", BodyAt + new Vector2(Random.Range(4f, 20f), Random.Range(5f, 12f)), Random.value < 0.5f, 90);
        Sfx.Play(SfxId.Stunned);
        Juice.Shake(Juice.ShakeKillAmp, 0.45f);
        yield return Wait(1f);
    }

    // ---- Core (DECISIONS 31): the rest between attacks. Hatch opens, the core drops to the bridge, stays CoreOpen, pulls back ----
    IEnumerator CoreWindow()
    {
        // DECISIONS 38 vent: the hatch opens first with the core still up inside (rope window, cyan corners), then the core drops
        hatchHook = true;
        for (float t = 0f; t < VentTime; t += Dt) { hatchOpen = Mathf.Max(hatchOpen, Mathf.Min(1f, t / 0.25f)); yield return null; }
        hatchHook = false;
        // DECISIONS 32 tell: a gold ring bursts out of the hatch with a chime ("chance"); the lock-on corners snap on when it lands
        Sfx.Play(SfxId.HackOk, 0.7f, 1.2f);
        DiceSkillKit.Ring(HatchAt, 0.5f, 2f * Px, GoldShot, 0.35f, 3.5f, 97);
        SpriteFx.Play("impact-ring", HatchAt, false, 101, 1.5f);
        for (float t = 0f; t < CoreDrop; t += Dt)
        {
            float u = Smooth(t / CoreDrop);
            hatchOpen = Mathf.Max(hatchOpen, u);
            core.Pos = Vector2.Lerp(HatchAt, CoreLow, u);
            yield return null;
        }
        core.Pos = CoreLow;
        hatchOpen = 1f;
        Juice.Shake(Juice.ShakeHurtAmp, 0.15f, Vector2.down);
        SpriteFx.Play("impact-ring", CoreLow, false, 101);
        coreOpen = true;
        coreOpenedAt = Time.time;
        yield return Wait(CoreOpen);
        coreOpen = false;
        for (float t = 0f; t < CoreRetract; t += Dt)
        {
            float u = Smooth(t / CoreRetract);
            core.Pos = Vector2.Lerp(CoreLow, HatchAt, u);
            hatchOpen = 1f - u;
            yield return null;
        }
        core.Pos = HatchAt;
        hatchOpen = 0f;
    }

    // RMB issen (PlayerController.IIssenTarget): the open core near the mouse and within issen range; land just past it on the bridge
    public bool PlanIssen(Vector2 mouse, Vector2 from, out Vector2 at, out Vector2 spot)
    {
        at = core.Pos;
        spot = default;
        float aim = DashRules.IssenMouseRadius + CoreHalf;
        if (dead || !coreOpen || (mouse - at).sqrMagnitude > aim * aim || Vector2.Distance(from, at) > DashRules.IssenRange) return false;
        float side = from.x <= at.x ? 1f : -1f, x = at.x + side * CoreIssenPast;
        if (x < 1.5f || x > RoomW - 1.5f) x = at.x - side * CoreIssenPast; // room edge: stop in front instead
        spot = new Vector2(x, Floor + 1.02f); // player centre = feet + 1
        return true;
    }

    public void IssenHit(Vector2 at) { Damage(CoreIssenDamage, core, at); LuckyDice.AddHitCharge(); }

    // ---- Rope targets (DECISIONS 38, PlayerController.IGrappleTarget): the hook or open hatch nearest the mouse within HookAim, in reach ----
    bool HatchHookable => hatchHook && !dead && hookCd <= 0f && core.Pos.y >= HatchAt.y - 0.05f; // open, the core still up inside

    public bool PlanGrapple(Vector2 mouse, Vector2 hand, out Vector2 anchor, out Vector2 pull, out bool hang)
    {
        anchor = pull = default;
        hang = false;
        float best = HookAim * HookAim;
        bool hatch = HatchHookable && Nearer(mouse, hand, HatchAt, ref best);
        if (hatch) { anchor = HatchAt; pull = HatchAt + Vector2.down * HatchStrikeBelow; }
        foreach (Vector2 h in Hooks)
            if (Nearer(mouse, hand, h, ref best)) { anchor = h; pull = h + Vector2.down * HangDrop; hang = true; hatch = false; }
        if (hatch) { hatchLatched = true; hookCd = HatchHookEvery; } // the latch spends the window: the strike lands even if the hatch shuts meanwhile
        return best < HookAim * HookAim;
    }

    static bool Nearer(Vector2 mouse, Vector2 hand, Vector2 at, ref float best)
    {
        float d = (mouse - at).sqrMagnitude;
        if (d >= best || Vector2.Distance(hand, at) > DashRules.GrappleRange) return false;
        best = d;
        return true;
    }

    // The hero arrived under the latched hatch: one strong hit on the raised core (counts for the dice like any landed hit)
    public bool GrappleStrike(Vector2 at)
    {
        if (dead || !hatchLatched) return false;
        hatchLatched = false;
        hatchL.FlashUntil = hatchR.FlashUntil = Time.time + 0.1f;
        Damage(HatchHookDamage, core, HatchAt + Vector2.down * 0.3f); // core hit FX (ring + gold bits) at the hatch mouth
        LuckyDice.AddHitCharge();
        player.DuelHitStop(Juice.HitStopKill);
        return true;
    }

    // The one entry point for skill damage (dice: LuckyDice.BossSkill with DiceDamage(face)). Hits whether or not the core is out;
    // the core flashes when it is out, else the whole body does. fxAt = where the hit spark / ring plays.
    public void SkillHit(int dmg, Vector2 fxAt)
    {
        if (dead) return; // killed while the talismans flew (LMB on the core): no number over the corpse
        Damage(dmg, core.Sr.enabled ? core : body, fxAt);
        PopNumber(dmg, fxAt + new Vector2(0f, 1.4f)); // DECISIONS 32: the damage pops big over the core, then shatters
    }

    // ---- A: bug swarm walls, then the arm slumps (opening) ----
    int lastGap = -1;

    IEnumerator AttackA(int n, float tell)
    {
        bugArm.Glow = true; // stays lit while it controls the swarm (DECISIONS 36)
        yield return Wait(tell);
        for (int i = 0; i < n; i++)
        {
            int g;
            do g = GapPick[Random.Range(0, GapPick.Length)]; while (g == 2 && lastGap == 2);
            lastGap = g;
            LaunchWave(GapLows[g], GapHs[g]);
            if (i == 0) StartCoroutine(BugShots()); // runs until the last wall leaves
            if (i < n - 1) yield return Conduct(WaveEvery);
        }
        yield return Conduct(-1f);
        yield return SlumpArm();
    }

    // A / D opening: the bug arm drops onto the bridge for SlumpTime (LMB swings on it hurt), then goes back up
    IEnumerator SlumpArm()
    {
        bugArm.Glow = false;
        yield return Move(bugArm, BugSlump, 0.2f);
        Juice.Shake(Juice.ShakeHurtAmp, 0.25f);
        Sfx.Play(SfxId.Stunned);
        armVulnerable = true;
        yield return Wait(SlumpTime);
        armVulnerable = false;
        yield return Move(bugArm, BugRest, 0.6f);
    }

    // ---- D: bug rain (DECISIONS 38). The lit arm conducts while columns dive at the player's x, then slumps like A ----
    IEnumerator AttackRain(int n, float tell)
    {
        bugArm.Glow = true;
        yield return Conduct(tell);
        SpriteFx.Play("enemy-warp-in", bugArm.Pos + new Vector2(0f, 1f), false, 101); // the swarm bursts up out of the palm
        float every = RainEvery[Phase - 1];
        for (int i = 0; i < n; i++)
        {
            LaunchColumn(player.transform.position.x);
            yield return Conduct(every);
        }
        yield return Conduct(-1f);
        yield return SlumpArm();
    }

    void LaunchColumn(float x)
    {
        int c = System.Array.IndexOf(rainStage, 0);
        if (c < 0) return;
        rainStage[c] = 1;
        rainT[c] = 0f;
        rainHit[c] = false;
        x = Mathf.Clamp(x, 0.5f, RoomW - 0.5f); // was 1.5: hugging the wall on a perch (x 0.45..0.65) no column could reach (DECISIONS 45)
        // playtest 11-12 (2nd): a column over the grate fell through it into the lane even with the player on the deck, so the bugs looked
        // like they dropped out of the floor. Now it lands on the grate top, and only falls into the lane when the player is down there.
        bool lane = OverLane(x) && player.transform.position.y - 1f < Floor - 0.5f; // player feet below the deck
        if (lane) x = Mathf.Clamp(x, LaneX1 + RainHalfW, LaneX2 - RainHalfW); // whole column inside the lane walls
        rainX[c] = Snap(x);
        rainG[c] = lane ? LaneY : Floor;
        for (int i = c * BugsPerCol; i < (c + 1) * BugsPerCol; i++) rainOff[i] = new Vector2(Random.Range(-RainHalfW, RainHalfW), Random.value * RainLen);
        rainLane[c].transform.position = new Vector3(rainX[c], Floor + 8f, 0f);
        rainMark[c].transform.position = new Vector3(rainX[c], rainG[c] + 0.125f, 0f);
        rainLane[c].enabled = rainMark[c].enabled = true;
        Sfx.Play(SfxId.EnemyTell, 0.5f, 1.3f);
    }

    void UpdateRain(float dt)
    {
        Vector2 pp = player.transform.position; // player centre = feet + 1, hit box like the walls
        int flutter = Time.frameCount / 4;
        bool blink = Mathf.FloorToInt(Time.time * 8f) % 2 == 0;
        for (int c = 0; c < RainCols; c++)
        {
            if (rainStage[c] == 0) continue;
            rainT[c] += dt;
            float x = rainX[c], g = rainG[c];
            if (rainStage[c] == 1)
            {
                rainMark[c].color = new Color(1f, 0.23f, 0.23f, blink ? 0.85f : 0.5f);
                rainLane[c].color = new Color(1f, 0.23f, 0.23f, blink ? 0.12f : 0.06f);
                if (rainT[c] < RainWarn) continue;
                rainStage[c] = 2;
                rainT[c] = 0f;
                rainLane[c].enabled = false;
                for (int i = c * BugsPerCol; i < (c + 1) * BugsPerCol; i++) rainBugs[i].enabled = true;
                Sfx.Play(SfxId.Swing, 0.5f, 1.4f);
            }
            float head = RainTop - RainSpeed * rainT[c];
            if (rainStage[c] == 2 && head <= g) // landed: a puff of dust and the clump is gone (playtest 11-13: no lingering floor carpet)
            {
                rainStage[c] = 0;
                rainMark[c].enabled = false;
                for (int i = c * BugsPerCol; i < (c + 1) * BugsPerCol; i++) rainBugs[i].enabled = false;
                SpriteFx.Play("land-dust", new Vector2(x - 0.6f, g), true, 101);
                SpriteFx.Play("land-dust", new Vector2(x + 0.6f, g), false, 101);
                Sfx.Play(SfxId.Hit, 0.35f, 1.4f);
                continue;
            }
            if (rainStage[c] == 3 && rainT[c] >= CarpetTime)
            {
                rainStage[c] = 0;
                for (int i = c * BugsPerCol; i < (c + 1) * BugsPerCol; i++) rainBugs[i].enabled = false;
                continue;
            }
            bool carpet = rainStage[c] == 3;
            float half = carpet ? Mathf.Lerp(RainHalfW, CarpetHalf, Smooth(rainT[c] / 0.15f)) : RainHalfW;
            if (!rainHit[c] && dt > 0f && Mathf.Abs(pp.x - x) < half + 0.35f &&
                (carpet ? pp.y - 0.85f < g + CarpetH && pp.y - 0.85f > g - 0.5f : pp.y - 0.85f < head + RainLen && pp.y + 0.8f > head) && // a carpet hits its own level only
                player.ReceiveEnemyHit(new HitInfo { Damage = 1, SourcePosition = new Vector2(x, pp.y), Unparryable = true }) == HitResult.Hit)
                rainHit[c] = true; // one hit per column (dash / i-frames pass through)
            Color tint = new Color(1f, 1f, 1f, carpet ? Mathf.Clamp01((CarpetTime - rainT[c]) / 0.25f) : 1f);
            for (int i = c * BugsPerCol; i < (c + 1) * BugsPerCol; i++)
            {
                Vector2 o = rainOff[i];
                float jx = ((i + flutter) % 3 - 1) * Px * (carpet ? 2f : 1f), jy = ((i + flutter) & 1) * Px;
                Vector2 b = carpet ? new Vector2(x + o.x / RainHalfW * half, g + 0.15f + 2f * jy) : new Vector2(x + o.x, head + o.y);
                if (carpet && g < Floor) b.x = Mathf.Clamp(b.x, LaneX1 + 0.1f, LaneX2 - 0.1f); // a lane carpet stays in the lane
                rainBugs[i].transform.position = new Vector3(Snap(b.x + jx), Snap(b.y + jy), 0f);
                rainBugs[i].color = tint;
            }
        }
    }

    bool BugsOut() // walls or rain columns still in the room
    {
        if (System.Array.IndexOf(waveOn, true) >= 0) return true;
        foreach (int st in rainStage) if (st != 0) return true;
        return false;
    }

    void LaunchWave(float lo, float h)
    {
        int w = System.Array.IndexOf(waveOn, false);
        if (w < 0) return;
        waveOn[w] = true;
        waveHit[w] = false;
        waveX[w] = -1.5f;
        gapLo[w] = lo;
        gapH[w] = h;
        float span = WaveTop - Floor - h;
        for (int i = w * BugsPerWave; i < (w + 1) * BugsPerWave; i++)
        {
            float y = Floor + Random.value * span;
            if (y >= lo) y += h;
            if (lo > Floor && i % 8 == 0) y = Random.Range(LaneY, Floor); // a jump-gap wall fills the under-bridge lane too (DECISIONS 45)
            bugOff[i] = new Vector2(Random.Range(-WaveHalfW, WaveHalfW), y);
            bugs[i].enabled = true;
        }
        Sfx.Play(SfxId.EnemyTell, 0.6f);
        SpriteFx.Play("enemy-warp-in", bugArm.Pos + new Vector2(0f, 1f), false, 101); // the swarm bursts out of the palm
    }

    // the bug arm conducts the swarm: figure-8 sway around its rest spot for s seconds (s < 0: until the last wall / rain column is gone)
    IEnumerator Conduct(float s)
    {
        for (float t = 0f; s < 0f ? BugsOut() : t < s; )
        {
            float dt = Dt;
            t += dt;
            conductT += dt * ConductSpeed;
            bugArm.Pos = BugRest + Vector2.Scale(ConductAmp, new Vector2(Mathf.Sin(conductT), Mathf.Sin(2f * conductT)));
            yield return null;
        }
    }

    // DECISIONS 36: the bug shots come from the centre (chest hatch) with the hatch's tell: glow, a crack open, the aim dots
    IEnumerator BugShots()
    {
        yield return Wait(BugShotEvery * 0.5f);
        while (System.Array.IndexOf(waveOn, true) >= 0)
        {
            if (!sideRunning) // a side barrage owns the hatch (and keeps the screen busy)
            {
                hatchL.Glow = hatchR.Glow = bugAim = hatchHook = true; // the cracked hatch is a rope window too (DECISIONS 38)
                Sfx.Play(SfxId.EnemyTell, 0.4f, 1.6f);
                for (float t = 0f; t < BugShotTell; t += Dt) { hatchOpen = BugShotCrack * t / BugShotTell; yield return null; }
                hatchL.Glow = hatchR.Glow = bugAim = false;
                Vector2 d = (Vector2)player.transform.position - HatchAt;
                SpriteFx.Play("sniper-muzzle", HatchAt, d.x < 0f, 102);
                Fire(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, BugShotSpeed);
                for (float t = 0f; t < 0.2f; t += Dt) { hatchOpen = BugShotCrack * (1f - t / 0.2f); yield return null; }
                hatchOpen = 0f;
                hatchHook = false;
            }
            yield return Wait(BugShotEvery);
        }
    }

    void UpdateWaves(float dt)
    {
        Vector2 pp = player.transform.position; // player centre = feet + 1, box 0.9 x 2 (hit test a little smaller)
        int flutter = Time.frameCount / 4;
        for (int w = 0; w < Waves; w++)
        {
            if (!waveOn[w]) continue;
            waveX[w] += WaveSpeed * dt;
            if (waveX[w] > RoomW + 1.5f)
            {
                waveOn[w] = false;
                for (int i = w * BugsPerWave; i < (w + 1) * BugsPerWave; i++) bugs[i].enabled = false;
                continue;
            }
            if (!waveHit[w] && dt > 0f && Mathf.Abs(pp.x - waveX[w]) < WaveHalfW + 0.35f)
            {
                float feet = pp.y - 0.85f, top = pp.y + 0.8f;
                if (InLane) { feet = Floor + 0.15f; top = Floor + 1.8f; } // DECISIONS 45: the lane is judged like the deck (floor gap = safe)
                if (feet < WaveTop && (feet < gapLo[w] || top > gapLo[w] + gapH[w]) &&
                    player.ReceiveEnemyHit(new HitInfo { Damage = 1, SourcePosition = new Vector2(waveX[w] - 1f, pp.y), Unparryable = true }) == HitResult.Hit)
                    waveHit[w] = true; // one hit per wall (a dash / i-frames pass through)
            }
            for (int i = w * BugsPerWave; i < (w + 1) * BugsPerWave; i++)
            {
                Vector2 o = bugOff[i];
                if (o.y < Floor) bugs[i].enabled = OverLane(waveX[w] + o.x); // lane bugs show only over the grate (inside the deck elsewhere)
                bugs[i].transform.position = new Vector3(Snap(waveX[w] + o.x) + ((i + flutter) % 3 - 1) * Px, Snap(o.y) + ((i + flutter) & 1) * Px, 0f);
            }
        }
    }

    // ---- B: danger zone, N slams (parry ring), last one sticks -> clash duel (opening) ----
    IEnumerator AttackB(int n, float tell)
    {
        float zx = Snap(Mathf.Clamp(player.transform.position.x, 3.5f, RoomW - 3.5f));
        ShowZone(zx, true);
        blade.Glow = true;
        Vector2 from = blade.Pos, above = new Vector2(zx, BladeHigh);
        for (float t = 0f; t < tell; t += Dt) { blade.Pos = Vector2.Lerp(from, above, Smooth(t / tell)); yield return null; }
        blade.Pos = above;
        blade.Glow = false;
        // Per-slam clock tt (0 = the previous slam resolved): slide to the new x in SlamShift, hold, drop in the last 0.12 s, slam at s.
        // First slam: the blade is already above zx (tell), slam at FirstSlam as before.
        float aim = SlamAim[Phase - 1], s = FirstSlam, tt = 0f, lockedUntil = -10f, fromX = zx, pressAt = -1f;
        int k = 0;
        bool struck = false, parried = false, hitAtImpact = false;
        ParryHeight h = ParryRing.RandomHeight(); // per slam, presentation only
        while (k < n)
        {
            float dt = Dt;
            tt += dt;
            bool last = k == n - 1;
            // the player's own LMB judged here, like EliteDuel (no parry state for a boss part). A press on a frozen frame (hit-stop:
            // dt 0) is remembered at the frozen clock and judged when the clock runs again (DECISIONS 43); input locks still drop it.
            if (input != null && input.AttackPressedRaw && !GameFlow.InputLocked) pressAt = tt;
            if (pressAt >= 0f && dt > 0f)
            {
                float p = pressAt;
                pressAt = -1f;
                if (p < lockedUntil) { }
                else if (!parried && p >= s - WindowEarly && p - dt <= s + WindowLate) parried = true;
                else if (p < s - WindowEarly || p > s + WindowLate) lockedUntil = p + Lockout; // anti-mash
            }
            // DECISIONS 33: after the slide the blade keeps following the player until the ring shows, then the spot is locked
            if (!struck && dt > 0f && (k == 0 || tt >= SlamShift) && tt < s - RingLead)
                zx = Mathf.MoveTowards(zx, Mathf.Clamp(player.transform.position.x, 3.5f, RoomW - 3.5f), TrackSpeed * dt);
            if (!struck)
            {
                // k > 0: rise off the floor while sliding sideways to the new target, then hold high; drop in the last 0.12 s
                float u = k > 0 ? Smooth(tt / SlamShift) : 1f;
                float x = Mathf.Lerp(fromX, zx, u);
                float y = tt >= s - SlamDrop ? Mathf.Lerp(BladeHigh, Floor, (tt - (s - SlamDrop)) / SlamDrop) : k > 0 ? Mathf.Lerp(Floor, BladeHigh, u) : BladeHigh;
                blade.Pos = new Vector2(x, y);
                ShowZone(Snap(x), true); // the danger zone travels with the blade
            }
            bool inZone = InZone(zx);
            if (!struck && tt >= s)
            {
                struck = true;
                hitAtImpact = inZone && player.CurrentState != PlayerController.State.Dash && player.CurrentState != PlayerController.State.Issen; // judged at impact too: stepping out during the parry window must not dodge it (a dash through the impact still does)
                blade.Pos = new Vector2(zx, last ? BladeStuckY : Floor);
                // DECISIONS 43: a big impact (whole-number scales keep the pixel grid): 3x ring, burst, sparks, wide dust
                Juice.Shake(0.22f, 0.25f, Vector2.down);
                Sfx.Play(SfxId.Hit, 0.85f, 0.6f);
                SpriteFx.Play("impact-ring", new Vector2(zx, Floor + 0.25f), false, 101, 3f);
                SpriteFx.Play("kill-burst", new Vector2(zx, Floor + 0.3f), false, 102, last ? 3f : 2f);
                SpriteFx.Play("hit-spark", new Vector2(zx, Floor + 0.5f), false, 103, 2f);
                SpriteFx.Play("land-dust", new Vector2(zx - 1.2f, Floor), true, 101, 2f);
                SpriteFx.Play("land-dust", new Vector2(zx + 1.2f, Floor), false, 101, 2f);
                SpriteFx.Play("land-dust", new Vector2(zx - 2.8f, Floor), true, 101, 2f);
                SpriteFx.Play("land-dust", new Vector2(zx + 2.8f, Floor), false, 101, 2f);
                if (last) Juice.Shake(0.3f, 0.35f, Vector2.down);
                if (!(inZone && parried)) LaunchShock(zx, inZone); // a clang stops the blade: no shockwave
                else shockA = shockB = -1;
            }
            if (struck && (parried || tt > s + WindowLate))
            {
                bool z = inZone || hitAtImpact;
                if (z && parried) { Clang(zx, h); CancelShock(); } // late parry (after the impact frame): take the waves back
                else if (z) player.ReceiveEnemyHit(new HitInfo { Damage = 1, SourcePosition = new Vector2(zx, player.transform.position.y), Unparryable = true });
                if (!z || !parried) ring.Hide();
                k++;
                struck = parried = hitAtImpact = false;
                h = ParryRing.RandomHeight();
                if (k < n) // re-target the player's x right now (DECISIONS 29); the anti-mash lock carries over into the new clock
                {
                    fromX = zx;
                    zx = Snap(Mathf.Clamp(player.transform.position.x, 3.5f, RoomW - 3.5f));
                    lockedUntil -= tt;
                    if (pressAt >= 0f) pressAt = Mathf.Max(0f, pressAt - tt); // a still-buffered press moves to the new clock
                    tt = 0f;
                    s = SlamShift + aim;
                }
            }
            else if (!struck && inZone && tt >= s - RingLead && (k == 0 || tt >= SlamShift))
            {
                int face = player.transform.position.x >= zx ? 1 : -1;
                ring.Show(new Vector2(player.transform.position.x - face * 0.45f, player.transform.position.y + ParryRing.Offset(h)), face, RingLead, s - tt, tt >= s - WindowEarly);
            }
            else if (!struck && !ring.Flashing) ring.Hide(); // let the clang's yellow merge flash finish (TestEnemy does the same)
            yield return null;
        }
        ShowZone(zx, false);
        // Mash window (DECISIONS 36): LMB swings on the steel hurt (OnSwing) while the arm jerks harder and harder to pull free
        var live = EnemyProjectile.Active; // a clean window: no leftover barrage shots while the player stands at the blade
        for (int i = live.Count - 1; i >= 0; i--) { GameObject g = live[i].gameObject; g.SetActive(false); Destroy(g); }
        stuckX = zx;
        mashHits = 0;
        BladeStuck = true;
        blade.Glow = true; // "hit me"
        Vector2 stuck = new Vector2(zx, BladeStuckY);
        float tugT = 0f, tugAmp = 0f;
        for (float t = 0f; t < StuckTime && mashHits < MashCap; )
        {
            float dt = Dt;
            t += dt;
            tugT += dt;
            float f = t / StuckTime;
            if (tugT >= Mathf.Lerp(TugEvery, TugEveryEnd, f)) // a jerk upward: dust and grit off the bridge
            {
                tugT = 0f;
                tugAmp = 0.15f + 0.35f * f;
                SpriteFx.Play("land-dust", new Vector2(zx - 0.6f, Floor), true, 101);
                SpriteFx.Play("land-dust", new Vector2(zx + 0.6f, Floor), false, 101);
                Sfx.Play(SfxId.Hit, 0.25f + 0.25f * f, 0.5f);
                Juice.Shake(Juice.ShakeHurtAmp * (0.4f + 0.6f * f), 0.1f, Vector2.up);
            }
            float jitter = (Time.frameCount / 2 % 2 == 0 ? 1f : -1f) * (0.03f + 0.09f * f) * (dt > 0f ? 1f : 0f);
            blade.Pos = stuck + new Vector2(jitter, tugAmp * Mathf.Max(0f, 1f - tugT / 0.15f));
            yield return null;
        }
        BladeStuck = false;
        blade.Glow = false;
        blade.Pos = stuck;
        // rips out: big dust burst, then flies back to its socket
        SpriteFx.Play("kill-burst", new Vector2(zx, Floor + 0.3f), false, 102);
        SpriteFx.Play("land-dust", new Vector2(zx - 0.9f, Floor), true, 101);
        SpriteFx.Play("land-dust", new Vector2(zx + 0.9f, Floor), false, 101);
        Sfx.Play(SfxId.Hit, 1f, 0.5f);
        Juice.Shake(Juice.ShakeKillAmp, 0.35f, Vector2.up);
        yield return Move(blade, new Vector2(zx, BladeHigh + 2f), 0.15f);
        yield return ReattachBlade();
    }

    // ---- E storm slam (playtest 11-13, the blade arm's own big move): charge red lightning over the centre, slam, live floor ----
    IEnumerator AttackStorm(float tell)
    {
        blade.Glow = true;
        stormFloor.color = new Color(1f, 0.23f, 0.23f, 0.3f);
        stormFloor.enabled = true; // blinking floor warning while it charges
        stormCharge = 0f;
        yield return Move(blade, new Vector2(StormX, BladeHigh + 1.5f), tell);
        Sfx.Play(SfxId.LastTell);
        float charge = StormCharge[Phase - 1];
        for (float t = 0f; t < charge; t += Dt)
        {
            stormCharge = t / charge;
            blade.Pos = new Vector2(StormX + (Time.frameCount / 2 % 2 == 0 ? 1f : -1f) * 0.04f * stormCharge, BladeHigh + 1.5f);
            yield return null;
        }
        stormCharge = -1f;
        yield return Move(blade, new Vector2(StormX, Floor), SlamDrop);
        Juice.Shake(0.3f, 0.45f, Vector2.down);
        DiceSkillKit.Overlay.Get().Flash(StormRed, 0.35f, 0.2f);
        Sfx.Play(SfxId.Stunned, 1f, 0.6f); // electric fizz, pitched down
        Sfx.Play(SfxId.Hit, 1f, 0.5f);
        SpriteFx.Play("impact-ring", new Vector2(StormX, Floor + 0.25f), false, 101, 3f);
        SpriteFx.Play("kill-burst", new Vector2(StormX, Floor + 0.3f), false, 102, 3f);
        for (int i = 1; i <= 4; i++)
        {
            SpriteFx.Play("land-dust", new Vector2(StormX - i * 3f, Floor), true, 101, 2f);
            SpriteFx.Play("land-dust", new Vector2(StormX + i * 3f, Floor), false, 101, 2f);
        }
        stormHit = false;
        stormLeft = StormFloorTime;
        stormGlow.enabled = true;
        StartCoroutine(StormFollowUp()); // playtest 11-13: the boss keeps attacking while the floor is live
        if (InZone(StormX) && player.ReceiveEnemyHit(new HitInfo { Damage = 1, SourcePosition = new Vector2(StormX, player.transform.position.y), Unparryable = true }) == HitResult.Hit)
            stormHit = true; // under the blade: that was the hit, the floor spares them
        for (float t = 0f; t < StormFloorTime; t += Dt)
        {
            blade.Pos = new Vector2(StormX + (Time.frameCount / 2 % 2 == 0 ? 1f : -1f) * 0.03f, Floor); // planted, humming
            yield return null;
        }
        stormLeft = 0f;
        stormFloor.enabled = stormGlow.enabled = false;
        blade.Glow = false;
        yield return Move(blade, new Vector2(StormX, BladeHigh + 2f), 0.2f);
        yield return ReattachBlade();
    }

    // While the floor is live (the player is up on a perch / a hook / a chunk): either bug rain columns dive at them, or the hatch
    // snipes single aimed shots (parryable from a perch / a chunk). Phase 1 lighter. Holds the director like a side barrage.
    IEnumerator StormFollowUp()
    {
        sideRunning = true;
        yield return Wait(0.3f);
        int count = Phase == 1 ? 2 : 3;
        if (Random.value < 0.5f)
            for (int i = 0; i < count; i++) { LaunchColumn(player.transform.position.x); yield return Wait(0.5f); }
        else
        {
            aimOn = true;
            for (float t = 0f; t < 0.25f; t += Dt) { hatchOpen = Mathf.Max(hatchOpen, t / 0.25f); yield return null; }
            hatchOpen = 1f;
            for (int i = 0; i < count; i++)
            {
                Vector2 d = (Vector2)player.transform.position - HatchAt;
                Sfx.Play(SfxId.EnemyTell, 0.5f, 1.5f);
                SpriteFx.Play("sniper-muzzle", HatchAt, d.x < 0f, 102);
                Fire(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, ShotSpeed * 0.85f);
                yield return Wait(0.45f);
            }
            aimOn = false;
            for (float t = 0f; t < 0.25f; t += Dt) { hatchOpen = 1f - t / 0.25f; yield return null; }
            hatchOpen = 0f;
        }
        sideRunning = false;
    }

    void UpdateStorm(float dt)
    {
        bool charging = stormCharge >= 0f, live = stormLeft > 0f;
        if (!charging && !live)
        {
            if (stormDrawn) { stormSegs.Begin(); stormSegs.Flush(); stormDrawn = false; }
            return;
        }
        bool blink = Mathf.FloorToInt(Time.time * 8f) % 2 == 0;
        if (live)
        {
            stormLeft -= dt;
            float fade = Mathf.Clamp01(stormLeft / 0.3f);
            bool hot = Random.value < 0.5f; // harsh flicker (playtest 11-13: "more violent red")
            stormFloor.color = hot ? new Color(1f, 0.1f, 0.05f, 0.95f * fade) : new Color(0.75f, 0f, 0.02f, 0.85f * fade);
            stormGlow.color = new Color(1f, 0f, 0f, (hot ? 0.28f : 0.16f) * fade);
            if (dt > 0f && Random.value < dt * 4f) Juice.Shake(0.05f, 0.08f); // the bridge buzzes
            Vector2 pp = player.transform.position; // centre = feet + 1, same feet line as the other hit tests
            if (!stormHit && dt > 0f && pp.y - 0.85f < Floor + StormFloorH && pp.y - 0.85f > Floor - 0.5f &&
                player.ReceiveEnemyHit(new HitInfo { Damage = 1, SourcePosition = new Vector2(pp.x, Floor), Unparryable = true }) == HitResult.Hit)
                stormHit = true; // one hit per storm (a dash / i-frames pass)
        }
        else stormFloor.color = new Color(1f, 0.23f, 0.23f, blink ? 0.7f : 0.3f); // warning: get off the floor
        if (dt <= 0f && stormDrawn) return; // frozen: keep the last frame
        stormFlickT -= dt;
        if (stormFlickT > 0f && stormDrawn) return;
        stormFlickT = StormFlick;
        stormDrawn = true;
        stormSegs.Begin();
        if (charging) // crackle around the blade's steel, more and longer as it charges
        {
            int bolts = 1 + Mathf.FloorToInt(stormCharge * 5f);
            Vector2 tip = blade.Pos;
            for (int b = 0; b < bolts; b++)
            {
                Vector2 a = tip + new Vector2(Random.Range(-BladeHalfW, BladeHalfW), Random.Range(0f, BladeLen));
                Vector2 end = a + Random.insideUnitCircle.normalized * Random.Range(0.6f, 0.8f + 1.4f * stormCharge);
                Bolt(a, end, 3, 1f);
            }
            stormSegs.Dot(tip, (6f + 10f * stormCharge) * Px, DiceSkillKit.A(StormCore, 0.9f), 110, 45f); // hot tip
        }
        else // live floor: jagged arcs crawling over the whole bridge
        {
            float fade = Mathf.Clamp01(stormLeft / 0.3f);
            for (float x = 0f; x < RoomW; x += Random.Range(1.2f, 2.2f))
            {
                Vector2 a = new Vector2(x, Floor + Random.Range(0f, 0.15f));
                Vector2 end = new Vector2(Mathf.Min(RoomW, x + Random.Range(1.5f, 3f)), Floor + Random.Range(0f, 0.15f));
                Bolt(a, end, 5, fade, 0.7f);
            }
            for (int k = 0; k < 5; k++) // sparks leaping off the floor
            {
                Vector2 a = new Vector2(Random.Range(0.5f, RoomW - 0.5f), Floor);
                Bolt(a, a + new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(0.8f, 1.6f)), 3, fade);
            }
        }
        stormSegs.Flush();
    }

    // one jagged red bolt a -> end (glow + light core); upOnly > 0 = a floor arc whose kinks only rise (up to upOnly)
    void Bolt(Vector2 a, Vector2 end, int jags, float alpha, float upOnly = 0f)
    {
        Vector2 d = end - a, n = new Vector2(-d.y, d.x).normalized, prev = a;
        for (int i = 1; i <= jags; i++)
        {
            float u = i / (float)jags;
            Vector2 p = Vector2.Lerp(a, end, u);
            if (i < jags) p += upOnly > 0f ? new Vector2(0f, Random.Range(0f, upOnly)) : n * Random.Range(-0.3f, 0.3f);
            stormSegs.Seg(prev, p, 8f * Px, DiceSkillKit.A(StormRed, 0.75f * alpha), 108);
            stormSegs.Seg(prev, p, 3f * Px, DiceSkillKit.A(StormCore, alpha), 109);
            prev = p;
        }
    }

    // The blade arm goes back onto its socket (DECISIONS 36): slides to the rest spot, overshoots a little, settles, clamps (spark + clank)
    IEnumerator ReattachBlade()
    {
        Vector2 over = BladeRest + (BladeRest - blade.Pos).normalized * ReattachOver;
        yield return Move(blade, over, ReattachTime);
        yield return Move(blade, BladeRest, 0.08f);
        blade.FlashUntil = Time.time + 0.06f;
        Vector2 joint = BladeRest + BladeWrist;
        SpriteFx.Play("parry-clash", joint, false, 101);
        SpriteFx.Play("hit-spark", joint - new Vector2(12f, 16f) / SpriteFx.Ppu, false, 102);
        Sfx.Play(SfxId.Parry, 0.6f, 0.55f); // low metal clank
        Juice.Shake(Juice.ShakeHurtAmp, 0.12f, Vector2.up);
    }

    // the slam lands where the blade's steel will be (tip on the floor at zx); the red zone is only the wider tell
    // DECISIONS 45: over the grate the blade punches through into the lane; over the solid deck it stops on it (the lane is safe)
    bool InZone(float zx)
    {
        Rect r = BladeBox(new Vector2(zx, Floor), 0f);
        r.xMin = Mathf.Min(r.xMin, zx - SlamHitHalf);
        r.xMax = Mathf.Max(r.xMax, zx + SlamHitHalf);
        r.yMin = OverLane(zx, BladeHalfW) ? LaneY : Floor + 0.1f;
        return PlayerIn(r);
    }

    // DECISIONS 45 under-bridge lane: the player's feet are down in it (player centre = feet + 1)
    // Same feet line as the hit tests (centre - 0.85): no band between "in the lane" and "on the deck" while dropping in / jumping out
    // (a floor-gap wall hit there although both ends are safe).
    bool InLane => player.transform.position.y - 0.85f < Floor;
    bool OnPerch => player.transform.position.y - 1f > PerchTop - 0.2f; // standing on a wall perch (or anything that high)
    static bool OverLane(float x, float half = 0f) => x + half > LaneX1 && x - half < LaneX2;

    // World box of the blade's steel. rotZ 0 = hanging down (slams; sweeps use ArmHitsPlayer). Only right angles: exact.
    static Rect BladeBox(Vector2 tip, float rotZ)
    {
        Vector2 up = Quaternion.Euler(0f, 0f, rotZ) * Vector2.up; // tip -> hilt
        Vector2 far = tip + up * BladeLen, side = new Vector2(Mathf.Abs(up.y), Mathf.Abs(up.x)) * BladeHalfW;
        return Rect.MinMaxRect(Mathf.Min(tip.x, far.x) - side.x, Mathf.Min(tip.y, far.y) - side.y, Mathf.Max(tip.x, far.x) + side.x, Mathf.Max(tip.y, far.y) + side.y);
    }

    // Sweep hit (DECISIONS 43): does the drawn arm (ArmSlice) touch the player while its tip moves fromX -> toX at height y?
    // Samples the player's hit box (same 0.7 x 1.65 as PlayerIn) in the arm's own frame, every SweepStep u along the move.
    bool ArmHitsPlayer(float fromX, float toX, float y, float rotZ, bool flipX)
    {
        Quaternion inv = Quaternion.Euler(0f, 0f, -rotZ);
        Vector2 pp = player.transform.position;
        int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(toX - fromX) / SweepStep));
        for (int k = 1; k <= steps; k++)
        {
            Vector2 tip = new Vector2(Mathf.Lerp(fromX, toX, (float)k / steps), y);
            for (int ix = -1; ix <= 1; ix++)
                for (int iy = 0; iy <= 4; iy++)
                {
                    Vector2 l = inv * (pp + new Vector2(ix * 0.35f, Mathf.Lerp(-0.85f, 0.8f, iy / 4f)) - tip); // x = across, y = up from the tip
                    if (flipX) l.x = -l.x; // the renderer is mirrored about the centre line (pivot x 0.5)
                    int u = Mathf.FloorToInt(l.y);
                    if (u < 0 || u >= ArmSlice.Length) continue;
                    if (l.x > Mathf.Max(ArmSlice[u].x * Px, -ArmUp + SweepInset) && l.x < Mathf.Min(ArmSlice[u].y * Px, ArmDown - SweepInset)) return true;
                }
        }
        return false;
    }

    bool PlayerIn(Rect r) // player centre = feet + 1, box 0.9 x 2 (hit test a little smaller, like the bug walls)
    {
        Vector2 pp = player.transform.position;
        return pp.x + 0.35f > r.xMin && pp.x - 0.35f < r.xMax && pp.y + 0.8f > r.yMin && pp.y - 0.85f < r.yMax;
    }

    void Clang(float zx, ParryHeight h)
    {
        int face = player.transform.position.x >= zx ? 1 : -1;
        Vector2 c = new Vector2(player.transform.position.x - face * 0.45f, player.transform.position.y + ParryRing.Offset(h));
        player.DuelClash(false, h, c);
        ring.Flash();
        SpriteFx.Play("parry_talisman", c, face > 0, 102); // blade on the player's left (face > 0): flipped
        Sfx.Play(SfxId.Parry);
        Juice.Shake(Juice.ShakeParryAmp, Juice.ShakeParryTime);
    }

    void ShowZone(float zx, bool on)
    {
        zoneFloor.enabled = zoneLane.enabled = on;
        zoneFloor.transform.position = new Vector3(zx, Floor + 0.125f, 0f);
        zoneLane.transform.position = new Vector3(zx, Floor + 4f, 0f);
    }

    // ---- B slam shockwave (DECISIONS 42): two low pixel waves run from the tip to the room edges; jump / chunk / hook to clear ----
    // slamOwns: the player is under the blade at impact -> the slam itself judges them, the waves start already spent for them
    void LaunchShock(float zx, bool slamOwns)
    {
        shockSpeed = ShockSpeed[Phase - 1];
        slamFlashT = SlamFlashTime;
        slamFlash.transform.position = new Vector3(zx, Floor + 5f * Px, 0f);
        slamFlash.enabled = true;
        shockA = shockB = -1;
        for (int k = 0; k < 2; k++)
        {
            int w = -1; // a free slot, else the oldest wave (never the slot just taken)
            for (int i = 0; i < ShockSlots; i++)
            {
                if (i == shockA) continue;
                if (w < 0 || (shockOn[w] && (!shockOn[i] || shockAge[i] > shockAge[w]))) w = i;
            }
            float dir = k == 0 ? -1f : 1f;
            StopShock(w, false); // a reused slot: hide its old columns until the new wave leaves
            shockOn[w] = true;
            shockHit[w] = slamOwns;
            shockDir[w] = dir;
            shockAge[w] = -ShockDelay;
            shockDust[w] = ShockDustEvery;
            shockX[w] = zx + dir * BladeHalfW;
            if (k == 0) shockA = w; else shockB = w;
        }
    }

    void CancelShock()
    {
        if (shockA >= 0) StopShock(shockA, true);
        if (shockB >= 0) StopShock(shockB, true);
        shockA = shockB = -1;
        slamFlash.enabled = false;
    }

    void StopShock(int w, bool puff)
    {
        if (puff && shockOn[w] && shockAge[w] >= 0f) SpriteFx.Play("land-dust", new Vector2(shockX[w], Floor), shockDir[w] < 0f, 101);
        shockOn[w] = false;
        for (int i = w * ShockCols; i < (w + 1) * ShockCols; i++) shockBody[i].enabled = shockCap[i].enabled = false;
    }

    void UpdateShock(float dt)
    {
        if (slamFlash.enabled) // impact: a pale floor flash that shrinks and fades
        {
            slamFlashT -= dt;
            float f = Mathf.Clamp01(slamFlashT / SlamFlashTime);
            slamFlash.color = new Color(1f, 0.97f, 0.8f, f);
            slamFlash.transform.localScale = new Vector3(Snap(4f + 6f * f), Snap(4f * Px + 10f * Px * f), 1f); // DECISIONS 43: ~3x the old 1.5..3.5 x 6 px
            if (slamFlashT <= 0f) slamFlash.enabled = false;
        }
        Vector2 pp = player.transform.position; // player centre = feet + 0.85..1, same box as the bug walls / rain
        float feet = pp.y - 0.85f;
        int flick = Time.frameCount / 3;
        for (int w = 0; w < ShockSlots; w++)
        {
            if (!shockOn[w]) continue;
            shockAge[w] += dt;
            if (shockAge[w] < 0f) continue; // ShockDelay: still under the floor flash
            float dir = shockDir[w], step = shockSpeed * dt;
            shockX[w] += dir * step;
            float x = shockX[w];
            if (x < -1f || x > RoomW + 1f) { StopShock(w, false); continue; } // the tail (ShockCols x ShockColW) is off-screen too
            shockDust[w] -= step;
            if (shockDust[w] <= 0f) { shockDust[w] = ShockDustEvery; SpriteFx.Play("land-dust", new Vector2(x - dir * 0.3f, Floor), dir < 0f, 101); }
            // hit: the tall front part (ShockHitBack u behind the front), feet below the crest; dash i-frames (Dodged) keep it live
            float rel = dir * (pp.x - x); // > 0: ahead of the front
            if (!shockHit[w] && dt > 0f && rel < 0.35f && rel > -ShockHitBack - 0.35f && feet < Floor + ShockH && !InLane && // runs on the deck: the lane under it is safe (DECISIONS 45)
                player.ReceiveEnemyHit(new HitInfo { Damage = 1, SourcePosition = new Vector2(x - dir, pp.y), Unparryable = true }) == HitResult.Hit)
                shockHit[w] = true;
            for (int c = 0; c < ShockCols; c++) // pixel crest: columns stepping down behind the front, a white cap on each, 1-px flicker
            {
                int i = w * ShockCols + c;
                float h = Mathf.Max(2f * Px, Snap(ShockH * ShockProfile[c]) + (c > 0 && ((c + flick) & 1) == 0 ? Px : 0f));
                float cx = Snap(x - dir * (c + 0.5f) * ShockColW);
                float a = 1f - c * 0.11f;
                shockBody[i].transform.position = new Vector3(cx, Floor + h * 0.5f, 0f);
                shockBody[i].transform.localScale = new Vector3(ShockColW, h, 1f);
                shockBody[i].color = new Color(1f, 0.86f, 0.42f, 0.85f * a);
                shockCap[i].transform.position = new Vector3(cx, Floor + h - Px, 0f);
                shockCap[i].transform.localScale = new Vector3(ShockColW, 2f * Px, 1f);
                shockCap[i].color = new Color(1f, 1f, 0.92f, a);
                shockBody[i].enabled = shockCap[i].enabled = true;
            }
        }
    }

    // ---- B': blade sweeps (DECISIONS 29). The arm lies flat (rotated 90, tip leading, hilt trailing), its tip on the LOW / HIGH line,
    // and slides across wall to wall (tip x 31 -> -19 or back). DECISIONS 43 / 45: the whole drawn arm is the hitbox (ArmSlice); it never
    // dips under the deck, so what is drawn is what hits. The red band (the arm's full height) is only the warning.
    // DECISIONS 36: every sweep starts and ends with the whole arm off-screen; at the end it flies back and reattaches ----
    IEnumerator AttackSweep(int n, float tell)
    {
        blade.Glow = true;
        yield return Move(blade, BladeOff, tell); // detaches and leaves the screen up-right (first sweep goes right -> left)
        bool low = Random.value < 0.5f;
        for (int i = 0; i < n; i++)
        {
            if (i > 0) low = Random.value < 0.7f ? !low : low; // mostly alternate (T)
            // x = the tip; the arm trails BladeArmLen behind it, so x0 / x1 keep all of it outside the 0..RoomW view
            float dir = i % 2 == 0 ? -1f : 1f, x0 = dir < 0f ? RoomW + 1f : -1f, x1 = dir < 0f ? -BladeArmLen - 1f : RoomW + BladeArmLen + 1f;
            float rot = dir * 90f; // the hilt points back (-dir), lying flat (DECISIONS 45)
            sweepY = SweepY[low ? 0 : 1];
            sweepFill.transform.localScale = new Vector3(RoomW, ArmDown + ArmUp - 2f * SweepInset, 1f);
            sweepFill.enabled = true;
            blade.Glow = true;
            blade.Sr.transform.rotation = Quaternion.Euler(0f, 0f, rot);
            // the art is lopsided (ArmSlice): mirrored for left -> right so both directions cover the same ArmDown..ArmUp band
            blade.Sr.flipX = dir > 0f;
            Sfx.Play(SfxId.EnemyTell, 0.8f, low ? 0.8f : 1.25f); // low tell = low pitch
            yield return Move(blade, new Vector2(x0, sweepY), SweepWarn[Phase - 1]); // off-screen: lines up behind the edge during the warning
            blade.Glow = false;
            sweepFill.enabled = false;
            Sfx.Play(SfxId.Swing, 1f, 0.6f);
            Juice.Shake(Juice.ShakeHurtAmp, 0.15f, new Vector2(dir, 0f));
            bool hit = false;
            float time = SweepTime[Phase - 1] * Mathf.Abs(x1 - x0) / (RoomW - 1f), prevX = x0; // the old speed over the longer run
            for (float t = 0f; t < time; )
            {
                float dt = Dt;
                t += dt;
                float x = Mathf.Lerp(x0, x1, t / time);
                blade.Pos = new Vector2(x, sweepY);
                // one contact per sweep: a dash (Dodged) at the first touch passes the whole 8 u arm, like it passed the blade before
                if (!hit && dt > 0f && ArmHitsPlayer(prevX, x, sweepY, rot, dir > 0f))
                {
                    player.ReceiveEnemyHit(new HitInfo { Damage = 1, SourcePosition = new Vector2(x - dir, player.transform.position.y), Unparryable = true });
                    hit = true;
                }
                prevX = x;
                yield return null;
            }
            blade.Pos = new Vector2(x1, sweepY);
            sweepY = -1f;
        }
        blade.Sr.transform.rotation = Quaternion.identity; // still off-screen: hang it again and bring it back from the right
        blade.Sr.flipX = false;
        blade.Pos = BladeOff;
        yield return ReattachBlade();
    }

    // ---- C: hatch barrage of small-shot volleys; a parried shot flies back and hurts the hatch ----
    IEnumerator AttackC(int n, float tell, bool side)
    {
        if (side) sideRunning = true;
        hatchL.Glow = hatchR.Glow = true;
        aimOn = true;
        for (float t = 0f; t < tell; t += Dt) { hatchOpen = Mathf.Min(1f, t / 0.3f); yield return null; }
        hatchOpen = 1f;
        hatchL.Glow = hatchR.Glow = false;
        hatchHook = true; // open with the core inside: rope window until it closes (DECISIONS 38)
        float every = VolleyEvery[Phase - 1];
        for (int i = 0; i < n; i++)
        {
            if (side && BladeStuck) break; // the overlap barrage stops once the blade sticks (mash window next)
            Vector2 d = (Vector2)player.transform.position - HatchAt;
            float aimDeg = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            Sfx.Play(SfxId.EnemyTell, 0.5f, 1.5f);
            SpriteFx.Play("sniper-muzzle", HatchAt, d.x < 0f, 102);
            if (!side && Phase == 3 && i % 2 == 1)
            {
                // spiral arc: one shot at a time sweeping the lower half, direction alternates per spiral volley
                bool cw = i % 4 == 1;
                for (int j = 0; j < SpiralShots; j++)
                {
                    float f = j / (SpiralShots - 1f);
                    Fire(Mathf.Lerp(cw ? -15f : -165f, cw ? -165f : -15f, f));
                    yield return Wait(SpiralStep);
                }
            }
            else
            {
                int c = side ? SideFanShots : FanShots;
                for (int j = 0; j < c; j++) Fire(aimDeg + FanSpread * (j / (c - 1f) - 0.5f));
            }
            yield return Wait(every);
        }
        aimOn = false;
        yield return Wait(HatchOpenAfter);
        hatchHook = false;
        for (float t = 0f; t < 0.3f; t += Dt) { hatchOpen = 1f - t / 0.3f; yield return null; }
        hatchOpen = 0f;
        if (side) sideRunning = false;
    }

    // every boss shot (C fan/spiral and A's bug shots) shares one big gold square look (user 11-11: bug bullets were too small)
    void Fire(float deg, float speed = ShotSpeed)
    {
        if (EnemyProjectile.Active.Count >= MaxShots) return;
        float r = deg * Mathf.Deg2Rad;
        // DECISIONS 45: shots fired at someone in the lane fly through the grate (and the deck) instead of stopping on it: the lane is no bunker.
        // Same for a wall perch: the shot aimed at someone hugging the wall (x < ~0.85) died on the perch's lip.
        EnemyProjectile p = EnemyProjectile.Spawn(HatchAt, new Vector2(Mathf.Cos(r), Mathf.Sin(r)) * speed, playerMask, InLane || OnPerch ? 0 : groundMask, new HitInfo { Damage = 1 }, GoldShot);
        p.transform.localScale = new Vector3(BulletScale, BulletScale, 1f); // visual only: hitbox stays EnemyProjectile's 0.5
        p.ReflectLife = ShotReflectLife;
        p.ReflectTarget = HatchAt; // reflected = a weakened lob that drops back into the hatch
    }

    void CheckReflectedShots()
    {
        var shots = EnemyProjectile.Active;
        for (int i = shots.Count - 1; i >= 0; i--)
        {
            EnemyProjectile p = shots[i];
            if (p == null || !p.Reflected) continue;
            Vector2 q = p.transform.position;
            if (Mathf.Abs(q.x - HatchAt.x) > 2f || Mathf.Abs(q.y - HatchAt.y) > 1.25f) continue;
            p.gameObject.SetActive(false); // leaves Active now (OnDisable)
            Destroy(p.gameObject);
            Damage(ReflectDamage, hatchL, q);
            if (dead) return;
        }
    }

    // ------------------------------------------------------------------ damage

    void OnSwing(Vector2 centre, Vector2 size)
    {
        if (dead) { if (breakable) FinishNear(centre, size); return; }
        if (coreOpen && Time.time >= coreHitAt &&
            Mathf.Abs(centre.x - core.Pos.x) <= size.x * 0.5f + CoreHalf && Mathf.Abs(centre.y - core.Pos.y) <= size.y * 0.5f + CoreHalf)
        {
            coreHitAt = Time.time + CoreHitEvery;
            Damage(CoreHitDamage, core, core.Pos + Vector2.ClampMagnitude(centre - core.Pos, CoreHalf));
            LuckyDice.AddHitCharge();
            player.DuelHitStop(Juice.HitStopHit);
            return;
        }
        if (BladeStuck) // mash window (DECISIONS 36): a swing touching the stuck steel; CoreHitEvery is the same per-swing safety
        {
            Rect s = BladeBox(new Vector2(stuckX, BladeStuckY), 0f);
            if (Time.time < mashHitAt || mashHits >= MashCap ||
                Mathf.Abs(centre.x - s.center.x) > (size.x + s.width) * 0.5f || Mathf.Abs(centre.y - s.center.y) > (size.y + s.height) * 0.5f) return;
            mashHitAt = Time.time + CoreHitEvery;
            mashHits++;
            Damage(MashDamage, blade, new Vector2(Mathf.Clamp(centre.x, s.xMin, s.xMax), Mathf.Clamp(centre.y, s.yMin, s.yMax)));
            LuckyDice.AddHitCharge();
            player.DuelHitStop(Juice.HitStopHit);
            return;
        }
        if (!armVulnerable) return;
        var arm = new Rect(bugArm.Pos.x - 1.5f, bugArm.Pos.y, 3f, 5.5f); // the slumped palm + lower arm
        if (Mathf.Abs(centre.x - arm.center.x) > (size.x + arm.width) * 0.5f || Mathf.Abs(centre.y - arm.center.y) > (size.y + arm.height) * 0.5f) return;
        Damage(ArmHitDamage, bugArm, new Vector2(Mathf.Clamp(centre.x, arm.xMin, arm.xMax), Mathf.Clamp(centre.y, arm.yMin, arm.yMax)));
        LuckyDice.AddHitCharge();
        player.DuelHitStop(Juice.HitStopHit);
    }

    void Damage(int amount, Part part, Vector2 at)
    {
        if (dead) return;
        hp -= amount;
        part.FlashUntil = Mathf.Max(part.FlashUntil, Time.time + 0.08f);
        if (part == hatchL) hatchR.FlashUntil = part.FlashUntil;
        Sfx.Play(SfxId.Hit);
        if (part == core) CoreHitFx(amount, at); // DECISIONS 32: bigger ring + gold debris, scaled by the damage
        else SpriteFx.Play("impact-ring", at, false, 101);
        SpriteFx.Play("hit-spark", at - new Vector2(12f, 16f) / SpriteFx.Ppu);
        Juice.Shake(Juice.ShakeKillAmp, Juice.ShakeKillTime);
        if (hp <= 0) Die();
    }

    // Core hit (DECISIONS 32): impact ring and a few gold bits flying off, both growing with the damage (1..3).
    static void CoreHitFx(int amount, Vector2 at)
    {
        SpriteFx.Play("impact-ring", at, false, 101, 1f + 0.4f * (amount - 1));
        for (int i = 0, n = 2 + 2 * amount; i < n; i++)
        {
            float a = Random.Range(0f, 2f * Mathf.PI), speed = Random.Range(2f, 3.5f) * (0.8f + 0.2f * amount);
            var bit = new GameObject("CoreBit");
            bit.transform.position = at;
            DiceSkillKit.Quad(bit.transform, Vector2.zero, Vector2.one * ((amount >= 2 ? 3f : 2f) * Px), GoldShot, 103, 45f * (i % 2));
            var f = bit.AddComponent<DiceSkillKit.Fade>();
            f.life = 0.3f + 0.05f * amount;
            f.fadeAt = 0.12f;
            f.drift = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * speed;
        }
    }

    // Skill damage number (DECISIONS 32): a gold holo digit pops in big, trembles, then shatters (ShatterFx). Own object: survives Die().
    static void PopNumber(int n, Vector2 at)
    {
        if (goldDigits == null || goldDigits.Length != 10 || goldDigits[0] == null)
        {
            Sprite[] s = Resources.LoadAll<Sprite>("DraftHud/holo_digits_gold");
            System.Array.Sort(s, (a, b) => string.CompareOrdinal(a.name, b.name));
            goldDigits = s.Length == 10 ? s : null;
        }
        if (goldDigits == null) return;
        var go = new GameObject("SkillDamageNumber");
        go.transform.position = at;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = goldDigits[Mathf.Clamp(n, 0, 9)];
        sr.sortingLayerName = "FX";
        sr.sortingOrder = 120;
        go.AddComponent<NumberPop>().sr = sr;
    }

    sealed class NumberPop : MonoBehaviour
    {
        const float In = 0.1f, Hold = 0.4f, Shiver = 0.12f, Size = 5f; // (T) s pop-in / on screen / trembling before the break; x scale
        public SpriteRenderer sr;
        Vector3 home;
        float t;

        void Start() { home = transform.position; Apply(); }

        void Update()
        {
            t += Time.deltaTime;
            if (t >= In + Hold)
            {
                transform.position = home;
                ShatterFx.Play(sr, Vector2.up, home, GoldShot);
                Destroy(gameObject);
                return;
            }
            Apply();
        }

        void Apply()
        {
            float s = t < In ? Mathf.Lerp(Size * 1.6f, Size, t / In) : Size;
            transform.localScale = new Vector3(s, s, 1f);
            bool shiver = t > In + Hold - Shiver; // cracking
            transform.position = home + (shiver ? new Vector3((Time.frameCount % 2 == 0 ? 1f : -1f) * Px * Size * 0.5f, 0f, 0f) : Vector3.zero);
        }
    }

    void Die()
    {
        dead = true;
        StopAllCoroutines();
        stormCharge = -1f; stormLeft = 0f; stormFloor.enabled = stormGlow.enabled = false; // killed mid-storm: UpdateStorm clears the bolts
        armVulnerable = BladeStuck = aimOn = bugAim = sideRunning = coreOpen = false;
        core.Pos = HatchAt;
        bugArm.Glow = blade.Glow = hatchL.Glow = hatchR.Glow = false;
        ShowZone(0f, false);
        sweepFill.enabled = false;
        sweepY = -1f;
        debris.CrumbleAll(); // DECISIONS 40: the ending room has no moving platforms
        blade.Sr.transform.rotation = Quaternion.identity; // killed mid-sweep: hang the arm down again
        blade.Sr.flipX = false;
        if (blade.Pos.x < 1f || blade.Pos.x > RoomW - 1f) blade.Pos = BladeRest; // killed while the arm was off-screen (sweep / reattach, DECISIONS 36): the ending must drop it in view
        ring.Hide();
        for (int w = 0; w < Waves; w++) waveOn[w] = false;
        for (int i = 0; i < bugs.Length; i++) bugs[i].enabled = false;
        for (int w = 0; w < ShockSlots; w++) StopShock(w, false); // DECISIONS 42: slam shockwaves gone
        slamFlash.enabled = false;
        hatchHook = hatchLatched = false; // DECISIONS 38: no rope window, no pending strike; rain columns gone
        for (int c = 0; c < RainCols; c++) { rainStage[c] = 0; rainLane[c].enabled = rainMark[c].enabled = false; }
        for (int i = 0; i < rainBugs.Length; i++) rainBugs[i].enabled = false;
        var shots = EnemyProjectile.Active;
        for (int i = shots.Count - 1; i >= 0; i--) { GameObject g = shots[i].gameObject; g.SetActive(false); Destroy(g); }
        ShatterFx.Play(holo, Vector2.down, HoloAt, Cyan); // the number breaks
        holo.enabled = false;
        Bgm.Stop();
        Sfx.Play(SfxId.Stunned);
        Juice.Shake(0.25f, 0.6f);
        StartCoroutine(Ending());
    }

    // ------------------------------------------------------------------ ending (DECISIONS 21): smash the machines -> collapse -> END-01

    IEnumerator Ending()
    {
        while (GameFlow.InputLocked) yield return null; // a dice skill still holding the input
        StartCoroutine(Move(bugArm, BugSlump, 0.8f));
        StartCoroutine(Move(blade, new Vector2(blade.Pos.x, BladeStuckY), 0.8f));
        yield return Wait(1.2f);
        // playtest 11-13 (user): the finish is striking the boss itself, not the machines around it. The core drops onto the bridge
        // and stays; FinishHits LMB swings on it, each one bursts a machine prop too; the last one is a big blow.
        breakable = true;
        Vector2 from = core.Pos;
        for (float t = 0f; t < 0.4f; t += Time.deltaTime) { core.Pos = Vector2.Lerp(from, CoreLow, Smooth(t / 0.4f)); yield return null; }
        core.Pos = CoreLow;
        SpriteFx.Play("impact-ring", CoreLow, false, 101, 2f);
        Juice.Shake(Juice.ShakeHurtAmp, 0.2f, Vector2.down);
        hint.text = Loc.T("STRIKE THE CORE  [LMB]", "코어를 베어라  [좌클릭]"); // B04
        hint.gameObject.SetActive(true);
        while (finishHits < FinishHits) yield return null;
        hint.gameObject.SetActive(false);
        SpriteFx.Play("boss-phase-fracture", CoreLow, false, 103); // playtest 11-13: the core shatters (glass cracks, red seams)
        Say("@mode radio\nbroadcast | 다음 숫자는… | The next number is… | 2.5\n"); // B05: it never says the number
        for (int i = 0; i < props.Length; i++) if (!propBroken[i]) BreakProp(i); // the last blow takes the rest
        yield return Wait(0.5f);
        GameFlow.InputLocked = ownLock = true;
        white.enabled = true;
        for (float t = 0f; t < 2.4f; t += Time.deltaTime)
        {
            Juice.Shake(0.2f, 0.3f);
            sink = t * 1.2f;
            white.color = new Color(1f, 1f, 1f, Mathf.Clamp01((t - 0.8f) / 1.4f));
            yield return null;
        }
        white.color = Color.white;
        // Same hand-off as StopButton: keep the lock; a talk-mode file clears it after its last line.
        DialogueScript s = ending != null ? DialogueScript.Parse(ending.text, ending.name) : null;
        if (s != null && s.talk && s.lines.Count > 0 && DialoguePlayer.Instance != null)
        {
            DialoguePlayer.Instance.Play(ending);
            while (GameFlow.InputLocked) yield return null;
        }
        else Debug.LogWarning("[BossFight] Ending file missing/not talk mode, or no DialoguePlayer; going straight to the title.");
        GameFlow.InputLocked = ownLock = true; // the dialogue released it: hold it through the illustration so the pause menu cannot open behind it
        yield return EndingIllustration();
        GameFlow.InputLocked = ownLock = false;
        StageFlow.LoadTitle();
    }

    // Full-screen painterly ending image: fade in, hold (any key/click skips after 1 s), fade out. Missing art = skipped.
    IEnumerator EndingIllustration()
    {
        Sprite art = DraftHud.Ui("ending_illustration");
        if (art == null) yield break;
        Canvas canvas = UiKit.CreateCanvas("EndingIllustrationCanvas", null, 300);
        UiKit.FilledImage(UiKit.Stretch("Bars", canvas.transform), new Color(0.02f, 0.03f, 0.08f, 1f)); // letterbox colour
        Image img = UiKit.FilledImage(UiKit.Stretch("Art", canvas.transform), Color.white);
        img.sprite = art;
        img.preserveAspect = true;
        CanvasGroup group = canvas.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime) { group.alpha = t; yield return null; }
        group.alpha = 1f;
        for (float t = 0f; t < 4f; t += Time.unscaledDeltaTime)
        {
            bool press = (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) ||
                         (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
            if (t > 1f && press) break;
            yield return null;
        }
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime) { group.alpha = 1f - t; yield return null; }
        group.alpha = 0f;
        Destroy(canvas.gameObject);
    }

    // Ending finish: a swing touching the dropped core = one blow (gold burst, body flash, a machine prop bursts); the last is bigger
    void FinishNear(Vector2 centre, Vector2 size)
    {
        if (finishHits >= FinishHits || Time.time < finishHitAt) return;
        if (Mathf.Abs(centre.x - core.Pos.x) > size.x * 0.5f + CoreHalf || Mathf.Abs(centre.y - core.Pos.y) > size.y * 0.5f + CoreHalf) return;
        finishHitAt = Time.time + CoreHitEvery;
        finishHits++;
        bool last = finishHits >= FinishHits;
        Vector2 at = core.Pos + Vector2.ClampMagnitude(centre - core.Pos, CoreHalf);
        float dir = core.Pos.x >= player.transform.position.x ? 1f : -1f;
        CoreHitFx(3, at);
        SpriteFx.Play("kill-burst", at, dir < 0f, 102, last ? 3f : 2f);
        body.FlashUntil = core.FlashUntil = Time.time + 0.1f;
        Sfx.Play(SfxId.Hit, 1f, last ? 0.5f : 0.8f);
        Juice.Shake(last ? 0.35f : Juice.ShakeKillAmp, last ? 0.5f : Juice.ShakeKillTime, new Vector2(dir, 0f));
        player.DuelHitStop(last ? Juice.HitStopKill * 2f : Juice.HitStopKill);
        if (last) DiceSkillKit.Overlay.Get().Flash(Color.white, 0.6f, 0.25f);
        for (int i = 0; i < props.Length; i++) if (!propBroken[i]) { BreakProp(i); break; }
    }

    void BreakProp(int i)
    {
        propBroken[i] = true;
        propsLeft--;
        Vector2 c = props[i].bounds.center;
        float dir = c.x >= player.transform.position.x ? 1f : -1f;
        ShatterFx.Play(props[i], new Vector2(dir, 0.5f), c, Cyan);
        props[i].enabled = false;
        SpriteFx.Play("kill-burst", c, dir < 0f, 102);
    }

    // ------------------------------------------------------------------ view

    void Draw()
    {
        float now = Time.time;
        bool blink = Mathf.FloorToInt(now * 8f) % 2 == 0;
        Color tint = PhaseTint[shownPhase - 1];
        float bob = started && !dead ? (Mathf.FloorToInt(now * 1.5f) % 2) * Px : 0f; // 1 px idle breathing on the arms
        Show(body, tint, now, blink, 0f);
        crack1.transform.position = crack2.transform.position = new Vector3(BodyAt.x, Snap(BodyAt.y - sink), 0f);
        hatchInside.transform.position = new Vector3(HatchAt.x, Snap(HatchAt.y - sink), 0f);
        Show(bugArm, tint, now, blink, bob);
        Show(blade, tint, now, blink, bob);
        float open = Smooth(hatchOpen) * 1.75f;
        hatchL.Pos = HatchAt + new Vector2(-1f - open, 0f);
        hatchR.Pos = HatchAt + new Vector2(1f + open, 0f);
        Show(hatchL, tint, now, blink, 0f);
        Show(hatchR, tint, now, blink, 0f);
        core.Sr.enabled = coreCable.enabled = (!dead || breakable) && core.Pos.y < HatchAt.y - 0.05f;
        if (core.Sr.enabled)
        {
            bool beat = coreOpen && Mathf.FloorToInt(now * 4f) % 2 == 0; // 2 Hz pulse while it can be hit: one step bigger + gold
            Show(core, beat ? GoldShot : Color.white, now, blink, 0f);
            core.Sr.transform.localScale = beat ? new Vector3(1.125f, 1.125f, 1f) : Vector3.one;
            float top = HatchAt.y - sink, bottom = core.Pos.y - sink;
            coreCable.transform.position = new Vector3(Snap(core.Pos.x), (top + bottom) * 0.5f, 0f);
            coreCable.transform.localScale = new Vector3(2f * Px, top - bottom, 1f);
            coreCable.color = new Color(Cyan.r, Cyan.g, Cyan.b, coreOpen && blink ? 0.95f : 0.6f);
        }
        DrawBracket(now);
        for (int i = 0; i < hookGlow.Length; i++) // hooks: slow soft glow pulse
            hookGlow[i].color = DiceSkillKit.A(DiceSkillKit.Cyan, 0.14f + 0.1f * Mathf.Sin(now * 3f + i * 1.7f));
        if (!dead)
        {
            bool glitch = shownPhase == 3 && Random.value < 0.12f; // phase 3: the hologram jitters
            holo.transform.position = new Vector3(Snap(HoloAt.x) + (glitch ? (Random.value < 0.5f ? -2f : 2f) * Px : 0f), Snap(HoloAt.y), 0f);
            holo.color = new Color(1f, 1f, 1f, !started ? 0.35f : glitch && Random.value < 0.5f ? 0.45f : 1f);
        }
        if (zoneFloor.enabled) zoneFloor.color = new Color(1f, 0.23f, 0.23f, blink ? 0.85f : 0.55f);
        if (sweepY >= 0f && !dead)
        {
            sweepFill.transform.position = new Vector3(RoomW * 0.5f, Snap(sweepY + (ArmUp - ArmDown) * 0.5f), 0f); // the arm's hit band (trimmed by SweepInset)
            sweepFill.color = new Color(1f, 0.23f, 0.23f, blink ? 0.22f : 0.1f); // the blinking band = height tell (warning only)
        }
        Vector2 aimTo = player.transform.position;
        bool aim = (aimOn || bugAim) && !dead;
        for (int i = 0; i < aimDots.Length; i++)
        {
            aimDots[i].enabled = aim;
            if (!aim) continue;
            Vector2 p = Vector2.Lerp(HatchAt, aimTo, (i + 1f) / (aimDots.Length + 1f));
            aimDots[i].transform.position = new Vector3(Snap(p.x), Snap(p.y), 0f);
            aimDots[i].color = new Color(GoldShot.r, GoldShot.g, GoldShot.b, blink ? 0.9f : 0.5f);
        }
    }

    // Gold lock-on corners around the open core (DECISIONS 32): snap in from wide when it lands, then a 2 Hz squeeze.
    // DECISIONS 38: the same corners in the rope's cyan around the hatch while it can be roped (core still inside).
    void DrawBracket(float now)
    {
        bool coreOn = coreOpen && core.Sr.enabled && !dead, on = coreOn || HatchHookable;
        const float Arm = 0.35f, W = 2f * Px;
        float k = now - (coreOn ? coreOpenedAt : hookOpenedAt);
        float r = (coreOn ? CoreHalf : HatchHookHalf) + 0.2f + (k < 0.2f ? 1.2f * (1f - k / 0.2f) : 0.08f * (1f - Mathf.Repeat(now * 2f, 1f)));
        Vector2 c = coreOn ? new Vector2(core.Pos.x, core.Pos.y - sink) : new Vector2(HatchAt.x, HatchAt.y - sink);
        Color col = DiceSkillKit.A(coreOn ? GoldShot : DiceSkillKit.Cyan, Mathf.FloorToInt(now * 8f) % 2 == 0 ? 1f : 0.7f);
        for (int i = 0; i < 4; i++)
        {
            float sx = i % 2 == 0 ? -1f : 1f, sy = i < 2 ? -1f : 1f;
            SpriteRenderer h = coreBracket[i * 2], v = coreBracket[i * 2 + 1];
            h.enabled = v.enabled = on;
            if (!on) continue;
            h.transform.position = new Vector3(Snap(c.x + sx * (r - Arm * 0.5f)), Snap(c.y + sy * r), 0f);
            h.transform.localScale = new Vector3(Arm, W, 1f);
            v.transform.position = new Vector3(Snap(c.x + sx * r), Snap(c.y + sy * (r - Arm * 0.5f)), 0f);
            v.transform.localScale = new Vector3(W, Arm, 1f);
            h.color = v.color = col;
        }
    }

    void Show(Part p, Color tint, float now, bool blink, float dy)
    {
        bool flash = now < p.FlashUntil;
        int v = flash ? BossArt.Flash : p.Glow && blink ? BossArt.Lit : BossArt.Normal;
        p.Sr.sprite = p.V[v];
        bool noVariants = p.V[BossArt.Normal] == p.V[BossArt.Flash]; // override art: no baked white flash
        bool noLit = p.V[BossArt.Normal] == p.V[BossArt.Lit];
        Material m = p.Sr.sharedMaterial;
        bool solid = noVariants && m.HasProperty(FlashId);
        if (solid) m.SetFloat(FlashId, flash ? 0.85f : 0f);
        p.Sr.color = noVariants && flash && !solid ? Color.white : noLit && v == BossArt.Lit ? new Color(1f, 0.55f, 0.45f) : tint;
        p.Sr.transform.position = new Vector3(Snap(p.Pos.x), Snap(p.Pos.y + dy - sink), 0f);
    }

    // ------------------------------------------------------------------ helpers

    IEnumerator Wait(float s) { for (float t = 0f; t < s; t += Dt) yield return null; }

    IEnumerator Move(Part p, Vector2 to, float time)
    {
        Vector2 from = p.Pos;
        for (float t = 0f; t < time; t += Dt) { p.Pos = Vector2.Lerp(from, to, Smooth(t / time)); yield return null; }
        p.Pos = to;
    }

    static float Smooth(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }
    static float Snap(float v) => Mathf.Round(v * 32f) / 32f;
}
