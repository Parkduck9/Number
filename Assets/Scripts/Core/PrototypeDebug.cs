using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Prototype-only on-screen debug text (OnGUI) + R to restart. Replaced by uGUI/TMP later.
public class PrototypeDebug : MonoBehaviour
{
    PlayerController player;
    PlayerHealth health;
    TestEnemy enemy;

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void OnGUI()
    {
        if (player == null) { player = FindFirstObjectByType<PlayerController>(); if (player != null) health = player.GetComponent<PlayerHealth>(); }
        if (enemy == null) enemy = FindFirstObjectByType<TestEnemy>();

        GUI.skin.label.fontSize = 20;
        string text = "";
        if (player != null)
            text += $"HP {health.Hp}/{health.MaxHp} / State {player.CurrentState} / Dash CD {player.DashCooldownRemaining:0.0}\n";
        if (enemy != null)
            text += $"Enemy {enemy.Current} / Number {enemy.Number.Value} / HP {enemy.Hp}\n";
        text += "A/D move  W jump  LMB attack  RMB parry  Shift dash  R restart";
        GUI.Label(new Rect(10, 10, 1200, 120), text);
    }
}
