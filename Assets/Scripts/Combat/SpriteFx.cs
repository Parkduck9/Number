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
        GameObject go = new GameObject("Fx_" + name);
        go.transform.position = position;
        go.transform.localScale = new Vector3(scale, scale, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.flipX = flipX;
        sr.sortingOrder = sortingOrder;
        sr.sprite = clip.frames[0];
        var player = go.AddComponent<Player>();
        player.sr = sr;
        player.frames = clip.frames;
        player.fps = clip.fps;
        player.loop = clip.loop;
        return go;
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
        GameObject go = new GameObject("Afterimage");
        go.transform.position = source.transform.position;
        go.transform.rotation = source.transform.rotation;
        go.transform.localScale = source.transform.lossyScale;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = source.sprite;
        sr.flipX = source.flipX;
        sr.flipY = source.flipY;
        sr.color = tint;
        sr.sortingLayerID = source.sortingLayerID;
        sr.sortingOrder = source.sortingOrder - 1;
        var fade = go.AddComponent<Fade>();
        fade.sr = sr;
        fade.life = life;
        fade.startAlpha = tint.a;
    }

    class Player : MonoBehaviour
    {
        public SpriteRenderer sr;
        public Sprite[] frames;
        public float fps;
        public bool loop;
        float t;

        void Update()
        {
            t += Time.deltaTime;
            int i = (int)(t * fps);
            if (i >= frames.Length)
            {
                if (!loop) { Destroy(gameObject); return; }
                i %= frames.Length;
            }
            sr.sprite = frames[i];
        }
    }

    class Fade : MonoBehaviour
    {
        public SpriteRenderer sr;
        public float life;
        public float startAlpha;
        float t;

        void Update()
        {
            t += Time.deltaTime;
            if (t >= life) { Destroy(gameObject); return; }
            Color c = sr.color;
            c.a = startAlpha * (1f - t / life);
            sr.color = c;
        }
    }
}
