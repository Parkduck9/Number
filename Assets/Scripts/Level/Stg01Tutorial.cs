using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// STG-01 tutorial (DECISIONS 22): the Luddite helper teaches the controls with radio lines and an on-screen prompt, one step at a time.
// A step ends when the player does it or after its timeout; Esc skips everything. Plays once per session (static flag survives death reloads).
// File format (stg01_tutorial.txt): "#step N" starts a step, "#prompt English | Korean" is its on-screen prompt, the rest are radio lines.
public class Stg01Tutorial : MonoBehaviour
{
    [SerializeField] TextAsset lines;

    const float StartDelay = 1.5f, StepTimeout = 8f, ParryTimeout = 20f, DiceHold = 6f; // (T) seconds
    const float MoveDistance = 3f, JumpSpeed = 2f, MaxStartX = 25f;                      // (T) units
    static readonly Color PlateColor = new Color32(0x0B, 0x0F, 0x1E, 0xC0);

    static bool done;
    static int nextStep; // a death reload mid-tutorial resumes at the step it was on, not at step 1

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { done = false; nextStep = 0; } // Domain Reload is off. Not reset by RespawnManager: a death reload must not replay it.

    struct Step { public string text, promptEn, promptKo; }

    PlayerController player;
    Rigidbody2D body;
    GameObject ui;
    TMP_Text prompt;
    LocalizedText promptLoc;
    bool skipped, running;

    IEnumerator Start()
    {
        if (done || lines == null) yield break;
        player = FindFirstObjectByType<PlayerController>();
        if (player == null || player.transform.position.x > MaxStartX) { done = true; yield break; } // entered mid-level: nothing to teach here
        body = player.GetComponent<Rigidbody2D>();
        var steps = Parse(lines.text);
        BuildUi();
        running = true;

        float t = Time.time + StartDelay;
        while (Time.time < t) { if (skipped) { Finish(); yield break; } yield return null; }

        for (int i = nextStep; i < steps.Count; i++)
        {
            nextStep = i;
            while (GameFlow.InputLocked) yield return null;
            if (DialoguePlayer.Instance != null) DialoguePlayer.Instance.Play(new TextAsset("@mode radio\n" + steps[i].text));
            ShowPrompt(steps[i], i + 1, steps.Count);

            float startX = player.transform.position.x, end = Time.time + (i == 2 ? ParryTimeout : StepTimeout);
            int parries = PlayerController.ParryCount;
            bool jumped = false;
            if (i == steps.Count - 1) end = Time.time + DiceHold; // explain only: nothing to do
            while (Time.time < end)
            {
                if (skipped) { Finish(); yield break; }
                if (body != null && body.linearVelocity.y > JumpSpeed) jumped = true;
                bool ok = i switch
                {
                    0 => jumped && Mathf.Abs(player.transform.position.x - startX) >= MoveDistance,
                    1 => player.CurrentState == PlayerController.State.Attack,
                    2 => PlayerController.ParryCount > parries,
                    3 => player.CurrentState == PlayerController.State.Issen,
                    _ => false,
                };
                if (ok) break;
                yield return null;
            }
        }
        Finish();
    }

    // Runs before PauseMenu (order 1000) and marks Esc as used, so the skip press does not also open the pause menu.
    // Ignored while paused / in dialogue, so Esc closing the pause menu does not skip the tutorial.
    void Update()
    {
        if (!running || skipped || PauseMenu.IsOpen || GameFlow.InputLocked || GameFlow.EscConsumed) return;
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
        skipped = true;
        GameFlow.EscConsumedFrame = Time.frameCount;
    }

    void Finish()
    {
        done = true;
        running = false;
        if (ui != null) Destroy(ui);
    }

    static List<Step> Parse(string text)
    {
        var steps = new List<Step>();
        var sb = new StringBuilder();
        Step cur = default;
        bool open = false;
        foreach (var raw in text.Split('\n'))
        {
            string line = raw.Trim();
            if (line.StartsWith("#step"))
            {
                if (open) { cur.text = sb.ToString(); steps.Add(cur); }
                open = true; cur = default; sb.Clear();
            }
            else if (line.StartsWith("#prompt"))
            {
                var p = line.Substring(7).Split('|');
                cur.promptEn = p[0].Trim();
                cur.promptKo = p.Length > 1 ? p[1].Trim() : cur.promptEn;
            }
            else if (open) sb.AppendLine(line);
        }
        if (open) { cur.text = sb.ToString(); steps.Add(cur); }
        return steps;
    }

    void BuildUi()
    {
        var canvas = UiKit.CreateCanvas("TutorialCanvas", transform, 35); // below the lucky dice HUD (40)
        ui = canvas.gameObject;
        var plate = UiKit.Rect("Plate", canvas.transform, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(620f, 64f));
        UiKit.FilledImage(plate, PlateColor);
        prompt = UiKit.Label("Prompt", plate, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(600f, 32f), "", "", 24, UiTheme.Current.text);
        promptLoc = prompt.GetComponent<LocalizedText>();
        UiKit.Label("Skip", plate, new Vector2(0.5f, 0.5f), new Vector2(0f, -17f), new Vector2(600f, 20f), "Esc: skip tutorial", "Esc: 튜토리얼 건너뛰기", 14, UiTheme.Current.dim);
    }

    void ShowPrompt(Step s, int n, int count) =>
        promptLoc.Set(prompt, $"{n}/{count}  {s.promptEn}", $"{n}/{count}  {s.promptKo}");
}
