using UnityEngine;

// Player volume levels (playtest 11-13, user: "let me tune the rest myself"). Two levels, 0..1, kept in PlayerPrefs
// so a WebGL reload remembers them. GameAudio multiplies its own mix by these; nothing else reads them.
public static class AudioLevels
{
    const string SfxKey = "vol_sfx", BgmKey = "vol_bgm";

    static float sfx = -1f, bgm = -1f;

    public static float Sfx
    {
        get { Load(); return sfx; }
        set { Load(); sfx = Mathf.Clamp01(value); PlayerPrefs.SetFloat(SfxKey, sfx); }
    }

    public static float Bgm
    {
        get { Load(); return bgm; }
        set { Load(); bgm = Mathf.Clamp01(value); PlayerPrefs.SetFloat(BgmKey, bgm); GameAudio.Hub?.ApplyBgmLevel(); }
    }

    static void Load()
    {
        if (sfx >= 0f) return;
        sfx = PlayerPrefs.GetFloat(SfxKey, 1f);
        bgm = PlayerPrefs.GetFloat(BgmKey, 1f);
    }
}
