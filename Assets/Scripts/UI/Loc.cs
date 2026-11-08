using System;
using TMPro;
using UnityEngine;

// Language setting. Wraps the existing flag DialogueLanguage.Korean (single source of truth) and persists it.
public static class Loc
{
    const string PrefKey = "number.lang.ko";
    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Load() => DialogueLanguage.Korean = PlayerPrefs.GetInt(PrefKey, 0) == 1;

    public static bool Korean => DialogueLanguage.Korean;

    public static void SetKorean(bool ko)
    {
        DialogueLanguage.Korean = ko;
        PlayerPrefs.SetInt(PrefKey, ko ? 1 : 0);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    public static string T(string en, string ko) => DialogueLanguage.Korean ? ko : en;
}

// Keeps a TMP label in sync with the language setting.
public class LocalizedText : MonoBehaviour
{
    string en, ko;
    TMP_Text label;

    public void Set(TMP_Text text, string english, string korean)
    {
        label = text; en = english; ko = korean;
        Refresh();
    }

    void OnEnable() { Loc.Changed += Refresh; Refresh(); }
    void OnDisable() { Loc.Changed -= Refresh; }

    void Refresh()
    {
        if (label != null) label.text = Loc.T(en, ko);
    }
}
