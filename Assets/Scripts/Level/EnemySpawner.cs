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

    class Entry { public GameObject template, current; public Vector3 pos; public Quaternion rot; public Transform parent; public float diedAt = -1f; }
    readonly List<Entry> entries = new List<Entry>();

    // Player's in-place swing box (PlayerController attackOffset 0.9 / attackSize 1.6 x 1.4) plus a little slack for the swing start.
    static readonly Vector2 SwingOffset = new Vector2(0.9f, 0f), SwingSize = new Vector2(2.1f, 1.8f);
    const float ShakeAmp = 0.12f, ShakeTime = 0.15f;

    SpriteRenderer sr;
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
            sr.sprite = Resources.Load<Sprite>("DraftHud/holo_window_red"); // placeholder look: 20x16 holo window, tinted and scaled up
            sr.sortingOrder = 5;
            sr.transform.localScale = new Vector3(size.x / 0.625f, size.y / 0.5f, 1f); // sprite is 20x16 px at PPU 32
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
        if (Broken) return;
        UpdateHit();
        UpdateRespawns();
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
            if (e.current != null) Destroy(e.current); // the dead body (inactive) would otherwise pile up
            e.current = Instantiate(e.template, e.pos, e.rot, e.parent);
            e.current.name = e.template.name.Replace(" (spawn template)", "");
            e.current.SetActive(true);
            e.diedAt = -1f;
            SpriteFx.Play("dash-burst", (Vector2)e.pos - new Vector2(0f, 1f)); // spawn puff (reuses the dash burst sheet)
        }
    }

    void UpdateLook()
    {
        if (sr == null) return;
        Color tint = Broken ? new Color(0.35f, 0.35f, 0.4f) : hits > 0 ? new Color(1f, 0.6f, 0.6f) : Color.white;
        SpriteFlash.Apply(sr, tint, Time.time < flashUntil, Color.white, Color.white, Broken ? 0.6f : 0f);
        transform.localPosition = basePos + (Time.time < shakeUntil ? (Vector3)(UnityEngine.Random.insideUnitCircle * ShakeAmp) : Vector3.zero);
    }
}
