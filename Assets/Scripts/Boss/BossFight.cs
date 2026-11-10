using System.Collections;
using TMPro;
using UnityEngine;

// STG-05 final boss v2 (DECISIONS 23 / 21, docs/design/impl/boss-v2.md): a huge fused machine fills the room background; only its
// parts attack. Director (coroutine): hologram shows attack icon + N, the part glows (tell) -> attack -> its opening -> rest -> next.
//   A bug arm:  N swarm walls sweep the room left -> right, each with one gap (floor / jump height); dash goes through. Then the arm
//               slumps on the floor SlumpTime: LMB swings on it hurt the boss.
//   B blade arm: red danger zone at the player, N slams with the ParryRing tell (LMB in the window = clang, no damage); the last one
//               sticks in the bridge: walk up to it -> EliteDuel with N exchanges (BossBladeFoe). Win = big damage.
//   C chest hatch: hatch opens, yellow aim dots, N golden shots (EnemyProjectile). A parried shot flies back; reaching the hatch hurts.
// HP is never shown: phase 2 / 3 darken + crack the body. Phase 3 overlaps C with A / B. HP 0: the hologram shatters, the body stops,
// the hero smashes the machine props (LMB), everything collapses into white -> END-01 lines -> title. Dice stay off (LuckyDice.BossActive).
// Dying reloads the scene (RespawnManager): the fight restarts. Timers use scaled time and stop while input is locked (duel, dialogue).
public class BossFight : MonoBehaviour
{
    [Header("Art overrides (empty = temporary code-drawn BossArt)")]
    [SerializeField] Sprite bodyArt;      // pivot bottom-left, placed at BodyAt (24 x 14 u)
    [SerializeField] Sprite bugArmArt;    // pivot bottom-centre = the palm
    [SerializeField] Sprite bladeArmArt;  // pivot bottom-centre = the blade tip
    [SerializeField] Sprite hatchArt;     // left half, centre pivot (the right half is the same sprite flipped)
    [Header("Ending")]
    [SerializeField] TextAsset ending;    // END-01 lines (talk mode)
    [SerializeField] TMP_FontAsset font;  // "smash the machines" hint

    // ---- numbers (T) ----
    const int MaxHp = 30;                                   // phase 1 = 30..21, 2 = 20..11, 3 = 10..1
    const int ArmHitDamage = 1, ReflectDamage = 2, DuelDamage = 6;
    const float StartX = 6.5f, StartAfter = 3f;             // fight starts when the player walks past x or after s
    const float Rest = 0.9f;                                // s between attacks
    const float WaveSpeed = 9f, WaveEvery = 1.3f, WaveHalfW = 0.6f, WaveTop = 9.5f, GapH = 3f, SlumpTime = 1.5f;
    static readonly float[] GapLows = { 2f, 3.8f };         // gap on the floor (stand) / at jump height
    const float ZoneHalf = 1.5f, StuckTime = 3f, FirstSlam = 0.5f;
    const float WindowEarly = 0.25f, WindowLate = 0.1f, Lockout = 0.25f; // same LMB window as EliteDuel
    static readonly float[] SlamGap = { 0.95f, 0.85f, 0.75f };  // by phase
    const float ShotSpeed = 9f, ShotReflectLife = 2.5f, HatchOpenAfter = 1f;
    static readonly float[] ShotEvery = { 0.45f, 0.4f, 0.32f }; // by phase

    // ---- layout (room 30 x 16.875, floor top y 2) ----
    const float Floor = 2f, RoomW = 30f, Px = 1f / 32f;
    static readonly Vector2 BodyAt = new Vector2(3f, 2f), HoloAt = new Vector2(15f, 11.75f), HatchAt = new Vector2(15f, 7.5f);
    static readonly Vector2 BugRest = new Vector2(5.5f, 6.5f), BugSlump = new Vector2(6.5f, 1.75f), BladeRest = new Vector2(24.5f, 6.5f);
    const float BladeHigh = 7.5f, BladeStuckY = 1.4f;
    static readonly float[] PropX = { 2f, 7f, 11f, 19f, 23f, 27.8f };
    static readonly int[] PropKind = { 0, 1, 2, 0, 1, 0 };   // BossArt.Props: rack, screen, cables

