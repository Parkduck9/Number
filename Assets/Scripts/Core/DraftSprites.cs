using System.Linq;
using UnityEngine;

// DRAFT art for a play test. Set Use = false to fall back to the placeholder squares. Removal: docs/design/impl/draft-sprites.md.
public static class DraftSprites
{
    public const bool Use = true;
    public const float WalkFps = 8f;

    static Sprite[] hero, citizen;
    public static Sprite[] Hero => hero ??= Load("DraftSprites/hero_walk_3f");
    public static Sprite[] Citizen => citizen ??= Load("DraftSprites/citizen_attack_3f");

    static Sprite[] Load(string path)
    {
        Sprite[] s = Resources.LoadAll<Sprite>(path).OrderBy(x => x.name).ToArray();
        return s.Length == 3 ? s : null; // missing/unsliced -> keep placeholder
    }

    // Swaps the placeholder square for frame 0: 2 units tall at PPU 32, feet at the 2-unit collider bottom (root centre - 1). Returns false = keep placeholder.
    public static bool Setup(SpriteRenderer r, Sprite[] frames)
    {
        if (!Use || r == null || frames == null) return false;
        r.sprite = frames[0];
        r.color = Color.white;
        r.transform.localScale = Vector3.one;
        r.transform.localPosition = new Vector3(0f, -1f, 0f);
        return true;
    }
}
