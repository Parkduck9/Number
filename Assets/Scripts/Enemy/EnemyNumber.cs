using TMPro;
using UnityEngine;

// Personal number (2-5) shown above an enemy. See docs/design/PROTOTYPE_ARCH.md section 4.
public class EnemyNumber : MonoBehaviour
{
    [SerializeField] TMP_Text label;
    [SerializeField, Range(0f, 1f)] float progress; // 0 = early, 1 = late (T: drives reroll weights)

    // Reroll weights for numbers 2,3,4,5 (T)
    [SerializeField] float[] earlyWeights = { 40f, 40f, 15f, 5f };
    [SerializeField] float[] lateWeights = { 5f, 15f, 40f, 40f };

    [SerializeField] float pulseSpeed = 2.5f;       // (T) pulses per second
    [SerializeField] float pulseScale = 1.3f;       // (T)
    [SerializeField] float specialPulseScale = 1.6f; // (T)

    static readonly Color Red = new Color(1f, 0.1647f, 0.1647f); // #FF2A2A
    static readonly Color Yellow = new Color(1f, 0.9f, 0.1f);
    static readonly Color White = Color.white;

    public int Value { get; private set; }

    bool pulsing;
    bool special;

    void Start()
    {
        Reroll();
    }

    void Update()
    {
        if (label == null) return;
        float s = 1f;
        if (pulsing)
        {
            float max = special ? specialPulseScale : pulseScale;
            s = Mathf.Lerp(1f, max, Mathf.PingPong(Time.time * pulseSpeed, 1f));
        }
        label.transform.localScale = new Vector3(s, s, 1f);
    }

    public void SetProgress(float p) { progress = Mathf.Clamp01(p); }

    public void Reroll()
    {
        float total = 0f;
        float[] w = new float[4];
        for (int i = 0; i < 4; i++)
        {
            w[i] = Mathf.Lerp(earlyWeights[i], lateWeights[i], progress);
            total += w[i];
        }
        float r = Random.value * total;
        int pick = 3;
        for (int i = 0; i < 4; i++)
        {
            if (r < w[i]) { pick = i; break; }
            r -= w[i];
        }
        Value = pick + 2;
        SetPulse(false, false);
        Refresh();
    }

    public void Decrement()
    {
        if (Value > 0) Value--;
        Refresh();
    }

    public void Hide()
    {
        pulsing = false;
        if (label != null)
        {
            label.gameObject.SetActive(false);
            label.transform.localScale = Vector3.one;
        }
    }

    public void SetPulse(bool on, bool isSpecial)
    {
        pulsing = on;
        special = on && isSpecial;
        Refresh();
    }

    void Refresh()
    {
        if (label == null) return;
        label.gameObject.SetActive(true);
        label.text = Value.ToString();
        label.color = Value <= 0 ? White : (special ? Yellow : Red);
        if (!pulsing) label.transform.localScale = Vector3.one;
    }
}
