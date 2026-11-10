using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Sandbox.unity only (never in Build Settings): 1..6 = fire that dice face now, F1..F6 = teleport to station, R = reload (respawns enemies),
// player cannot die. Dice are unlocked and charged. Added by NUMBER/Build Sandbox scene.
public class SandboxHarness : MonoBehaviour
{
    public float[] stationX = { 10f, 36f, 62f, 88f, 114f, 140f }; // player x per station (F1..F6)
    public float stationFeetY = 1f;

    static readonly Key[] Digits = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6 };
    static readonly Key[] FKeys = { Key.F1, Key.F2, Key.F3, Key.F4, Key.F5, Key.F6 };
    static readonly FieldInfo DialBoard = typeof(BroadcastHackUi).GetField("dialBoard", BindingFlags.NonPublic | BindingFlags.Instance);

    PlayerController player;
    PlayerHealth health;
    LuckyDice dice;
    BroadcastHackUi hackUi;

    void Start()
    {
        LuckyDice.SetUnlocked(true);
        var canvas = UiKit.CreateCanvas("Sandbox Hint", transform, 50);
        UiKit.Label("Hint", canvas.transform, new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(900f, 28f),
            "1-6: dice skill   F1-F6: station   R: reset   E: hack board",
            "1-6: 주사위 스킬   F1-F6: 구역 이동   R: 초기화   E: 해킹", 18, Color.white);
    }

    void Update()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
            if (player == null) return;
            health = player.GetComponent<PlayerHealth>();
        }
        if (dice == null) dice = player.GetComponent<LuckyDice>();
        if (hackUi == null) hackUi = FindFirstObjectByType<BroadcastHackUi>();
        LuckyDice.SetUnlocked(true);
        if (health != null && !health.IsDead) health.SetInvulnerable(true);

        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.rKey.wasPressedThisFrame && !DialOpen()) { Reload(); return; }
        for (int i = 0; i < FKeys.Length; i++)
            if (kb[FKeys[i]].wasPressedThisFrame) Teleport(stationX[i]);
        if (dice == null || DialOpen()) return; // the hack dial uses the digit keys
        for (int i = 0; i < Digits.Length; i++)
            if (kb[Digits[i]].wasPressedThisFrame) dice.ForceRoll(i + 1);
    }

    bool DialOpen() => hackUi != null && DialBoard != null && DialBoard.GetValue(hackUi) != null;

    void Teleport(float x)
    {
        var rb = player.Body;
        Vector2 p = new Vector2(x, stationFeetY + 1f);
        rb.position = p;
        rb.linearVelocity = Vector2.zero;
        player.transform.position = p;
    }

    void Reload()
    {
#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(gameObject.scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
        SceneManager.LoadScene(gameObject.scene.name);
#endif
    }
}
