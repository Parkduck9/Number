using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

// Game-wide sound API (decision 10-A). Self-creating: no scene or prefab setup needed.
//   Sfx.Play(SfxId.Hit);              one-shot, pooled, per-id cooldown
//   Bgm.Play(BgmId.Field);            loops; fades from the current track
//   GameAudio.MuteAll(true/false);    SFX-02 "breath stop": cut every sound at once (AudioListener.volume)
//   GameAudio.BreathStop(1.5f);       cut everything for N seconds, then come back
// WebGL: browsers keep audio locked until the first user gesture. Until then SFX are dropped and the
// requested BGM is held and started on the first key/click/touch.
// Observer: the hub listens to every BroadcastBoard.NumberChanged, so the broadcast chime (SFX-01),
// the hack-success sound and the NUM-0 breath stop need no calls from other scripts.
public static class Sfx
{
    public static void Play(SfxId id, float volume = 1f) => GameAudio.Hub?.PlaySfx(id, volume);
    public static void Play(SfxId id, float volume, float pitch) => GameAudio.Hub?.PlaySfx(id, volume, pitch); // pitch > 0: exact, no jitter (EliteDuel clang steps)
}

public static class Bgm
{
    public static void Play(BgmId id) => GameAudio.Hub?.PlayBgm(id);
    public static void Stop() => GameAudio.Hub?.PlayBgm(BgmId.None);
    public static BgmId Current => GameAudio.Hub != null ? GameAudio.Hub.Requested : BgmId.None;
}

public class GameAudio : MonoBehaviour
{
    // ---- tunables (T) ----
    const int Voices = 10;               // simultaneous SFX
    const float SfxVolume = 0.8f;
    const float BgmVolume = 0.45f;   // before the player's level (AudioLevels.Bgm)
    const float BgmFade = 0.6f;          // seconds, fade out old / fade in new
    const float SameSfxCooldown = 0.04f; // same id within this time is skipped (multi-hit frames)
    const float PitchJitter = 0.05f;     // +-5% on combat sounds so repeats don't sound identical
    public static bool BreathStopOnZero = true;     // broadcast becomes 0 -> chime, then silence
    public static float BreathStopSeconds = 1.5f;
    const float ChimeLength = 1.9f;      // sfx_broadcast length; silence starts after it
    const float HackOkLength = 1.0f;     // sfx_hack_ok length

    public static GameAudio Hub { get; private set; }
    public static bool Muted { get; private set; }
    public BgmId Requested { get; private set; }

    AudioClip[] sfxClips;
    float[] lastPlayed;
    AudioSource[] voices;
    int nextVoice;
    AudioSource bgm;
    BgmId playing;
    float fadeDir;                       // -1 fading out, +1 fading in, 0 steady
    bool unlocked;
    float breathStopAt = -1f, breathEndAt = -1f;
    readonly System.Collections.Generic.List<BroadcastBoard> watched = new System.Collections.Generic.List<BroadcastBoard>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Create()
    {
        var go = new GameObject("GameAudio");
        DontDestroyOnLoad(go);
        go.AddComponent<GameAudio>();
    }

    public static void MuteAll(bool mute)
    {
        Muted = mute;
        AudioListener.volume = mute ? 0f : 1f; // instant cut, game keeps running
    }

    public static void BreathStop(float seconds)
    {
        if (Hub == null) return;
        MuteAll(true);
        Hub.breathStopAt = -1f;
        Hub.breathEndAt = Time.unscaledTime + seconds;
    }

    void Awake()
    {
        Hub = this;
        int n = System.Enum.GetValues(typeof(SfxId)).Length;
        sfxClips = new AudioClip[n];
        lastPlayed = new float[n];
        for (int i = 0; i < n; i++)
        {
            sfxClips[i] = Resources.Load<AudioClip>("Sfx/sfx_" + Snake(((SfxId)i).ToString()));
            lastPlayed[i] = -1f;
            if (sfxClips[i] == null) Debug.LogWarning($"[GameAudio] missing clip for SfxId.{(SfxId)i}");
        }
        voices = new AudioSource[Voices];
        for (int i = 0; i < Voices; i++) voices[i] = NewSource(false);
        bgm = NewSource(true);

        // Editor / desktop never lock audio; WebGL waits for the first gesture.
        unlocked = Application.platform != RuntimePlatform.WebGLPlayer;
        if (!unlocked) InputSystem.onAnyButtonPress.CallOnce(_ => unlocked = true);
    }

