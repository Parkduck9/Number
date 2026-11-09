using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Trigger that fades out when the player touches it.
//   Gate: teleports the player to `destination` (feet position) and fades back in (STG-01 SEG-A door -> SEG-B).
//   Goal: stays faded out (stage clear). Next stage is not built yet.
[RequireComponent(typeof(Collider2D))]
public class LevelTransition : MonoBehaviour
{
    public enum Mode { Gate, Goal }

    [SerializeField] Mode mode = Mode.Gate;
    [SerializeField] Transform destination; // feet position of the arrival point (Gate only)
    [SerializeField] float fadeTime = 0.3f; // (T) seconds per fade direction

    static Image fadeImage;
    static bool fadeInPending; // set before a stage change; the next scene starts black and fades in
    bool busy;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook()
    {
        fadeInPending = false;
        fadeImage = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!fadeInPending) return;
        fadeInPending = false;
        EnsureFader();
        SetAlpha(1f);
        fadeImage.gameObject.AddComponent<FadeIn>();
    }

    class FadeIn : MonoBehaviour
    {
        float t;
        void Update()
        {
            t += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f); // a load hitch must not skip the fade
            SetAlpha(Mathf.Clamp01(1f - t / 0.3f));
            if (t >= 0.3f) Destroy(this);
        }
    }

    void Awake() => GetComponent<Collider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (busy) return;
        PlayerController pc = other.GetComponentInParent<PlayerController>();
        if (pc == null || GameFlow.IsPlayerDead(pc)) return;
        if (mode == Mode.Goal && !StageClear.TryClear(transform.position)) return; // DECISIONS 15: all enemies dead + spawners broken
        StartCoroutine(Run(pc));
    }

    IEnumerator Run(PlayerController pc)
    {
        busy = true;
        GameFlow.InputLocked = true;
        yield return Fade(1f);

        if (mode == Mode.Goal)
        {
            fadeInPending = true;
            if (!StageFlow.LoadNext()) StageFlow.LoadTitle(); // both unlock input when they load
            yield break;
        }

        Vector2 pos = destination.position + Vector3.up; // pivot is the center of the 2-unit body
        var rb = pc.GetComponent<Rigidbody2D>();
        pc.transform.position = pos;
        rb.position = pos;
        rb.linearVelocity = Vector2.zero;

        yield return null; // let SegmentCameraBounds switch bounds and snap while still black
        yield return null;
        yield return Fade(0f);
        GameFlow.InputLocked = false;
        busy = false;
    }

    IEnumerator Fade(float target)
    {
        EnsureFader();
        float start = fadeImage.color.a;
        for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            SetAlpha(Mathf.Lerp(start, target, t / fadeTime));
            yield return null;
        }
        SetAlpha(target);
    }

    static void SetAlpha(float a) => fadeImage.color = new Color(0f, 0f, 0f, a);

    static void EnsureFader()
    {
        if (fadeImage != null) return;
        var go = new GameObject("Fade Overlay");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        var imgGo = new GameObject("Black");
        imgGo.transform.SetParent(go.transform, false);
        fadeImage = imgGo.AddComponent<Image>();
        fadeImage.raycastTarget = false;
        var rt = fadeImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        SetAlpha(0f);
    }
}
