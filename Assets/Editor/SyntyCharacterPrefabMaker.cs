using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Character Prefab Maker for Synty Polygon Assets
/// Allows easy mixing and matching of character equipment with live preview
/// Supports: Battle Royale, Fantasy Kingdom, Military, Dungeon, Dungeon Realms, 
/// Modular Fantasy Hero, and Knights packs
/// </summary>
public class SyntyCharacterPrefabMaker : EditorWindow
{
    #region Editor Window Setup
    [MenuItem("Tools/Synty/Character Prefab Maker")]
    public static void ShowWindow()
    {
        var window = GetWindow<SyntyCharacterPrefabMaker>("Character Prefab Maker");
        window.minSize = new Vector2(800, 600);
        window.Show();
    }
    #endregion

    #region Private Fields
    // Base character
    private GameObject baseCharacterPrefab;
    private GameObject previewInstance;

    // Preview camera and rendering
    private Camera previewCamera;
    private RenderTexture previewTexture;
    private GameObject previewRoot;

    // Attachment categories
    private Dictionary<string, List<GameObject>> attachmentsByCategory = new Dictionary<string, List<GameObject>>();
    private Dictionary<string, GameObject> activeAttachments = new Dictionary<string, GameObject>();
    private Dictionary<string, int> selectedIndices = new Dictionary<string, int>();

    // Body part management (for modular characters like Fantasy Hero)
    private Dictionary<string, Transform> bodyPartContainers = new Dictionary<string, Transform>();
    private Dictionary<string, List<GameObject>> bodyPartOptions = new Dictionary<string, List<GameObject>>();
    private Dictionary<string, int> selectedBodyPartIndices = new Dictionary<string, int>();
    private bool hasModularBodyParts = false;

    // Asset loading
    private string[] assetSearchPaths = new string[]
    {
        "Assets/AssetStore/PolygonBattleRoyale",
        "Assets/AssetStore/PolygonFantasyKingdom",
        "Assets/AssetStore/PolygonMilitary",
        "Assets/AssetStore/PolygonDungeon",
        "Assets/AssetStore/PolygonDungeonRealms",
        "Assets/AssetStore/PolygonModularFantasyHero",
        "Assets/AssetStore/PolygonKnights",
        "Assets" // Fallback to search entire Assets folder
    };

    // UI State
    private Vector2 scrollPosition;
    private Vector2 attachmentScrollPosition;
    private bool showAdvancedOptions = false;
    private bool showDebugInfo = true;
    private string newPrefabName = "NewCharacter";
    private string savePath = "Assets/GeneratedCharacters";

    // Preview controls
    private Vector2 previewRotation = new Vector2(10, 45); // Start at front-right angle (10° up, 45° around)
    private float previewZoom = 2.5f;
    private bool isPreviewDragging = false;
    private Vector2 lastMousePosition;

    // Color customization
    private bool enableColorCustomization = false;
    private Color primaryColor = Color.white;
    private Color secondaryColor = Color.white;

    #endregion

    #region Unity Editor Callbacks
    private void OnEnable()
    {
        SetupPreviewScene();
        EditorApplication.update += Update;
    }

    private void OnDisable()
    {
        CleanupPreviewScene();
        EditorApplication.update -= Update;
    }

    private void OnDestroy()
    {
        CleanupPreviewScene();
    }
    #endregion

    #region Preview Scene Setup
    private void SetupPreviewScene()
    {
        // Create preview render texture
        if (previewTexture == null)
        {
            previewTexture = new RenderTexture(512, 512, 24);
            previewTexture.antiAliasing = 4;
        }

        // Create preview root object
        if (previewRoot == null)
        {
            previewRoot = new GameObject("PreviewRoot");
            previewRoot.hideFlags = HideFlags.HideAndDontSave;
        }

        // Create preview camera
        if (previewCamera == null)
        {
            var cameraObj = new GameObject("PreviewCamera");
            cameraObj.hideFlags = HideFlags.HideAndDontSave;
            cameraObj.transform.SetParent(previewRoot.transform);

            previewCamera = cameraObj.AddComponent<Camera>();
            previewCamera.targetTexture = previewTexture;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            previewCamera.orthographic = false;
            previewCamera.fieldOfView = 30;
            previewCamera.nearClipPlane = 0.1f;
            previewCamera.farClipPlane = 100f;

            // Position camera
            previewCamera.transform.position = new Vector3(0, 1.5f, -3);
            previewCamera.transform.LookAt(new Vector3(0, 1, 0));

            // Add light
            var lightObj = new GameObject("PreviewLight");
            lightObj.hideFlags = HideFlags.HideAndDontSave;
            lightObj.transform.SetParent(previewRoot.transform);

            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = Color.white;
            lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
        }
    }

    private void CleanupPreviewScene()
    {
        if (previewInstance != null)
        {
            DestroyImmediate(previewInstance);
            previewInstance = null;
        }

        if (previewCamera != null)
        {
            DestroyImmediate(previewCamera.gameObject);
            previewCamera = null;
        }

        if (previewRoot != null)
        {
            DestroyImmediate(previewRoot);
            previewRoot = null;
        }

        if (previewTexture != null)
        {
            previewTexture.Release();
            DestroyImmediate(previewTexture);
            previewTexture = null;
        }
    }
    #endregion

    #region Main GUI
    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();

        // Left Panel - Controls
        DrawControlPanel();

