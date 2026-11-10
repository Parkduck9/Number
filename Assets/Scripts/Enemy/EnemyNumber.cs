using System.Linq;
using TMPro;
using UnityEngine;

// Personal number (2-5) shown above an enemy. It never changes during a chain.
// See docs/design/PROTOTYPE_ARCH.md section 4 and 7 ("추가 확정 3").
public class EnemyNumber : MonoBehaviour
{
    [SerializeField] TMP_Text label;
    [SerializeField, Range(0f, 1f)] float progress; // 0 = early, 1 = late (T: drives reroll weights)

    // Reroll weights for numbers 2,3,4,5 (T)
    [SerializeField] float[] earlyWeights = { 40f, 40f, 15f, 5f };
    [SerializeField] float[] lateWeights = { 5f, 15f, 40f, 40f };

    [Header("Variant")]
    [SerializeField] int minValue = 2; // inclusive. If the range size matches the weight arrays they are used, else a linear early/late ramp
    [SerializeField] int maxValue = 5;
    [SerializeField] int fixedNumber = 0; // > 0 = always this number (tutorial enemies); 0 = weighted random
    [SerializeField] Color numberColor = new Color(1f, 0.1647f, 0.1647f); // red = melee, gold (#FFC93C) = ranged

    [SerializeField] float pulseSpeed = 2.5f; // (T) pulses per second
    [SerializeField] float pulseScale = 1.3f; // (T)

    // Hologram window tunables (T). World units (1 px = 1/32). The window sits in front of the eyes on the facing side and flips when the enemy turns.
    const float HoloEyeX = 0.15f, HoloEyeY = 0.55f;   // visor/eye point, relative to the enemy root (x mirrored by facing)
    const float HoloWinX = 0.95f, HoloWinY = 1.05f;   // window centre, relative to the enemy root (x mirrored by facing)
    const float HoloSquashX = 0.75f;                   // panel seen at an angle: X squash
    const float HoloAlpha = 0.85f, HoloFlickerAlpha = 0.35f;
    const float HoloFlickerMin = 1.5f, HoloFlickerMax = 4f;   // seconds between 1-frame flickers
    const float HoloJitterMin = 3f, HoloJitterMax = 7f;       // seconds between 1-px jitters (each lasts HoloJitterTime)
    const float HoloJitterTime = 0.06f;
    const float HoloBobSpeed = 1.5f;                   // rad/s, 1 px amplitude (rounded to the pixel grid)
    const float HoloUnfoldTime = 0.12f;                // vertical unfold on appear / collapse on Hide
    const float HoloBeamAlpha = 0.18f, HoloBeamWidth = 1f / 32f;
    const float EmberDigitWidth = 14f; // px: the digit width number-ember was drawn for
    static readonly Color Red = new Color(1f, 0.1647f, 0.1647f); // #FF2A2A

    public int Value { get; private set; }

    int pinned; // > 0 = forced number (broadcast rule), wins over fixedNumber
    bool pulsing;
    bool flameOn;
    bool broken; // android overload: label flickers between ERR and the number
    int lastErrShown = -1; // -1 = not set yet
    GameObject flame;

    // DraftHud hologram window (20x16 px window + 7x11 glyph, gold = ranged, red = melee) replaces the TMP number; the label stays but empty.
    Transform holoRoot, holoPanel;
    SpriteRenderer holoWindow, holoDigit;
    LineRenderer[] holoBeams;
    bool holoGold, holoFront; // holoFront = in front of the eyes (humanoids); bugs keep it above the head
    SpriteRenderer faceRenderer;
    Color holoTint = Color.white;
    float unfoldFrom = -10f, collapseFrom = -10f; // Time.time of the unfold / collapse start
    float nextFlicker, nextJitter, jitterUntil;
    int flickerFrame = -1;
    Vector2 jitter;
    int lastFace = 1;
    static Material beamMat;

    // Dice override (skill 5 countdown): another value in another digit sheet over the enemy's own number. Hide() clears it.
    bool overriding;
    int overrideValue;
    Sprite[] overrideSheet;
    static Sprite[] cyanDigits;
    static bool cyanLoaded;

