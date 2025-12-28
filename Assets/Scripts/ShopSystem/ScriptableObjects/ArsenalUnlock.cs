using UnityEngine;

/// <summary>
/// Represents a single unlock in the Interdimensional Arsenal.
/// This is JUST the unlock ticket - the actual item data lives elsewhere.
/// Auto-generates a unique GUID on creation.
/// </summary>
[CreateAssetMenu(fileName = "NewArsenalUnlock", menuName = "Arsenal/Arsenal Unlock")]
public class ArsenalUnlock : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Auto-generated GUID. DO NOT EDIT.")]
    [SerializeField] private string unlockID;

    [Header("Display")]
    public string unlockName;
    [TextArea(2, 4)] public string description;
    public Sprite icon;

    [Header("Cost")]
    [Tooltip("IA XP required to unlock permanently")]
    public int iaCost;

    [Header("Unlock Type")]
    public UnlockType unlockType;

    [Header("Default State")]
    [Tooltip("If true, this is unlocked from the start (no purchase needed)")]
    public bool isDefaultUnlocked = false;

    // Read-only access to GUID
    public string UnlockID => unlockID;
    public bool IsDefaultUnlocked => isDefaultUnlocked;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Auto-generate GUID on asset creation
        if (string.IsNullOrEmpty(unlockID))
        {
            unlockID = System.Guid.NewGuid().ToString();
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[ArsenalUnlock] Generated GUID for '{unlockName}': {unlockID}");
        }

        // Warn if name is empty
        if (string.IsNullOrEmpty(unlockName))
        {
            Debug.LogWarning($"[ArsenalUnlock] Unlock has no name! (ID: {unlockID})");
        }
    }

    /// <summary>
    /// Context menu to regenerate GUID (use if you need to reset it)
    /// </summary>
    [ContextMenu("Regenerate GUID")]
    private void RegenerateGUID()
    {
        string oldID = unlockID;
        unlockID = System.Guid.NewGuid().ToString();
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.LogWarning($"[ArsenalUnlock] GUID regenerated for '{unlockName}':\nOLD: {oldID}\nNEW: {unlockID}");
    }
#endif
}

public enum UnlockType
{
    Weapon,
    Ability,
    StatUpgrade,
    Utility
}