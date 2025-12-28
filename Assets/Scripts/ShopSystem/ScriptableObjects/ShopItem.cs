using UnityEngine;

[CreateAssetMenu(fileName = "NewShopItem", menuName = "Shop/Shop Item")]
public class ShopItem : ScriptableObject
{
    [Header("Display")]
    public string itemName;
    [TextArea(2, 4)] public string description;
    public Sprite icon;

    [Header("Cost")]
    public int kredCost;

    [Header("Type")]
    public ShopItemType itemType;

    [Header("Weapon/Ability (if applicable)")]
    public GameObject weaponPrefab;
    public GameObject abilityPrefab;

    [Header("Values (if applicable)")]
    public int healAmount = 0;
    public float damageMultiplier = 1f;
    public float fireRateMultiplier = 1f;
    public float moveSpeedMultiplier = 1f;
    public float abilityCooldownMultiplier = 1f;

    [Header("Arsenal Integration")]
    [Tooltip("Leave empty for consumables/buffs. For weapons/abilities, must match ArsenalUnlock.unlockID")]
    public string requiredUnlockID => requiredUnlock != null ? requiredUnlock.UnlockID : "";

    [Header("Arsenal Integration")]
    [Tooltip("Which ArsenalUnlock is required to purchase this item?")]
    public ArsenalUnlock requiredUnlock; // Changed from string to reference

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Validate that unlock type matches item type
        if (requiredUnlock != null)
        {
            if (itemType == ShopItemType.WeaponUnlock && requiredUnlock.unlockType != UnlockType.Weapon)
            {
                Debug.LogWarning($"[ShopItem] {itemName}: Item is WeaponUnlock but requiredUnlock is {requiredUnlock.unlockType}!");
            }

            if (itemType == ShopItemType.AbilityUnlock && requiredUnlock.unlockType != UnlockType.Ability)
            {
                Debug.LogWarning($"[ShopItem] {itemName}: Item is AbilityUnlock but requiredUnlock is {requiredUnlock.unlockType}!");
            }
        }
    }
#endif

}

public enum ShopItemType
{
    WeaponUnlock,       // Temporary weapon for this run
    AbilityUnlock,      // Temporary ability for this run
    HealQueen,          // Consumable
    AmmoRefill,         // Consumable
    ExtraLife,          // Consumable
    DamageBuff,         // Buff (this run only)
    FireRateBuff,       // Buff
    MoveSpeedBuff,      // Buff
    AbilityCooldownBuff // Buff
}