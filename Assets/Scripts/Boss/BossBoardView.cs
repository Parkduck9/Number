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
    SpriteRenderer portrait; // DraftHud: Embector on the big screen, angry while a wave is up. No art = no portrait.
    Sprite idle, angry;

    void Awake()
    {
        idle = DraftHud.Ui("boss_embector_idle");
        angry = DraftHud.Ui("boss_embector_angry");
        if (idle == null || angry == null) return;
        var go = new GameObject("Embector (draft)");
        go.transform.SetParent(board != null ? board.transform : transform, false);
        go.transform.localPosition = new Vector3(-4.2f, 7.5f, 0f); // upper-left of the screen (board root is at y 3.5, the screen at y 11)
        portrait = go.AddComponent<SpriteRenderer>();
        portrait.sprite = idle;
        portrait.sortingOrder = 5; // above the screen panel (0), below the number (10)
    }

    void SetAngry(bool on) { if (portrait != null) portrait.sprite = on ? angry : idle; }

    void OnEnable()
    {
        if (fight != null) { fight.WaveStarted += OnWaveStarted; fight.WaveCleared += OnWaveCleared; fight.Unlocked += OnUnlocked; }
        if (board != null) board.NumberChanged += OnNumberChanged;
        Loc.Changed += Redraw;
        Show("EMBECTOR", "엠벡터");
    }

    void OnDisable()
    {
        if (fight != null) { fight.WaveStarted -= OnWaveStarted; fight.WaveCleared -= OnWaveCleared; fight.Unlocked -= OnUnlocked; }
        if (board != null) board.NumberChanged -= OnNumberChanged;
        Loc.Changed -= Redraw;
    }

    void OnWaveCleared(int i) { SetAngry(false); }

    void OnWaveStarted(int i)
    {
        SetAngry(true);
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
        SetAngry(false);
        Show("STOP BUTTON UNLOCKED", "정지 버튼 열림");
    }

    void Show(string en, string ko) { shownEn = en; shownKo = ko; Redraw(); }
    void Redraw() { if (message != null) message.text = Loc.T(shownEn, shownKo); }
}
