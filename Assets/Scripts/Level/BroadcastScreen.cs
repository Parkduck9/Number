using UnityEngine;

// The big broadcast screen painted on the STG-01 street backdrop shows the broadcast number of the zone the player stands in
// (same digit sprites as the HUD: magenta = AI number, cyan = hacked). No board in the player's zone -> the screen stays blank.
[RequireComponent(typeof(SpriteRenderer))]
public class BroadcastScreen : MonoBehaviour
{
    SpriteRenderer sr;
    Transform player;
    int shownNumber = -1;
    bool shownHacked;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.enabled = false;
    }

    void Update()
    {
        if (player == null)
        {
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc == null) return;
            player = pc.transform;
        }
        BroadcastBoard b = BroadcastBoard.At(player.position);
        if (b == null) { sr.enabled = false; shownNumber = -1; return; }
        if (sr.enabled && b.Number == shownNumber && b.IsHacked == shownHacked) return;
        Sprite[] digits = DraftHud.BroadcastDigits(b.IsHacked);
        if (digits == null) return;
        shownNumber = b.Number;
        shownHacked = b.IsHacked;
        sr.sprite = digits[Mathf.Clamp(shownNumber, 0, digits.Length - 1)];
        sr.enabled = true;
    }
}