    AudioSource NewSource(bool loop)
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.loop = loop;
        s.spatialBlend = 0f;
        return s;
    }

    public void PlaySfx(SfxId id, float volume, float pitch = 0f)
    {
        int i = (int)id;
        AudioClip clip = sfxClips[i];
        if (!unlocked || clip == null) return;
        float now = Time.unscaledTime;
        if (now - lastPlayed[i] < SameSfxCooldown) return;
        lastPlayed[i] = now;

        AudioSource v = voices[nextVoice];
        nextVoice = (nextVoice + 1) % Voices;
        v.pitch = pitch > 0f ? pitch : IsCombat(id) ? 1f + Random.Range(-PitchJitter, PitchJitter) : 1f;
        v.PlayOneShot(clip, volume * SfxVolume * AudioLevels.Sfx);
    }

    float BgmTarget => BgmVolume * AudioLevels.Bgm;

    // The player moved the music slider: take it without waiting for the next fade.
    public void ApplyBgmLevel()
    {
        if (bgm != null && bgm.isPlaying && fadeDir >= 0f) bgm.volume = BgmTarget;
    }

    public void PlayBgm(BgmId id)
    {
        if (id == Requested) return;
        Requested = id;
        fadeDir = bgm.isPlaying ? -1f : 1f; // fade out the old track first, Update swaps it
        if (!bgm.isPlaying) Swap();
    }

    void Swap()
    {
        playing = Requested;
        bgm.Stop();
        if (playing == BgmId.None) { fadeDir = 0f; return; }
        bgm.clip = Resources.Load<AudioClip>("Bgm/bgm_" + Snake(playing.ToString()));
        if (bgm.clip == null) { Debug.LogWarning($"[GameAudio] missing BGM {playing}"); fadeDir = 0f; return; }
        if (!unlocked) return; // started from Update once the browser allows audio
        bgm.volume = 0f;
        bgm.Play();
        fadeDir = 1f;
    }

    void Update()
    {
        float now = Time.unscaledTime;
        WatchBoards();

        // BGM requested before the WebGL unlock: start it now.
        if (unlocked && playing != BgmId.None && bgm.clip != null && !bgm.isPlaying && fadeDir >= 0f)
        {
            bgm.volume = 0f;
            bgm.Play();
            fadeDir = 1f;
        }

        if (fadeDir != 0f)
        {
            bgm.volume = Mathf.MoveTowards(bgm.volume, fadeDir > 0f ? BgmTarget : 0f, BgmVolume * Time.unscaledDeltaTime / BgmFade);
            if (fadeDir < 0f && bgm.volume <= 0f) Swap();
            else if (fadeDir > 0f && bgm.volume >= BgmTarget) fadeDir = 0f;
        }

        if (breathStopAt >= 0f && now >= breathStopAt) BreathStop(BreathStopSeconds);
        if (breathEndAt >= 0f && now >= breathEndAt) { breathEndAt = -1f; MuteAll(false); }
    }

    // Observer: subscribe to boards as they appear (BroadcastBoard keeps a static All list).
    void WatchBoards()
    {
        var all = BroadcastBoard.All;
        for (int i = 0; i < all.Count; i++)
            if (!watched.Contains(all[i])) { watched.Add(all[i]); all[i].NumberChanged += OnBroadcast; }
        for (int i = watched.Count - 1; i >= 0; i--)
            if (watched[i] == null || !all.Contains(watched[i]))
            {
                if (watched[i] != null) watched[i].NumberChanged -= OnBroadcast;
                watched.RemoveAt(i);
            }
    }

    void OnBroadcast(BroadcastBoard board, int newNumber, int oldNumber)
    {
        if (board.IsHacked) Sfx.Play(SfxId.HackOk); // the player's hack just set this number
        else Sfx.Play(SfxId.Broadcast, 0.4f);      // AI broadcast or counter-hack restore; quiet, it sat over the music (playtest 11-13)
        if (BreathStopOnZero && newNumber == 0) breathStopAt = Time.unscaledTime + (board.IsHacked ? HackOkLength : ChimeLength);
    }

    void OnDestroy()
    {
        foreach (var b in watched) if (b != null) b.NumberChanged -= OnBroadcast;
        if (Hub == this) { Hub = null; MuteAll(false); }
    }

    static bool IsCombat(SfxId id) => id <= SfxId.Stunned;

    // "EnemyTell" -> "enemy_tell"
    static string Snake(string s)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < s.Length; i++)
        {
            if (char.IsUpper(s[i]) && i > 0) sb.Append('_');
            sb.Append(char.ToLowerInvariant(s[i]));
        }
        return sb.ToString();
    }
}
