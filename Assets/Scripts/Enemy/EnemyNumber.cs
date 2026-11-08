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

    [Header("Last-strike flame (T, placeholder)")]
    [SerializeField] int flameCount = 4;
    [SerializeField] float flameSize = 0.12f;
    [SerializeField] float flameHeight = 0.6f; // above label center (enemy-local units)
    [SerializeField] float flameSpread = 0.3f;
    [SerializeField] float flameFlicker = 12f;

    static readonly Color Red = new Color(1f, 0.1647f, 0.1647f); // #FF2A2A
    static readonly Color DarkRed = new Color(0.55f, 0.05f, 0.05f);

    public int Value { get; private set; }

    bool pulsing;
    bool flameOn;
    Transform[] flame;
    SpriteRenderer[] flameSr;

    void Awake()
    {
        Reroll(); // before TestEnemy.Start reads Value
    }

    void Update()
    {
        if (label == null) return;
        float s = pulsing ? Mathf.Lerp(1f, pulseScale, Mathf.PingPong(Time.time * pulseSpeed, 1f)) : 1f;
        label.transform.localScale = new Vector3(s, s, 1f);
        if (flameOn) UpdateFlame();
    }

    public void SetProgress(float p) { progress = Mathf.Clamp01(p); }

    public void Reroll()
    {
        if (fixedNumber > 0)
        {
            Value = fixedNumber;
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

    public void Hide()
    {
        pulsing = false;
        SetFlame(false);
        if (label != null)
        {
            label.gameObject.SetActive(false);
            label.transform.localScale = Vector3.one;
        }
    }

    public void SetPulse(bool on)
    {
        pulsing = on;
        Refresh();
    }

    // Faint red pixel flame above the number = "next strike is the last one".
    public void SetFlame(bool on)
    {
        if (on == flameOn) return;
        flameOn = on;
        if (on && flame == null && label != null) BuildFlame();
        if (flame != null)
            foreach (Transform t in flame) t.gameObject.SetActive(on);
        Refresh(); // non-red numbers (gold) look redder while the flame burns
    }

    void BuildFlame()
    {
        flame = new Transform[flameCount];
        flameSr = new SpriteRenderer[flameCount];
        for (int i = 0; i < flameCount; i++)
        {
            GameObject go = new GameObject("Flame");
            go.transform.SetParent(label.transform.parent, false); // sibling of the label: unaffected by its scale
            go.transform.localScale = new Vector3(flameSize, flameSize, 1f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ParryFx.Square;
            sr.sortingOrder = 21;
            flame[i] = go.transform;
            flameSr[i] = sr;
        }
    }

    void UpdateFlame()
    {
        if (flame == null) return;
        float step = Mathf.Floor(Time.time * flameFlicker); // jumpy pixel flicker, not smooth
        for (int i = 0; i < flame.Length; i++)
        {
            float h = Mathf.Abs(Mathf.Sin((step + i * 3.1f) * 12.9898f) * 43758.5453f) % 1f;
            float h2 = Mathf.Abs(Mathf.Sin((step + i * 7.7f) * 78.233f) * 12345.678f) % 1f;
            float x = (i - (flame.Length - 1) * 0.5f) / Mathf.Max(1, flame.Length - 1) * 2f * flameSpread;
            flame[i].localPosition = label.transform.localPosition + new Vector3(x, flameHeight + h * flameSize * 4f, 0f);
            Color c = h2 > 0.5f ? Red : DarkRed;
            c.a = 0.55f;
            flameSr[i].color = c;
        }
    }

    void Refresh()
    {
        if (label == null) return;
        label.gameObject.SetActive(true);
        label.text = Value.ToString();
        label.color = flameOn ? Color.Lerp(numberColor, Red, 0.6f) : numberColor;
        if (!pulsing) label.transform.localScale = Vector3.one;
    }
}
