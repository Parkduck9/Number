using UnityEngine;

// The boss's blade arm stuck in the bridge, as an EliteDuel foe (boss-v2 attack B). BossFight places it at the stuck blade
// (floor + 1, the player's centre height) and gets the result back through EndDuel / DuelFinish.
public class BossBladeFoe : MonoBehaviour, IDuelFoe
{
    public BossFight Boss;

    public bool InDuel { get; set; }
    public bool DuelReady => Boss != null && Boss.BladeStuck;
    public SpriteRenderer Visual => null; // the arm is a boss part: no nudge / shatter

    public void DuelPose(int frame, bool flash) => Boss.BladeDuelPose(frame, flash);
    public void EndDuel() { InDuel = false; Boss.OnDuelEnded(false); }
    public void DuelFinish() { InDuel = false; Boss.OnDuelEnded(true); }
}
