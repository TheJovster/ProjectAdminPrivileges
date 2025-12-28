using System.Collections;
using UnityEngine;

namespace ProjectAdminPrivileges.Dialogue
{
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private DialogueUI dialogueUI;
        [SerializeField] private GameObject dialoguePanel;

        [Header("Settings")]
        [SerializeField] private bool autoAdvance = true;
        [SerializeField] private float autoAdvanceDelay = 1.5f;

        private DialogueData currentDialogue;
        private int currentLineIndex = 0;
        private bool isActive = false;
        private bool isTyping = false;
        private bool waitingForInput = false; // NEW: Track when waiting
        private Coroutine activeCoroutine;

        public bool IsActive => isActive;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }
        }

        private void Update()
        {
            if (!isActive) return;

            // Space or Click to skip typewriter or advance
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                if (isTyping)
                {
                    SkipTypewriter();
                }
                else if (waitingForInput) // CHANGED: Only advance if explicitly waiting
                {
                    waitingForInput = false; // Clear flag
                    NextLine();
                }
            }
        }

        public void StartDialogue(DialogueData dialogue)
        {
            if (dialogue == null)
            {
                Debug.LogError("[DialogueManager] Null DialogueData!");
                return;
            }

            Debug.Log($"[DialogueManager] Starting: {dialogue.name}");

            currentDialogue = dialogue;
            currentLineIndex = 0;
            isActive = true;
            waitingForInput = false;

            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(true);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetGameState(GameManager.GameState.Dialogue);
            }

            ShowCurrentLine();
        }

        private void ShowCurrentLine()
        {
            if (currentLineIndex >= currentDialogue.lines.Length)
            {
                if (currentDialogue.choices != null && currentDialogue.choices.Length > 0)
                {
                    ShowChoices();
                }
                else
                {
                    EndDialogue();
                }
                return;
            }

            DialogueData.DialogueLine line = currentDialogue.lines[currentLineIndex];
            Debug.Log($"[DialogueManager] Line {currentLineIndex}: {line.speaker} - {line.text}");

            dialogueUI.SetActiveSpeaker(line.speaker);

            string speakerName = line.speaker switch
            {
                Speaker.MC => "You",
                Speaker.Queen => "Queen",
                Speaker.Narrator => "Narrator",
                _ => "Unknown"
            };
            dialogueUI.SetSpeakerName(speakerName);

            dialogueUI.ShowContinuePrompt(false);

            if (activeCoroutine != null)
            {
                StopCoroutine(activeCoroutine);
            }
            activeCoroutine = StartCoroutine(TypeLine(line));
        }


        private IEnumerator TypeLine(DialogueData.DialogueLine line)
        {
            isTyping = true;
            waitingForInput = false;
            dialogueUI.DialogueText.text = "";

            // FIXED: Ensure charactersPerSecond is never 0
            float cps = line.charactersPerSecond > 0 ? line.charactersPerSecond : 30f;
            float delay = 1f / cps;

            Debug.Log($"[DialogueManager] Typing: '{line.text}' at {cps} cps (delay: {delay}s)");

            // Type out character by character
            foreach (char c in line.text)
            {
                dialogueUI.DialogueText.text += c;
                yield return new WaitForSecondsRealtime(delay);
            }

            Debug.Log("[DialogueManager] Typing complete");

            // Typing complete
            isTyping = false;
            waitingForInput = true;
            dialogueUI.ShowContinuePrompt(true);

            // Auto-advance if enabled
            if (autoAdvance)
            {
                Debug.Log($"[DialogueManager] Auto-advance: waiting {autoAdvanceDelay}s");
                yield return new WaitForSecondsRealtime(autoAdvanceDelay);

                if (waitingForInput && isActive)
                {
                    Debug.Log("[DialogueManager] Auto-advancing to next line");
                    waitingForInput = false;
                    NextLine();
                }
            }
        }

        private void SkipTypewriter()
        {
            Debug.Log("[DialogueManager] Skipping typewriter");

            if (activeCoroutine != null)
            {
                StopCoroutine(activeCoroutine);
                activeCoroutine = null;
            }

            DialogueData.DialogueLine line = currentDialogue.lines[currentLineIndex];
            dialogueUI.DialogueText.text = line.text;
            dialogueUI.ShowContinuePrompt(true);
            isTyping = false;
            waitingForInput = true; // CHANGED: Now waiting
        }

        private void NextLine()
        {
            Debug.Log("[DialogueManager] Advancing to next line");

            if (activeCoroutine != null)
            {
                StopCoroutine(activeCoroutine);
                activeCoroutine = null;
            }

            currentLineIndex++;
            ShowCurrentLine();
        }

        private void ShowChoices()
        {
            waitingForInput = false;
            dialogueUI.ShowChoices(currentDialogue.choices);
        }

        public void OnChoiceSelected(int index)
        {
            if (index < 0 || index >= currentDialogue.choices.Length)
            {
                Debug.LogError($"[DialogueManager] Invalid choice: {index}");
                return;
            }

            DialogueData.DialogueChoice choice = currentDialogue.choices[index];
            Debug.Log($"[DialogueManager] Choice: {choice.choiceText} ({choice.affectionChange:+0;-0;0})");

            if (RelationshipManager.Instance != null)
            {
                RelationshipManager.Instance.ModifyAffection(choice.affectionChange);
            }

            dialogueUI.HideChoices();

            if (choice.nextDialogue != null)
            {
                StartDialogue(choice.nextDialogue);
            }
            else
            {
                EndDialogue();
            }
        }

        private void EndDialogue()
        {
            Debug.Log("[DialogueManager] Ending dialogue");

            isActive = false;
            isTyping = false;
            waitingForInput = false;

            if (activeCoroutine != null)
            {
                StopCoroutine(activeCoroutine);
                activeCoroutine = null;
            }

            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetGameState(GameManager.GameState.Playing);
            }
        }
    }
}