    static readonly Color[] PhaseTint = { Color.white, new Color(0.8f, 0.8f, 0.86f), new Color(0.62f, 0.6f, 0.68f) };
    static readonly Color Red = new Color32(0xFF, 0x3B, 0x3B, 0xFF), GoldShot = new Color32(0xF2, 0xC9, 0x4C, 0xFF), Cyan = new Color32(0x4C, 0xE0, 0xFF, 0xFF);
    // Unlit Sprites/Default. Pooled renderers share one material per texture (squares, bugs); every other renderer owns one:
    // the 2D renderer drew neighbouring same-material runtime sprites with ONE texture (the hologram showed the body).
    static Material squareMat, bugMat;
    readonly System.Collections.Generic.List<Material> ownMats = new System.Collections.Generic.List<Material>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { squareMat = bugMat = null; } // Domain Reload is off

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
    Part body, bugArm, blade, hatchL, hatchR;
    SpriteRenderer crack1, crack2, hatchInside, holo;
    float hatchOpen, sink;
    int hp = MaxHp, shownPhase = 1, bladePoseY;
    bool started, dead, breakable, armVulnerable, sideRunning, duelWon, ownLock;
    int playerMask, groundMask;
    ParryRing ring;
    BossBladeFoe foe;

    public bool BladeStuck { get; private set; }
    int Phase => hp * 3 > MaxHp * 2 ? 1 : hp * 3 > MaxHp ? 2 : 3;
    static float Dt => GameFlow.InputLocked ? 0f : Time.deltaTime;

    // swarm pool: Waves walls x BugsPerWave bugs
    const int Waves = 3, BugsPerWave = 56;
    readonly float[] waveX = new float[Waves], gapLo = new float[Waves];
    readonly bool[] waveOn = new bool[Waves], waveHit = new bool[Waves];
    readonly SpriteRenderer[] bugs = new SpriteRenderer[Waves * BugsPerWave];
    readonly Vector2[] bugOff = new Vector2[Waves * BugsPerWave];

    SpriteRenderer zoneFloor, zoneLane, white;
    readonly SpriteRenderer[] aimDots = new SpriteRenderer[14];
    bool aimOn;
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
        body = MakePart("Body", new[] { BossArt.Body, BossArt.Body, BossArt.BodyFlash }, bodyArt, -107, BodyAt);
        crack1 = Make("Cracks1", BossArt.Crack1, "Background", -106, BodyAt);
        crack2 = Make("Cracks2", BossArt.Crack2, "Background", -106, BodyAt);
        crack1.enabled = crack2.enabled = false;
        hatchInside = Make("HatchInside", BossArt.HatchInside, "Background", -105, HatchAt);
        hatchL = MakePart("HatchL", BossArt.Hatch, hatchArt, -104, HatchAt);
        hatchR = MakePart("HatchR", BossArt.Hatch, hatchArt, -104, HatchAt);
        hatchR.Sr.flipX = true;
        holo = Make("Hologram", BossArt.Holo, "Background", -103, HoloAt);
        bugArm = MakePart("BugArm", BossArt.BugArm, bugArmArt, -102, BugRest);
        blade = MakePart("BladeArm", BossArt.BladeArm, bladeArmArt, -101, BladeRest);

