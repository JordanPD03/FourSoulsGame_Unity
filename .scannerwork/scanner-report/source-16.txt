using UnityEngine;

/// <summary>
/// Efecto que añade un slot de monstruo adicional al tablero.
/// Usado por cartas/eventos que expanden los espacios de monstruos.
/// </summary>
[CreateAssetMenu(fileName = "AddMonsterSlotEffect", menuName = "Card Effects/Add Monster Slot")]
public class AddMonsterSlotEffect : CardEffect
{
    [Header("Slot Configuration")]
    [Tooltip("Número de slots a añadir")]
    [SerializeField] private int slotsToAdd = 1;

    public override bool RequiresTarget => false;

    protected override void ExecuteInternal(PlayerData player, CardData card, GameManager gameManager, TargetSelection target = null)
    {
        if (MonsterSlotManager.Instance == null)
        {
            Debug.LogError("[AddMonsterSlotEffect] No hay MonsterSlotManager en la escena");
            return;
        }

        int slotsAdded = 0;
        for (int i = 0; i < slotsToAdd; i++)
        {
            if (MonsterSlotManager.Instance.AddMonsterSlot())
            {
                slotsAdded++;
            }
            else
            {
                Debug.LogWarning($"[AddMonsterSlotEffect] Solo se pudieron añadir {slotsAdded}/{slotsToAdd} slots");
                break;
            }
        }

        if (slotsAdded > 0)
        {
            Debug.Log($"[AddMonsterSlotEffect] {player.playerName} añadió {slotsAdded} slot(s) de monstruo. Total: {MonsterSlotManager.Instance.GetSlotCount()}");
        }
    }
}
