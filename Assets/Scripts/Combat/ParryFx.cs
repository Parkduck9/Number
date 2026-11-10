using UnityEngine;

// Combat FX helpers: shared 1x1 square, attacker nudge, player slash. Sprite-sheet FX live in SpriteFx.
public static class ParryFx
{
    static Sprite square;

    // Shared 1x1 white sprite (scale it to size).
    public static Sprite Square
    {
        get
        {
            if (square == null)
            {
                Texture2D tex = new Texture2D(1, 1) { filterMode = FilterMode.Point };
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                square = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }
            return square;
        }
    }

    // Player swing: sword-slash sprite FX with its arc centered on the attack box + the attacker's visual steps forward.
    public static void Slash(Vector2 center, Transform attackerVisual, int facing, float time = 0.08f, float nudge = 0.125f)
    {
        // The arc's center is this far from the sheet pivot (facing right); x mirrors when facing left.
        Vector2 arcCenter = new Vector2(14f, 30f) / SpriteFx.Ppu;
        SpriteFx.Play("sword-slash", center - new Vector2(facing * arcCenter.x, arcCenter.y), facing < 0, 90);
        NudgeVisual(attackerVisual, facing, time, nudge);
    }

    // Attacker's visual steps forward for a moment (enemy melee strike; its tell is ParryRing).
    public static void NudgeVisual(Transform attackerVisual, int facing, float time = 0.08f, float nudge = 0.125f)
    {
        if (attackerVisual == null) return;
        Nudge n = attackerVisual.GetComponent<Nudge>();
        if (n == null) n = attackerVisual.gameObject.AddComponent<Nudge>();
        n.Begin(facing * nudge, time);
    }

    class Nudge : MonoBehaviour
    {
        Vector3 basePos;
        float until = -1f;

        void Awake() { basePos = transform.localPosition; }

        public void Begin(float dx, float time)
        {
            transform.localPosition = basePos + new Vector3(dx, 0f, 0f);
            until = Time.time + time;
        }

        void Update()
        {
            if (until >= 0f && Time.time >= until) { transform.localPosition = basePos; until = -1f; }
        }
    }
}
