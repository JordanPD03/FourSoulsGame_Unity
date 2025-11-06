using UnityEngine;

/// <summary>
/// Efecto que permite robar cartas adicionales.
/// </summary>
[CreateAssetMenu(fileName = "DrawCardEffect", menuName = "Four Souls/Effects/Draw Card")]
public class DrawCardEffect : CardEffect
{
    [Header("Draw Card Settings")]
    [Tooltip("Cantidad de cartas a robar")]
    public int cardCount = 1;

    [Tooltip("Si true, roba con animación; si false, instantáneo")]
    public bool animated = true;

    protected override void ExecuteInternal(PlayerData player, CardData card, GameManager gameManager, TargetSelection target)
    {
        if (player == null || gameManager == null)
        {
            Debug.LogWarning("DrawCardEffect: player o gameManager es null");
            return;
        }

        Debug.Log($"[DrawCardEffect] {player.playerName} roba {cardCount} carta(s)");

        for (int i = 0; i < cardCount; i++)
        {
            // DrawCard siempre usa al jugador actual y el mazo de Loot
            // La animación la controla el GameManager según el contexto
            CardData drawnCard = gameManager.DrawCard(DeckType.Loot, player);
            if (drawnCard != null)
            {
                Debug.Log($"[DrawCardEffect] Carta robada: {drawnCard.cardName}");
            }
        }
    }

    public override string GetDescription()
    {
        return cardCount == 1 
            ? "Roba 1 carta" 
            : $"Roba {cardCount} cartas";
    }
}
