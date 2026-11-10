using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Dice art (Assets/Resources/DraftHud/dice_*.png, 24x24 die, 8x8 pips), loaded once. Getters return null when the art is missing.
public static class DiceArt
{
    static Sprite[] roll, faces;
    static Sprite off, pipEmpty, pipFull;
    static bool loaded;

    public static Sprite[] Roll { get { Init(); return roll; } }              // 4 tumbling frames
    public static Sprite Face(int n) { Init(); return faces != null && n >= 1 && n <= 6 ? faces[n - 1] : null; }
    public static Sprite Off { get { Init(); return off; } }
    public static Sprite PipEmpty { get { Init(); return pipEmpty; } }
    public static Sprite PipFull { get { Init(); return pipFull; } }
    public static bool Ready { get { Init(); return roll != null && faces != null; } }

    static void Init()
    {
        if (loaded) return;
        loaded = true;
        roll = Set("dice_roll_", 0, 4);
        faces = Set("dice_face_", 1, 6);
        off = Resources.Load<Sprite>("DraftHud/dice_off");
        pipEmpty = Resources.Load<Sprite>("DraftHud/dice_pip_empty");
        pipFull = Resources.Load<Sprite>("DraftHud/dice_pip_full");
    }

    static Sprite[] Set(string prefix, int first, int count)
    {
        var s = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            s[i] = Resources.Load<Sprite>("DraftHud/" + prefix + (first + i));
            if (s[i] == null) return null;
        }
        return s;
    }
}

// DECISIONS 18 roll presentation (~0.7 s): a big die is thrown in from the upper-left, lands, bounces once, rolls into the screen
// centre and stops on the face; flash + the number pops (divination throw: talisman-yellow sparks). Never rotated: tumbling = roll frames.
// Screen-space overlay on the 960x540 reference canvas, own clock (unscaled, the roll runs under GameFlow.InputLocked).
public class DiceRollView : MonoBehaviour
{
    const int Scale = 4;                       // (T) 24 px die at 4x = 96 reference px
    const float Die = 24f * Scale;
    const float FlyTime = 0.25f, BounceTime = 0.18f, RollTime = 0.12f, FlashTime = 0.15f, TailTime = 0.25f; // (T) 0.25 + 0.3 + 0.15
    const float FloorY = -Die * 0.5f;          // die pivot is bottom-centre, so at rest the die is centred on the screen
    const float SquashTime = 0.07f, Squash = 0.2f, BounceHeight = 44f;
    const int TumbleFrames = 3;                // dice_roll_0..2 (see Play)
    static readonly Vector2 ThrowFrom = new Vector2(-560f, 330f), Land1 = new Vector2(-200f, FloorY), Land2 = new Vector2(-72f, FloorY);
    static readonly Color Talisman = new Color32(0xF2, 0xC9, 0x4C, 0xFF);
    static readonly Color Ink = new Color32(0x14, 0x10, 0x1C, 0xFF);
    static readonly Color Dim = new Color32(0x0B, 0x0F, 0x1E, 0x60);

    GameObject root;
    CanvasGroup group;
    Image dim, flash, die, shadow;
    RectTransform dieRt, shadowRt, ring;
    CanvasGroup ringGroup;
    TMP_Text number, numberShadow;
    readonly Image[] sparks = new Image[8];
    readonly Vector2[] sparkDir = new Vector2[8];
    Coroutine tail;

    public static DiceRollView Create()
    {
        var v = new GameObject("DiceRollView").AddComponent<DiceRollView>();
        v.Build();
        return v;
    }

