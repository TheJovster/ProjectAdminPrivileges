using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectAdminPrivileges.Dialogue
{
    /// <summary>
    /// Handles individual choice button behavior.
    /// Attach to each choice button prefab.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class DialogueChoiceButton : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI choiceText;
        [SerializeField] private Button button;

        private int choiceIndex;

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (choiceText == null)
            {
                choiceText = GetComponentInChildren<TextMeshProUGUI>();
            }

            button.onClick.AddListener(OnClicked);
        }

        public void Setup(string text, int index)
        {
            choiceText.text = text;
            choiceIndex = index;
            gameObject.SetActive(true);
        }

        private void OnClicked()
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnChoiceSelected(choiceIndex);
            }
        }
    }
}