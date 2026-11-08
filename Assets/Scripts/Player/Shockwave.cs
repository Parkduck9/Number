using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Upgrade 8-A (docs/design/DECISIONS.md): Space = shockwave. Every enemy in a box `range` units in front of the player
// is staggered for `staggerSeconds`. Locked until SetUnlocked(true) (STG-03, the Luddite). Added at runtime by PlayerController.
[RequireComponent(typeof(PlayerController), typeof(PlayerInputReader))]
public class Shockwave : MonoBehaviour
{
    [Header("Shockwave (T)")]
    [SerializeField] float range = 3f;   // distance in front of the player's center
    [SerializeField] float height = 2f;  // box height, centered on the player
    [SerializeField] float staggerSeconds = 1f;
    [SerializeField] float cooldown = 5f;
    [SerializeField] Color fxColor = new Color(0.298f, 0.909f, 0.847f); // #4CE8D8, same as the dash afterimage

    // Runtime-only unlock (no save). Survives scene loads, resets when the game restarts.
    public static bool Unlocked { get; private set; }
    public static event Action<bool> UnlockChanged;

    public static void SetUnlocked(bool value)
    {
        if (Unlocked == value) return;
        Unlocked = value;
        UnlockChanged?.Invoke(value);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Unlocked = false; UnlockChanged = null; }

    public event Action Used; // for a HUD / SFX / camera shake to subscribe to
    public float CooldownRemaining => Mathf.Max(0f, readyAt - Time.time);
    public bool Ready => Unlocked && CooldownRemaining <= 0f;

    static readonly HashSet<TestEnemy> hitEnemies = new HashSet<TestEnemy>();
    static readonly Color ReadyColor = new Color(0.298f, 0.909f, 0.847f); // #4CE8D8
    PlayerController player;
    PlayerInputReader input;
    float readyAt;
    bool pendingPress; // Space during a hit-stop: fired on the first unfrozen frame instead of being dropped

    // Runtime HUD (bottom-left, code-built, no scene change): visible once unlocked, shows READY or the cooldown.
    GameObject hudRoot;
    TMP_Text hudText;
    int shownTenths = -1;
    UnityEngine.UI.Image hudIcon; // DraftSprites: shockwave_icon / shockwave_icon_cooldown instead of the text line
    Sprite iconReady, iconCooldown;

    void Awake()
    {
        player = GetComponent<PlayerController>();
        input = GetComponent<PlayerInputReader>();
    }

    void OnEnable() { Loc.Changed += OnLanguageChanged; }
    void OnDisable() { Loc.Changed -= OnLanguageChanged; }
    void OnDestroy() { if (hudRoot != null) Destroy(hudRoot); }

    void Update()
    {
        if (Time.timeScale == 0f) { pendingPress |= input.ShockwavePressed; return; } // hit-stop / pause: keep the press
        if (GameFlow.InputLocked) { pendingPress = false; readyAt += Time.deltaTime; UpdateHud(); return; } // dialogue: cooldown pauses like the player's timers
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame)
        {
            SetUnlocked(!Unlocked);
            Debug.Log($"[Shockwave] debug unlock: {Unlocked}");
        }
#endif
        bool pressed = input.ShockwavePressed || pendingPress;
        pendingPress = false;
        if (pressed && Ready &&
            player.CurrentState != PlayerController.State.Hurt && player.CurrentState != PlayerController.State.Dead)
            Fire();
        UpdateHud();
    }

    void Fire()
    {
        readyAt = Time.time + cooldown;
        Sfx.Play(SfxId.Shockwave);
        Vector2 center = Center(player.Facing);
        Vector2 size = new Vector2(range, height);
        ParryFx.Swing(center, size, fxColor, null, player.Facing, 0.15f, 0f);

        hitEnemies.Clear();
        foreach (Collider2D col in Physics2D.OverlapBoxAll(center, size, 0f, player.EnemyMask))
        {
            TestEnemy enemy = col.GetComponentInParent<TestEnemy>();
            if (enemy != null && hitEnemies.Add(enemy)) enemy.ForceStagger(staggerSeconds);
        }
        Used?.Invoke();
    }

    Vector2 Center(int facing) => (Vector2)transform.position + new Vector2(facing * range * 0.5f, 0f);

    // ---- HUD ----
    void OnLanguageChanged() { shownTenths = -1; }

    void UpdateHud()
    {
        if (hudRoot == null) BuildHud();
        if (hudRoot.activeSelf != Unlocked) hudRoot.SetActive(Unlocked);
        if (!Unlocked) return;
        int tenths = Mathf.CeilToInt(CooldownRemaining * 10f); // rebuild the string only when the shown value changes
        if (tenths == shownTenths) return;
        shownTenths = tenths;
        bool ready = tenths <= 0;
        if (hudIcon != null) // draft icon: caption is just the key / the seconds left
        {
            hudIcon.sprite = ready ? iconReady : iconCooldown;
            hudText.text = ready ? "SPACE" : $"{tenths / 10f:0.0}s";
        }
        else
            hudText.text = ready ? Loc.T("SPACE  Shockwave: READY", "SPACE  충격파: 준비")
                                 : Loc.T($"SPACE  Shockwave: {tenths / 10f:0.0}s", $"SPACE  충격파: {tenths / 10f:0.0}초");
        hudText.color = ready ? ReadyColor : UiTheme.Current.dim;
    }

    void BuildHud()
    {
        var canvas = UiKit.CreateCanvas("ShockwaveHud", null, 40); // below the broadcast HUD (50)
        hudRoot = canvas.gameObject;
        iconReady = Resources.Load<Sprite>("DraftUi/shockwave_icon");
        iconCooldown = Resources.Load<Sprite>("DraftUi/shockwave_icon_cooldown");
        bool icon = DraftSprites.Use && iconReady != null && iconCooldown != null;
        if (icon) // 16px art at 3x = 48 reference px, bottom-left; the caption sits to its right
        {
            var ir = UiKit.Rect("Icon", canvas.transform, Vector2.zero, new Vector2(16f + 24f, 16f + 24f), new Vector2(48f, 48f));
            hudIcon = UiKit.FilledImage(ir, Color.white);
            hudIcon.sprite = iconReady;
        }
        var rt = UiKit.Rect("Text", canvas.transform, Vector2.zero, new Vector2((icon ? 16f + 48f + 8f : 16f) + 180f, icon ? 40f : 24f), new Vector2(360f, 24f));
        hudText = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (UiTheme.Current.font != null) hudText.font = UiTheme.Current.font;
        hudText.fontSize = 18;
        hudText.alignment = TextAlignmentOptions.Left;
        hudText.textWrappingMode = TextWrappingModes.NoWrap;
        hudText.raycastTarget = false;
        hudRoot.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(Center(player != null ? player.Facing : 1), new Vector3(range, height, 0f));
    }
}