        // Right Panel - Preview
        DrawPreviewPanel();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawControlPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(400));

        GUILayout.Label("Synty Character Prefab Maker", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Base Character Selection
        DrawBaseCharacterSection();
        EditorGUILayout.Space();

        // Body Parts (for modular characters)
        if (baseCharacterPrefab != null && previewInstance != null)
        {
            if (hasModularBodyParts)
            {
                DrawBodyPartsSection();
                EditorGUILayout.Space();
            }

            // Attachment Categories
            DrawAttachmentsSection();
            EditorGUILayout.Space();

            // Color Customization
            DrawColorCustomizationSection();
            EditorGUILayout.Space();

            // Debug Info
            DrawDebugInfoSection();
            EditorGUILayout.Space();

            // Save Prefab Section
            DrawSavePrefabSection();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawPreviewPanel()
    {
        EditorGUILayout.BeginVertical();

        GUILayout.Label("Preview", EditorStyles.boldLabel);

        if (previewTexture != null)
        {
            // Calculate preview area
            Rect previewRect = GUILayoutUtility.GetRect(400, 400, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            // Handle mouse input for rotation
            HandlePreviewInput(previewRect);

            // Draw preview texture
            EditorGUI.DrawPreviewTexture(previewRect, previewTexture, null, ScaleMode.ScaleToFit);

            // Draw preview controls
            DrawPreviewControls(previewRect);
        }
        else
        {
            EditorGUILayout.HelpBox("Preview not available", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }
    #endregion

    #region Base Character Section
    private void DrawBaseCharacterSection()
    {
        GUILayout.Label("Base Character", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        GameObject newBase = (GameObject)EditorGUILayout.ObjectField(
            "Character Prefab",
            baseCharacterPrefab,
            typeof(GameObject),
            false
        );

        if (EditorGUI.EndChangeCheck() && newBase != baseCharacterPrefab)
        {
            baseCharacterPrefab = newBase;
            RefreshCharacterPreview();
            // Don't auto-scan - let user click the button
            Repaint();
        }

        if (baseCharacterPrefab == null)
        {
            EditorGUILayout.HelpBox(
                "Select a base character prefab from your Synty asset packs.\n\n" +
                "Supported packs:\n" +
                "• Battle Royale\n" +
                "• Fantasy Kingdom\n" +
                "• Military\n" +
                "• Dungeon / Dungeon Realms\n" +
                "• Modular Fantasy Hero\n" +
                "• Knights",
                MessageType.Info
            );
        }
        else
        {
            EditorGUILayout.HelpBox(
                $"Character loaded: {baseCharacterPrefab.name}\n\n" +
                "Click 'Scan for Attachments' below to find equipment,\n" +
                "or save this character as-is without modifications.",
                MessageType.Info
            );
        }

        if (GUILayout.Button("Quick Scan Project for Characters"))
        {
            QuickScanForCharacters();
        }
    }
    #endregion

    #region Attachments Section
    private void DrawAttachmentsSection()
    {
        GUILayout.Label("Equipment & Attachments", EditorStyles.boldLabel);

        if (attachmentsByCategory.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "No attachments found. Click 'Scan for Attachments' to search your project.",
                MessageType.Info
            );

            if (GUILayout.Button("Scan for Attachments"))
            {
                ScanForAttachments();
            }
            return;
        }

        attachmentScrollPosition = EditorGUILayout.BeginScrollView(
            attachmentScrollPosition,
            GUILayout.Height(300)
        );

        foreach (var category in attachmentsByCategory.Keys.OrderBy(k => k))
        {
            DrawAttachmentCategory(category);
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh Attachments"))
        {
            ScanForAttachments();
        }
        if (GUILayout.Button("Clear All"))
        {
            ClearAllAttachments();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawAttachmentCategory(string category)
    {
        var attachments = attachmentsByCategory[category];
        if (attachments.Count == 0) return;

        EditorGUILayout.BeginVertical("box");

        // Category header
        EditorGUILayout.LabelField(category, EditorStyles.boldLabel);

        // Get current selection
        if (!selectedIndices.ContainsKey(category))
        {
            selectedIndices[category] = 0;
        }

        // Create dropdown options
        string[] options = new string[attachments.Count + 1];
        options[0] = "None";
        for (int i = 0; i < attachments.Count; i++)
        {
            options[i + 1] = attachments[i] != null ? attachments[i].name : "Missing";
        }

        EditorGUI.BeginChangeCheck();
        int newIndex = EditorGUILayout.Popup(selectedIndices[category], options);

        if (EditorGUI.EndChangeCheck())
        {
            selectedIndices[category] = newIndex;
            UpdateAttachment(category, newIndex - 1); // -1 because index 0 is "None"
        }

        EditorGUILayout.EndVertical();
    }
    #endregion

    #region Color Customization Section
    private void DrawColorCustomizationSection()
    {
        GUILayout.Label("Color Customization (Experimental)", EditorStyles.boldLabel);

        enableColorCustomization = EditorGUILayout.Toggle("Enable Color Tinting", enableColorCustomization);

        if (enableColorCustomization)
        {
            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();
            primaryColor = EditorGUILayout.ColorField("Primary Color", primaryColor);
            secondaryColor = EditorGUILayout.ColorField("Secondary Color", secondaryColor);

            if (EditorGUI.EndChangeCheck())
            {
                ApplyColorCustomization();
            }

            if (GUILayout.Button("Reset Colors"))
            {
                primaryColor = Color.white;
                secondaryColor = Color.white;
                ApplyColorCustomization();
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.HelpBox(
                "Note: Color customization works best with Synty's shader setup. " +
                "Some assets may not support color changes.",
                MessageType.Info
            );
        }
    }
    #endregion

    #region Debug Info Section
    private void DrawDebugInfoSection()
    {
        showDebugInfo = EditorGUILayout.Foldout(showDebugInfo, "Debug Info (Show Active Parts)", true);

        if (showDebugInfo)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Character Hierarchy Status:", EditorStyles.boldLabel);

            if (previewInstance == null)
            {
                EditorGUILayout.LabelField("No character loaded", EditorStyles.miniLabel);
            }
            else
            {
                // Show active renderers count
                Renderer[] renderers = previewInstance.GetComponentsInChildren<Renderer>(true);
                int activeRenderers = renderers.Count(r => r != null && r.gameObject.activeInHierarchy);
                EditorGUILayout.LabelField($"Active Renderers: {activeRenderers} / {renderers.Length}", EditorStyles.miniLabel);

                // Show body parts status
                if (hasModularBodyParts)
                {
                    EditorGUILayout.LabelField($"Body Part Categories: {bodyPartContainers.Count}", EditorStyles.miniLabel);

                    foreach (var kvp in bodyPartContainers)
                    {
                        string status = "";
                        if (bodyPartOptions.ContainsKey(kvp.Key))
                        {
                            int activeCount = bodyPartOptions[kvp.Key].Count(go => go != null && go.activeSelf);
                            int totalCount = bodyPartOptions[kvp.Key].Count;
                            status = $"{activeCount}/{totalCount} active";
                        }
                        EditorGUILayout.LabelField($"  • {kvp.Key}: {status}", EditorStyles.miniLabel);
                    }
                }

                // Show attachments
                if (activeAttachments.Count > 0)
                {
                    EditorGUILayout.LabelField($"Active Attachments: {activeAttachments.Count}", EditorStyles.miniLabel);
                    foreach (var kvp in activeAttachments)
                    {
                        if (kvp.Value != null)
                        {
                            EditorGUILayout.LabelField($"  • {kvp.Key}: {kvp.Value.name}", EditorStyles.miniLabel);
                        }
                    }
                }
            }

            if (GUILayout.Button("Print Full Hierarchy to Console"))
            {
                PrintHierarchy();
            }

            EditorGUILayout.EndVertical();
        }
    }

    private void PrintHierarchy()
    {
        if (previewInstance == null)
        {
            Debug.Log("No character loaded");
            return;
        }

        Debug.Log("=== CHARACTER HIERARCHY ===");
        PrintTransformHierarchy(previewInstance.transform, 0);
    }

    private void PrintTransformHierarchy(Transform t, int depth)
    {
        string indent = new string(' ', depth * 2);
        string activeStatus = t.gameObject.activeSelf ? "✓" : "✗";
        Renderer renderer = t.GetComponent<Renderer>();
        string renderInfo = renderer != null ? " [HAS RENDERER]" : "";

        Debug.Log($"{indent}{activeStatus} {t.name}{renderInfo}");

        foreach (Transform child in t)
        {
            PrintTransformHierarchy(child, depth + 1);
        }
    }
    #endregion

    #region Save Prefab Section
    private void DrawSavePrefabSection()
    {
        GUILayout.Label("Save Character Prefab", EditorStyles.boldLabel);

        newPrefabName = EditorGUILayout.TextField("Prefab Name", newPrefabName);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Save Path", savePath);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string path = EditorUtility.SaveFolderPanel("Select Save Location", "Assets", "");
            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith(Application.dataPath))
                {
                    savePath = "Assets" + path.Substring(Application.dataPath.Length);
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "Advanced Options");
        if (showAdvancedOptions)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.HelpBox(
                "Advanced options for prefab creation:\n" +
                "• Combine meshes for performance\n" +
                "• Optimize materials\n" +
                "• Add LOD groups (future)",
                MessageType.Info
            );
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        GUI.enabled = previewInstance != null && !string.IsNullOrEmpty(newPrefabName);
        if (GUILayout.Button("Save as New Prefab", GUILayout.Height(30)))
        {
            SaveCharacterPrefab();
        }
        GUI.enabled = true;
    }
    #endregion

    #region Preview Controls
    private void DrawPreviewControls(Rect previewRect)
    {
        Rect controlRect = new Rect(previewRect.x + 5, previewRect.yMax - 80, previewRect.width - 10, 75);

        GUI.Box(controlRect, GUIContent.none, EditorStyles.helpBox);

        GUILayout.BeginArea(new Rect(controlRect.x + 5, controlRect.y + 5, controlRect.width - 10, controlRect.height - 10));

        GUILayout.Label("Preview Controls:", EditorStyles.miniLabel);
        GUILayout.Label("• Left Mouse Drag: Orbit Camera", EditorStyles.miniLabel);
        GUILayout.Label("• Scroll Wheel: Zoom In/Out", EditorStyles.miniLabel);

        if (GUILayout.Button("Reset View", GUILayout.Width(80)))
        {
            previewRotation = new Vector2(10, 45); // Front-right view
            previewZoom = 2.5f;
            UpdatePreviewCamera();
        }

        GUILayout.EndArea();
    }

    private void HandlePreviewInput(Rect previewRect)
    {
        Event e = Event.current;

        if (previewRect.Contains(e.mousePosition))
        {
            // Handle rotation with left mouse drag
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                isPreviewDragging = true;
                lastMousePosition = e.mousePosition;
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && isPreviewDragging)
            {
                Vector2 delta = e.mousePosition - lastMousePosition;
                previewRotation.y += delta.x * 0.5f;
                previewRotation.x -= delta.y * 0.5f;
                previewRotation.x = Mathf.Clamp(previewRotation.x, -89f, 89f);

                lastMousePosition = e.mousePosition;
                UpdatePreviewCamera();
                Repaint();
                e.Use();
            }
            else if (e.type == EventType.MouseUp && e.button == 0)
            {
                isPreviewDragging = false;
                e.Use();
            }

            // Handle zoom with scroll wheel
            if (e.type == EventType.ScrollWheel)
            {
                previewZoom += e.delta.y * 0.1f;
                previewZoom = Mathf.Clamp(previewZoom, 1f, 5f);
                UpdatePreviewCamera();
                Repaint();
                e.Use();
            }
        }
    }

    private void UpdatePreviewCamera()
    {
        if (previewCamera == null || previewInstance == null) return;

        // Character stays at origin, camera orbits around it
        Vector3 characterPosition = Vector3.zero;
        Vector3 lookAtPoint = new Vector3(0, 1f, 0); // Look at character's center (roughly chest height)

        // Convert rotation to radians
        float horizontalAngle = previewRotation.y * Mathf.Deg2Rad;
        float verticalAngle = previewRotation.x * Mathf.Deg2Rad;

        // Calculate camera position on a sphere around the character
        // Using spherical coordinates: x = r*sin(θ)*cos(φ), y = r*sin(φ), z = r*sin(θ)*sin(φ)
        float distance = previewZoom;

        float x = distance * Mathf.Cos(verticalAngle) * Mathf.Sin(horizontalAngle);
        float y = distance * Mathf.Sin(verticalAngle) + 1f; // Offset by 1 to orbit around chest height
        float z = distance * Mathf.Cos(verticalAngle) * Mathf.Cos(horizontalAngle);

        // Position camera on the sphere
        previewCamera.transform.position = characterPosition + new Vector3(x, y, z);

        // Always look at the character's center
        previewCamera.transform.LookAt(lookAtPoint);

        // Keep character facing forward (don't rotate with camera)
        if (previewInstance != null)
        {
            previewInstance.transform.rotation = Quaternion.identity;
        }
    }
    #endregion

    #region Character Management
    private void RefreshCharacterPreview()
    {
        // Cleanup old instance
        if (previewInstance != null)
        {
            DestroyImmediate(previewInstance);
        }

        if (baseCharacterPrefab == null) return;

        // Create new instance
        previewInstance = Instantiate(baseCharacterPrefab);
        previewInstance.hideFlags = HideFlags.HideAndDontSave;
        previewInstance.transform.SetParent(previewRoot.transform);
        previewInstance.transform.localPosition = Vector3.zero;
        previewInstance.transform.localRotation = Quaternion.identity;

        // Set layer for preview camera only
        SetLayerRecursively(previewInstance, 31);

        // Reset attachments
        activeAttachments.Clear();
        selectedIndices.Clear();

        // Detect and setup modular body parts
        DetectModularBodyParts();

        UpdatePreviewCamera();
        Repaint();
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
    #endregion

    #region Body Parts Section
    private void DrawBodyPartsSection()
    {
        GUILayout.Label("Body Parts (Modular Character)", EditorStyles.boldLabel);

        if (bodyPartContainers.Count == 0)
        {
            EditorGUILayout.HelpBox("No modular body parts detected.", MessageType.Info);
            return;
        }

        EditorGUILayout.BeginVertical("box");

        foreach (var kvp in bodyPartContainers.OrderBy(k => k.Key))
        {
            string partName = kvp.Key;
            Transform container = kvp.Value;

            if (!bodyPartOptions.ContainsKey(partName) || bodyPartOptions[partName].Count == 0)
                continue;

            EditorGUILayout.BeginHorizontal();

            // Part name
            EditorGUILayout.LabelField(partName, GUILayout.Width(150));

            // Dropdown for options
            if (!selectedBodyPartIndices.ContainsKey(partName))
            {
                selectedBodyPartIndices[partName] = FindActiveBodyPartIndex(partName);
            }

            string[] options = bodyPartOptions[partName]
                .Select(go => go != null ? go.name : "Missing")
                .ToArray();

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUILayout.Popup(selectedBodyPartIndices[partName], options);

            if (EditorGUI.EndChangeCheck())
            {
                selectedBodyPartIndices[partName] = newIndex;
                SetActiveBodyPart(partName, newIndex);
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();

        if (GUILayout.Button("Randomize Body Parts"))
        {
            RandomizeBodyParts();
        }
    }

    private void DetectModularBodyParts()
    {
        bodyPartContainers.Clear();
        bodyPartOptions.Clear();
        selectedBodyPartIndices.Clear();
        hasModularBodyParts = false;

        if (previewInstance == null) return;

        Debug.Log("=== Scanning Character Hierarchy ===");

        // Recursively scan the entire character hierarchy for parent objects with multiple children
        ScanForBodyPartContainers(previewInstance.transform, 0);

        if (bodyPartContainers.Count > 0)
        {
            hasModularBodyParts = true;
            Debug.Log($"✓ Detected modular character! Found {bodyPartContainers.Count} body part categories:");

            foreach (var kvp in bodyPartContainers)
            {
                int activeIndex = FindActiveBodyPartIndex(kvp.Key);
                Debug.Log($"  - {kvp.Key}: {bodyPartOptions[kvp.Key].Count} options (active: {activeIndex})");
            }
        }
        else
        {
            Debug.Log("✗ No modular body parts detected. Character may have single combined mesh.");
        }
    }

    private void ScanForBodyPartContainers(Transform parent, int depth)
    {
        // Safety check - don't scan too deep
        if (depth > 10) return;

        // Check each child of this parent
        foreach (Transform child in parent)
        {
            // If this child has multiple children that are GameObjects with renderers,
            // it's likely a body part container
            if (child.childCount > 1)
            {
                List<GameObject> potentialOptions = new List<GameObject>();
                bool hasRenderers = false;

                foreach (Transform grandchild in child)
                {
                    // Check if grandchild has a renderer (mesh)
                    if (grandchild.GetComponent<Renderer>() != null ||
                        grandchild.GetComponentInChildren<Renderer>() != null)
                    {
                        potentialOptions.Add(grandchild.gameObject);
                        hasRenderers = true;
                    }
                }

                // If we found multiple children with renderers, this is a body part container
                if (hasRenderers && potentialOptions.Count > 1)
                {
                    string containerName = child.name;

                    // Avoid duplicates
                    if (!bodyPartContainers.ContainsKey(containerName))
                    {
                        bodyPartContainers[containerName] = child;
                        bodyPartOptions[containerName] = potentialOptions;

                        Debug.Log($"  Found container: {containerName} with {potentialOptions.Count} options");
                    }
                }
            }

            // Recursively scan children
            ScanForBodyPartContainers(child, depth + 1);
        }
    }

    private int FindActiveBodyPartIndex(string partName)
    {
        if (!bodyPartOptions.ContainsKey(partName)) return 0;

        var options = bodyPartOptions[partName];
        for (int i = 0; i < options.Count; i++)
        {
            if (options[i] != null && options[i].activeSelf)
            {
                return i;
            }
        }

        // If none are active, activate the first one
        if (options.Count > 0 && options[0] != null)
        {
            options[0].SetActive(true);
            return 0;
        }

        return 0;
    }

    private void SetActiveBodyPart(string partName, int index)
    {
        if (!bodyPartOptions.ContainsKey(partName)) return;

        var options = bodyPartOptions[partName];

        Debug.Log($"Setting {partName} to option {index} ({(index < options.Count && options[index] != null ? options[index].name : "null")})");

        // Disable all options
        foreach (var option in options)
        {
            if (option != null)
            {
                option.SetActive(false);
            }
        }

        // Enable selected option
        if (index >= 0 && index < options.Count && options[index] != null)
        {
            options[index].SetActive(true);
            Debug.Log($"  ✓ Enabled: {options[index].name}");
        }

        // Force preview refresh
        if (previewCamera != null)
        {
            previewCamera.Render();
        }

        Repaint();
    }

    private void RandomizeBodyParts()
    {
        System.Random random = new System.Random();

        foreach (var partName in bodyPartOptions.Keys)
        {
            int randomIndex = random.Next(0, bodyPartOptions[partName].Count);
            selectedBodyPartIndices[partName] = randomIndex;
            SetActiveBodyPart(partName, randomIndex);
        }

        Debug.Log("Randomized all body parts");
        Repaint();
    }
    #endregion

    #region Attachment Management
    private void ScanForAttachments()
    {
        if (baseCharacterPrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a base character first.", "OK");
            return;
        }

        try
        {
            attachmentsByCategory.Clear();

            // Define common attachment categories for Synty packs
            string[] categories = new string[]
            {
                "Helmet", "Hat", "Hair", "Beard", "Mask", "Goggles", "Glasses",
                "Chest", "Torso", "Armor", "Cape", "Backpack",
                "Shoulder", "Elbow", "Gloves", "Hands",
                "Belt", "Hip", "Legs", "Knee", "Boots",
                "Weapon", "Shield", "Sword", "Bow", "Staff", "Gun", "Rifle",
                "Accessory", "Attachment", "Prop"
            };

            foreach (string category in categories)
            {
                attachmentsByCategory[category] = new List<GameObject>();
            }

            // Get the base character's directory to search nearby
            string basePath = AssetDatabase.GetAssetPath(baseCharacterPrefab);
            string baseDirectory = System.IO.Path.GetDirectoryName(basePath);

            // Prioritize searching in the same pack as the character
            List<string> searchPaths = new List<string> { baseDirectory };

            // Add parent directories up to 3 levels
            string parentDir = baseDirectory;
            for (int i = 0; i < 3; i++)
            {
                parentDir = System.IO.Path.GetDirectoryName(parentDir);
                if (!string.IsNullOrEmpty(parentDir) && parentDir.StartsWith("Assets"))
                {
                    searchPaths.Add(parentDir);
                }
            }

            EditorUtility.DisplayProgressBar("Scanning", "Searching for attachment prefabs...", 0f);

            int totalPaths = searchPaths.Count;
            int currentPath = 0;

            // Search for attachment prefabs
            foreach (string searchPath in searchPaths)
            {
                currentPath++;
                EditorUtility.DisplayProgressBar(
                    "Scanning",
                    $"Searching: {searchPath}",
                    (float)currentPath / totalPaths
                );

                if (!AssetDatabase.IsValidFolder(searchPath))
                    continue;

                string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { searchPath });

                int totalGuids = guids.Length;
                int processedGuids = 0;

                foreach (string guid in guids)
                {
                    processedGuids++;

                    // Update progress bar less frequently (every 50 assets)
                    if (processedGuids % 50 == 0)
                    {
                        EditorUtility.DisplayProgressBar(
                            "Scanning",
                            $"Processing prefabs... {processedGuids}/{totalGuids}",
                            (float)processedGuids / totalGuids
                        );
                    }

                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                    // Skip character files themselves
                    string lowerPath = assetPath.ToLower();
                    if (lowerPath.Contains("character_") ||
                        lowerPath.Contains("chr_") ||
                        lowerPath.Contains("/characters/") ||
                        lowerPath.Contains("_parts"))
                        continue;

                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                    if (prefab == null) continue;

                    // Only process prefabs that are likely attachments
                    // They should be relatively small (not full characters)
                    int transformCount = prefab.GetComponentsInChildren<Transform>(true).Length;
                    if (transformCount > 50)
                        continue;

                    // Check if it has renderers (visible objects)
                    if (prefab.GetComponentInChildren<Renderer>() == null)
                        continue;

                    // Categorize based on name and path
                    string prefabName = prefab.name.ToLower();
                    string pathLower = assetPath.ToLower();

                    foreach (string category in categories)
                    {
                        string catLower = category.ToLower();
                        if (prefabName.Contains(catLower) || pathLower.Contains("/" + catLower))
                        {
                            if (!attachmentsByCategory[category].Contains(prefab))
                            {
                                attachmentsByCategory[category].Add(prefab);
                            }
                            break;
                        }
                    }
                }
            }

            EditorUtility.ClearProgressBar();

            // Remove empty categories
            var emptyCategories = attachmentsByCategory.Where(kvp => kvp.Value.Count == 0)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var category in emptyCategories)
            {
                attachmentsByCategory.Remove(category);
            }

            int totalAttachments = attachmentsByCategory.Sum(kvp => kvp.Value.Count);

            Debug.Log($"Scan complete! Found {totalAttachments} attachment prefabs in {attachmentsByCategory.Count} categories");

            if (totalAttachments == 0)
            {
                EditorUtility.DisplayDialog(
                    "No Attachments Found",
                    $"No attachment prefabs found near the selected character.\n\n" +
                    $"Searched in: {baseDirectory}\n\n" +
                    "Try:\n" +
                    "1. Selecting a character from a different pack\n" +
                    "2. Checking if your pack includes separate attachment prefabs\n" +
                    "3. Using the Modular Fantasy Hero pack (720 modular pieces)",
                    "OK"
                );
            }
        }
        catch (System.Exception ex)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"Error during attachment scan: {ex.Message}\n{ex.StackTrace}");
            EditorUtility.DisplayDialog("Scan Error", $"An error occurred: {ex.Message}", "OK");
        }
    }

    private void UpdateAttachment(string category, int attachmentIndex)
    {
        if (previewInstance == null) return;

        // Remove existing attachment in this category
        if (activeAttachments.ContainsKey(category))
        {
            if (activeAttachments[category] != null)
            {
                DestroyImmediate(activeAttachments[category]);
            }
            activeAttachments.Remove(category);
        }

        // Add new attachment if index is valid
        if (attachmentIndex >= 0 && attachmentIndex < attachmentsByCategory[category].Count)
        {
            GameObject attachmentPrefab = attachmentsByCategory[category][attachmentIndex];
            if (attachmentPrefab != null)
            {
                AttachEquipmentPrefab(attachmentPrefab, category);
            }
        }

        Repaint();
    }

    private void AttachEquipmentPrefab(GameObject equipmentPrefab, string category)
    {
        if (previewInstance == null) return;

        // Find appropriate attachment point in the character hierarchy
        Transform attachPoint = FindAttachmentPoint(category);

        if (attachPoint == null)
        {
            Debug.LogWarning($"Could not find attachment point for {category}. Trying fallback points...");

            // Try generic fallbacks
            attachPoint = FindChildRecursive(previewInstance.transform, "Root");
            if (attachPoint == null)
                attachPoint = previewInstance.transform;
        }

        // Instantiate the equipment prefab
        GameObject equipment = (GameObject)PrefabUtility.InstantiatePrefab(equipmentPrefab);

        if (equipment == null)
        {
            Debug.LogError($"Failed to instantiate prefab: {equipmentPrefab.name}");
            return;
        }

        equipment.hideFlags = HideFlags.HideAndDontSave;
        equipment.transform.SetParent(attachPoint, false);
        equipment.transform.localPosition = Vector3.zero;
        equipment.transform.localRotation = Quaternion.identity;
        equipment.transform.localScale = Vector3.one;

        // Set layer for preview
        SetLayerRecursively(equipment, 31);

        activeAttachments[category] = equipment;

        Debug.Log($"Attached {equipmentPrefab.name} to {attachPoint.name}");
    }

    private Transform FindAttachmentPoint(string category)
    {
        if (previewInstance == null) return null;

        // Common bone names in Synty rigs
        Dictionary<string, string[]> boneMapping = new Dictionary<string, string[]>
        {
            { "Helmet", new[] { "Head", "head", "Bip001 Head" } },
            { "Hat", new[] { "Head", "head", "Bip001 Head" } },
            { "Hair", new[] { "Head", "head", "Bip001 Head" } },
            { "Beard", new[] { "Head", "head", "Bip001 Head" } },
            { "Mask", new[] { "Head", "head", "Bip001 Head" } },
            { "Chest", new[] { "Spine2", "Spine_02", "Bip001 Spine2" } },
            { "Backpack", new[] { "Spine2", "Spine_02", "Bip001 Spine2" } },
            { "Shoulder", new[] { "Shoulder_L", "LeftShoulder", "Bip001 L Shoulder" } },
            { "Weapon", new[] { "Hand_R", "RightHand", "Bip001 R Hand" } },
            { "Shield", new[] { "Hand_L", "LeftHand", "Bip001 L Hand" } },
            { "Hip", new[] { "Hips", "Root", "Bip001 Pelvis" } },
        };

        string[] possibleBones = boneMapping.ContainsKey(category)
            ? boneMapping[category]
            : new[] { "Root", "Hips", "Bip001" };

        // Search for bones
        foreach (string boneName in possibleBones)
        {
            Transform bone = FindChildRecursive(previewInstance.transform, boneName);
            if (bone != null) return bone;
        }

        // Default to root
        return previewInstance.transform;
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
            return parent;

        foreach (Transform child in parent)
        {
            Transform result = FindChildRecursive(child, name);
            if (result != null) return result;
        }

        return null;
    }

    private void ClearAllAttachments()
    {
        foreach (var attachment in activeAttachments.Values)
        {
            if (attachment != null)
            {
                DestroyImmediate(attachment);
            }
        }

        activeAttachments.Clear();
        selectedIndices.Clear();
        Repaint();
    }
    #endregion

    #region Color Customization
    private void ApplyColorCustomization()
    {
        if (!enableColorCustomization || previewInstance == null) return;

        // Find all renderers in preview instance
        Renderer[] renderers = previewInstance.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            // Create material instance
            Material[] materials = renderer.materials;

            for (int i = 0; i < materials.Length; i++)
            {
                // Check if material has color properties
                if (materials[i].HasProperty("_Color"))
                {
                    materials[i].SetColor("_Color", primaryColor);
                }
                if (materials[i].HasProperty("_ColorTint"))
                {
                    materials[i].SetColor("_ColorTint", primaryColor);
                }
            }

            renderer.materials = materials;
        }
    }
    #endregion

    #region Prefab Saving
    private void SaveCharacterPrefab()
    {
        if (previewInstance == null)
        {
            EditorUtility.DisplayDialog("Error", "No character to save!", "OK");
            return;
        }

        if (string.IsNullOrEmpty(newPrefabName))
        {
            EditorUtility.DisplayDialog("Error", "Please enter a prefab name!", "OK");
            return;
        }

        // Ensure save directory exists
        if (!AssetDatabase.IsValidFolder(savePath))
        {
            string[] folders = savePath.Split('/');
            string currentPath = folders[0];

            for (int i = 1; i < folders.Length; i++)
            {
                string newPath = currentPath + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(newPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath = newPath;
            }
        }

        // Create a scene instance (not the preview instance)
        GameObject characterInstance = Instantiate(baseCharacterPrefab);
        characterInstance.name = newPrefabName;

        // Apply body part selections if modular character
        if (hasModularBodyParts)
        {
            ApplyBodyPartSelectionsToInstance(characterInstance);
        }

        // Reattach all active attachments
        foreach (var kvp in activeAttachments)
        {
            string category = kvp.Key;
            GameObject sourceAttachment = kvp.Value;

            if (sourceAttachment == null) continue;

            // Find source prefab
            int index = selectedIndices[category] - 1;
            if (index >= 0 && index < attachmentsByCategory[category].Count)
            {
                GameObject attachmentPrefab = attachmentsByCategory[category][index];

                // Find attachment point in new instance
                Transform attachPoint = FindAttachmentPointInInstance(characterInstance.transform, category);

                if (attachPoint != null && attachmentPrefab != null)
                {
                    GameObject newAttachment = Instantiate(attachmentPrefab);
                    newAttachment.transform.SetParent(attachPoint);
                    newAttachment.transform.localPosition = Vector3.zero;
                    newAttachment.transform.localRotation = Quaternion.identity;
                    newAttachment.transform.localScale = Vector3.one;
                }
            }
        }

        // Apply color customization if enabled
        if (enableColorCustomization)
        {
            Renderer[] renderers = characterInstance.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    // Create material instance to avoid modifying source materials
                    Material newMat = new Material(materials[i]);

                    if (newMat.HasProperty("_Color"))
                        newMat.SetColor("_Color", primaryColor);
                    if (newMat.HasProperty("_ColorTint"))
                        newMat.SetColor("_ColorTint", primaryColor);

                    materials[i] = newMat;
                }
                renderer.sharedMaterials = materials;
            }
        }

        // Save as prefab
        string prefabPath = $"{savePath}/{newPrefabName}.prefab";
        prefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(characterInstance, prefabPath);

        // Cleanup temporary instance
        DestroyImmediate(characterInstance);

        if (savedPrefab != null)
        {
            EditorUtility.DisplayDialog(
                "Success",
                $"Character prefab saved to:\n{prefabPath}",
                "OK"
            );

            // Select the new prefab
            Selection.activeObject = savedPrefab;
            EditorGUIUtility.PingObject(savedPrefab);
        }
        else
        {
            EditorUtility.DisplayDialog("Error", "Failed to save prefab!", "OK");
        }
    }

    private void ApplyBodyPartSelectionsToInstance(GameObject instance)
    {
        foreach (var kvp in selectedBodyPartIndices)
        {
            string partName = kvp.Key;
            int selectedIndex = kvp.Value;

            // Find the body part container in the new instance
            Transform partContainer = FindChildRecursive(instance.transform, partName);

            if (partContainer == null) continue;

            // Disable all children
            foreach (Transform child in partContainer)
            {
                child.gameObject.SetActive(false);
            }

            // Enable the selected one
            if (selectedIndex >= 0 && selectedIndex < partContainer.childCount)
            {
                partContainer.GetChild(selectedIndex).gameObject.SetActive(true);
            }
        }
    }

    private Transform FindAttachmentPointInInstance(Transform root, string category)
    {
        // Same logic as FindAttachmentPoint but for a different instance
        Dictionary<string, string[]> boneMapping = new Dictionary<string, string[]>
        {
            { "Helmet", new[] { "Head", "head", "Bip001 Head" } },
            { "Hat", new[] { "Head", "head", "Bip001 Head" } },
            { "Hair", new[] { "Head", "head", "Bip001 Head" } },
            { "Beard", new[] { "Head", "head", "Bip001 Head" } },
            { "Mask", new[] { "Head", "head", "Bip001 Head" } },
            { "Chest", new[] { "Spine2", "Spine_02", "Bip001 Spine2" } },
            { "Backpack", new[] { "Spine2", "Spine_02", "Bip001 Spine2" } },
            { "Shoulder", new[] { "Shoulder_L", "LeftShoulder", "Bip001 L Shoulder" } },
            { "Weapon", new[] { "Hand_R", "RightHand", "Bip001 R Hand" } },
            { "Shield", new[] { "Hand_L", "LeftHand", "Bip001 L Hand" } },
            { "Hip", new[] { "Hips", "Root", "Bip001 Pelvis" } },
        };

        string[] possibleBones = boneMapping.ContainsKey(category)
            ? boneMapping[category]
            : new[] { "Root", "Hips", "Bip001" };

        foreach (string boneName in possibleBones)
        {
            Transform bone = FindChildRecursive(root, boneName);
            if (bone != null) return bone;
        }

        return root;
    }
    #endregion

    #region Utility Functions
    private void QuickScanForCharacters()
    {
        EditorUtility.DisplayProgressBar("Scanning", "Looking for Synty characters...", 0f);

        List<GameObject> characters = new List<GameObject>();

        try
        {
            // Search in common character folder names
            string[] characterFolderNames = new string[]
            {
                "Characters", "Character", "Prefabs/Characters", "Models/Characters"
            };

            // First, try to find character folders
            string[] allFolders = AssetDatabase.GetSubFolders("Assets");
            List<string> searchFolders = new List<string>();

            foreach (string folder in allFolders)
            {
                foreach (string charFolder in characterFolderNames)
                {
                    string possiblePath = folder + "/" + charFolder;
                    if (AssetDatabase.IsValidFolder(possiblePath))
                    {
                        searchFolders.Add(possiblePath);
                    }
                }
            }

            // If no character folders found, search in asset packs directly
            if (searchFolders.Count == 0)
            {
                foreach (string searchPath in assetSearchPaths)
                {
                    if (AssetDatabase.IsValidFolder(searchPath))
                    {
                        searchFolders.Add(searchPath);
                    }
                }
            }

            int totalFolders = searchFolders.Count;
            int currentFolder = 0;

            foreach (string searchPath in searchFolders)
            {
                currentFolder++;
                EditorUtility.DisplayProgressBar(
                    "Scanning",
                    $"Searching: {System.IO.Path.GetFileName(searchPath)}",
                    (float)currentFolder / totalFolders
                );

                if (!AssetDatabase.IsValidFolder(searchPath)) continue;

                string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { searchPath });

                foreach (string guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                    // Look for character prefabs (typically contain "Character", "Chr_", "Char_")
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath).ToLower();

                    if (fileName.Contains("character") ||
                        fileName.Contains("chr_") ||
                        fileName.Contains("char_") ||
                        fileName.StartsWith("character"))
                    {
                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                        if (prefab != null)
                        {
                            // Check if it has a skinned mesh renderer (typical for characters)
                            if (prefab.GetComponentInChildren<SkinnedMeshRenderer>() != null)
                            {
                                characters.Add(prefab);

                                // Limit to prevent overwhelming the menu
                                if (characters.Count >= 50)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }

                if (characters.Count >= 50)
                    break;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error during character scan: {ex.Message}");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        if (characters.Count > 0)
        {
            // Show selection window
            GenericMenu menu = new GenericMenu();

            foreach (var character in characters)
            {
                GameObject c = character;
                string menuPath = c.name;

                // Add pack name if we can determine it
                string assetPath = AssetDatabase.GetAssetPath(c);
                string[] pathParts = assetPath.Split('/');
                if (pathParts.Length > 1 && pathParts[1].Contains("Polygon"))
                {
                    menuPath = pathParts[1] + "/" + c.name;
                }

                menu.AddItem(new GUIContent(menuPath), false, () => {
                    baseCharacterPrefab = c;
                    RefreshCharacterPreview();

                    // Ask user if they want to scan for attachments
                    if (EditorUtility.DisplayDialog(
                        "Character Loaded",
                        $"Character '{c.name}' loaded successfully!\n\n" +
                        "Would you like to scan for attachments now?\n" +
                        "(You can also click 'Scan for Attachments' later)",
                        "Yes, Scan Now",
                        "Skip for Now"))
                    {
                        ScanForAttachments();
                    }
                });
            }

            menu.ShowAsContext();
        }
        else
        {
            EditorUtility.DisplayDialog(
                "No Characters Found",
                "No Synty character prefabs found in the project.\n\n" +
                "Make sure you have imported one of the supported Synty packs.\n\n" +
                "Supported packs:\n" +
                "• Battle Royale\n" +
                "• Fantasy Kingdom\n" +
                "• Military\n" +
                "• Dungeon / Dungeon Realms\n" +
                "• Modular Fantasy Hero\n" +
                "• Knights",
                "OK"
            );
        }
    }

    private void Update()
    {
        // Render preview camera every frame
        if (previewCamera != null && previewTexture != null)
        {
            previewCamera.Render();
        }
    }
    #endregion
}