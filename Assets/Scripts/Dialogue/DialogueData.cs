using System.Collections.Generic;
using UnityEngine;

// Language setting. Default English (in-game text rule). Prototype toggles with L.
public static class DialogueLanguage
{
    public static bool Korean;
}

public class DialogueLine
{
    public string speaker, ko, en;
    public float duration = -1f; // radio only; <0 = automatic
    public string Text => DialogueLanguage.Korean && !string.IsNullOrEmpty(ko) ? ko : (string.IsNullOrEmpty(en) ? ko : en);
}

// Parses a line file:  @mode radio|talk  /  speaker | ko | en [| seconds]  /  # comment
public class DialogueScript
{
    public bool talk;
    public readonly List<DialogueLine> lines = new List<DialogueLine>();

    public static DialogueScript Parse(string text, string nameForLog = "")
    {
        var script = new DialogueScript();
        int n = 0;
        foreach (string raw in text.Split('\n'))
        {
            n++;
            string line = raw.Trim().TrimStart('\uFEFF'); // BOM is not whitespace for Trim; would hide '@mode talk'
            if (line.Length == 0 || line[0] == '#') continue;
            if (line.StartsWith("@mode"))
            {
                script.talk = line.Substring(5).Trim().ToLowerInvariant() == "talk";
                continue;
            }
            string[] p = line.Split('|');
            if (p.Length < 3) { Debug.LogWarning($"[Dialogue] {nameForLog} line {n}: need 'speaker | ko | en'"); continue; }
            var l = new DialogueLine { speaker = p[0].Trim(), ko = p[1].Trim(), en = p[2].Trim() };
            if (p.Length > 3 && float.TryParse(p[3].Trim(), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float d)) l.duration = d;
            script.lines.Add(l);
        }
        return script;
    }
}