    void Build()
    {
        var canvas = UiKit.CreateCanvas("DiceRollCanvas", transform, 95); // above the HUDs / enemy pointers, below stage clear (150) and pause (200)
        root = canvas.gameObject;
        group = root.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        var center = new Vector2(0.5f, 0.5f);

        dim = UiKit.FilledImage(UiKit.Stretch("Dim", canvas.transform), Dim);
        shadowRt = UiKit.Rect("Shadow", canvas.transform, center, Vector2.zero, new Vector2(Die - 2 * Scale, 2 * Scale));
        shadow = UiKit.FilledImage(shadowRt, new Color(Ink.r, Ink.g, Ink.b, 0.5f));

        ring = UiKit.Rect("Ring", canvas.transform, center, Vector2.zero, new Vector2(Die, Die));
        ringGroup = ring.gameObject.AddComponent<CanvasGroup>();
        float e = Scale;
        Edge(new Vector2(0.5f, 1f), new Vector2(Die, e));
        Edge(new Vector2(0.5f, 0f), new Vector2(Die, e));
        Edge(new Vector2(0f, 0.5f), new Vector2(e, Die));
        Edge(new Vector2(1f, 0.5f), new Vector2(e, Die));

        dieRt = UiKit.Rect("Die", canvas.transform, center, Vector2.zero, new Vector2(Die, Die));
        dieRt.pivot = new Vector2(0.5f, 0f);
        die = UiKit.FilledImage(dieRt, Color.white);

        for (int i = 0; i < sparks.Length; i++)
        {
            sparks[i] = UiKit.FilledImage(UiKit.Rect("Spark", canvas.transform, center, Vector2.zero, new Vector2(2 * Scale, 2 * Scale)), i % 2 == 0 ? Talisman : Color.white);
            float a = (i + 0.5f) * Mathf.PI * 2f / sparks.Length;
            sparkDir[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        numberShadow = Number("NumberShadow", Ink);
        number = Number("Number", Talisman);
        flash = UiKit.FilledImage(UiKit.Stretch("Flash", canvas.transform), Color.clear);
        root.SetActive(false);
    }

    void Edge(Vector2 anchor, Vector2 size) => UiKit.FilledImage(UiKit.Rect("Edge", ring, anchor, Vector2.zero, size), Talisman);

    TMP_Text Number(string name, Color color)
    {
        var rt = UiKit.Rect(name, root.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 72f));
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (UiTheme.Current.font != null) t.font = UiTheme.Current.font;
        t.fontSize = 48;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        return t;
    }

    // Runs the throw; returns after FlyTime + BounceTime + RollTime + FlashTime. The die then fades on its own while the skill starts.
    public IEnumerator Play(int face)
    {
        if (tail != null) { StopCoroutine(tail); tail = null; }
        root.SetActive(true);
        group.alpha = 1f;
        bool art = DiceArt.Ready;
        die.enabled = shadow.enabled = art;
        die.color = Color.white;
        number.text = numberShadow.text = face.ToString();
        SetNumber(0f);
        ringGroup.alpha = 0f;
        foreach (var s in sparks) s.enabled = false;
        flash.color = Color.clear;
        Sfx.Play(SfxId.Dash, 0.7f);

        float t = 0f, spin = 0f;
        bool landed1 = false, landed2 = false;
        const float t1 = FlyTime, t2 = t1 + BounceTime, t3 = t2 + RollTime;
        while (t < t3)
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            t += dt;
            Vector2 p;
            float rate; // roll frames per second
            if (t < t1)
            {
                float k = t / t1;
                p = new Vector2(Mathf.Lerp(ThrowFrom.x, Land1.x, k), Mathf.Lerp(ThrowFrom.y, Land1.y, k * k)); // thrown: falls faster and faster
                rate = 24f;
            }
            else if (t < t2)
            {
                if (!landed1) { landed1 = true; Sfx.Play(SfxId.Parry, 0.5f); }
                float k = (t - t1) / BounceTime;
                p = new Vector2(Mathf.Lerp(Land1.x, Land2.x, k), FloorY + BounceHeight * 4f * k * (1f - k));
                rate = 18f;
            }
            else
            {
                if (!landed2) { landed2 = true; Sfx.Play(SfxId.Parry, 0.3f); }
                float k = (t - t2) / RollTime;
                p = new Vector2(Mathf.Lerp(Land2.x, 0f, 1f - (1f - k) * (1f - k)), FloorY); // rolls to a stop
                rate = Mathf.Lerp(16f, 6f, k);
            }
            spin += dt * rate;
            // Tilted frames only: dice_roll_3 is the upright die showing one pip (same pixels as dice_face_1), so cycling into it
            // flashed a "1" just before the real face. (DECISIONS 27 bug: the landed face must be the rolled face.)
            if (art) die.sprite = DiceArt.Roll[(int)spin % TumbleFrames];
            dieRt.anchoredPosition = Snap(p);
            float sinceLand = landed2 ? t - t2 : landed1 ? t - t1 : -1f;
            SetSquash(sinceLand);
            dim.color = new Color(Dim.r, Dim.g, Dim.b, Dim.a * Mathf.Clamp01(t / 0.1f));
            float height = Mathf.Max(0f, p.y - FloorY);
            shadowRt.anchoredPosition = Snap(new Vector2(p.x, FloorY - Scale));
            shadow.color = new Color(Ink.r, Ink.g, Ink.b, 0.5f * Mathf.Clamp01(1f - height / 200f));
            yield return null;
        }

        // Stop: face + flash + the number pops above the die.
        dieRt.anchoredPosition = new Vector2(0f, FloorY);
        dieRt.localScale = Vector3.one;
        if (art) die.sprite = DiceArt.Face(face);
        Sfx.Play(SfxId.HackOk, 0.8f);
        foreach (var s in sparks) s.enabled = true;
        float f = 0f;
        while (f < FlashTime)
        {
            f += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            Flash(f);
            yield return null;
        }
        tail = StartCoroutine(Tail(f));
    }

