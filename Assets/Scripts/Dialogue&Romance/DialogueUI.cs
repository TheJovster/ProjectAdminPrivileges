using UnityEngine;
using TMPro;

namespace ProjectAdminPrivileges.Dialogue
{
    public class DialogueUI : MonoBehaviour
    {
        [Header("Portraits")]
        [SerializeField] private CharacterPortrait mcPortrait;
        [SerializeField] private CharacterPortrait queenPortrait;

        [Header("Dialogue Elements")]
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private GameObject continuePrompt;

        [Header("Choices")]
        [SerializeField] private GameObject choiceContainer;
        [SerializeField] private DialogueChoiceButton[] choiceButtons;

        public TextMeshProUGUI DialogueText => dialogueText;

        private void Awake()
        {
            // Start with choices hidden
            if (choiceContainer != null)
            {
                choiceContainer.SetActive(false);
            }

            if (continuePrompt != null)
            {
                continuePrompt.SetActive(false);
            }
        }

        public void SetActiveSpeaker(Speaker speaker)
        {
            if (speaker == Speaker.MC)
            {
                mcPortrait.SetSpeaking();
                queenPortrait.SetIdle();
            }
            else if(speaker == Speaker.Queen)
            {
                mcPortrait.SetIdle();
                queenPortrait.SetSpeaking();
            }
            else if(speaker == Speaker.Narrator)
            {
                mcPortrait.SetIdle();
                queenPortrait.SetIdle();
            }
        }

        public void SetSpeakerName(string name)
        {
            speakerNameText.text = name;
        }

        public void ShowContinuePrompt(bool show)
        {
            continuePrompt.SetActive(show);
        }

        public void ShowChoices(DialogueData.DialogueChoice[] choices)
        {
            choiceContainer.SetActive(true);
            continuePrompt.SetActive(false);

            // Activate and setup buttons based on number of choices
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (i < choices.Length)
                {
                    choiceButtons[i].gameObject.SetActive(true);
                    choiceButtons[i].Setup(choices[i].choiceText, i);
                }
                else
                {
                    choiceButtons[i].gameObject.SetActive(false);
                }
            }
        }

        public void HideChoices()
        {
            choiceContainer.SetActive(false);
        }
    }
}