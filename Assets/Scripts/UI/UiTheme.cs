using TMPro;
using UnityEngine;

// ALL placeholder style values for the title screen and pause menu live here (one asset, one restyle point).
// Asset: Assets/Resources/UiTheme.asset (created by NUMBER/Build Title Scene). Falls back to these defaults if missing.
[CreateAssetMenu(menuName = "NUMBER/UI Theme", fileName = "UiTheme")]
public class UiTheme : ScriptableObject
{
    public TMP_FontAsset font; // Mulmaru SDF
    public Color background = new Color32(0x0B, 0x0F, 0x1E, 0xFF);   // dark navy
    public Color panelFill = new Color32(0x0B, 0x0F, 0x1E, 0xF2);
    public Color border = new Color32(0x3A, 0x3F, 0x4B, 0xFF);
    public Color dim = new Color32(0x8A, 0x90, 0xA0, 0xFF);          // normal button text
    public Color text = Color.white;                                 // title, selected button text
    public Color selectedBorder = new Color32(0xC8, 0xCC, 0xD8, 0xFF);
    public Color dimmer = new Color32(0x00, 0x00, 0x00, 0xA0);       // behind the pause panel
    [Tooltip("Sizes are multiples of 12 (Mulmaru native) at the 960x540 reference.")]
    public int titleSize = 96, buttonSize = 24;
    public Vector2 referenceResolution = new Vector2(960f, 540f);
    public Vector2 buttonSize2 = new Vector2(360f, 48f);
    public float buttonStep = 60f;
    public int frameWidth = 2; // pixel frame thickness (reference px)

    static UiTheme current;
    public static UiTheme Current
    {
        get
        {
            if (current == null)
            {
                current = Resources.Load<UiTheme>("UiTheme");
                if (current == null) current = CreateInstance<UiTheme>(); // defaults, default TMP font
            }
            return current;
        }
    }
}
