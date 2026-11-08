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
    Transform holoRoot;
    SpriteRenderer holoWindow, holoDigit;
    bool holoGold;

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
        holoRoot.localPosition = label.transform.localPosition;
        holoWindow = NewHoloPart("Window", 19, DraftHud.HoloWindow(holoGold));
        holoDigit = NewHoloPart("Digit", 20, null);
    }

    SpriteRenderer NewHoloPart(string name, int order, Sprite sprite)
    {
        var go = new GameObject(name);
        go.transform.SetParent(holoRoot, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return sr;
    }

    // Holo mode: digit sprite + tint instead of label text. flame = last-strike tint (redder), err = overload flicker frame.
    void ShowHolo(bool err)
    {
        holoDigit.sprite = DraftHud.HoloDigits(holoGold)[Mathf.Clamp(Value, 0, 9)];
        holoDigit.enabled = !err;
        Color c = err ? Color.white : flameOn ? Color.Lerp(Color.white, Red, 0.6f) : Color.white;
        holoWindow.color = c;
        holoDigit.color = c;
    }

    void Update()
    {
        if (label == null) return;
        if (broken)
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
        if (holoRoot != null) holoRoot.localScale = new Vector3(s, s, 1f);
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
        broken = false;
        pulsing = false;
        SetFlame(false);
        if (label != null)
        {
            label.gameObject.SetActive(false);
            label.transform.localScale = Vector3.one;
        }
        if (holoRoot != null) holoRoot.gameObject.SetActive(false);
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
            top = label.transform.localPosition + new Vector3(0f, 8f / SpriteFx.Ppu, 0f);
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
            holoRoot.gameObject.SetActive(true);
            ShowHolo(false);
        }
        else label.text = Value.ToString();
        label.color = flameOn ? Color.Lerp(numberColor, Red, 0.6f) : numberColor;
        if (!pulsing) label.transform.localScale = Vector3.one;
    }
}
