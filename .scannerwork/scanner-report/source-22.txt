using UnityEngine;

/// <summary>
/// Efecto que cura al jugador.
/// </summary>
[CreateAssetMenu(fileName = "HealEffect", menuName = "Four Souls/Effects/Heal")]
public class HealEffect : CardEffect
{
    [Header("Heal Settings")]
    [Tooltip("Cantidad de vida a restaurar")]
    public int healAmount = 1;

    [Tooltip("Si true, no puede exceder vida máxima")]
    public bool respectMaxHealth = true;

    protected override void ExecuteInternal(PlayerData player, CardData card, GameManager gameManager, TargetSelection target)
    {
        if (player == null || gameManager == null)
        {
            Debug.LogWarning("HealEffect: player o gameManager es null");
            return;
        }

        int actualHeal = healAmount;
        if (respectMaxHealth)
        {
            actualHeal = Mathf.Min(healAmount, player.maxHealth - player.health);
        }

        if (actualHeal > 0)
        {
            player.health = Mathf.Min(player.health + actualHeal, player.maxHealth);
            Debug.Log($"[HealEffect] {player.playerName} recuperó {actualHeal} de vida (ahora: {player.health}/{player.maxHealth})");
        }
        else
        {
            Debug.Log($"[HealEffect] {player.playerName} ya tiene vida completa");
        }
    }

    public override bool CanExecute(PlayerData player, CardData card, GameManager gameManager)
    {
        if (respectMaxHealth)
        {
            return player.health < player.maxHealth;
        }
        return true;
    }

    public override string GetDescription()
    {
        return $"Recupera {healAmount} de vida";
    }
}
