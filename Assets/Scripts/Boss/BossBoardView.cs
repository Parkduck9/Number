using TMPro;
using UnityEngine;

// Message line on the boss's big screen (the digit itself is BroadcastBoard's own label). Pure view: listens to BossFight + the board.
public class BossBoardView : MonoBehaviour
{
    [SerializeField] BossFight fight;
    [SerializeField] BroadcastBoard board;
    [SerializeField] TMP_Text message;

    bool unlocked;
    string shownEn, shownKo; // last message, re-shown on a language switch

    void OnEnable()
    {
        if (fight != null) { fight.WaveStarted += OnWaveStarted; fight.Unlocked += OnUnlocked; }
        if (board != null) board.NumberChanged += OnNumberChanged;
        Loc.Changed += Redraw;
        Show("EMBECTOR", "엠벡터");
    }

    void OnDisable()
    {
        if (fight != null) { fight.WaveStarted -= OnWaveStarted; fight.Unlocked -= OnUnlocked; }
        if (board != null) board.NumberChanged -= OnNumberChanged;
        Loc.Changed -= Redraw;
    }

    void OnWaveStarted(int i)
    {
        var w = fight.Waves[i];
        Show(w.announceEn, w.announceKo);
    }

    void OnNumberChanged(BroadcastBoard b, int n, int old)
    {
        if (unlocked || !b.IsHacked) return;
        Show("...cute.", "...귀엽군.");
    }

    void OnUnlocked()
    {
        unlocked = true;
        Show("STOP BUTTON UNLOCKED", "정지 버튼 열림");
    }

    void Show(string en, string ko) { shownEn = en; shownKo = ko; Redraw(); }
    void Redraw() { if (message != null) message.text = Loc.T(shownEn, shownKo); }
}
