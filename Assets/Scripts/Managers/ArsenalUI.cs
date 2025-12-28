using ProjectAdminPrivileges.Combat.Weapons;
using ProjectAdminPrivileges.ShopSystem;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Arsenal UI - Displays permanent unlocks purchasable with IA XP.
/// Shows all ArsenalUnlock items, filtered by category tabs.
/// </summary>
public class ArsenalUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject arsenalPanel;
    [SerializeField] private TextMeshProUGUI iaxpDisplayText;
    [SerializeField] private Transform unlockContainer; // Parent for unlock buttons
    [SerializeField] private GameObject unlockButtonPrefab;
    [SerializeField] private Button closeButton;

    [Header("Category Tabs")]
    [SerializeField] private Button allTab;
    [SerializeField] private Button weaponsTab;
    [SerializeField] private Button abilitiesTab;
    [SerializeField] private Button statsTab;

    [Header("Scroll View")]
    [SerializeField] private ScrollRect scrollRect;

    private UnlockType currentFilter = UnlockType.Weapon; // Default to weapons
    private List<ArsenalUnlockButton> instantiatedButtons = new List<ArsenalUnlockButton>();

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseArsenal);

        // Setup tab buttons
        if (allTab != null)
            allTab.onClick.AddListener(() => SetFilter(UnlockType.Weapon)); // "All" shows weapons first

        if (weaponsTab != null)
            weaponsTab.onClick.AddListener(() => SetFilter(UnlockType.Weapon));

        if (abilitiesTab != null)
            abilitiesTab.onClick.AddListener(() => SetFilter(UnlockType.Ability));

        if (statsTab != null)
            statsTab.onClick.AddListener(() => SetFilter(UnlockType.StatUpgrade));

        // Subscribe to IA XP changes
        if (IAExperienceManager.Instance != null)
        {
            IAExperienceManager.Instance.OnIAXPChanged += OnIAXPChanged;
        }

        // Start hidden
        if (arsenalPanel != null)
            arsenalPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (IAExperienceManager.Instance != null)
        {
            IAExperienceManager.Instance.OnIAXPChanged -= OnIAXPChanged;
        }
    }

    /// <summary>
    /// Open Arsenal UI and populate with unlocks
    /// </summary>
    public void OpenArsenal()
    {
        if (arsenalPanel != null)
            arsenalPanel.SetActive(true);

        UpdateIAXPDisplay();
        PopulateUnlocks();

        Debug.Log("[ArsenalUI] Arsenal opened");
    }

    /// <summary>
    /// Close Arsenal UI
    /// </summary>
    public void CloseArsenal()
    {
        if (arsenalPanel != null)
            arsenalPanel.SetActive(false);

        Debug.Log("[ArsenalUI] Arsenal closed");
    }

    /// <summary>
    /// Populate unlock grid with filtered items
    /// </summary>
    private void PopulateUnlocks()
    {
        // Clear existing buttons
        foreach (var button in instantiatedButtons)
        {
            if (button != null)
                Destroy(button.gameObject);
        }
        instantiatedButtons.Clear();

        if (ArsenalManager.Instance == null)
        {
            Debug.LogError("[ArsenalUI] ArsenalManager.Instance is null!");
            return;
        }

        // Get all unlocks from ArsenalManager
        ArsenalUnlock[] allUnlocks = ArsenalManager.Instance.GetAllUnlocks();

        if (allUnlocks == null || allUnlocks.Length == 0)
        {
            Debug.LogWarning("[ArsenalUI] No unlocks found in ArsenalManager!");
            return;
        }

        // Filter by current tab
        List<ArsenalUnlock> filteredUnlocks = new List<ArsenalUnlock>();
        foreach (var unlock in allUnlocks)
        {
            if (unlock == null) continue;

            // Filter by current category
            if (unlock.unlockType == currentFilter)
            {
                filteredUnlocks.Add(unlock);
            }
        }

        // Sort: Unlocked first, then by cost
        filteredUnlocks.Sort((a, b) =>
        {
            bool aUnlocked = ArsenalManager.Instance.IsUnlocked(a.UnlockID);
            bool bUnlocked = ArsenalManager.Instance.IsUnlocked(b.UnlockID);

            if (aUnlocked && !bUnlocked) return -1;
            if (!aUnlocked && bUnlocked) return 1;
            return a.iaCost.CompareTo(b.iaCost);
        });

        // Instantiate buttons
        foreach (var unlock in filteredUnlocks)
        {
            CreateUnlockButton(unlock);
        }

        // Reset scroll to top
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f;
        }

        Debug.Log($"[ArsenalUI] Populated {filteredUnlocks.Count} unlocks for category {currentFilter}");
    }

    /// <summary>
    /// Create single unlock button
    /// </summary>
    private void CreateUnlockButton(ArsenalUnlock unlock)
    {
        if (unlockButtonPrefab == null || unlockContainer == null)
        {
            Debug.LogError("[ArsenalUI] Missing prefab or container!");
            return;
        }

        GameObject buttonObj = Instantiate(unlockButtonPrefab, unlockContainer);
        ArsenalUnlockButton button = buttonObj.GetComponent<ArsenalUnlockButton>();

        if (button != null)
        {
            button.Setup(unlock);
            instantiatedButtons.Add(button);
        }
        else
        {
            Debug.LogError("[ArsenalUI] Unlock button prefab missing ArsenalUnlockButton component!");
            Destroy(buttonObj);
        }
    }

    /// <summary>
    /// Update IA XP display text
    /// </summary>
    private void UpdateIAXPDisplay()
    {
        if (iaxpDisplayText == null) return;

        if (IAExperienceManager.Instance != null)
        {
            int totalXP = IAExperienceManager.Instance.TotalIAXP;
            iaxpDisplayText.text = $"IA Experience: {totalXP}";
        }
        else
        {
            iaxpDisplayText.text = "IA Experience: ???";
        }
    }

    /// <summary>
    /// Called when IA XP changes - refresh display and buttons
    /// </summary>
    private void OnIAXPChanged(int newTotal)
    {
        UpdateIAXPDisplay();
        RefreshAllButtons();
    }

    /// <summary>
    /// Refresh all instantiated buttons (after purchase, affordability changes)
    /// </summary>
    public void RefreshAllButtons()
    {
        foreach (var button in instantiatedButtons)
        {
            if (button != null)
            {
                button.RefreshButton();
            }
        }
    }

    /// <summary>
    /// Change category filter and repopulate
    /// </summary>
    private void SetFilter(UnlockType filter)
    {
        currentFilter = filter;
        PopulateUnlocks();
        Debug.Log($"[ArsenalUI] Filter set to {filter}");
    }
}
