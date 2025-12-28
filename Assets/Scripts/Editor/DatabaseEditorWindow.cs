using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Database editor for Shop Items and Arsenal Unlocks.
/// Window → Admin Privileges → Database Editor
/// </summary>
public class DatabaseEditorWindow : EditorWindow
{
    private enum Tab { ShopItems, ArsenalUnlocks }
    private Tab currentTab = Tab.ShopItems;

    // Data
    private List<ShopItem> shopItems = new List<ShopItem>();
    private List<ArsenalUnlock> arsenalUnlocks = new List<ArsenalUnlock>();

    // UI State
    private Vector2 scrollPosition;
    private string searchQuery = "";
    private ShopItemType shopItemTypeFilter = (ShopItemType)(-1); // -1 = All
    private UnlockType unlockTypeFilter = (UnlockType)(-1); // -1 = All

    // Persistent settings (stored in EditorPrefs)
    private string shopItemFolderPath = "Assets/ScriptableObjects/ShopItems";
    private string arsenalUnlockFolderPath = "Assets/ScriptableObjects/ArsenalUnlocks";
    private const string SHOP_ITEM_FOLDER_KEY = "DatabaseEditor_ShopItemFolder";
    private const string ARSENAL_UNLOCK_FOLDER_KEY = "DatabaseEditor_ArsenalUnlockFolder";

    // Foldout state
    private Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();

    // Styles
    private GUIStyle headerStyle;
    private GUIStyle itemBoxStyle;
    private bool stylesInitialized = false;

    [MenuItem("Window/Admin Privileges/Database Editor")]
    public static void ShowWindow()
    {
        DatabaseEditorWindow window = GetWindow<DatabaseEditorWindow>("Database Editor");
        window.minSize = new Vector2(600, 400);
        window.Show();
    }

    private void OnEnable()
    {
        // Load saved folder paths
        shopItemFolderPath = EditorPrefs.GetString(SHOP_ITEM_FOLDER_KEY, "Assets/ScriptableObjects/ShopItems");
        arsenalUnlockFolderPath = EditorPrefs.GetString(ARSENAL_UNLOCK_FOLDER_KEY, "Assets/ScriptableObjects/ArsenalUnlocks");

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

        stylesInitialized = true;
    }