    IEnumerator Tail(float f)
    {
        float t = 0f;
        while (t < TailTime)
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            t += dt; f += dt;
            Flash(f);
            group.alpha = 1f - t / TailTime;
            yield return null;
        }
        root.SetActive(false);
        tail = null;
    }

    // Flash phase, f = seconds since the die stopped (keeps animating through the tail).
    void Flash(float f)
    {
        float k = Mathf.Clamp01(f / FlashTime);
        flash.color = new Color(1f, 1f, 1f, 0.3f * Mathf.Clamp01(1f - f / 0.12f));
        die.color = Color.Lerp(new Color(1f, 0.95f, 0.75f), Color.white, k); // warm flash settling to the plain face
        ring.anchoredPosition = Vector2.zero;
        ring.localScale = Vector3.one * Mathf.Lerp(1f, 1.8f, Mathf.Clamp01(f / 0.25f));
        ringGroup.alpha = 1f - Mathf.Clamp01(f / 0.25f);
        SetNumber(UiMotion.BackOut(f / 0.12f));
        float s = Mathf.Clamp01(f / 0.3f);
        for (int i = 0; i < sparks.Length; i++)
        {
            float dist = Die * 0.55f + 80f * (1f - (1f - s) * (1f - s));
            sparks[i].rectTransform.anchoredPosition = Snap(sparkDir[i] * dist);
            Color c = sparks[i].color; c.a = 1f - s;
            sparks[i].color = c;
        }
    }

    void SetNumber(float pop)
    {
        Vector2 at = new Vector2(0f, Die * 0.5f + 40f);
        number.rectTransform.anchoredPosition = at;
        numberShadow.rectTransform.anchoredPosition = at + new Vector2(Scale, -Scale);
        number.rectTransform.localScale = numberShadow.rectTransform.localScale = Vector3.one * pop;
    }

    void SetSquash(float sinceLand)
    {
        float k = sinceLand >= 0f && sinceLand < SquashTime ? 1f - sinceLand / SquashTime : 0f;
        dieRt.localScale = new Vector3(1f + Squash * k, 1f - Squash * k, 1f);
    }

    static Vector2 Snap(Vector2 p) => new Vector2(Mathf.Round(p.x / Scale) * Scale, Mathf.Round(p.y / Scale) * Scale);
}
