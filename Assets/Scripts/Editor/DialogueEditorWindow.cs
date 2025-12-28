using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using ProjectAdminPrivileges.Dialogue;

/// <summary>
/// Database editor for Dialogue Data.
/// Window → Admin Privileges → Dialogue Editor
/// </summary>
public class DialogueEditorWindow : EditorWindow
{
    // Data
    private List<DialogueData> dialogues = new List<DialogueData>();

    // UI State
    private Vector2 scrollPosition;
    private string searchQuery = "";
    private bool showOnlyWithChoices = false;
    private bool showOnlyWithNextDialogue = false;

    // Foldout state
    private Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();

    // Persistent settings (stored in EditorPrefs)
    private string dialogueFolderPath = "Assets/ScriptableObjects/Dialogues";
    private const string DIALOGUE_FOLDER_KEY = "DialogueEditor_DialogueFolder";

    // Styles
    private GUIStyle headerStyle;
    private GUIStyle itemBoxStyle;
    private GUIStyle speakerLabelStyle;
    private bool stylesInitialized = false;

    [MenuItem("Window/Admin Privileges/Dialogue Editor")]
    public static void ShowWindow()
    {
        DialogueEditorWindow window = GetWindow<DialogueEditorWindow>("Dialogue Editor");
        window.minSize = new Vector2(700, 400);
        window.Show();
    }

    private void OnEnable()
    {
        // Load saved folder path
        dialogueFolderPath = EditorPrefs.GetString(DIALOGUE_FOLDER_KEY, "Assets/ScriptableObjects/Dialogues");

        RefreshDatabase();
    }

    private void InitializeStyles()
    {
        if (stylesInitialized) return;

        headerStyle = new GUIStyle(EditorStyles.boldLabel);
        headerStyle.fontSize = 14;
        headerStyle.padding = new RectOffset(5, 5, 5, 5);

        itemBoxStyle = new GUIStyle(GUI.skin.box);
        itemBoxStyle.padding = new RectOffset(10, 10, 10, 10);
        itemBoxStyle.margin = new RectOffset(0, 0, 5, 5);

        speakerLabelStyle = new GUIStyle(EditorStyles.miniLabel);
        speakerLabelStyle.fontStyle = FontStyle.Bold;

        stylesInitialized = true;
    }

