using UnityEngine;

/// <summary>
/// Define una entrada en el mazo con una carta y su cantidad de copias.
/// </summary>
[System.Serializable]
public class DeckEntry
{
    [Tooltip("La carta a incluir en el mazo")]
    public CardDataSO card;
    
    [Tooltip("Número de copias de esta carta en el mazo")]
    [Range(1, 20)]
    public int quantity = 1;

    public DeckEntry(CardDataSO cardData, int qty)
    {
        card = cardData;
        quantity = qty;
    }
}

/// <summary>
/// Configuración de un mazo completo (Loot, Treasure, Monster).
/// Permite definir cuántas copias de cada carta hay en el mazo.
/// </summary>
[CreateAssetMenu(fileName = "New Deck Config", menuName = "Four Souls/Deck Configuration", order = 2)]
public class DeckConfiguration : ScriptableObject
{
    [Header("Deck Information")]
    [Tooltip("Nombre del mazo (Loot, Treasure, Monster, etc.)")]
    public string deckName = "Loot Deck";
    
    [Tooltip("Tipo de cartas en este mazo")]
    public DeckType deckType = DeckType.Loot;

    [Header("Deck Contents")]
    [Tooltip("Lista de cartas y sus cantidades en el mazo")]
    public DeckEntry[] cards = new DeckEntry[0];

    [Header("Statistics (Read Only)")]
    [Tooltip("Total de cartas en el mazo")]
    public int totalCards = 0;

    /// <summary>
    /// Calcula el total de cartas en el mazo.
    /// </summary>
    private void OnValidate()
    {
        totalCards = 0;
        foreach (var entry in cards)
        {
            if (entry != null && entry.card != null)
            {
                totalCards += entry.quantity;
            }
        }
    }

    /// <summary>
    /// Obtiene todas las cartas del mazo expandidas (con duplicados).
    /// </summary>
    public CardDataSO[] GetExpandedDeck()
    {
        var expandedList = new System.Collections.Generic.List<CardDataSO>();
        
        foreach (var entry in cards)
        {
            if (entry != null && entry.card != null)
            {
                for (int i = 0; i < entry.quantity; i++)
                {
                    expandedList.Add(entry.card);
                }
            }
        }
        
        return expandedList.ToArray();
    }
}
