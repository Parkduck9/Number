using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Pause menu (Esc or P; browsers eat Esc in fullscreen). Bootstraps once at start and persists; it only opens in scenes with a player.
// Pausing = Time.timeScale 0 + GameFlow.InputLocked (freezes input, enemy AI, projectiles).
// Coexistence:
//  - Only opens when GameFlow.InputLocked is false, so it never fights dialogue/transitions.
//  - The parry hit-stop sets timeScale back to 1 when it ends; LateUpdate re-asserts 0 while paused.
//  - Any scene load (death reload, restart, to title) force-closes the menu.
[DefaultExecutionOrder(1000)] // after other scripts have used Esc this frame
public class PauseMenu : MonoBehaviour
{
    static PauseMenu instance;

    GameObject root;
    GameObject createdEventSystem;
    bool open;
    bool lockedAtFrameStart;
    PlayerController player;
    PlayerHealth health;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (instance != null) return;
        var go = new GameObject("PauseMenu");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<PauseMenu>();
    }

    void Awake()
    {
        root = Build();
        root.SetActive(false);
        SceneManager.sceneLoaded += OnSceneLoaded;
        FindPlayer();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this) instance = null;
        if (open) { Time.timeScale = 1f; GameFlow.InputLocked = false; }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (open) Close(unlock: false); // RespawnManager / scene start resets InputLocked itself
        FindPlayer();
    }

    void FindPlayer()
    {
        player = FindFirstObjectByType<PlayerController>();
        health = player != null ? player.GetComponent<PlayerHealth>() : null;
    }

    bool CanOpen => player != null && !GameFlow.InputLocked && !lockedAtFrameStart && !GameFlow.EscConsumed && (health == null || !health.IsDead);

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || !(kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame)) return;
        if (open) Close(unlock: true);
        else if (CanOpen) Open();
    }

    void LateUpdate()
    {
        if (open && Time.timeScale != 0f) Time.timeScale = 0f; // hit-stop end must not unpause
        lockedAtFrameStart = GameFlow.InputLocked; // a lock that ended this frame (e.g. Esc cancelled a choice) can't open the menu
    }

    void Open()
    {
        open = true;
        GameFlow.InputLocked = true;
        Time.timeScale = 0f;
        UiKit.EnsureEventSystem(out createdEventSystem);
        root.SetActive(true);
        EventSystem.current.SetSelectedGameObject(root.GetComponentInChildren<UnityEngine.UI.Button>().gameObject);
    }

    void Close(bool unlock)
    {
        open = false;
        EventSystem.current?.SetSelectedGameObject(null);
        root.SetActive(false);
        Time.timeScale = 1f;
        if (unlock) GameFlow.InputLocked = false;
        if (createdEventSystem != null) Destroy(createdEventSystem);
        createdEventSystem = null;
    }

    GameObject Build()
    {
        var t = UiTheme.Current;
        var canvas = UiKit.CreateCanvas("PauseCanvas", transform, 200);

        var dim = UiKit.Stretch("Dim", canvas.transform);
        UiKit.FilledImage(dim, t.dimmer, true); // also blocks clicks to anything below

        var panelSize = new Vector2(t.buttonSize2.x + 96f, 4 * t.buttonStep + 96f);
        var fill = UiKit.Frame("Panel", canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero, panelSize, out _);
        UiKit.Label("Title", fill, new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(panelSize.x, 36f), "PAUSED", "일시정지", t.buttonSize, t.text);

        var column = UiKit.Rect("Menu", fill, new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(t.buttonSize2.x, 0f));
        int i = 0;
        UiKit.AddButton(column, "Resume", "계속하기", i++, () => Close(unlock: true));
        UiKit.AddButton(column, "Restart from checkpoint", "체크포인트에서 재시작", i++, RestartFromCheckpoint);
        UiKit.AddButton(column, "Language: English", "언어: 한국어", i++, () => Loc.SetKorean(!Loc.Korean));
        UiKit.AddButton(column, "To title", "타이틀로", i++, ToTitle);
        return canvas.gameObject;
    }

    void RestartFromCheckpoint()
    {
        Close(unlock: true); // test scenes may have no RespawnManager to clear the lock
        RespawnManager.RestartFromCheckpoint();
    }

    void ToTitle()
    {
        Close(unlock: true);
        StageFlow.LoadTitle();
    }
}
