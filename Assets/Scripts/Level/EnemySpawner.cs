using System;
using System.Collections.Generic;
using UnityEngine;

// DECISIONS 15: breakable spawn device. While it stands, a watched enemy that dies comes back at its original spot after
// respawnDelay (a fresh copy of the enemy as it was at scene start). hitsToBreak player hits break it: no more respawns,
// enemies already alive stay. StageClear (StageGoal / LevelTransition Goal) needs every spawner Broken.
// Hit detection: enemies are found through TestEnemy, so the spawner reads the player's Attack state itself (no PlayerController change).
[DefaultExecutionOrder(-1000)] // Awake before the enemies' own Awake: templates are copied while still pristine
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] int hitsToBreak = 2;
    [SerializeField] float respawnDelay = 2.5f;
    [SerializeField] List<TestEnemy> watched = new List<TestEnemy>(); // empty = every TestEnemy in the scene at Start
    [SerializeField] Vector2 size = new Vector2(1.6f, 1.2f);          // collider / sprite size in units

    public static event Action<EnemySpawner> BrokenEvent;
    public bool Broken { get; private set; }

    class Entry { public GameObject template, current; public Vector3 pos; public Quaternion rot; public Transform parent; public float diedAt = -1f; public TestEnemy spawned; public float graceUntil; }
    readonly List<Entry> entries = new List<Entry>();

    // Player's in-place swing box (PlayerController attackOffset 0.9 / attackSize 1.6 x 1.4) plus a little slack for the swing start.
    static readonly Vector2 SwingOffset = new Vector2(0.9f, 0f), SwingSize = new Vector2(2.1f, 1.8f);
    const float SafeRadius = 4f, SpawnGrace = 0.6f; // no respawn while the player is this close; a fresh enemy waits this long before acting
    const float ShakeAmp = 0.12f, ShakeTime = 0.15f;

    SpriteRenderer sr;
    Sprite[] art; // intact / damaged / broken (52x48, centre pivot); null = placeholder look
    // StageBuildKit puts the device 0.8 above the floor; the 48 px (1.5 u) art is lowered so its base stands on the floor.
    const float ArtY = 0.75f - 0.8f;
    BoxCollider2D box;
    PlayerController player;
    PlayerController.State prevState;
    int hits;
    float flashUntil, shakeUntil;
    Vector3 basePos;

    void Awake()
    {
        box = GetComponent<BoxCollider2D>();
        if (box == null) box = gameObject.AddComponent<BoxCollider2D>();
        box.isTrigger = true; // not a body: enemies and the player pass through it
        box.size = size;
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr == null)
        {
            sr = new GameObject("Look").AddComponent<SpriteRenderer>();
            sr.transform.SetParent(transform, false);
            sr.sortingLayerName = "World"; // behind the characters, like the other level props
            sr.sortingOrder = 5;
            art = new[] { Resources.Load<Sprite>("DraftUi/spawner_intact"), Resources.Load<Sprite>("DraftUi/spawner_damaged"), Resources.Load<Sprite>("DraftUi/spawner_broken") };
            if (art[0] != null)
            {
                sr.sprite = art[0];
                sr.transform.localPosition = new Vector3(0f, ArtY, 0f);
            }
            else
            {
                art = null;
                sr.sprite = Resources.Load<Sprite>("DraftHud/holo_window_red"); // placeholder look: 20x16 holo window, tinted and scaled up
                sr.transform.localScale = new Vector3(size.x / 0.625f, size.y / 0.5f, 1f); // sprite is 20x16 px at PPU 32
            }
        }
        basePos = transform.localPosition;
        BuildTemplates();
    }

    void OnEnable() => StageClear.Spawners.Add(this);
    void OnDisable() => StageClear.Spawners.Remove(this);

    void Start() => player = FindFirstObjectByType<PlayerController>();

    // Each watched enemy is copied while deactivated, so the template holds only scene-serialized state (no runtime holograms / swarm yet).
    void BuildTemplates()
    {
        if (watched.Count == 0)
        {
            StageClear.SegmentAt(transform.position.x, out float xMin, out float xMax); // default: this device's own segment
            foreach (var e in FindObjectsByType<TestEnemy>(FindObjectsSortMode.None))
                if (e.transform.position.x >= xMin && e.transform.position.x <= xMax && !StageClear.IsWatched(e)) watched.Add(e); // a second spawner with an empty list takes only what is still free
        }
        foreach (var e in watched)
        {
            if (e == null || !e.gameObject.activeSelf) continue; // placed inactive on purpose: not ours to respawn
            var go = e.gameObject;
            go.SetActive(false);
            var t = Instantiate(go, go.transform.parent);
            go.SetActive(true);
            t.name = go.name + " (spawn template)";
            entries.Add(new Entry { template = t, current = go, pos = go.transform.position, rot = go.transform.rotation, parent = go.transform.parent });
        }
    }

    public bool Watches(TestEnemy enemy)
    {
        foreach (var e in entries) if (e.current != null && e.current.GetComponent<TestEnemy>() == enemy) return true;
        return false;
    }

    void Update()
    {
        UpdateLook();
        if (GameFlow.EnemiesHeld) { HoldTimers(); if (!Broken) UpdateHit(); return; } // dice hold: no respawn / grace progress
        UpdateGrace();
        if (Broken) return;
        UpdateHit();
        UpdateRespawns();
    }

    void HoldTimers()
    {
        foreach (var e in entries)
        {
            if (e.diedAt >= 0f) e.diedAt += Time.deltaTime;
            if (e.spawned != null) e.graceUntil += Time.deltaTime;
        }
    }

    // Respawned enemies act again after SpawnGrace (also when the device breaks during the grace).
    void UpdateGrace()
    {
        foreach (var e in entries)
        {
            if (e.spawned == null || Time.time < e.graceUntil) continue;
            if (e.spawned.Current == TestEnemy.State.Stunned) // hit during the grace: stay dead (Start would revive it)
            {
                SpriteFx.Play("hit-spark", (Vector2)e.spawned.transform.position - new Vector2(12f, 16f) / SpriteFx.Ppu);
                e.spawned.gameObject.SetActive(false);
            }
            else e.spawned.enabled = true;
            e.spawned = null;
        }
    }

    void UpdateHit()
    {
        if (player == null) return;
        var st = player.CurrentState;
        if (st == PlayerController.State.Attack && prevState != PlayerController.State.Attack) TryHit();
        prevState = st;
    }

    void TryHit()
    {
        Vector2 c = (Vector2)player.transform.position + new Vector2(player.Facing * SwingOffset.x, SwingOffset.y);
        if (!box.bounds.Intersects(new Bounds(c, SwingSize))) return;
        hits++;
        flashUntil = Time.time + 0.1f;
        shakeUntil = Time.time + ShakeTime;
        Spark(c);
        Sfx.Play(SfxId.Hit);
        Juice.Shake(Juice.ShakeHitAmp, Juice.ShakeHitTime);
        if (hits >= hitsToBreak) Break();
    }

    void Break()
    {
        Broken = true;
        for (int i = 0; i < Juice.KillSparks; i++)
            Spark((Vector2)transform.position + new Vector2(UnityEngine.Random.Range(-size.x, size.x) * 0.5f, UnityEngine.Random.Range(-size.y, size.y) * 0.5f));
        MomentFx.SpawnerBreak(transform.position);
        Sfx.Play(SfxId.Stunned);
        Juice.Shake(Juice.ShakeKillAmp, Juice.ShakeKillTime);
        BrokenEvent?.Invoke(this);
    }

    static void Spark(Vector2 p) => SpriteFx.Play("hit-spark", p - new Vector2(12f, 16f) / SpriteFx.Ppu, UnityEngine.Random.value < 0.5f);

    void UpdateRespawns()
    {
        foreach (var e in entries)
        {
            if (e.current != null && e.current.activeSelf) continue; // alive (or still falling over: inactive only after the vanish delay)
            if (e.diedAt < 0f) { e.diedAt = Time.time; continue; }
            if (Time.time - e.diedAt < respawnDelay) continue;
            if (player != null && Vector2.Distance(player.transform.position, e.pos) < SafeRadius) continue; // wait until the player steps away
            if (e.current != null) Destroy(e.current); // the dead body (inactive) would otherwise pile up
            e.current = Instantiate(e.template, e.pos, e.rot, e.parent);
            e.current.name = e.template.name.Replace(" (spawn template)", "");
            e.current.SetActive(true);
            e.spawned = e.current.GetComponent<TestEnemy>();
            if (e.spawned != null) { e.spawned.enabled = false; e.graceUntil = Time.time + SpawnGrace; } // frozen AI: cannot attack for a moment
            e.diedAt = -1f;
            Collider2D body = e.current.GetComponent<Collider2D>();
            if (MomentFx.EnemyWarpIn(new Vector2(e.pos.x, body != null ? body.bounds.min.y : e.pos.y - 1f)) == null) // warp-in column stands on the enemy's feet
                SpriteFx.Play("dash-burst", (Vector2)e.pos - new Vector2(0f, 1f)); // sheet not in yet: old spawn puff
        }
    }

    void UpdateLook()
    {
        if (sr == null) return;
        Color tint = Color.white;
        if (art != null) sr.sprite = art[Broken ? 2 : hits > 0 ? 1 : 0]; // the art shows the damage itself
        else tint = Broken ? new Color(0.35f, 0.35f, 0.4f) : hits > 0 ? new Color(1f, 0.6f, 0.6f) : Color.white;
        SpriteFlash.Apply(sr, tint, Time.time < flashUntil, Color.white, Color.white, Broken && art == null ? 0.6f : 0f);
        transform.localPosition = basePos + (Time.time < shakeUntil ? (Vector3)(UnityEngine.Random.insideUnitCircle * ShakeAmp) : Vector3.zero);
    }
}
