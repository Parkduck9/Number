using System.Linq;
using UnityEngine;

// DRAFT HUD / UI art (Assets/Resources/DraftHud, DraftUi), loaded once. Every getter returns null when the art is missing,
// and the callers then keep their TMP text / placeholder. Switched off together with DraftSprites.Use. Doc: docs/design/impl/draft-sprites.md.
public static class DraftHud
{
    static Sprite[] faces, broadcastAi, broadcastHacked, arcsAi, arcsHacked, holoGold, holoRed;
    static Sprite winGold, winRed;
    static bool loaded;

    public static Sprite[] Faces { get { Init(); return faces; } }                 // 6 steps: hits taken 0..5
    public static Sprite[] BroadcastDigits(bool hacked) { Init(); return hacked ? broadcastHacked : broadcastAi; } // 0..9
    public static Sprite[] BroadcastArcs(bool hacked) { Init(); return hacked ? arcsHacked : arcsAi; }             // [0] left, [1] right
    public static Sprite[] HoloDigits(bool gold) { Init(); return gold ? holoGold : holoRed; }                      // 0..9
    public static Sprite HoloWindow(bool gold) { Init(); return gold ? winGold : winRed; }
    public static bool HoloReady { get { Init(); return holoGold != null && holoRed != null && winGold != null && winRed != null; } }

    public static Sprite Ui(string name) => DraftSprites.Use ? Resources.Load<Sprite>("DraftUi/" + name) : null;

    static void Init()
    {
        if (loaded) return;
        loaded = true;
        if (!DraftSprites.Use) return;
        var f = new Sprite[6];
        for (int i = 0; i < 6; i++) f[i] = Resources.Load<Sprite>("DraftHud/face_" + i);
        faces = f.All(s => s != null) ? f : null;
        broadcastAi = Strip("broadcast_digits_magenta", 10);
        broadcastHacked = Strip("broadcast_digits_cyan", 10);
        arcsAi = Strip("broadcast_arcs_magenta", 2);
        arcsHacked = Strip("broadcast_arcs_cyan", 2);
        holoGold = Strip("holo_digits_gold", 10);
        holoRed = Strip("holo_digits_red", 10);
        winGold = Resources.Load<Sprite>("DraftHud/holo_window_gold");
        winRed = Resources.Load<Sprite>("DraftHud/holo_window_red");
    }

    static Sprite[] Strip(string name, int count)
    {
        Sprite[] s = Resources.LoadAll<Sprite>("DraftHud/" + name).OrderBy(x => x.name).ToArray();
        return s.Length == count ? s : null;
    }
}
