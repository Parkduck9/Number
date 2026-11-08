using TMPro;
using UnityEngine;

// Message line on the boss's big screen (the digit itself is BroadcastBoard's own label). Pure view: listens to BossFight + the board.
public class BossBoardView : MonoBehaviour
{
    [SerializeField] BossFight fight;
    [SerializeField] BroadcastBoard board;
    [SerializeField] TMP_Text message;

    bool unlocked;

    void OnEnable()
    {
        if (fight != null) { fight.WaveStarted += OnWaveStarted; fight.Unlocked += OnUnlocked; }
        if (board != null) board.NumberChanged += OnNumberChanged;
        Show(Loc.T("EMBECTOR", "엠벡터"));
    }

    void OnDisable()
    {
        if (fight != null) { fight.WaveStarted -= OnWaveStarted; fight.Unlocked -= OnUnlocked; }
        if (board != null) board.NumberChanged -= OnNumberChanged;
    }

    void OnWaveStarted(int i)
    {
        var w = fight.Waves[i];
        Show(Loc.T(w.announceEn, w.announceKo));
    }

    void OnNumberChanged(BroadcastBoard b, int n, int old)
    {
        if (unlocked || !b.IsHacked) return;
        Show(Loc.T("...cute.", "...귀엽군."));
    }

    void OnUnlocked()
    {
        unlocked = true;
        Show(Loc.T("STOP BUTTON UNLOCKED", "정지 버튼 열림"));
    }

    void Show(string s) { if (message != null) message.text = s; }
}
