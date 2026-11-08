using System.Collections;
using System.Reflection;
using UnityEngine;

// STG-03: walk up to the Luddite, talk (blocking), receive the shockwave device, the exit door opens. Once only.
[RequireComponent(typeof(BoxCollider2D))]
public class LudditeMeeting : MonoBehaviour
{
    [SerializeField] TextAsset talk;
    [SerializeField] GameObject exit;

    bool done;

    void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (done || other.GetComponentInParent<PlayerController>() == null) return;
        done = true;
        if (DialoguePlayer.Instance != null && talk != null) DialoguePlayer.Instance.Play(talk);
        else Debug.LogWarning("[LudditeMeeting] No DialoguePlayer or talk file; talk skipped.");
        StartCoroutine(Finish());
    }

    IEnumerator Finish()
    {
        yield return null; // let the talk box lock input
        while (GameFlow.InputLocked) yield return null;
        GrantUpgrade();
        if (exit != null) exit.SetActive(true);
    }

    // PLACEHOLDER HOOK: the upgrade branch adds `public class Shockwave` with `static void SetUnlocked(bool)`.
    // Resolved by reflection so this branch compiles without it. After the merge, replace with Shockwave.SetUnlocked(true).
    static void GrantUpgrade()
    {
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!asm.GetName().Name.StartsWith("Assembly-CSharp")) continue;
            var m = asm.GetType("Shockwave")?.GetMethod("SetUnlocked", BindingFlags.Public | BindingFlags.Static);
            if (m == null) continue;
            m.Invoke(null, new object[] { true });
            return;
        }
        Debug.Log("[LudditeMeeting] TODO(upgrade): Shockwave not found; replace this with Shockwave.SetUnlocked(true) after merge");
    }
}