        for (int i = 0; i < bugs.Length; i++) { bugs[i] = Make("Bug", BossArt.Bug, "FX", 40, Vector2.zero); bugs[i].enabled = false; }
        zoneLane = Make("DangerLane", ParryFx.Square, "World", 4, Vector2.zero);
        zoneFloor = Make("DangerZone", ParryFx.Square, "World", 5, Vector2.zero);
        zoneLane.transform.localScale = new Vector3(ZoneHalf * 2f, 8f, 1f);
        zoneFloor.transform.localScale = new Vector3(ZoneHalf * 2f, 0.25f, 1f);
        zoneLane.color = new Color(1f, 0.23f, 0.23f, 0.12f);
        zoneLane.enabled = zoneFloor.enabled = false;
        for (int i = 0; i < aimDots.Length; i++)
        {
            aimDots[i] = Make("AimDot", ParryFx.Square, "FX", 30, Vector2.zero);
            aimDots[i].transform.localScale = new Vector3(2f * Px, 2f * Px, 1f);
            aimDots[i].enabled = false;
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

    Part MakePart(string name, Sprite[] v, Sprite art, int order, Vector2 pos)
    {
        var p = new Part { V = art != null ? new[] { art, art, art } : v, Pos = pos };
        p.Sr = Make(name, p.V[BossArt.Normal], "Background", order, pos);
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
        foe = new GameObject("BladeDuelFoe").AddComponent<BossBladeFoe>();
        foe.transform.SetParent(transform, false);
        foe.Boss = this;
        StartCoroutine(Director());
    }

    void OnEnable() => PlayerController.Swung += OnSwing;
    void OnDisable() => PlayerController.Swung -= OnSwing;

    void OnDestroy()
    {
        foreach (Material m in ownMats) Destroy(m);
        if (ownLock) GameFlow.InputLocked = false; // scene unload mid-collapse: never leave the game locked
    }

    void Update()
    {
        if (player == null) return;
        UpdateWaves(Dt);
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
        yield return Wait(1f);
        int last = -1;
        while (true)
        {
            if (GameFlow.IsPlayerDead(player)) yield break; // respawn reload follows
            int ph = Phase;
            if (ph != shownPhase) yield return PhaseShift(ph);
            int atk = ph == 1 ? (last == 0 ? 1 : 0) : last < 0 ? Random.Range(0, 3) : (last + Random.Range(1, 3)) % 3; // 0 A, 1 B, 2 C (no repeats)
            last = atk;
            int n = ph == 1 ? Random.Range(3, 5) : ph == 2 ? Random.Range(4, 7) : Random.Range(5, 8);
            float tell = ph == 1 ? 1f : 0.8f;
            BossArt.DrawHolo(atk, n); // icon index == attack index
            Sfx.Play(SfxId.EnemyTell);
            if (ph == 3 && atk != 2 && (atk == 1 || Random.value < 0.5f)) StartCoroutine(AttackC(Mathf.Max(3, n - 2), tell, true)); // overlap
            yield return atk == 0 ? AttackA(n, tell) : atk == 1 ? AttackB(n, tell) : AttackC(n, tell, false);
            BossArt.DrawHolo(-1, -1);
            while (sideRunning) yield return null;
            yield return Wait(Rest);
        }
    }

    IEnumerator PhaseShift(int ph)
    {
        shownPhase = ph;
        crack1.enabled = ph >= 2;
        crack2.enabled = ph >= 3;
        body.FlashUntil = Time.time + 0.12f;
        Sfx.Play(SfxId.Stunned);
        Juice.Shake(Juice.ShakeKillAmp, 0.45f);
        yield return Wait(1f);
    }

    // ---- A: bug swarm walls, then the arm slumps (opening) ----
    IEnumerator AttackA(int n, float tell)
    {
        bugArm.Glow = true;
        yield return Wait(tell);
        bugArm.Glow = false;
        for (int i = 0; i < n; i++)
        {
            LaunchWave(GapLows[Random.Range(0, GapLows.Length)]);
            if (i < n - 1) yield return Wait(WaveEvery);
        }
        yield return Wait(0.6f);
        yield return Move(bugArm, BugSlump, 0.2f);
        Juice.Shake(Juice.ShakeHurtAmp, 0.25f);
        Sfx.Play(SfxId.Stunned);
        armVulnerable = true;
        yield return Wait(SlumpTime);
        armVulnerable = false;
        yield return Move(bugArm, BugRest, 0.6f);
    }

    void LaunchWave(float lo)
    {
        int w = System.Array.IndexOf(waveOn, false);
        if (w < 0) return;
        waveOn[w] = true;
        waveHit[w] = false;
        waveX[w] = -1.5f;
        gapLo[w] = lo;
        float span = WaveTop - Floor - GapH;
        for (int i = w * BugsPerWave; i < (w + 1) * BugsPerWave; i++)
        {
            float y = Floor + Random.value * span;
            if (y >= lo) y += GapH;
            bugOff[i] = new Vector2(Random.Range(-WaveHalfW, WaveHalfW), y);
            bugs[i].enabled = true;
        }
        Sfx.Play(SfxId.EnemyTell, 0.6f);
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
                if (feet < WaveTop && (feet < gapLo[w] || top > gapLo[w] + GapH) &&
                    player.ReceiveEnemyHit(new HitInfo { Damage = 1, SourcePosition = new Vector2(waveX[w] - 1f, pp.y), Unparryable = true }) == HitResult.Hit)
                    waveHit[w] = true; // one hit per wall (a dash / i-frames pass through)
            }
            for (int i = w * BugsPerWave; i < (w + 1) * BugsPerWave; i++)
            {
                Vector2 o = bugOff[i];
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
        float gap = SlamGap[Phase - 1], lead = Mathf.Min(gap * 0.8f, 0.6f), tt = 0f, lockedUntil = -10f;
        int k = 0;
        bool struck = false, parried = false;
        ParryHeight h = ParryRing.RandomHeight(); // per slam, presentation only
        while (k < n)
        {
            float dt = Dt;
            tt += dt;
            float s = FirstSlam + k * gap, since = tt - (s - gap);
            bool last = k == n - 1;
            if (input != null && input.AttackPressedRaw && dt > 0f) // the player's own LMB judged here, like EliteDuel (no parry state for a boss part)
            {
                if (tt < lockedUntil) { }
                else if (!parried && tt >= s - WindowEarly && tt - dt <= s + WindowLate) parried = true;
                else if (tt < s - WindowEarly || tt > s + WindowLate) lockedUntil = tt + Lockout; // anti-mash
            }
            // blade: rises back after a slam, drops in the last 0.12 s before the next
            float y = tt >= s - 0.12f ? Mathf.Lerp(BladeHigh, Floor, (tt - (s - 0.12f)) / 0.12f)
                    : k > 0 ? Mathf.Lerp(Floor, BladeHigh, (since - 0.1f) / 0.25f) : BladeHigh;
            if (!struck) blade.Pos = new Vector2(zx, y);
            bool inZone = InZone(zx);
            if (!struck && tt >= s)
            {
                struck = true;
                blade.Pos = new Vector2(zx, last ? BladeStuckY : Floor);
                Juice.Shake(Juice.ShakeHurtAmp, 0.18f, Vector2.down);
                Sfx.Play(SfxId.Hit, 0.7f, 0.6f);
                SpriteFx.Play("impact-ring", new Vector2(zx, Floor), false, 101);
            }
            if (struck && (parried || tt > s + WindowLate))
            {
                if (inZone && parried) Clang(zx, h);
                else if (inZone) player.ReceiveEnemyHit(new HitInfo { Damage = 1, SourcePosition = new Vector2(zx, player.transform.position.y), Unparryable = true });
                if (!inZone || !parried) ring.Hide();
                k++;
                struck = parried = false;
                h = ParryRing.RandomHeight();
            }
            else if (!struck && inZone && tt >= s - lead)
            {
                int face = player.transform.position.x >= zx ? 1 : -1;
                ring.Show(new Vector2(player.transform.position.x - face * 0.45f, player.transform.position.y + ParryRing.Offset(h)), face, lead, s - tt, tt >= s - WindowEarly);
            }
            else if (!struck) ring.Hide();
            yield return null;
        }
        ShowZone(zx, false);
        BladeStuck = true;
        var live = EnemyProjectile.Active; // Fable review: shots frozen by the duel would land with no reaction time when it ends
        for (int i = live.Count - 1; i >= 0; i--) { GameObject g = live[i].gameObject; g.SetActive(false); Destroy(g); }
        blade.Glow = true; // "come here"
        duelWon = false;
        foe.transform.position = new Vector3(zx, Floor + 1f, 0f);
        for (float t = 0f; t < StuckTime; t += Dt)
        {
            if (EliteDuel.TryBegin(foe, n, player)) { while (foe.InDuel) yield return null; break; }
            yield return null;
        }
        BladeStuck = false;
        blade.Glow = false;
        bladePoseY = 0;
        yield return Move(blade, BladeRest, duelWon ? 0.25f : 0.7f); // won: recoils fast
    }

    bool InZone(float zx)
    {
        Vector2 pp = player.transform.position;
        return Mathf.Abs(pp.x - zx) < ZoneHalf + 0.35f && pp.y - 1f < BladeHigh;
    }

    void Clang(float zx, ParryHeight h)
    {
        int face = player.transform.position.x >= zx ? 1 : -1;
        Vector2 c = new Vector2(player.transform.position.x - face * 0.45f, player.transform.position.y + ParryRing.Offset(h));
        player.DuelClash(false, h);
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

    // EliteDuel callbacks (BossBladeFoe)
    public void BladeDuelPose(int frame, bool flash)
    {
        bladePoseY = frame == 1 ? 3 : frame == 2 ? -2 : 0;
        if (flash) blade.FlashUntil = Time.time + 0.06f;
    }

    public void OnDuelEnded(bool won)
    {
        duelWon = won;
        if (won) { body.FlashUntil = Time.time + 0.12f; Damage(DuelDamage, blade, foe.transform.position); }
    }

    // ---- C: hatch barrage; a parried shot flies back and hurts the hatch ----
    IEnumerator AttackC(int n, float tell, bool side)
    {
        if (side) sideRunning = true;
        hatchL.Glow = hatchR.Glow = true;
        aimOn = true;
        for (float t = 0f; t < tell; t += Dt) { hatchOpen = Mathf.Min(1f, t / 0.3f); yield return null; }
        hatchOpen = 1f;
        hatchL.Glow = hatchR.Glow = false;
        float every = ShotEvery[Phase - 1];
        for (int i = 0; i < n; i++)
        {
            if (side && BladeStuck) break; // the overlap barrage stops once the blade sticks (duel next)
            Vector2 d = (Vector2)player.transform.position - HatchAt;
            EnemyProjectile p = EnemyProjectile.Spawn(HatchAt, d.normalized * ShotSpeed, playerMask, groundMask, new HitInfo { Damage = 1 }, GoldShot);
            p.ReflectLife = ShotReflectLife;
            p.ReflectTarget = HatchAt; // reflected = a weakened lob that drops back into the hatch
            Sfx.Play(SfxId.EnemyTell, 0.5f, 1.5f);
            yield return Wait(every);
        }
        aimOn = false;
        yield return Wait(HatchOpenAfter);
        for (float t = 0f; t < 0.3f; t += Dt) { hatchOpen = 1f - t / 0.3f; yield return null; }
        hatchOpen = 0f;
        if (side) sideRunning = false;
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
        if (dead) { if (breakable) BreakNear(centre, size); return; }
        if (!armVulnerable) return;
        var arm = new Rect(bugArm.Pos.x - 1.5f, bugArm.Pos.y, 3f, 5.5f); // the slumped palm + lower arm
        if (Mathf.Abs(centre.x - arm.center.x) > (size.x + arm.width) * 0.5f || Mathf.Abs(centre.y - arm.center.y) > (size.y + arm.height) * 0.5f) return;
        Damage(ArmHitDamage, bugArm, new Vector2(Mathf.Clamp(centre.x, arm.xMin, arm.xMax), Mathf.Clamp(centre.y, arm.yMin, arm.yMax)));
        player.DuelHitStop(Juice.HitStopHit);
    }

    void Damage(int amount, Part part, Vector2 at)
    {
        if (dead) return;
        hp -= amount;
        part.FlashUntil = Mathf.Max(part.FlashUntil, Time.time + 0.08f);
        if (part == hatchL) hatchR.FlashUntil = part.FlashUntil;
        Sfx.Play(SfxId.Hit);
        SpriteFx.Play("impact-ring", at, false, 101);
        SpriteFx.Play("hit-spark", at - new Vector2(12f, 16f) / SpriteFx.Ppu);
        Juice.Shake(Juice.ShakeKillAmp, Juice.ShakeKillTime);
        if (hp <= 0) Die();
    }

    void Die()
    {
        dead = true;
        StopAllCoroutines();
        armVulnerable = BladeStuck = aimOn = sideRunning = false;
        bugArm.Glow = blade.Glow = hatchL.Glow = hatchR.Glow = false;
        ShowZone(0f, false);
        ring.Hide();
        for (int w = 0; w < Waves; w++) waveOn[w] = false;
        for (int i = 0; i < bugs.Length; i++) bugs[i].enabled = false;
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
        while (GameFlow.InputLocked) yield return null; // the duel finisher hold
        StartCoroutine(Move(bugArm, BugSlump, 0.8f));
        StartCoroutine(Move(blade, new Vector2(blade.Pos.x, BladeStuckY), 0.8f));
        yield return Wait(1.2f);
        hint.text = Loc.T("SMASH THE MACHINES  [LMB]", "기계를 부숴라  [좌클릭]");
        hint.gameObject.SetActive(true);
        breakable = true;
        while (propsLeft > 0) yield return null;
        hint.gameObject.SetActive(false);
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
        GameFlow.InputLocked = ownLock = false;
        StageFlow.LoadTitle();
    }

    void BreakNear(Vector2 centre, Vector2 size)
    {
        int best = -1;
        float bestD = float.MaxValue;
        for (int i = 0; i < props.Length; i++)
        {
            if (propBroken[i]) continue;
            Bounds b = props[i].bounds;
            b.Expand(new Vector3(size.x + 1.2f, size.y + 1.2f, 0f)); // "near": the swing box plus 0.6 u
            float d = Mathf.Abs(b.center.x - centre.x);
            if (b.Contains(new Vector3(centre.x, centre.y, b.center.z)) && d < bestD) { best = i; bestD = d; }
        }
        if (best < 0) return;
        propBroken[best] = true;
        propsLeft--;
        Vector2 c = props[best].bounds.center;
        float dir = c.x >= player.transform.position.x ? 1f : -1f;
        ShatterFx.Play(props[best], new Vector2(dir, 0.5f), c, Cyan);
        props[best].enabled = false;
        SpriteFx.Play("kill-burst", c, dir < 0f, 102);
        Sfx.Play(SfxId.Hit);
        Juice.Shake(Juice.ShakeKillAmp, Juice.ShakeKillTime, new Vector2(dir, 0f));
        player.DuelHitStop(Juice.HitStopKill);
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
        Show(blade, tint, now, blink, bob + bladePoseY * Px);
        float open = Smooth(hatchOpen) * 1.75f;
        hatchL.Pos = HatchAt + new Vector2(-1f - open, 0f);
        hatchR.Pos = HatchAt + new Vector2(1f + open, 0f);
        Show(hatchL, tint, now, blink, 0f);
        Show(hatchR, tint, now, blink, 0f);
        if (!dead)
        {
            bool glitch = shownPhase == 3 && Random.value < 0.12f; // phase 3: the hologram jitters
            holo.transform.position = new Vector3(Snap(HoloAt.x) + (glitch ? (Random.value < 0.5f ? -2f : 2f) * Px : 0f), Snap(HoloAt.y), 0f);
            holo.color = new Color(1f, 1f, 1f, !started ? 0.35f : glitch && Random.value < 0.5f ? 0.45f : 1f);
        }
        if (zoneFloor.enabled) zoneFloor.color = new Color(1f, 0.23f, 0.23f, blink ? 0.85f : 0.55f);
        Vector2 aimTo = player.transform.position;
        for (int i = 0; i < aimDots.Length; i++)
        {
            aimDots[i].enabled = aimOn && !dead;
            if (!aimOn) continue;
            Vector2 p = Vector2.Lerp(HatchAt, aimTo, (i + 1f) / (aimDots.Length + 1f));
            aimDots[i].transform.position = new Vector3(Snap(p.x), Snap(p.y), 0f);
            aimDots[i].color = new Color(GoldShot.r, GoldShot.g, GoldShot.b, blink ? 0.9f : 0.5f);
        }
        if (breakable)
            for (int i = 0; i < props.Length; i++)
                if (!propBroken[i]) props[i].color = blink ? Color.white : new Color(0.65f, 0.95f, 1f); // "hit me"
    }

    void Show(Part p, Color tint, float now, bool blink, float dy)
    {
        bool flash = now < p.FlashUntil;
        int v = flash ? BossArt.Flash : p.Glow && blink ? BossArt.Lit : BossArt.Normal;
        p.Sr.sprite = p.V[v];
        bool noVariants = p.V[BossArt.Normal] == p.V[BossArt.Lit];
        p.Sr.color = noVariants && flash ? Color.white : noVariants && v == BossArt.Lit ? new Color(1f, 0.55f, 0.45f) : tint;
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
