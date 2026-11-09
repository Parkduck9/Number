using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// DECISIONS 15 (R2-03): small arrows at the screen edge pointing at the off-screen enemies / unbroken spawner that still block the exit.
// Shown while fewer than ShowBelow remain, and for FlashTime after the goal refuses the player. Hidden while paused, in dialogue or in a fade.
// Counts exactly what StageClear counts (the goal's camera segment). Code-drawn: a tiny triangle texture, no art.
public class EnemyPointer : MonoBehaviour
{
    const int ShowBelow = 5, MaxArrows = 5;
    const float FlashTime = 3f, Margin = 24f, ArrowSize = 27f, ScanEvery = 0.2f;
    static readonly Color EnemyColor = new Color32(0xD7, 0x7E, 0x54, 0xFF);   // palette lantern orange
    static readonly Color SpawnerColor = new Color32(0x42, 0xA9, 0xBD, 0xFF); // palette dim cyan (distinct from enemies)

    static EnemyPointer instance;
    public static void Flash() { if (instance != null) instance.flashUntil = Time.unscaledTime + FlashTime; }

    // One pointer per scene that has an exit (Goal) and camera segments: stages. Title / STG-05 have none.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Hook()
    {
        SceneManager.sceneLoaded -= OnLoaded; // AfterSceneLoad runs once per app start: later scene loads need the event
        SceneManager.sceneLoaded += OnLoaded;
        OnLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void OnLoaded(Scene scene, LoadSceneMode mode)
    {
        if (FindFirstObjectByType<SegmentCameraBounds>() == null) return;
        new GameObject("EnemyPointer").AddComponent<EnemyPointer>();
    }

    struct Target { public Vector3 pos; public bool spawner; public float dist; }

    readonly List<Target> targets = new List<Target>();
    readonly List<Image> arrows = new List<Image>();
    RectTransform canvasRect;
    Transform goal;
    Camera cam;
    Texture2D tex;
    Sprite sprite;
    bool ownsSprite; // true = the code-made fallback (destroyed with us); false = a loaded resource
    PlayerController player;
    float flashUntil = -1f, nextScan;
    int remaining;

    void Awake()
    {
        instance = this;
        foreach (var lt in FindObjectsByType<LevelTransition>(FindObjectsSortMode.None))
            if (lt.IsGoal) { goal = lt.transform; break; }
        if (goal == null) { Destroy(gameObject); return; }
        player = FindFirstObjectByType<PlayerController>();

        var canvas = UiKit.CreateCanvas("EnemyPointerCanvas", transform, 90);
        canvasRect = canvas.GetComponent<RectTransform>();
        Sprite art = DraftHud.Ui("enemy_pointer"); // white 9x9 stencil (tinted below); fallback = code-made triangle
        if (art != null) sprite = art;
        else
        {
            tex = new Texture2D(9, 9, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[81];
            for (int y = 0; y < 9; y++)                       // triangle pointing right (+x): column x is filled while |y-4| <= (8-x)/2
                for (int x = 0; x < 9; x++)
                    px[y * 9 + x] = Mathf.Abs(y - 4) * 2 <= 8 - x ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            tex.SetPixels32(px); tex.Apply();
            sprite = Sprite.Create(tex, new Rect(0, 0, 9, 9), new Vector2(0.5f, 0.5f), 9f);
            ownsSprite = true;
        }
        for (int i = 0; i < MaxArrows; i++)
        {
            var rt = UiKit.Rect("Arrow", canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(ArrowSize, ArrowSize));
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            img.enabled = false;
            arrows.Add(img);
        }
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
        if (ownsSprite && sprite != null) Destroy(sprite);
        if (tex != null) Destroy(tex);
    }

    void Update()
    {
        if (cam == null) cam = Camera.main;
        bool hide = cam == null || PauseMenu.IsOpen || GameFlow.InputLocked || (player != null && GameFlow.IsPlayerDead(player));
        if (Time.unscaledTime >= nextScan) { nextScan = Time.unscaledTime + ScanEvery; Scan(); }
        bool show = !hide && remaining > 0 && (remaining < ShowBelow || Time.unscaledTime < flashUntil);
        int used = 0;
        if (show) used = Place();
        for (int i = used; i < arrows.Count; i++) arrows[i].enabled = false;
    }

    void Scan()
    {
        targets.Clear();
        StageClear.SegmentAt(goal.position.x, out float xMin, out float xMax);
        Vector3 from = player != null ? player.transform.position : goal.position;
        foreach (var e in FindObjectsByType<TestEnemy>(FindObjectsSortMode.None))
        {
            float x = e.transform.position.x;
            if (e.Current != TestEnemy.State.Stunned && x >= xMin && x <= xMax)
                targets.Add(new Target { pos = e.transform.position, dist = (e.transform.position - from).sqrMagnitude });
        }
        foreach (var s in StageClear.Spawners)
        {
            if (s == null || s.Broken) continue;
            float x = s.transform.position.x;
            if (x >= xMin && x <= xMax)
                targets.Add(new Target { pos = s.transform.position, spawner = true, dist = (s.transform.position - from).sqrMagnitude });
        }
        remaining = targets.Count;
        targets.Sort((a, b) => a.dist.CompareTo(b.dist));
    }

    // Arrow for each off-screen target (nearest first, MaxArrows total). Returns the number of arrows used.
    int Place()
    {
        Vector2 half = canvasRect.rect.size * 0.5f;
        int used = 0;
        for (int i = 0; i < targets.Count && used < MaxArrows; i++)
        {
            Vector3 vp = cam.WorldToViewportPoint(targets[i].pos);
            if (vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f) continue; // on screen
            Vector2 dir = new Vector2((vp.x - 0.5f) * half.x * 2f, (vp.y - 0.5f) * half.y * 2f);
            if (vp.z < 0f) dir = -dir;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
            float k = Mathf.Min((half.x - Margin) / Mathf.Max(0.0001f, Mathf.Abs(dir.x)), (half.y - Margin) / Mathf.Max(0.0001f, Mathf.Abs(dir.y)));
            var rt = (RectTransform)arrows[used].transform;
            rt.anchoredPosition = dir * k;
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            arrows[used].color = targets[i].spawner ? SpawnerColor : EnemyColor;
            arrows[used].enabled = true;
            used++;
        }
        return used;
    }
}
