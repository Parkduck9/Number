using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Hook for anything that reacts to the zone's broadcast number (enemies later: NUM-3 / NUM-7 / NUM-0 rules are still [proposal]).
// Implement this and call board.AddListener(this). No rule is implemented here on purpose.
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

    [Header("Counter-hack hold time (Q-09 UNDECIDED - provisional GDD proposal values, tune after playtest)")]
    // TODO(Q-09): user decides the real hold times. GDD proposal: default 8s, 0 shorter (5s).
    [SerializeField] float holdSeconds = 8f;
    [SerializeField] float holdSecondsForZero = 5f;

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
