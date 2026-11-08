using System;
using System.Collections.Generic;
using UnityEngine;

// STG-05 final boss (Embector's big screen). Three waves; the screen broadcasts a number each wave.
// Win = hack the board while the LAST wave is up -> the stop button unlocks (sticky).
// Dying reloads the scene (RespawnManager), so the fight simply restarts at wave 1.
// State machine = enum + switch (same style as TestEnemy). Views listen to the events below.
public class BossFight : MonoBehaviour
{
    [Serializable]
    public struct Wave
    {
        public int broadcastNumber;      // number the AI broadcasts during this wave
        public int citizens, bugs, androids;
        public string announceEn, announceKo;
    }

    [SerializeField] BroadcastBoard board;
    [SerializeField] GameObject stopButton; // inactive in the scene; activated on unlock
    [SerializeField] Transform[] spawnPoints;
    [SerializeField] GameObject citizenTemplate, bugTemplate, androidTemplate; // inactive scene objects, cloned per spawn

    [Header("Tunables (T)")]
    [SerializeField] float startDistance = 8f;       // (T) player this close (x) to the board -> fight starts
    [SerializeField] float announceSeconds = 2f;     // (T) wave title on screen before enemies appear
    [SerializeField] float spawnInterval = 0.6f;     // (T) seconds between spawns inside a wave
    [SerializeField] float spawnSafeRadius = 4f;     // (T) never spawn this close to the player
    [SerializeField] float betweenWavesSeconds = 1.5f; // (T) pause after a wave is cleared
    [SerializeField] Wave[] waves =                  // (T) wave table
    {
        new Wave { broadcastNumber = 3, citizens = 4, bugs = 0, androids = 0, announceEn = "WAVE 1 - LUCKY 3", announceKo = "1차 - 행운의 3" },
        new Wave { broadcastNumber = 7, citizens = 3, bugs = 2, androids = 0, announceEn = "WAVE 2 - LUCKY 7", announceKo = "2차 - 행운의 7" },
        new Wave { broadcastNumber = 0, citizens = 3, bugs = 0, androids = 2, announceEn = "WAVE 3 - LUCKY 0", announceKo = "3차 - 행운의 0" },
    };

    public event Action<int> WaveStarted;  // wave index
    public event Action<int> WaveCleared;  // wave index
    public event Action Unlocked;

    public Wave[] Waves => waves;

    enum State { Idle, Announce, Spawning, Fighting, Unlocked }
    State state;
    int waveIndex = -1;
    float elapsed;           // timer for the current state; only advances while input is not locked
    int spawned, spawnTotal;
    bool clearedFired;
    PlayerController player;
    readonly List<TestEnemy> alive = new List<TestEnemy>();
    bool wasHacked;          // board.IsHacked last frame
    float lastProgress;      // board.CounterHackProgress last frame (drops back to 0 on a re-hack)

    void Start() { player = FindFirstObjectByType<PlayerController>(); }

    void Update()
    {
        if (GameFlow.InputLocked) return;
        if (NewHackThisFrame() && waveIndex == waves.Length - 1 && state != State.Unlocked) Unlock();
        elapsed += Time.deltaTime;
        switch (state)
        {
            case State.Idle:
                if (player != null && board != null &&
                    Mathf.Abs(player.transform.position.x - board.transform.position.x) <= startDistance)
                    StartWave(0);
                break;
            case State.Announce:
                if (elapsed >= announceSeconds) Enter(State.Spawning);
                break;
            case State.Spawning:
                if (elapsed >= spawnInterval)
                {
                    elapsed = 0f;
                    SpawnNext();
                    if (spawned >= spawnTotal) Enter(State.Fighting);
                }
                break;
            case State.Fighting:
                if (clearedFired) { if (elapsed >= betweenWavesSeconds && waveIndex < waves.Length - 1) StartWave(waveIndex + 1); }
                else if (AllStunned())
                {
                    clearedFired = true;
                    elapsed = 0f;
                    WaveCleared?.Invoke(waveIndex);
                    // last wave cleared: stay here, a hack still wins
                }
                break;
        }
    }

    void Enter(State s) { state = s; elapsed = 0f; }

    void StartWave(int i)
    {
        waveIndex = i;
        Wave w = waves[i];
        alive.Clear();
        spawned = 0;
        spawnTotal = w.androids + w.bugs + w.citizens;
        clearedFired = false;
        board.SetAiNumber(w.broadcastNumber);
        Enter(State.Announce);
        WaveStarted?.Invoke(i);
    }

    // Spawn order: androids, bugs, citizens; alternating spawn points, skipping one the player stands next to.
    void SpawnNext()
    {
        Wave w = waves[waveIndex];
        GameObject template = spawned < w.androids ? androidTemplate
            : spawned < w.androids + w.bugs ? bugTemplate : citizenTemplate;
        Transform point = spawnPoints[spawned % spawnPoints.Length];
        if (player != null && spawnPoints.Length > 1 &&
            Mathf.Abs(player.transform.position.x - point.position.x) < spawnSafeRadius) // x only: spawn y is high above the floor
            point = spawnPoints[(spawned + 1) % spawnPoints.Length];
        spawned++;
        if (template == null) return;
        GameObject clone = Instantiate(template, point.position, Quaternion.identity);
        clone.SetActive(true);
        var enemy = clone.GetComponent<TestEnemy>();
        if (enemy != null) alive.Add(enemy);
    }

    bool AllStunned()
    {
        foreach (TestEnemy e in alive)
            if (e != null && e.Current != TestEnemy.State.Stunned) return false;
        return true;
    }

    // Win hack: only a hack STARTED during the last wave counts (a wave-2 hack still on screen does not).
    // Polled instead of NumberChanged: hacking the number already shown (e.g. 0 in wave 3) raises no event.
    bool NewHackThisFrame()
    {
        if (board == null) return false;
        bool hacked = board.IsHacked;
        float progress = board.CounterHackProgress;
        bool isNew = hacked && (!wasHacked || progress < lastProgress);
        wasHacked = hacked;
        lastProgress = progress;
        return isNew;
    }

    void Unlock()
    {
        Enter(State.Unlocked);
        if (stopButton != null) stopButton.SetActive(true); // sticky: State.Unlocked has no way out
        Unlocked?.Invoke();
    }
}