    // DraftHud/holo_digits_cyan (0..9, same layout as holo_digits_gold); null when missing.
    public static Sprite[] CyanDigits
    {
        get
        {
            if (cyanLoaded) return cyanDigits;
            cyanLoaded = true;
            if (!DraftSprites.Use) return null;
            Sprite[] s = Resources.LoadAll<Sprite>("DraftHud/holo_digits_cyan").OrderBy(x => x.name).ToArray();
            cyanDigits = s.Length == 10 ? s : null;
            return cyanDigits;
        }
    }

    public void SetOverride(int value, Sprite[] digitSheet)
    {
        overriding = true;
        overrideValue = value;
        overrideSheet = digitSheet;
        Refresh();
    }

    public void ClearOverride()
    {
        if (!overriding) return; // already cleared (Hide): do not bring back a hidden number
        overriding = false;
        overrideSheet = null;
        lastErrShown = -1; // overload flicker resumes
        Refresh();
    }

    void Awake()
    {
        BuildHolo();
        Reroll(); // before TestEnemy.Start reads Value
    }

    void BuildHolo()
    {
        if (label == null || !DraftHud.HoloReady) return;
        holoGold = numberColor.g > 0.5f; // gold #FFC93C vs red #FF2A2A
        holoRoot = new GameObject("HoloNumber").transform;
        holoRoot.SetParent(label.transform.parent, false);
        holoRoot.localPosition = Vector3.zero; // positions are set per frame, relative to the enemy root
        holoPanel = new GameObject("Panel").transform;
        holoPanel.SetParent(holoRoot, false);
        holoWindow = NewHoloPart("Window", 19, DraftHud.HoloWindow(holoGold));
        holoDigit = NewHoloPart("Digit", 20, null);
        var te = GetComponent<TestEnemy>();
        holoFront = te == null || !BugSwarmBootstrap.IsBug(te);
        if (holoFront && Shader.Find("Sprites/Default") != null) BuildBeams();
        unfoldFrom = Time.time; // spawn: unfold
        nextFlicker = Time.time + Random.Range(HoloFlickerMin, HoloFlickerMax);
        nextJitter = Time.time + Random.Range(HoloJitterMin, HoloJitterMax);
    }

