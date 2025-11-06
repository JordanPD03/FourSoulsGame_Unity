using UnityEngine;
using DG.Tweening;

/// <summary>
/// Efecto que inflige daño a un objetivo (jugador o monstruo).
/// </summary>
[CreateAssetMenu(fileName = "DealDamageEffect", menuName = "Four Souls/Effects/Deal Damage")]
public class DealDamageEffect : CardEffect
{
    [Header("Deal Damage Settings")]
    [Tooltip("Cantidad de daño a infligir")]
    public int damageAmount = 1;

    [Tooltip("Si true, el jugador elige el objetivo; si false, se aplica al monstruo activo")]
    public bool requiresTarget = false;

    public override bool RequiresTarget => true;

    public override TargetType[] AllowedTargets => new[] { TargetType.Player, TargetType.Monster };

    protected override void ExecuteInternal(PlayerData player, CardData card, GameManager gameManager, TargetSelection target)
    {
        if (player == null || gameManager == null)
        {
            Debug.LogWarning("DealDamageEffect: player o gameManager es null");
            return;
        }

        if (target == null || target.targetType == TargetType.None)
        {
            Debug.LogWarning("[DealDamageEffect] No se proporcionó objetivo para el daño");
            return;
        }

        switch (target.targetType)
        {
            case TargetType.Player:
                if (target.targetPlayer != null)
                {
                    // Integración básica: restar vida
                    gameManager.ChangePlayerHealth(target.targetPlayer, -damageAmount);
                    Debug.Log($"[DealDamageEffect] {target.targetPlayer.playerName} recibe {damageAmount} de daño");
                }
                break;
            case TargetType.Monster:
                // Buscar el slot del monstruo y aplicar daño
                if (MonsterSlotManager.Instance != null)
                {
                    var slots = MonsterSlotManager.Instance.GetAllSlots();
                    foreach (var slot in slots)
                    {
                        if (slot.HasMonster() && slot.GetMonsterData() == target.targetMonsterCard)
                        {
                            // Animar daño primero
                            slot.PlayTakeDamageAnimation();
                            
                            // Aplicar daño después de un pequeño delay (para que se vea la animación)
                            DOVirtual.DelayedCall(0.3f, () =>
                            {
                                if (slot != null && slot.HasMonster())
                                {
                                    MonsterSlotManager.Instance.DamageMonster(slot, damageAmount);
                                }
                            });
                            
                            Debug.Log($"[DealDamageEffect] Monstruo '{target.targetMonsterCard.cardName}' recibe {damageAmount} de daño");
                            break;
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("[DealDamageEffect] No hay MonsterSlotManager para aplicar daño a monstruo");
                }
                break;
        }
    }

    public override string GetDescription()
    {
        return $"Inflige {damageAmount} de daño";
    }
}
