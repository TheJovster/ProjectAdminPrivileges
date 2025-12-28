using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectAdminPrivileges.ShopSystem;
using ProjectAdminPrivileges.Combat.Weapons;

/// <summary>
/// Individual unlock button in Arsenal UI.
/// Shows locked/unlocked state, handles purchase with IA XP.
/// </summary>
public class ArsenalUnlockButton : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Button purchaseButton;
    [SerializeField] private TextMeshProUGUI buttonText;
    [SerializeField] private GameObject lockedOverlay; // Optional - dark overlay when locked

    [Header("Visual States")]
    [SerializeField] private Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] private Color unlockedColor = Color.white;
    [SerializeField] private Color cannotAffordColor = new Color(0.8f, 0.3f, 0.3f, 1f);

    private ArsenalUnlock currentUnlock;

    private void Awake()
    {
        if (purchaseButton != null)
        {
            purchaseButton.onClick.AddListener(OnPurchaseClicked);
        }
    }

    /// <summary>
    /// Setup button with unlock data
    /// </summary>
    public void Setup(ArsenalUnlock unlock)
    {
        currentUnlock = unlock;

        if (unlock == null)
        {
            Debug.LogError("[ArsenalUnlockButton] Setup called with null unlock!");
            gameObject.SetActive(false);
            return;
        }

        // Set display values
        if (nameText != null)
            nameText.text = unlock.unlockName;

        if (descriptionText != null)
            descriptionText.text = unlock.description;

        if (costText != null)
            costText.text = $"{unlock.iaCost} IA XP";

        if (iconImage != null && unlock.icon != null)
            iconImage.sprite = unlock.icon;

        RefreshButton();
    }

    /// <summary>
    /// Update button visual state based on unlock status and affordability
    /// </summary>
    public void RefreshButton()
    {
        if (currentUnlock == null) return;

        bool isUnlocked = ArsenalManager.Instance != null &&
                          ArsenalManager.Instance.IsUnlocked(currentUnlock.UnlockID);
        bool canAfford = IAExperienceManager.Instance != null &&
                         IAExperienceManager.Instance.TotalIAXP >= currentUnlock.iaCost;

        if (isUnlocked)
        {
            // Already unlocked state
            if (buttonText != null)
                buttonText.text = "UNLOCKED";

            if (purchaseButton != null)
                purchaseButton.interactable = false;

            if (iconImage != null)
                iconImage.color = unlockedColor;

            if (lockedOverlay != null)
                lockedOverlay.SetActive(false);

            // Green checkmark color
            if (buttonText != null)
                buttonText.color = new Color(0.2f, 0.8f, 0.2f, 1f);
        }
        else if (!canAfford)
        {
            // Cannot afford state
            if (buttonText != null)
                buttonText.text = "UNLOCK";

            if (purchaseButton != null)
                purchaseButton.interactable = false;

            if (iconImage != null)
                iconImage.color = lockedColor;

            if (lockedOverlay != null)
                lockedOverlay.SetActive(true);

            // Red text for cost
            if (costText != null)
                costText.color = cannotAffordColor;
        }
        else
        {
            // Can afford, not yet unlocked
            if (buttonText != null)
                buttonText.text = "UNLOCK";

            if (purchaseButton != null)
                purchaseButton.interactable = true;

            if (iconImage != null)
                iconImage.color = unlockedColor;

            if (lockedOverlay != null)
                lockedOverlay.SetActive(false);

            // White text
            if (costText != null)
                costText.color = Color.white;
        }
    }

    private void OnPurchaseClicked()
    {
        if (currentUnlock == null)
        {
            Debug.LogError("[ArsenalUnlockButton] OnPurchaseClicked with null unlock!");
            return;
        }

        if (ArsenalManager.Instance == null)
        {
            Debug.LogError("[ArsenalUnlockButton] ArsenalManager.Instance is null!");
            return;
        }

        // Attempt purchase
        ArsenalManager.Instance.PurchaseUnlock(currentUnlock);

        // Refresh this button
        RefreshButton();

        // Tell Arsenal UI to refresh all buttons (in case other unlocks became affordable)
        ArsenalUI arsenalUI = GetComponentInParent<ArsenalUI>();
        if (arsenalUI != null)
        {
            arsenalUI.RefreshAllButtons();
        }
    }
}