    void BuildBeams()
    {
        if (beamMat == null) beamMat = new Material(Shader.Find("Sprites/Default"));
        holoBeams = new LineRenderer[2];
        for (int i = 0; i < 2; i++)
        {
            var go = new GameObject("Beam" + i);
            go.transform.SetParent(holoRoot, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.positionCount = 2;
            lr.sharedMaterial = beamMat;
            lr.startWidth = lr.endWidth = HoloBeamWidth;
            lr.sortingOrder = 18;
            holoBeams[i] = lr;
        }
    }

    SpriteRenderer NewHoloPart(string name, int order, Sprite sprite)
    {
        var go = new GameObject(name);
        go.transform.SetParent(holoPanel, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return sr;
    }

    // Holo mode: digit sprite + tint instead of label text. flame = last-strike tint (redder), err = overload flicker frame.
    void ShowHolo(bool err)
    {
        Sprite[] sheet = overriding && overrideSheet != null && overrideSheet.Length >= 10 ? overrideSheet : DraftHud.HoloDigits(holoGold);
        holoDigit.sprite = sheet[Mathf.Clamp(overriding ? overrideValue : Value, 0, 9)];
        holoDigit.enabled = !err;
        holoTint = err ? Color.white : flameOn ? Color.Lerp(Color.white, Red, 0.6f) : Color.white;
        ApplyHoloColor(HoloAlpha);
    }

    void ApplyHoloColor(float alpha)
    {
        Color c = holoTint;
        c.a = alpha;
        holoWindow.color = c;
        holoDigit.color = c;
        if (holoBeams == null) return;
        Color b = holoGold ? new Color(1f, 0.788f, 0.235f) : Red;
        b.a = HoloBeamAlpha * alpha / HoloAlpha;
        foreach (var lr in holoBeams) lr.startColor = lr.endColor = b;
    }

    // Window position relative to the enemy root: in front of the eyes on the facing side (bugs: where the label is, above the head).
    Vector2 HoloPos(int face) => holoFront ? new Vector2(face * HoloWinX, HoloWinY) : (Vector2)label.transform.localPosition;

    int Face()
    {
        if (faceRenderer == null)
            foreach (var r in transform.GetComponentsInChildren<SpriteRenderer>(true))
                if (!r.transform.IsChildOf(holoRoot)) { faceRenderer = r; break; }
        if (faceRenderer != null) lastFace = faceRenderer.flipX ? -1 : 1; // TestEnemy.ApplyVisual: flipX = facing < 0
        return lastFace;
    }

    // Hologram life: placement, angled panel, beams, flicker/jitter/bob, unfold/collapse. Time.time stands still in hit-stop / pause, so it all freezes with the rest.
    void UpdateHolo(float s)
    {
        float now = Time.time;
        int face = Face();
        float unfold = Mathf.Clamp01((now - unfoldFrom) / HoloUnfoldTime);
        if (collapseFrom >= 0f)
        {
            unfold = 1f - Mathf.Clamp01((now - collapseFrom) / HoloUnfoldTime);
            if (unfold <= 0f) { collapseFrom = -10f; holoRoot.gameObject.SetActive(false); return; }
        }
        if (Time.timeScale > 0f)
        {
            if (now >= nextFlicker) { flickerFrame = Time.frameCount; nextFlicker = now + Random.Range(HoloFlickerMin, HoloFlickerMax); }
            if (now >= nextJitter)
            {
                jitterUntil = now + HoloJitterTime;
                jitter = new Vector2(Random.value < 0.5f ? -1f : 1f, Random.value < 0.5f ? -1f : 0f) / SpriteFx.Ppu;
                nextJitter = now + Random.Range(HoloJitterMin, HoloJitterMax);
            }
        }
        Vector2 j = now < jitterUntil ? jitter : Vector2.zero;
        float bob = Mathf.Round(Mathf.Sin(now * HoloBobSpeed) * 0.5f + 0.5f) / SpriteFx.Ppu; // 0..1 px, stays on the pixel grid
        Vector2 pos = HoloPos(face) + j + new Vector2(0f, bob);
        holoPanel.localPosition = pos;
        float sx = holoFront ? HoloSquashX : 1f;
        holoPanel.localScale = new Vector3(s * sx, s * Mathf.Max(0.01f, unfold), 1f);
        ApplyHoloColor(Time.frameCount == flickerFrame ? HoloFlickerAlpha : HoloAlpha);
        if (flame != null) flame.transform.localPosition = new Vector3(pos.x, pos.y + 8f / SpriteFx.Ppu * s * unfold, 0f);
        if (holoBeams == null) return;
        // Beams: eye point -> the window's near-side corners (near = toward the enemy), narrowing with the unfold.
        Vector3 eye = new Vector3(face * HoloEyeX, HoloEyeY, 0f);
        float hw = 10f / SpriteFx.Ppu * s * sx, hh = 8f / SpriteFx.Ppu * s * unfold;
        float nx = pos.x - face * hw;
        holoBeams[0].SetPosition(0, eye); holoBeams[0].SetPosition(1, new Vector3(nx, pos.y + hh, 0f));
        holoBeams[1].SetPosition(0, eye); holoBeams[1].SetPosition(1, new Vector3(nx, pos.y - hh, 0f));
    }

    void Update()
    {
        if (label == null) return;
        if (broken && !overriding)
        {
            int errShown = Mathf.FloorToInt(Time.time * 12f) % 2 == 0 ? 1 : 0;
            if (errShown != lastErrShown) // assign only on flips: no per-frame string garbage
            {
                lastErrShown = errShown;
                if (holoRoot != null) ShowHolo(errShown == 1);
                else
                {
                    label.text = errShown == 1 ? "ERR" : Value.ToString();
                    label.color = errShown == 1 ? Color.white : numberColor;
                }
            }
        }
        float s = pulsing ? Mathf.Lerp(1f, pulseScale, Mathf.PingPong(Time.time * pulseSpeed, 1f)) : 1f;
        float pop = (Time.time - popAt) / PopTime; // brief punch on top of the pulse
        if (pop >= 0f && pop < 1f) s *= 1f + PopScale * (1f - pop);
        label.transform.localScale = new Vector3(s, s, 1f);
        holoScale = s;
    }

    float holoScale = 1f;

    // After TestEnemy.Update has set the sprite's flipX, so the window never lags a frame behind a turn.
    void LateUpdate()
    {
        if (label != null && holoRoot != null && holoRoot.gameObject.activeSelf) UpdateHolo(holoScale);
    }

    const float PopTime = 0.12f, PopScale = 0.5f; // (T) punch length / extra scale when a strike uses up one count
    float popAt = -10f;

    public void Pop() { popAt = Time.time; }

    public void SetProgress(float p) { progress = Mathf.Clamp01(p); }

    // Pins the number for later rerolls (0 = none). Does not reroll by itself.
    public void Pin(int n) { pinned = n; }

    public void Reroll()
    {
        if (pinned > 0 || fixedNumber > 0)
        {
            Value = pinned > 0 ? pinned : fixedNumber;
            SetPulse(false);
            SetFlame(false);
            Refresh();
            return;
        }
        int n = Mathf.Max(1, maxValue - minValue + 1);
        bool useArrays = n == earlyWeights.Length && n == lateWeights.Length;
        float total = 0f;
        float[] w = new float[n];
        for (int i = 0; i < n; i++)
        {
            w[i] = useArrays ? Mathf.Lerp(earlyWeights[i], lateWeights[i], progress)
                             : Mathf.Lerp(n - i, i + 1, progress);
            total += w[i];
        }
        float r = Random.value * total;
        int pick = n - 1;
        for (int i = 0; i < n; i++)
        {
            if (r < w[i]) { pick = i; break; }
            r -= w[i];
        }
        Value = pick + minValue;
        SetPulse(false);
        SetFlame(false);
        Refresh();
    }

    // Overload look: Update flickers the label. Turning it off restores the number.
    public void SetBroken(bool on)
    {
        if (broken == on) return;
        broken = on;
        lastErrShown = -1;
        if (!on) Refresh();
    }

    public void Hide()
    {
        overriding = false;
        overrideSheet = null;
        broken = false;
        pulsing = false;
        SetFlame(false);
        if (label != null)
        {
            label.gameObject.SetActive(false);
            label.transform.localScale = Vector3.one;
        }
        if (holoRoot != null)
        {
            if (gameObject.activeInHierarchy && holoRoot.gameObject.activeSelf) collapseFrom = Time.time; // UpdateHolo switches it off after the collapse
            else holoRoot.gameObject.SetActive(false);
        }
    }

    public void SetPulse(bool on)
    {
        pulsing = on;
        Refresh();
    }

    // Faint red flame on top of the number = "next strike is the last one".
    public void SetFlame(bool on)
    {
        if (on == flameOn) return;
        flameOn = on;
        Refresh(); // non-red numbers (gold) look redder while the flame burns; also re-activates a hidden label
        if (on && label != null) BuildFlame();
        else if (flame != null) { Destroy(flame); flame = null; }
    }

    // Looping number-ember scaled to the label width (sheet is drawn for a 14px digit). Sibling of the label: unaffected by its pulse scale.
    void BuildFlame()
    {
        Vector3 top;
        float scale;
        if (holoRoot != null) // holo window: 16 px tall, 20 px wide
        {
            Vector2 hp = HoloPos(lastFace); // UpdateHolo keeps it on the window afterwards
            top = new Vector3(hp.x, hp.y + 8f / SpriteFx.Ppu, 0f);
            scale = Mathf.Max(1f, Mathf.Round(20f / EmberDigitWidth));
        }
        else
        {
            label.ForceMeshUpdate();
            Bounds b = label.textBounds;
            top = label.transform.localPosition + new Vector3(b.center.x, b.max.y, 0f);
            scale = Mathf.Max(1f, Mathf.Round(b.size.x / (EmberDigitWidth / SpriteFx.Ppu))); // integer keeps the pixel grid
        }
        flame = SpriteFx.Play("number-ember", Vector2.zero, false, 21, scale);
        if (flame == null) return;
        flame.transform.SetParent(label.transform.parent, false);
        flame.transform.localPosition = top;
    }

    void Refresh()
    {
        if (label == null) return;
        label.gameObject.SetActive(true);
        if (holoRoot != null)
        {
            label.text = ""; // the hologram window shows the number
            collapseFrom = -10f;
            if (!holoRoot.gameObject.activeSelf) unfoldFrom = Time.time; // appears: vertical unfold
            holoRoot.gameObject.SetActive(true);
            ShowHolo(false);
        }
        else label.text = (overriding ? overrideValue : Value).ToString();
        label.color = flameOn ? Color.Lerp(numberColor, Red, 0.6f) : numberColor;
        if (!pulsing) label.transform.localScale = Vector3.one;
    }
}
