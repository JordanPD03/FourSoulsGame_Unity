using UnityEngine;

/// <summary>
/// Efecto de carta: añade 1 o más slots de tienda (hasta el máximo).
/// </summary>
[CreateAssetMenu(fileName = "AddShopSlotEffect", menuName = "CardEffects/Add Shop Slot", order = 0)]
public class AddShopSlotEffect : CardEffect
{
    [Min(1)] public int slotsToAdd = 1;

    public override bool RequiresTarget => false;

    protected override void ExecuteInternal(PlayerData player, CardData card, GameManager gameManager, TargetSelection target)
    {
        if (gameManager == null)
        {
            Debug.LogError("[AddShopSlotEffect] No hay GameManager");
            return;
        }

        int current = gameManager.GetShopCards().Count;
        int newSize = current + Mathf.Max(1, slotsToAdd);
        
        if (gameManager.ExpandShop(newSize))
        {
            // Expandir slots visuales
            if (TreasureShopManager.Instance != null)
            {
                TreasureShopManager.Instance.ExpandShop(newSize);
            }
            Debug.Log($"[AddShopSlotEffect] Tienda expandida a {newSize} slots");
        }
        else
        {
            Debug.Log("[AddShopSlotEffect] No se expandió la tienda (posible máximo alcanzado)");
        }
    }
}
