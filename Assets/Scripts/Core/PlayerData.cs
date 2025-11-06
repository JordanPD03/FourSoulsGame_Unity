using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Datos de un jugador (independiente de la vista)
/// Serializable para sincronización en red
/// </summary>
[System.Serializable]
public class PlayerData
{
    public int playerId;
    public string playerName;
    public List<CardData> hand = new List<CardData>();
    public List<CardData> activeItems = new List<CardData>();
    public List<CardData> passiveItems = new List<CardData>();
    
    // Stats de Four Souls
    public int health = 2;
    public int maxHealth = 2;
    public int coins = 3;
    public int attackDamage = 1;
    public int souls = 0;
    
    // Estado
    public bool isDead = false;
    public CharacterType character;
    
    public PlayerData(int id, string name, CharacterType characterType = CharacterType.Isaac)
    {
        playerId = id;
        playerName = name;
        character = characterType;
        health = 2;
        maxHealth = 2;
        coins = 3;
        attackDamage = 1;
        souls = 0;
    }
    
    /// <summary>
    /// Verifica si el jugador puede realizar una acción en su turno
    /// </summary>
    public bool CanAct()
    {
        return !isDead;
    }
    
    /// <summary>
    /// Verifica si alcanzó la condición de victoria
    /// </summary>
    public bool HasWon()
    {
        return souls >= 4; // Four Souls = 4 almas para ganar
    }
    
    /// <summary>
    /// Agrega una carta a la mano
    /// </summary>
    public void AddCardToHand(CardData card)
    {
        if (card != null)
            hand.Add(card);
    }
    
    /// <summary>
    /// Remueve una carta de la mano
    /// </summary>
    public bool RemoveCardFromHand(CardData card)
    {
        return hand.Remove(card);
    }
    
    /// <summary>
    /// Descarta cartas hasta tener el límite (10 en Four Souls)
    /// </summary>
    public List<CardData> DiscardToHandLimit(int limit = 10)
    {
        List<CardData> discarded = new List<CardData>();
        
        while (hand.Count > limit)
        {
            // Por ahora descarta las últimas, luego será elección del jugador
            CardData card = hand[hand.Count - 1];
            hand.RemoveAt(hand.Count - 1);
            discarded.Add(card);
        }
        
        return discarded;
    }
}

/// <summary>
/// Personajes jugables de Four Souls
/// </summary>
public enum CharacterType
{
    Isaac,
    Magdalene,
    Cain,
    Judas,
    BlueBaby,
    Eve,
    Samson,
    Azazel,
    Lazarus,
    Eden,
    TheLost,
    Lilith,
    Keeper,
    Apollyon
}
