using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Title scene UI, built from code: key art backdrop, logo art (fallback: "NUMB" solid + "ER" dim), and the menu
// Start / Language / Controls (toggles the controls panel) / Quit (non-WebGL). Style: UiTheme.
public class TitleScreen : MonoBehaviour
{
    [SerializeField] string firstScene = "STG00"; // falls back to STG01 if STG00 is not in Build Settings

    // Layout at the 960x540 reference. Font sizes are multiples of 12 (Mulmaru native).
    const int LogoSize = 48;
    const float LogoY = 0f, MenuTopY = -40f;
    static readonly Color BackdropTint = new Color(0.4f, 0.4f, 0.45f, 1f);

    GameObject controls;

    void Start()
    {
        RespawnManager.ResetState(); // new game: no checkpoint record / pending respawn from the last run
        LuckyDice.SetUnlocked(true); // new game (new story 11-12): Yudo has his die from the first fight
        Stg01Tutorial.ResetRun(); // new game: the tutorial map teaches again
        BossFight.ResetRun(); // new game: no boss phase checkpoint
        LuckyDice.Charges = 0;
        Time.timeScale = 1f;
        GameFlow.InputLocked = false;
        var cam = Camera.main;
        if (cam != null) cam.backgroundColor = UiTheme.Current.background;
        if (cam != null && FindFirstObjectByType<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>(); // old Title scenes have none

        UiKit.EnsureEventSystem(out _);
        var t = UiTheme.Current;
        var canvas = UiKit.CreateCanvas("TitleCanvas", transform, 0);
        var center = new Vector2(0.5f, 0.5f);

        Sprite bgArt = DraftHud.Ui("title_bg"); // 960x540 key art without logo, dimmed so the logo and menu stay on top
        if (bgArt != null) UiKit.FilledImage(UiKit.Stretch("Backdrop", canvas.transform), BackdropTint).sprite = bgArt;

        Sprite logoArt = DraftHud.Ui("logo_number"); // 200x44 art at 2 ref px per art pixel; falls back to the TMP text
        if (logoArt != null)
        {
            var logoImg = UiKit.FilledImage(UiKit.Rect("Logo", canvas.transform, center, new Vector2(0f, LogoY + 10f), logoArt.rect.size * 2f), Color.white);
            logoImg.sprite = logoArt;
        }
        else
        {
            string logo = "NUMB<color=#" + ColorUtility.ToHtmlStringRGB(t.dim) + ">ER</color>";
            UiKit.Label("Logo", canvas.transform, center, new Vector2(0f, LogoY), new Vector2(800f, 60f), logo, logo, LogoSize, t.text);
        }

        var column = UiKit.Rect("Menu", canvas.transform, center, new Vector2(0f, MenuTopY), new Vector2(t.buttonSize2.x, 0f));
        int i = 0;
        var start = UiKit.AddButton(column, "Start", "시작", i++, () => SceneManager.LoadScene(Application.CanStreamedLevelBeLoaded(firstScene) ? firstScene : "STG01"));
        UiKit.AddButton(column, "Language: English", "언어: 한국어", i++, () => Loc.SetKorean(!Loc.Korean));
        UiKit.AddButton(column, "Controls", "조작법", i++, () => controls.SetActive(!controls.activeSelf));
        if (Application.platform != RuntimePlatform.WebGLPlayer)
            UiKit.AddButton(column, "Quit", "종료", i++, Quit);
        UiKit.VolumePair(canvas.transform, center, new Vector2(0f, MenuTopY - i * t.buttonStep - 10f)); // below the last button

        controls = UiKit.ControlsPanel(canvas.transform, center, new Vector2(0f, LogoY)); // covers the logo while open
        controls.SetActive(false);
        EventSystem.current.SetSelectedGameObject(start.gameObject);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (controls.activeSelf && kb != null && kb.escapeKey.wasPressedThisFrame) controls.SetActive(false);
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
