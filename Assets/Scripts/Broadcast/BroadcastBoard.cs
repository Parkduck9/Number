using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Optional hook for anything that reacts to the zone's broadcast number.
// Implement this and call board.AddListener(this). The enemy rules (NUM-3 / NUM-7 / NUM-0) live in TestEnemy + BroadcastRules and subscribe to NumberChanged instead.
public interface IBroadcastListener
{
    void OnBroadcastChanged(BroadcastBoard board, int newNumber, int oldNumber);
}

// Zone billboard: holds the zone's current broadcast number (GDD ch.5 2, ch.6 hacking rules).
// - AI number = what Embector broadcasts. A hack replaces it; the AI's counter-hack restores it when the hold time is over.
// - Game time (Time.time) is used, so it matches enemy timers.
public class BroadcastBoard : MonoBehaviour
{
    public static readonly List<BroadcastBoard> All = new List<BroadcastBoard>();

    [SerializeField, Range(0, 9)] int aiNumber = 3;
    [SerializeField] TMP_Text label; // optional world-space text on the billboard

    [Tooltip("World x range (min,max) this board's broadcast covers. (0,0) = whole scene.")]
    [SerializeField] Vector2 zoneX;

    [Header("Counter-hack hold time (confirmed DECISIONS 3-A: 8s, 0 shorter 5s; tune after playtest)")]
    [SerializeField] float holdSeconds = 8f;
    [SerializeField] float holdSecondsForZero = 5f;

    // The board whose zone contains pos.x (nearest if several), or null when pos is outside every zone.
    public static BroadcastBoard At(Vector2 pos)
    {
        BroadcastBoard best = null;
        float bestD = float.MaxValue;
        foreach (BroadcastBoard b in All)
        {
            bool whole = b.zoneX.x == 0f && b.zoneX.y == 0f;
            if (!whole && (pos.x < b.zoneX.x || pos.x > b.zoneX.y)) continue;
            float d = Vector2.Distance(pos, b.transform.position);
            if (d < bestD) { bestD = d; best = b; }
        }
        return best;
    }

    public int Number { get; private set; }
    public int AiNumber => aiNumber;
    public bool IsHacked { get; private set; }
    // 0 right after the hack -> 1 when the AI takes the number back. 0 when not hacked.
    public float CounterHackProgress => IsHacked ? Mathf.Clamp01((Time.time - hackedAt) / hackDuration) : 0f;

    public event Action<BroadcastBoard, int, int> NumberChanged; // board, new, old
    readonly List<IBroadcastListener> listeners = new List<IBroadcastListener>();

    float hackedAt, hackDuration;

    void Awake() { Number = aiNumber; Refresh(); }
    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Update()
    {
        if (IsHacked && Time.time - hackedAt >= hackDuration)
        {
            IsHacked = false; // [confirmed] number returns to the AI's number
            SetNumber(aiNumber);
        }
    }

    public void AddListener(IBroadcastListener l) { if (!listeners.Contains(l)) listeners.Add(l); }
    public void RemoveListener(IBroadcastListener l) { listeners.Remove(l); }

    // Player hack: show `n` until the AI's counter-hack finishes. Hacking again restarts the counter-hack.
    public void Hack(int n)
    {
        n = Mathf.Clamp(n, 0, 9);
        IsHacked = true;
        hackedAt = Time.time;
        hackDuration = n == 0 ? holdSecondsForZero : holdSeconds;
        SetNumber(n);
    }

    // For stage scripts: the AI broadcasts a new number (only applies when not hacked).
    public void SetAiNumber(int n)
    {
        aiNumber = Mathf.Clamp(n, 0, 9);
        if (!IsHacked) SetNumber(aiNumber);
    }

    void SetNumber(int n)
    {
        int old = Number;
        Number = n;
        Refresh();
        if (n == old) return;
        NumberChanged?.Invoke(this, n, old);
        for (int i = listeners.Count - 1; i >= 0; i--)
            if (i < listeners.Count) listeners[i].OnBroadcastChanged(this, n, old); // a listener may remove several listeners
    }

    void Refresh()
    {
        if (label != null) label.text = Number.ToString();
    }
}
