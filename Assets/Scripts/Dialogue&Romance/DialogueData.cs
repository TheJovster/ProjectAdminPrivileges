using UnityEngine;
using ProjectAdminPrivileges.Audio;

namespace ProjectAdminPrivileges.Dialogue
{
    public enum Speaker
    {
        MC,
        Queen,
        Narrator
    }

    /// <summary>
    /// ScriptableObject that holds dialogue content.
    /// Create via: Right-click → Create → Dialogue → Dialogue Data
    /// </summary>
    [CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/Dialogue Data")]
    public class DialogueData : ScriptableObject
    {
        [System.Serializable]
        public class DialogueLine
        {
            public Speaker speaker;
            [TextArea(2, 6)]
            public string text;
            public AudioClipData voiceClip; // Optional
            [Range(10, 100)]
            public float charactersPerSecond = 30f;
        }

        [System.Serializable]
        public class DialogueChoice
        {
            [TextArea(1, 3)]
            public string choiceText;
            [Tooltip("Positive = increase affection, Negative = decrease")]
            public int affectionChange;
            [Tooltip("Dialogue to play after this choice")]
            public DialogueData nextDialogue;
        }

        [Header("Dialogue Lines")]
        public DialogueLine[] lines;

        [Header("Choices (Optional)")]
        [Tooltip("If no choices, dialogue ends or continues to nextDialogue")]
        public DialogueChoice[] choices;

        [Header("Auto-Continue")]
        [Tooltip("If no choices, automatically continue to this dialogue")]
        public DialogueData nextDialogue;
    }
}