using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Title scene UI, built from code. One flickering hologram number is the only focal point; the logo reads
// "NUMB" solid + "ER" dim (numb-er). Menu: Start / Language / Controls (toggles the controls panel) / Quit (non-WebGL).
// Style: UiTheme. The number color is the broadcast gold (BroadcastHackUi), not a theme value.
public class TitleScreen : MonoBehaviour
{
    [SerializeField] string firstScene = "STG00"; // falls back to STG01 if STG00 is not in Build Settings

    // Layout at the 960x540 reference. Font sizes are multiples of 12 (Mulmaru native).
    // Mulmaru's digit 0 has a diagonal notch that reads as a broken 8 at this size; its capital O is the same
    // plain oval (same width, same 2 px stroke), so the hologram "zero" uses that glyph.
    const string Number = "O";
    const int NumberSize = 192, LogoSize = 48;
    const float NumberY = 150f, LogoY = 0f, MenuTopY = -40f;
    static readonly Vector2 WindowSize = new Vector2(160f, 192f); // translucent hologram window behind the digit
    static readonly Color32 Gold = new Color32(0xFF, 0xD2, 0x4A, 0xFF);
    const byte WindowBorderAlpha = 0x50, WindowFillAlpha = 0x18;

    GameObject controls;

    void Start()
    {
        RespawnManager.ResetState(); // new game: no checkpoint record / pending respawn from the last run
        LuckyDice.SetUnlocked(false); // new game: the Luddite upgrade must not leak in
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

        // Hologram: gold window (border + fill) with the digit on top; all three flicker together.
        var window = UiKit.Rect("Hologram", canvas.transform, center, new Vector2(0f, NumberY), WindowSize);
        var border = UiKit.FilledImage(window, WithAlpha(Gold, WindowBorderAlpha));
        var fill = UiKit.FilledImage(UiKit.Stretch("Fill", window, t.frameWidth), WithAlpha(Gold, WindowFillAlpha));
        var digit = UiKit.Label("Number", canvas.transform, center, new Vector2(0f, NumberY), new Vector2(400f, 240f), Number, Number, NumberSize, Gold);
        window.gameObject.AddComponent<HologramFlicker>().Init(border, fill, digit);

        Sprite logoArt = DraftHud.Ui("logo_number"); // 160x40 art at 2 ref px per art pixel; falls back to the TMP text
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

        controls = UiKit.ControlsPanel(canvas.transform, center, new Vector2(0f, LogoY)); // covers the logo while open
        controls.SetActive(false);
        EventSystem.current.SetSelectedGameObject(start.gameObject);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (controls.activeSelf && kb != null && kb.escapeKey.wasPressedThisFrame) controls.SetActive(false);
    }

    static Color32 WithAlpha(Color32 c, byte a) { c.a = a; return c; }

    static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
