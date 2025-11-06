using UnityEngine;

/// <summary>
/// Efecto que otorga monedas al jugador.
/// </summary>
[CreateAssetMenu(fileName = "GainCoinsEffect", menuName = "Four Souls/Effects/Gain Coins")]
public class GainCoinsEffect : CardEffect
{
    [Header("Gain Coins Settings")]
    [Tooltip("Cantidad de monedas a otorgar")]
    public int coinAmount = 1;

    protected override void ExecuteInternal(PlayerData player, CardData card, GameManager gameManager, TargetSelection target)
    {
        if (player == null || gameManager == null)
        {
            Debug.LogWarning("GainCoinsEffect: player o gameManager es null");
            return;
        }

    // Usar origen desde la pila de descarte de Loot para la animación
    gameManager.ChangePlayerCoinsFromLoot(player, coinAmount);
        Debug.Log($"[GainCoinsEffect] {player.playerName} ganó {coinAmount} monedas");
    }

    public override string GetDescription()
    {
        return coinAmount >= 0 
            ? $"+{coinAmount}¢" 
            : $"{coinAmount}¢";
    }
}
