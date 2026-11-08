using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// Title scene UI, built from code. Style: UiTheme. Quit is hidden on WebGL.
public class TitleScreen : MonoBehaviour
{
    [SerializeField] string firstScene = "STG00"; // falls back to STG01 if STG00 is not in Build Settings

    void Start()
    {
        RespawnManager.ResetState(); // new game: no checkpoint record / pending respawn from the last run
        Shockwave.SetUnlocked(false); // new game: the Luddite upgrade must not leak in
        Time.timeScale = 1f;
        GameFlow.InputLocked = false;
        var cam = Camera.main;
        if (cam != null) cam.backgroundColor = UiTheme.Current.background;
        if (cam != null && FindFirstObjectByType<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>(); // old Title scenes have none

        UiKit.EnsureEventSystem(out _);
        var t = UiTheme.Current;
        var canvas = UiKit.CreateCanvas("TitleCanvas", transform, 0);

        UiKit.Label("Title", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(800f, 120f), "NUMBER", "NUMBER", t.titleSize, t.text);

        var column = UiKit.Rect("Menu", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(t.buttonSize2.x, 0f));
        int i = 0;
        var start = UiKit.AddButton(column, "Start", "시작", i++, () => SceneManager.LoadScene(Application.CanStreamedLevelBeLoaded(firstScene) ? firstScene : "STG01"));
        UiKit.AddButton(column, "Language: English", "언어: 한국어", i++, () => Loc.SetKorean(!Loc.Korean));
        if (Application.platform != RuntimePlatform.WebGLPlayer)
            UiKit.AddButton(column, "Quit", "종료", i++, Quit);

        UiKit.ControlsPanel(canvas.transform, new Vector2(0.5f, 0f), new Vector2(0f, 70f));
        EventSystem.current.SetSelectedGameObject(start.gameObject);
    }

    static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
