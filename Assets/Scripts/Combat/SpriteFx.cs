using System.Collections.Generic;
using UnityEngine;

// Plays pixel-art sprite-sheet FX: Resources/FX/<name>.png (grid, row-major from top-left) + <name>.json (pixelfx meta).
// Time is scaled on purpose: FX freeze during hit-stop and pause.
public static class SpriteFx
{
    public const float Ppu = 32f; // same as the game art (1x), so FX pixels line up with the game pixel grid

    [System.Serializable] public class Root { public Meta meta; }
    [System.Serializable] public class Meta { public Info pixelfx; }
    [System.Serializable]
    public class Info
    {
        public float fps;
        public bool loop;
        public int frames;
        public int[] frameSize;
        public int[] pivot; // pixels from the frame's top-left
        public int columns;
    }

    class Clip
    {
        public Sprite[] frames;
        public float fps;
        public bool loop;
    }

    static readonly Dictionary<string, Clip> cache = new Dictionary<string, Clip>();

    static Clip Load(string name)
    {
        if (cache.TryGetValue(name, out Clip clip) && clip.frames[0] != null) return clip;
        Texture2D tex = Resources.Load<Texture2D>("FX/" + name);
        TextAsset json = Resources.Load<TextAsset>("FX/" + name);
        if (tex == null || json == null)
        {
            Debug.LogWarning("SpriteFx: missing Resources/FX/" + name);
            return null;
        }
        Info info = JsonUtility.FromJson<Root>(json.text).meta.pixelfx;
        int w = info.frameSize[0], h = info.frameSize[1];
        int rows = (info.frames + info.columns - 1) / info.columns;
        if (tex.width != info.columns * w || tex.height != rows * h)
        {
            Debug.LogError($"SpriteFx: Resources/FX/{name}.png is {tex.width}x{tex.height}, expected {info.columns * w}x{rows * h} (NPOT rescaled on import?)");
            return null;
        }
        // Unity rects start bottom-left, the sheet and pivot start top-left.
        Vector2 pivot = new Vector2((float)info.pivot[0] / w, (float)(h - info.pivot[1]) / h);
        clip = new Clip { frames = new Sprite[info.frames], fps = info.fps, loop = info.loop };
        for (int i = 0; i < info.frames; i++)
        {
            int col = i % info.columns, row = i / info.columns;
            Rect rect = new Rect(col * w, tex.height - (row + 1) * h, w, h);
            clip.frames[i] = Sprite.Create(tex, rect, pivot, Ppu, 0, SpriteMeshType.FullRect);
        }
        cache[name] = clip;
        return clip;
    }

    // Spawns the effect with its pivot at 'position'. One-shot effects destroy themselves; loops run until the object is destroyed.
    // 'scale' multiplies the shared PPU size (only the number ember uses it).
    public static GameObject Play(string name, Vector2 position, bool flipX = false, int sortingOrder = 100, float scale = 1f)
    {
        Clip clip = Load(name);
        if (clip == null) return null;
        Fx fx = Get(position, Quaternion.identity, new Vector3(scale, scale, 1f));
        SpriteRenderer sr = fx.sr;
        sr.flipX = flipX;
        sr.sortingOrder = sortingOrder;
        sr.sprite = clip.frames[0];
        fx.frames = clip.frames;
        fx.fps = clip.fps;
        fx.loop = clip.loop;
        return fx.gameObject;
    }

    // Pool: finished one-shots / afterimages are disabled and reused (no GameObject churn per run-dust / speed-line / spark).
    // Pooled objects live in the scene, so a scene change destroys them; Get skips those. Callers that Destroy a returned loop are fine.
    static readonly Stack<Fx> pool = new Stack<Fx>();
    static int fxLayer = -1;
    static int FxLayer => fxLayer >= 0 ? fxLayer : (fxLayer = SortingLayer.NameToID("FX")); // DECISIONS 9 Q6; 0 (Default) if missing

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => pool.Clear(); // Domain Reload is off

    static Fx Get(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        Fx fx = null;
        while (pool.Count > 0 && fx == null) fx = pool.Pop();
        if (fx == null)
        {
            var go = new GameObject("Fx");
            fx = go.AddComponent<Fx>();
            fx.sr = go.AddComponent<SpriteRenderer>();
        }
        Transform t = fx.transform;
        t.SetPositionAndRotation(position, rotation);
        t.localScale = scale;
        SpriteRenderer sr = fx.sr;
        sr.flipX = sr.flipY = false;
        sr.color = Color.white;
        sr.sortingLayerID = FxLayer; // PlayBehind / Afterimage then copy the source's layer
        sr.enabled = true;
        fx.frames = null;
        fx.t = 0f;
        fx.enabled = true;
        return fx;
    }

    static void Release(Fx fx)
    {
        fx.enabled = false;
        fx.sr.enabled = false;
        fx.sr.sprite = null;
        if (fx.transform.parent != null) fx.transform.SetParent(null, false);
        pool.Push(fx);
    }

    // Same, drawn just behind 'behind' (its sorting layer, order - 1), rotated by 'angle' degrees, and parented to 'follow' if given.
    public static GameObject PlayBehind(string name, Vector2 position, bool flipX, SpriteRenderer behind, float angle = 0f, Transform follow = null)
    {
        GameObject go = Play(name, position, flipX, behind.sortingOrder - 1);
        if (go == null) return null;
        go.GetComponent<SpriteRenderer>().sortingLayerID = behind.sortingLayerID;
        go.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        if (follow != null) go.transform.SetParent(follow, true);
        return go;
    }

    // Dash afterimage: copy of the visual's current sprite, tinted and fading out, just behind it.
    public static void Afterimage(SpriteRenderer source, Color tint, float life)
    {
        Transform src = source.transform;
        Fx fx = Get(src.position, src.rotation, src.lossyScale);
        SpriteRenderer sr = fx.sr;
        sr.sprite = source.sprite;
        sr.flipX = source.flipX;
        sr.flipY = source.flipY;
        sr.color = tint;
        sr.sortingLayerID = source.sortingLayerID;
        sr.sortingOrder = source.sortingOrder - 1;
        fx.life = life;
        fx.startAlpha = tint.a;
    }

    // frames != null: sprite-sheet player; else afterimage fade.
    class Fx : MonoBehaviour
    {
        public SpriteRenderer sr;
        public Sprite[] frames;
        public float fps;
        public bool loop;
        public float life;
        public float startAlpha;
        public float t;

        void Update()
        {
            t += Time.deltaTime;
            if (frames != null)
            {
                int i = (int)(t * fps);
                if (i >= frames.Length)
                {
                    if (!loop) { Release(this); return; }
                    i %= frames.Length;
                }
                sr.sprite = frames[i];
                return;
            }
            if (t >= life) { Release(this); return; }
            Color c = sr.color;
            c.a = startAlpha * (1f - t / life);
            sr.color = c;
        }
    }
}
