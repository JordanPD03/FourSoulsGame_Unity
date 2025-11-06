using UnityEngine;

/// <summary>
/// Datos de una carta individual (independiente de la vista)
/// Serializable para sincronización en red
/// </summary>
[System.Serializable]
public class CardData
{
    public int cardId;              // ID único para sincronización
    public string cardName;
    public CardType cardType;
    public string description;
    
    // Paths para cargar sprites desde Resources (legacy - usar frontSprite/backSprite cuando sea posible)
    public string frontSpritePath;
    public string backSpritePath;
    
    // Sprites directos (preferido sobre paths)
    public Sprite frontSprite;
    public Sprite backSprite;
    
    // Estado visual
    public bool isFaceUp = true;
    
    // Stats de combate (monstruos)
    public int health;              // Vida del monstruo
    public int diceRequirement;     // Requisito de dado (X+) para poder dañarlo
    public int attackDamage;        // Daño que realiza el monstruo
    
    // Recompensas (monstruos)
    public int rewardCoins;         // Monedas base que otorga al derrotarlo
    public int rewardLootCards;     // Cartas de loot que roba el jugador
    public int rewardTreasure;      // Tesoros que roba el jugador
    public int rewardSoulsMin;      // Almas mínimas (p.e. bosses 1)
    public int rewardSoulsMax;      // Almas máximas (p.e. bosses 2)
    
    // Trigger de combate (opcional)
    public bool hasCombatTrigger;           // Si el monstruo reacciona a tiradas
    public int combatTriggerRollValue;      // Valor exacto que activa el trigger
    public int combatTriggerAttackerDamage; // Daño al atacante cuando se activa
    public int combatTriggerMonsterHeal;    // Curación del monstruo cuando se activa
    
    // Efectos (por ahora texto, luego será un sistema de efectos)
    public string effectDescription;
    
    // Propiedades especiales
    public bool isUnique = false;
    public bool canPlayOnOtherTurn = false;
    public bool isSingleUse = true;
    public bool isPassive = false;
    public bool isEternal = false; // Objetos eternos no pueden perderse (inicio de personaje)
    
    // Estado de uso para objetos activos (carga/recarga)
    // true = listo para usar; false = agotado hasta la siguiente recarga del propietario
    public bool isReady = true;
    
    // Referencia al ScriptableObject original (para ejecutar efectos)
    [System.NonSerialized]
    public CardDataSO sourceScriptableObject;
    
    public CardData(int id, string name, CardType type, string spritePath)
    {
        cardId = id;
        cardName = name;
        cardType = type;
        frontSpritePath = spritePath;
    }
}

/// <summary>
/// Tipos de cartas en Four Souls
/// </summary>
public enum CardType
{
    Loot,           // Cartas de botín (úsalas en tu turno)
    Treasure,       // Tesoros (permanentes o activables)
    Monster,        // Monstruos normales
    Boss,           // Monstruos jefes
    Room,           // Cartas de habitación (bonus rooms)
    Character,      // Carta de personaje
    Soul,           // Almas (condición de victoria)
    Curse,          // Maldiciones
    Bonus           // Bonificaciones
}