    private void OnGUI()
    {
        InitializeStyles();

        // Header
        EditorGUILayout.Space(10);
        GUILayout.Label("Admin Privileges - Database Editor", headerStyle);
        EditorGUILayout.Space(5);

        // Tab buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Toggle(currentTab == Tab.ShopItems, "Shop Items", EditorStyles.toolbarButton))
        {
            currentTab = Tab.ShopItems;
        }
        if (GUILayout.Toggle(currentTab == Tab.ArsenalUnlocks, "Arsenal Unlocks", EditorStyles.toolbarButton))
        {
            currentTab = Tab.ArsenalUnlocks;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Toolbar
        DrawToolbar();

        EditorGUILayout.Space(5);

        // Content
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        {
            if (currentTab == Tab.ShopItems)
            {
                DrawShopItems();
            }
            else
            {
                DrawArsenalUnlocks();
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        {
            // Search bar
            GUILayout.Label("Search:", GUILayout.Width(60));
            searchQuery = EditorGUILayout.TextField(searchQuery, EditorStyles.toolbarSearchField, GUILayout.Width(200));

            GUILayout.FlexibleSpace();

            // Type filter
            if (currentTab == Tab.ShopItems)
            {
                GUILayout.Label("Type:", GUILayout.Width(40));
                shopItemTypeFilter = (ShopItemType)EditorGUILayout.EnumPopup(shopItemTypeFilter, EditorStyles.toolbarDropDown, GUILayout.Width(150));
            }
            else
            {
                GUILayout.Label("Type:", GUILayout.Width(40));
                unlockTypeFilter = (UnlockType)EditorGUILayout.EnumPopup(unlockTypeFilter, EditorStyles.toolbarDropDown, GUILayout.Width(150));
            }

            GUILayout.Space(10);

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

        // Settings row (folder paths)
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        {
            GUILayout.Label("Create New In:", GUILayout.Width(95));

            if (currentTab == Tab.ShopItems)
            {
                shopItemFolderPath = EditorGUILayout.TextField(shopItemFolderPath);

                if (GUILayout.Button("Browse", GUILayout.Width(60)))
                {
                    string path = EditorUtility.OpenFolderPanel("Select Shop Items Folder", shopItemFolderPath, "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        // Convert absolute path to relative
                        if (path.StartsWith(Application.dataPath))
                        {
                            shopItemFolderPath = "Assets" + path.Substring(Application.dataPath.Length);
                            EditorPrefs.SetString(SHOP_ITEM_FOLDER_KEY, shopItemFolderPath);
                        }
                        else
                        {
                            Debug.LogWarning("[Database Editor] Selected folder is outside the project!");
                        }
                    }
                }
            }
            else
            {
                arsenalUnlockFolderPath = EditorGUILayout.TextField(arsenalUnlockFolderPath);

                if (GUILayout.Button("Browse", GUILayout.Width(60)))
                {
                    string path = EditorUtility.OpenFolderPanel("Select Arsenal Unlocks Folder", arsenalUnlockFolderPath, "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        // Convert absolute path to relative
                        if (path.StartsWith(Application.dataPath))
                        {
                            arsenalUnlockFolderPath = "Assets" + path.Substring(Application.dataPath.Length);
                            EditorPrefs.SetString(ARSENAL_UNLOCK_FOLDER_KEY, arsenalUnlockFolderPath);
                        }
                        else
                        {
                            Debug.LogWarning("[Database Editor] Selected folder is outside the project!");
                        }
                    }
                }
            }

            // Save on text field change
            if (GUI.changed)
            {
                if (currentTab == Tab.ShopItems)
                {
                    EditorPrefs.SetString(SHOP_ITEM_FOLDER_KEY, shopItemFolderPath);
                }
                else
                {
                    EditorPrefs.SetString(ARSENAL_UNLOCK_FOLDER_KEY, arsenalUnlockFolderPath);
                }
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawShopItems()
    {
        var filteredItems = shopItems
            .Where(item => item != null)
            .Where(item => PassesSearchFilter(item.itemName, item.description))
            .Where(item => shopItemTypeFilter == (ShopItemType)(-1) || item.itemType == shopItemTypeFilter)
            .OrderBy(item => item.itemType)
            .ThenBy(item => item.itemName)
            .ToList();

        if (filteredItems.Count == 0)
        {
            EditorGUILayout.HelpBox("No shop items found. Click 'Create New' to add one.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField($"Showing {filteredItems.Count} of {shopItems.Count} items", EditorStyles.miniLabel);
        EditorGUILayout.Space(5);

        // Group by type
        var groupedItems = filteredItems.GroupBy(item => item.itemType);

        foreach (var group in groupedItems)
        {
            // Category header
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                bool isCategoryExpanded = GetFoldoutState($"Category_{group.Key}");
                isCategoryExpanded = EditorGUILayout.Foldout(isCategoryExpanded, $"{group.Key} ({group.Count()})", true, EditorStyles.foldoutHeader);
                SetFoldoutState($"Category_{group.Key}", isCategoryExpanded);

                if (isCategoryExpanded)
                {
                    foreach (var item in group)
                    {
                        DrawShopItemEntry(item);
                    }
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
    }

    private void DrawShopItemEntry(ShopItem item)
    {
        EditorGUILayout.BeginVertical(itemBoxStyle);
        {
            // Header row with foldout
            EditorGUILayout.BeginHorizontal();
            {
                bool isExpanded = GetFoldoutState(AssetDatabase.GetAssetPath(item));
                isExpanded = EditorGUILayout.Foldout(isExpanded, "", true);
                SetFoldoutState(AssetDatabase.GetAssetPath(item), isExpanded);

                // Icon preview
                if (item.icon != null)
                {
                    GUILayout.Box(item.icon.texture, GUILayout.Width(32), GUILayout.Height(32));
                }
                else
                {
                    GUILayout.Box("No Icon", GUILayout.Width(32), GUILayout.Height(32));
                }

                EditorGUILayout.BeginVertical();
                {
                    EditorGUILayout.LabelField(item.itemName, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"{item.kredCost} Credits | {item.itemType}", EditorStyles.miniLabel);
                }
                EditorGUILayout.EndVertical();

                GUILayout.FlexibleSpace();

                // Quick actions
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeObject = item;
                    EditorGUIUtility.PingObject(item);
                }

                if (GUILayout.Button("Duplicate", GUILayout.Width(70)))
                {
                    DuplicateShopItem(item);
                }
            }
            EditorGUILayout.EndHorizontal();

            // Expanded details
            if (GetFoldoutState(AssetDatabase.GetAssetPath(item)))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(5);

                SerializedObject serializedItem = new SerializedObject(item);
                serializedItem.Update();

                // Display fields
                EditorGUILayout.PropertyField(serializedItem.FindProperty("itemName"));
                EditorGUILayout.PropertyField(serializedItem.FindProperty("description"));
                EditorGUILayout.PropertyField(serializedItem.FindProperty("icon"));
                EditorGUILayout.PropertyField(serializedItem.FindProperty("kredCost"));
                EditorGUILayout.PropertyField(serializedItem.FindProperty("itemType"));

                EditorGUILayout.Space(5);

                // Type-specific fields
                if (item.itemType == ShopItemType.WeaponUnlock)
                {
                    EditorGUILayout.PropertyField(serializedItem.FindProperty("weaponPrefab"));
                }
                else if (item.itemType == ShopItemType.AbilityUnlock)
                {
                    EditorGUILayout.PropertyField(serializedItem.FindProperty("abilityPrefab"));
                }
                else if (item.itemType == ShopItemType.HealQueen)
                {
                    EditorGUILayout.PropertyField(serializedItem.FindProperty("healAmount"));
                }
                else if (item.itemType == ShopItemType.DamageBuff)
                {
                    EditorGUILayout.PropertyField(serializedItem.FindProperty("damageMultiplier"));
                }
                else if (item.itemType == ShopItemType.FireRateBuff)
                {
                    EditorGUILayout.PropertyField(serializedItem.FindProperty("fireRateMultiplier"));
                }
                else if (item.itemType == ShopItemType.MoveSpeedBuff)
                {
                    EditorGUILayout.PropertyField(serializedItem.FindProperty("moveSpeedMultiplier"));
                }
                else if (item.itemType == ShopItemType.AbilityCooldownBuff)
                {
                    EditorGUILayout.PropertyField(serializedItem.FindProperty("abilityCooldownMultiplier"));
                }

                EditorGUILayout.Space(5);
                EditorGUILayout.PropertyField(serializedItem.FindProperty("requiredUnlock"));

                // Show unlock ID if assigned
                if (item.requiredUnlock != null)
                {
                    EditorGUILayout.LabelField("Required Unlock ID", item.requiredUnlockID, EditorStyles.miniLabel);
                }

                serializedItem.ApplyModifiedProperties();
                EditorGUI.indentLevel--;
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawArsenalUnlocks()
    {
        var filteredUnlocks = arsenalUnlocks
            .Where(unlock => unlock != null)
            .Where(unlock => PassesSearchFilter(unlock.unlockName, unlock.description))
            .Where(unlock => unlockTypeFilter == (UnlockType)(-1) || unlock.unlockType == unlockTypeFilter)
            .OrderBy(unlock => unlock.unlockType)
            .ThenBy(unlock => unlock.unlockName)
            .ToList();

        if (filteredUnlocks.Count == 0)
        {
            EditorGUILayout.HelpBox("No arsenal unlocks found. Click 'Create New' to add one.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField($"Showing {filteredUnlocks.Count} of {arsenalUnlocks.Count} unlocks", EditorStyles.miniLabel);
        EditorGUILayout.Space(5);

        // Group by type
        var groupedUnlocks = filteredUnlocks.GroupBy(unlock => unlock.unlockType);

        foreach (var group in groupedUnlocks)
        {
            // Category header
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                bool isCategoryExpanded = GetFoldoutState($"Category_{group.Key}");
                isCategoryExpanded = EditorGUILayout.Foldout(isCategoryExpanded, $"{group.Key} ({group.Count()})", true, EditorStyles.foldoutHeader);
                SetFoldoutState($"Category_{group.Key}", isCategoryExpanded);

                if (isCategoryExpanded)
                {
                    foreach (var unlock in group)
                    {
                        DrawArsenalUnlockEntry(unlock);
                    }
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
    }

    private void DrawArsenalUnlockEntry(ArsenalUnlock unlock)
    {
        EditorGUILayout.BeginVertical(itemBoxStyle);
        {
            // Header row with foldout
            EditorGUILayout.BeginHorizontal();
            {
                bool isExpanded = GetFoldoutState(AssetDatabase.GetAssetPath(unlock));
                isExpanded = EditorGUILayout.Foldout(isExpanded, "", true);
                SetFoldoutState(AssetDatabase.GetAssetPath(unlock), isExpanded);

                // Icon preview
                if (unlock.icon != null)
                {
                    GUILayout.Box(unlock.icon.texture, GUILayout.Width(32), GUILayout.Height(32));
                }
                else
                {
                    GUILayout.Box("No Icon", GUILayout.Width(32), GUILayout.Height(32));
                }

                EditorGUILayout.BeginVertical();
                {
                    EditorGUILayout.LabelField(unlock.unlockName, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"{unlock.iaCost} IA XP | {unlock.unlockType} | Default: {unlock.IsDefaultUnlocked}", EditorStyles.miniLabel);
                }
                EditorGUILayout.EndVertical();

                GUILayout.FlexibleSpace();

                // Quick actions
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeObject = unlock;
                    EditorGUIUtility.PingObject(unlock);
                }

                if (GUILayout.Button("Duplicate", GUILayout.Width(70)))
                {
                    DuplicateArsenalUnlock(unlock);
                }
            }
            EditorGUILayout.EndHorizontal();

            // Expanded details
            if (GetFoldoutState(AssetDatabase.GetAssetPath(unlock)))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(5);

                SerializedObject serializedUnlock = new SerializedObject(unlock);
                serializedUnlock.Update();

                // Display fields
                EditorGUILayout.PropertyField(serializedUnlock.FindProperty("unlockName"));
                EditorGUILayout.PropertyField(serializedUnlock.FindProperty("description"));
                EditorGUILayout.PropertyField(serializedUnlock.FindProperty("icon"));
                EditorGUILayout.PropertyField(serializedUnlock.FindProperty("iaCost"));
                EditorGUILayout.PropertyField(serializedUnlock.FindProperty("unlockType"));
                EditorGUILayout.PropertyField(serializedUnlock.FindProperty("isDefaultUnlocked"));

                EditorGUILayout.Space(5);

                // Show GUID (read-only)
                EditorGUILayout.LabelField("Unlock ID (GUID)", unlock.UnlockID, EditorStyles.miniLabel);

                // Find shop items that reference this unlock
                var referencingItems = shopItems.Where(item => item != null && item.requiredUnlock == unlock).ToList();
                if (referencingItems.Count > 0)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField($"Referenced by {referencingItems.Count} Shop Item(s):", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    foreach (var item in referencingItems)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"• {item.itemName} ({item.itemType})", EditorStyles.miniLabel);
                        if (GUILayout.Button("→", GUILayout.Width(30)))
                        {
                            Selection.activeObject = item;
                            EditorGUIUtility.PingObject(item);
                            currentTab = Tab.ShopItems;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUI.indentLevel--;
                }

                serializedUnlock.ApplyModifiedProperties();
                EditorGUI.indentLevel--;
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void RefreshDatabase()
    {
        // Find all ShopItem assets
        string[] shopItemGuids = AssetDatabase.FindAssets("t:ShopItem");
        shopItems = shopItemGuids
            .Select(guid => AssetDatabase.LoadAssetAtPath<ShopItem>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(item => item != null)
            .ToList();

        // Find all ArsenalUnlock assets
        string[] unlockGuids = AssetDatabase.FindAssets("t:ArsenalUnlock");
        arsenalUnlocks = unlockGuids
            .Select(guid => AssetDatabase.LoadAssetAtPath<ArsenalUnlock>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(unlock => unlock != null)
            .ToList();

        Debug.Log($"[Database Editor] Loaded {shopItems.Count} Shop Items, {arsenalUnlocks.Count} Arsenal Unlocks");
        Repaint();
    }

    private void CreateNew()
    {
        if (currentTab == Tab.ShopItems)
        {
            // Ensure folder exists
            if (!AssetDatabase.IsValidFolder(shopItemFolderPath))
            {
                // Try to create the folder path
                string[] folders = shopItemFolderPath.Split('/');
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

            // Create new ShopItem
            ShopItem newItem = CreateInstance<ShopItem>();
            newItem.itemName = "New Shop Item";

            // Auto-generate filename
            string fileName = "NewShopItem";
            string path = AssetDatabase.GenerateUniqueAssetPath($"{shopItemFolderPath}/{fileName}.asset");

            AssetDatabase.CreateAsset(newItem, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshDatabase();
            Selection.activeObject = newItem;
            EditorGUIUtility.PingObject(newItem);

            Debug.Log($"[Database Editor] Created Shop Item at: {path}");
        }
        else
        {
            // Ensure folder exists
            if (!AssetDatabase.IsValidFolder(arsenalUnlockFolderPath))
            {
                // Try to create the folder path
                string[] folders = arsenalUnlockFolderPath.Split('/');
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

            // Create new ArsenalUnlock
            ArsenalUnlock newUnlock = CreateInstance<ArsenalUnlock>();
            newUnlock.unlockName = "New Arsenal Unlock";

            // Auto-generate filename
            string fileName = "NewArsenalUnlock";
            string path = AssetDatabase.GenerateUniqueAssetPath($"{arsenalUnlockFolderPath}/{fileName}.asset");

            AssetDatabase.CreateAsset(newUnlock, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshDatabase();
            Selection.activeObject = newUnlock;
            EditorGUIUtility.PingObject(newUnlock);

            Debug.Log($"[Database Editor] Created Arsenal Unlock at: {path}");
        }
    }

    private void DuplicateShopItem(ShopItem original)
    {
        ShopItem duplicate = Instantiate(original);
        duplicate.itemName = original.itemName + " (Copy)";

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

    private void DuplicateArsenalUnlock(ArsenalUnlock original)
    {
        ArsenalUnlock duplicate = Instantiate(original);
        duplicate.unlockName = original.unlockName + " (Copy)";

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

        Debug.LogWarning($"[Database Editor] Duplicated unlock will have a NEW GUID. Shop items referencing the original will need to be updated.");
    }

    private bool PassesSearchFilter(string name, string description)
    {
        if (string.IsNullOrEmpty(searchQuery)) return true;

        string query = searchQuery.ToLower();
        return (name != null && name.ToLower().Contains(query)) ||
               (description != null && description.ToLower().Contains(query));
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