    private void OnGUI()
    {
        InitializeStyles();

        // Header
        EditorGUILayout.Space(10);
        GUILayout.Label("Admin Privileges - Dialogue Editor", headerStyle);
        EditorGUILayout.Space(5);

        // Toolbar
        DrawToolbar();

        EditorGUILayout.Space(5);

        // Content
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        {
            DrawDialogues();
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawToolbar()
    {
        // Search and filters
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        {
            // Search bar
            GUILayout.Label("Search:", GUILayout.Width(60));
            searchQuery = EditorGUILayout.TextField(searchQuery, EditorStyles.toolbarSearchField, GUILayout.Width(200));

            GUILayout.Space(10);

            // Filters
            showOnlyWithChoices = GUILayout.Toggle(showOnlyWithChoices, "With Choices", EditorStyles.toolbarButton, GUILayout.Width(100));
            showOnlyWithNextDialogue = GUILayout.Toggle(showOnlyWithNextDialogue, "With Next", EditorStyles.toolbarButton, GUILayout.Width(80));

            GUILayout.FlexibleSpace();

            // Refresh button
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RefreshDatabase();
            }

            // Create new button
            if (GUILayout.Button("Create New", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                CreateNew();
            }
        }
        EditorGUILayout.EndHorizontal();

        // Settings row (folder path)
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        {
            GUILayout.Label("Create New In:", GUILayout.Width(95));

            dialogueFolderPath = EditorGUILayout.TextField(dialogueFolderPath);

            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Dialogues Folder", dialogueFolderPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    // Convert absolute path to relative
                    if (path.StartsWith(Application.dataPath))
                    {
                        dialogueFolderPath = "Assets" + path.Substring(Application.dataPath.Length);
                        EditorPrefs.SetString(DIALOGUE_FOLDER_KEY, dialogueFolderPath);
                    }
                    else
                    {
                        Debug.LogWarning("[Dialogue Editor] Selected folder is outside the project!");
                    }
                }
            }

            // Save on text field change
            if (GUI.changed)
            {
                EditorPrefs.SetString(DIALOGUE_FOLDER_KEY, dialogueFolderPath);
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawDialogues()
    {
        var filteredDialogues = dialogues
            .Where(dialogue => dialogue != null)
            .Where(dialogue => PassesSearchFilter(dialogue))
            .Where(dialogue => !showOnlyWithChoices || (dialogue.choices != null && dialogue.choices.Length > 0))
            .Where(dialogue => !showOnlyWithNextDialogue || dialogue.nextDialogue != null)
            .OrderBy(dialogue => dialogue.name)
            .ToList();

        if (filteredDialogues.Count == 0)
        {
            EditorGUILayout.HelpBox("No dialogues found. Click 'Create New' to add one.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField($"Showing {filteredDialogues.Count} of {dialogues.Count} dialogues", EditorStyles.miniLabel);
        EditorGUILayout.Space(5);

        foreach (var dialogue in filteredDialogues)
        {
            DrawDialogueEntry(dialogue);
        }
    }

    private void DrawDialogueEntry(DialogueData dialogue)
    {
        EditorGUILayout.BeginVertical(itemBoxStyle);
        {
            // Header row with foldout
            EditorGUILayout.BeginHorizontal();
            {
                bool isExpanded = GetFoldoutState(AssetDatabase.GetAssetPath(dialogue));
                isExpanded = EditorGUILayout.Foldout(isExpanded, "", true);
                SetFoldoutState(AssetDatabase.GetAssetPath(dialogue), isExpanded);

                // Dialogue name and info
                EditorGUILayout.BeginVertical();
                {
                    EditorGUILayout.LabelField(dialogue.name, EditorStyles.boldLabel);

                    // Quick info
                    string info = $"{dialogue.lines?.Length ?? 0} lines";
                    if (dialogue.choices != null && dialogue.choices.Length > 0)
                    {
                        info += $" | {dialogue.choices.Length} choices";
                    }
                    if (dialogue.nextDialogue != null)
                    {
                        info += $" → {dialogue.nextDialogue.name}";
                    }
                    EditorGUILayout.LabelField(info, EditorStyles.miniLabel);
                }
                EditorGUILayout.EndVertical();

                GUILayout.FlexibleSpace();

                // Quick preview of first line
                if (dialogue.lines != null && dialogue.lines.Length > 0)
                {
                    string preview = dialogue.lines[0].text;
                    if (preview.Length > 50)
                    {
                        preview = preview.Substring(0, 47) + "...";
                    }
                    EditorGUILayout.LabelField($"\"{preview}\"", EditorStyles.miniLabel, GUILayout.Width(300));
                }

                // Quick actions
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeObject = dialogue;
                    EditorGUIUtility.PingObject(dialogue);
                }

                if (GUILayout.Button("Duplicate", GUILayout.Width(70)))
                {
                    DuplicateDialogue(dialogue);
                }
            }
            EditorGUILayout.EndHorizontal();

            // Expanded details
            if (GetFoldoutState(AssetDatabase.GetAssetPath(dialogue)))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(5);

                SerializedObject serializedDialogue = new SerializedObject(dialogue);
                serializedDialogue.Update();

                // Display lines
                SerializedProperty linesProperty = serializedDialogue.FindProperty("lines");
                if (linesProperty != null && linesProperty.isArray)
                {
                    EditorGUILayout.LabelField($"Dialogue Lines ({linesProperty.arraySize})", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;

                    for (int i = 0; i < linesProperty.arraySize; i++)
                    {
                        SerializedProperty lineProperty = linesProperty.GetArrayElementAtIndex(i);

                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        {
                            EditorGUILayout.BeginHorizontal();
                            {
                                EditorGUILayout.LabelField($"Line {i + 1}", EditorStyles.boldLabel, GUILayout.Width(60));

                                // Speaker with color
                                SerializedProperty speakerProp = lineProperty.FindPropertyRelative("speaker");
                                Speaker speaker = (Speaker)speakerProp.enumValueIndex;

                                GUI.color = speaker == Speaker.MC ? new Color(0.5f, 0.8f, 1f) : new Color(1f, 0.6f, 0.8f);
                                EditorGUILayout.PropertyField(speakerProp, GUIContent.none, GUILayout.Width(80));
                                GUI.color = Color.white;

                                GUILayout.FlexibleSpace();

                                // Remove button
                                if (GUILayout.Button("X", GUILayout.Width(25)))
                                {
                                    linesProperty.DeleteArrayElementAtIndex(i);
                                    serializedDialogue.ApplyModifiedProperties();
                                    break;
                                }
                            }
                            EditorGUILayout.EndHorizontal();

                            // Text
                            SerializedProperty textProp = lineProperty.FindPropertyRelative("text");
                            EditorGUILayout.PropertyField(textProp, GUIContent.none);

                            // Voice clip and speed
                            EditorGUILayout.BeginHorizontal();
                            {
                                SerializedProperty voiceClipProp = lineProperty.FindPropertyRelative("voiceClip");
                                EditorGUILayout.PropertyField(voiceClipProp, new GUIContent("Voice"), GUILayout.Width(300));

                                SerializedProperty speedProp = lineProperty.FindPropertyRelative("charactersPerSecond");
                                EditorGUILayout.PropertyField(speedProp, new GUIContent("Speed"), GUILayout.Width(150));
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                        EditorGUILayout.EndVertical();
                        EditorGUILayout.Space(3);
                    }

                    // Add line button
                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("+ Add Line", GUILayout.Width(100)))
                        {
                            linesProperty.arraySize++;
                            serializedDialogue.ApplyModifiedProperties();
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space(10);

                // Display choices
                SerializedProperty choicesProperty = serializedDialogue.FindProperty("choices");
                if (choicesProperty != null && choicesProperty.isArray)
                {
                    EditorGUILayout.LabelField($"Choices ({choicesProperty.arraySize})", EditorStyles.boldLabel);

                    if (choicesProperty.arraySize > 0)
                    {
                        EditorGUI.indentLevel++;

                        for (int i = 0; i < choicesProperty.arraySize; i++)
                        {
                            SerializedProperty choiceProperty = choicesProperty.GetArrayElementAtIndex(i);

                            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                            {
                                EditorGUILayout.BeginHorizontal();
                                {
                                    EditorGUILayout.LabelField($"Choice {i + 1}", EditorStyles.boldLabel, GUILayout.Width(70));

                                    GUILayout.FlexibleSpace();

                                    // Remove button
                                    if (GUILayout.Button("X", GUILayout.Width(25)))
                                    {
                                        choicesProperty.DeleteArrayElementAtIndex(i);
                                        serializedDialogue.ApplyModifiedProperties();
                                        break;
                                    }
                                }
                                EditorGUILayout.EndHorizontal();

                                SerializedProperty choiceTextProp = choiceProperty.FindPropertyRelative("choiceText");
                                EditorGUILayout.PropertyField(choiceTextProp, new GUIContent("Text"));

                                EditorGUILayout.BeginHorizontal();
                                {
                                    SerializedProperty affectionProp = choiceProperty.FindPropertyRelative("affectionChange");
                                    EditorGUILayout.PropertyField(affectionProp, new GUIContent("Affection"), GUILayout.Width(250));

                                    SerializedProperty choiceNextDialogueProp = choiceProperty.FindPropertyRelative("nextDialogue");
                                    EditorGUILayout.PropertyField(choiceNextDialogueProp, new GUIContent("Next Dialogue"));
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                            EditorGUILayout.EndVertical();
                            EditorGUILayout.Space(3);
                        }

                        EditorGUI.indentLevel--;
                    }

                    // Add choice button
                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("+ Add Choice", GUILayout.Width(100)))
                        {
                            choicesProperty.arraySize++;
                            serializedDialogue.ApplyModifiedProperties();
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space(10);

                // Next dialogue (if no choices)
                SerializedProperty nextDialogueProp = serializedDialogue.FindProperty("nextDialogue");
                EditorGUILayout.PropertyField(nextDialogueProp, new GUIContent("Auto-Continue To"));

                // Show chain visualization
                if (dialogue.nextDialogue != null)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Chain:", EditorStyles.miniLabel, GUILayout.Width(50));

                        DialogueData current = dialogue.nextDialogue;
                        int chainLength = 1;
                        int maxDisplay = 5;

                        while (current != null && chainLength <= maxDisplay)
                        {
                            if (GUILayout.Button(current.name, EditorStyles.miniButton, GUILayout.Width(120)))
                            {
                                Selection.activeObject = current;
                                EditorGUIUtility.PingObject(current);
                            }

                            if (current.nextDialogue != null && chainLength < maxDisplay)
                            {
                                EditorGUILayout.LabelField("→", EditorStyles.miniLabel, GUILayout.Width(20));
                            }

                            current = current.nextDialogue;
                            chainLength++;
                        }

                        if (current != null)
                        {
                            EditorGUILayout.LabelField("...", EditorStyles.miniLabel);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }

                serializedDialogue.ApplyModifiedProperties();
                EditorGUI.indentLevel--;
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void RefreshDatabase()
    {
        // Find all DialogueData assets
        string[] dialogueGuids = AssetDatabase.FindAssets("t:DialogueData");
        dialogues = dialogueGuids
            .Select(guid => AssetDatabase.LoadAssetAtPath<DialogueData>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(dialogue => dialogue != null)
            .ToList();

        Debug.Log($"[Dialogue Editor] Loaded {dialogues.Count} Dialogues");
        Repaint();
    }

    private void CreateNew()
    {
        // Ensure folder exists
        if (!AssetDatabase.IsValidFolder(dialogueFolderPath))
        {
            // Try to create the folder path
            string[] folders = dialogueFolderPath.Split('/');
            string currentPath = folders[0]; // "Assets"

            for (int i = 1; i < folders.Length; i++)
            {
                string nextPath = currentPath + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath = nextPath;
            }
        }

        // Create new DialogueData
        DialogueData newDialogue = CreateInstance<DialogueData>();

        // Auto-generate filename
        string fileName = "NewDialogue";
        string path = AssetDatabase.GenerateUniqueAssetPath($"{dialogueFolderPath}/{fileName}.asset");

        AssetDatabase.CreateAsset(newDialogue, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        RefreshDatabase();
        Selection.activeObject = newDialogue;
        EditorGUIUtility.PingObject(newDialogue);

        // Auto-expand the new dialogue
        SetFoldoutState(path, true);

        Debug.Log($"[Dialogue Editor] Created Dialogue at: {path}");
    }

    private void DuplicateDialogue(DialogueData original)
    {
        DialogueData duplicate = Instantiate(original);

        string originalPath = AssetDatabase.GetAssetPath(original);
        string directory = System.IO.Path.GetDirectoryName(originalPath);
        string extension = System.IO.Path.GetExtension(originalPath);
        string fileName = System.IO.Path.GetFileNameWithoutExtension(originalPath);
        string newPath = AssetDatabase.GenerateUniqueAssetPath($"{directory}/{fileName}_Copy{extension}");

        AssetDatabase.CreateAsset(duplicate, newPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        RefreshDatabase();
        Selection.activeObject = duplicate;
        EditorGUIUtility.PingObject(duplicate);
    }

    private bool PassesSearchFilter(DialogueData dialogue)
    {
        if (string.IsNullOrEmpty(searchQuery)) return true;

        string query = searchQuery.ToLower();

        // Search in dialogue name
        if (dialogue.name.ToLower().Contains(query))
        {
            return true;
        }

        // Search in line text
        if (dialogue.lines != null)
        {
            foreach (var line in dialogue.lines)
            {
                if (line.text != null && line.text.ToLower().Contains(query))
                {
                    return true;
                }
            }
        }

        // Search in choice text
        if (dialogue.choices != null)
        {
            foreach (var choice in dialogue.choices)
            {
                if (choice.choiceText != null && choice.choiceText.ToLower().Contains(query))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool GetFoldoutState(string key)
    {
        if (!foldoutStates.ContainsKey(key))
        {
            foldoutStates[key] = false;
        }
        return foldoutStates[key];
    }

    private void SetFoldoutState(string key, bool state)
    {
        foldoutStates[key] = state;
    }
}