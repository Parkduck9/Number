using System;
using UnityEngine;

// Speaker table. Add a row per character: id (used in line files), names, optional portrait, name color.
[CreateAssetMenu(menuName = "NUMBER/Dialogue Speakers", fileName = "Speakers")]
public class DialogueSpeakers : ScriptableObject
{
    [Serializable]
    public class Speaker
    {
        public string id;
        public string nameKo;
        public string nameEn;
        public Sprite portrait; // optional; a color block is shown when empty
        public Color color = Color.white;
    }

    public Speaker[] speakers;

    public Speaker Find(string id)
    {
        if (speakers != null)
            foreach (var s in speakers)
                if (s.id == id) return s;
        return null;
    }
}
