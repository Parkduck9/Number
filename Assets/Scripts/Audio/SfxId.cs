// Sound ids. The enum name is the clip file name in Assets/Audio/Resources/Sfx|Bgm (lower-cased, "sfx_"/"bgm_" prefix).
// e.g. SfxId.EnemyTell -> Resources/Sfx/sfx_enemy_tell. Keep the order: values are only used in code, never serialized.
public enum SfxId
{
    Swing,      // player attack swing
    Hit,        // player attack lands
    Parry,      // frontal strike deflected
    Dash,
    Hurt,       // player takes damage
    EnemyTell,  // enemy strike wind-up
    LastTell,   // last strike (number 1) wind-up
    Stunned,    // enemy stunned / counter window opens
    Broadcast,  // SFX-01 billboard number changed (AI)
    HackOk,     // player hack confirmed
    Shockwave,  // upgrade: Space shockwave
    UiSelect,
}

public enum BgmId
{
    None,
    Field,
    Boss,
}
