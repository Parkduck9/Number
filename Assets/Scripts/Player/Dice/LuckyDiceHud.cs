using UnityEngine;
using UnityEngine.UI;

// Lucky dice HUD (DECISIONS 18), bottom-left in the old shockwave icon slot: 24 px die at 2x (48 reference px) + 7 charge pips (lucky seven) (8 px at 2x).
// dice_off while uncharged / busy, dice_face_6 lit when a roll is ready (one talisman-yellow pulse when it becomes ready).
// Hidden until unlocked (STG-02 gift), like the shockwave HUD was.
public class LuckyDiceHud : MonoBehaviour
{
    const float Scale = 2f;
    const float Die = 24f * Scale, Pip = 8f * Scale;
    const float PulseTime = 0.35f;
    static readonly Vector2 DieCenter = new Vector2(16f + Die * 0.5f, 16f + Die * 0.5f);
    static readonly Color Talisman = new Color32(0xF2, 0xC9, 0x4C, 0xFF);
    static readonly Color OffTint = new Color(0.55f, 0.55f, 0.6f, 1f); // fallback when dice_off art is missing

    LuckyDice dice;
    GameObject root;
    Image die;
    readonly Image[] pips = new Image[LuckyDice.ChargesPerRoll];
    RectTransform pulse;
    CanvasGroup pulseGroup;
    float pulseT = PulseTime;
    int shownCharges = -1;
    bool shownReady, wasReady;

    public static LuckyDiceHud Create(LuckyDice owner)
    {
        if (!DiceArt.Ready) return null; // no art: no HUD (the controls panel still explains E)
        var hud = new GameObject("LuckyDiceHud").AddComponent<LuckyDiceHud>();
        hud.dice = owner;
        hud.Build();
        return hud;
    }

    void Build()
    {
        var canvas = UiKit.CreateCanvas("LuckyDiceCanvas", transform, 40); // the shockwave HUD's order: below the broadcast HUD (50)
        root = canvas.gameObject;
        var origin = Vector2.zero;
        die = UiKit.FilledImage(UiKit.Rect("Die", canvas.transform, origin, DieCenter, new Vector2(Die, Die)), Color.white);
        for (int i = 0; i < pips.Length; i++)
        {
            var pos = new Vector2(16f + Die + 8f + Pip * 0.5f + i * (Pip + 2f), DieCenter.y);
            pips[i] = UiKit.FilledImage(UiKit.Rect("Pip", canvas.transform, origin, pos, new Vector2(Pip, Pip)), Color.white);
        }

        pulse = UiKit.Rect("Pulse", canvas.transform, origin, DieCenter, new Vector2(Die, Die));
        pulseGroup = pulse.gameObject.AddComponent<CanvasGroup>();
        pulseGroup.alpha = 0f;
        pulseGroup.blocksRaycasts = false;
        Edge(new Vector2(0.5f, 1f), new Vector2(Die, Scale));
        Edge(new Vector2(0.5f, 0f), new Vector2(Die, Scale));
        Edge(new Vector2(0f, 0.5f), new Vector2(Scale, Die));
        Edge(new Vector2(1f, 0.5f), new Vector2(Scale, Die));
        root.SetActive(false);
    }

    void Edge(Vector2 anchor, Vector2 size) => UiKit.FilledImage(UiKit.Rect("Edge", pulse, anchor, Vector2.zero, size), Talisman);

    void Update()
    {
        if (dice == null) { Destroy(gameObject); return; }
        bool show = LuckyDice.Unlocked;
        if (root.activeSelf != show) root.SetActive(show);
        if (!show) { wasReady = false; return; }

        int charges = Mathf.Clamp(LuckyDice.Charges, 0, LuckyDice.ChargesPerRoll);
        bool ready = charges >= LuckyDice.ChargesPerRoll && !dice.Busy;
        if (ready && !wasReady) pulseT = 0f;
        wasReady = ready;

        if (charges != shownCharges || ready != shownReady)
        {
            shownCharges = charges;
            shownReady = ready;
            Sprite off = DiceArt.Off;
            die.sprite = ready || off == null ? DiceArt.Face(6) : off;
            die.color = ready || off != null ? Color.white : OffTint;
            for (int i = 0; i < pips.Length; i++)
            {
                Sprite s = i < charges ? DiceArt.PipFull : DiceArt.PipEmpty;
                pips[i].sprite = s;
                pips[i].enabled = s != null;
            }
        }

        if (pulseT < PulseTime)
        {
            pulseT += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(pulseT / PulseTime);
            pulse.localScale = Vector3.one * Mathf.Lerp(1f, 1.6f, k);
            pulseGroup.alpha = 1f - k;
        }
    }
}
