using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

/// <summary>
/// ScriptableObject que define una carta de Four Souls.
/// Crea nuevas cartas desde: Assets > Create > Four Souls > Card
/// </summary>
[CreateAssetMenu(fileName = "New Card", menuName = "Four Souls/Card", order = 1)]
public class CardDataSO : ScriptableObject
{
    [Header("Información Básica")]
    [Tooltip("ID único de la carta")]
    public int cardId;
    
    [Tooltip("Nombre de la carta")]
    public string cardName = "New Card";
    
    [Tooltip("Tipo de carta")]
    public CardType cardType = CardType.Loot;
    
    [TextArea(3, 6)]
    [Tooltip("Descripción del efecto de la carta")]
    public string description = "";

    [Header("Sprites")]
    [Tooltip("Sprite de la cara frontal de la carta")]
    public Sprite frontSprite;
    
    [Tooltip("Sprite de la carta volteada (dorso). Si no se asigna, usa el dorso por defecto del mazo.")]
    public Sprite backSprite;

    [Header("Stats de Combate (solo monstruos)")]
    [Tooltip("Vida del monstruo")]
    public int health = 0;

    [Tooltip("Mínimo en el dado para poder dañarlo (ej: 3 = 3+)")]
    public int diceRequirement = 0;

    [FormerlySerializedAs("damage"), FormerlySerializedAs("atack")]
    [Tooltip("Daño de ataque del monstruo")]
    public int attackDamage = 0;

    [Header("Recompensas al derrotar (monstruos)")]
    [Tooltip("Monedas base que otorga al derrotarlo")]
    public int rewardCoins = 0;

    [Tooltip("Cartas de loot que roba el jugador al derrotarlo")]
    public int rewardLootCards = 0;

    [Tooltip("Tesoros que roba el jugador al derrotarlo")]
    public int rewardTreasure = 0;

    [Tooltip("Almas mínimas (opcional). Para valor fijo, deja Min=0 y usa Max como valor fijo")]
    public int rewardSoulsMin = 0;

    [Tooltip("Almas máximas. El sistema usa el mayor entre Min y Max como valor fijo de almas")]
    public int rewardSoulsMax = 0;

    [Header("Efectos")]
    [Tooltip("Lista de efectos que ejecuta esta carta")]
    public List<CardEffect> effects = new List<CardEffect>();
    
    [Header("Trigger de Combate (opcional, monstruos)")]
    [Tooltip("Si es verdadero, el monstruo reacciona a ciertos resultados del dado durante el combate")]
    public bool hasCombatTrigger = false;

    [Tooltip("Valor exacto del dado del atacante que activa el efecto (ej: 5)")]
    public int combatTriggerRollValue = 0;

    [Tooltip("Daño que recibe el atacante cuando se activa el trigger")]
    public int combatTriggerAttackerDamage = 0;

    [Tooltip("Curación que recibe el monstruo cuando se activa el trigger")]
    public int combatTriggerMonsterHeal = 0;
    
    [Header("Propiedades Especiales")]
    [Tooltip("Es una carta única (solo puede haber una en juego)")]
    public bool isUnique = false;
    
    [Tooltip("Puede ser jugada durante el turno de otro jugador")]
    public bool canPlayOnOtherTurn = false;
    
    [Tooltip("Se descarta inmediatamente después de usar")]
    public bool isSingleUse = true;
    
    [Tooltip("Es un objeto pasivo (permanece en juego)")]
    public bool isPassive = false;
    
    [Header("Persistencia del Objeto")]
    [Tooltip("Si es verdadero, el objeto es eterno y no puede perderse (objeto inicial del personaje)")]
    public bool isEternal = false;

    [Header("Auto-Asignación de Sprites")]
    [Tooltip("Si está activo, al cambiar el tipo de carta se intentará asignar automáticamente un sprite de dorso por defecto.")]
    public bool autoAssignBackSprite = true;

    [Header("Loot - Opciones")]
    [Tooltip("Si es un Trinket: al usarse se convierte en un Tesoro permanente")]
    public bool lootConvertsToTreasure = false;
    [Tooltip("Si 'Trinket', tesoro al que se convierte")]
    public CardDataSO lootTreasureResult;

    [Header("Treasure - Opciones")]
    [Tooltip("Este tesoro puede destruirse por penalización. Los objetos eternos no son destruibles.")]
    public bool treasureDestroyable = true;

    [Header("Monster - Opciones")]
    [Tooltip("Rango del enemigo (normal o jefe)")]
    public MonsterRank monsterRank = MonsterRank.Normal;
    [Tooltip("Almas fijas otorgadas por el jefe (además de otras recompensas)")]
    public int bossSouls = 0;

    /// <summary>
    /// Convierte este ScriptableObject a CardData para el sistema de juego
    /// </summary>
    public CardData ToCardData()
    {
        CardData data = new CardData(cardId, cardName, cardType, "");
        data.description = description;
    // Stats de monstruo
    data.health = health;
    data.diceRequirement = diceRequirement;
    data.attackDamage = attackDamage;

    // Recompensas
    data.rewardCoins = rewardCoins;
    data.rewardLootCards = rewardLootCards;
    data.rewardTreasure = rewardTreasure;
    data.rewardSoulsMin = rewardSoulsMin;
    data.rewardSoulsMax = rewardSoulsMax;
        data.isUnique = isUnique;
        data.canPlayOnOtherTurn = canPlayOnOtherTurn;
        data.isSingleUse = isSingleUse;
    data.isPassive = isPassive;
    data.isEternal = isEternal;
        data.frontSprite = frontSprite;
        data.backSprite = backSprite;
        data.sourceScriptableObject = this; // Mantener referencia al SO original
        
    // Trigger de combate
    data.hasCombatTrigger = hasCombatTrigger;
    data.combatTriggerRollValue = combatTriggerRollValue;
    data.combatTriggerAttackerDamage = combatTriggerAttackerDamage;
    data.combatTriggerMonsterHeal = combatTriggerMonsterHeal;
        
        return data;
    }

    /// <summary>
    /// Ejecuta todos los efectos de esta carta
    /// </summary>
    public void ExecuteEffects(PlayerData player, GameManager gameManager, TargetSelection target = null)
    {
        if (effects == null || effects.Count == 0)
        {
            Debug.LogWarning($"[CardDataSO] {cardName} no tiene efectos para ejecutar");
            return;
        }

        CardData cardData = ToCardData();
        
        foreach (var effect in effects)
        {
            if (effect != null)
            {
                effect.Execute(player, cardData, gameManager, target);
            }
            else
            {
                Debug.LogWarning($"[CardDataSO] {cardName} tiene un efecto null en su lista");
            }
        }
    }

    /// <summary>
    /// Verifica si todos los efectos de la carta pueden ejecutarse
    /// </summary>
    public bool CanExecuteEffects(PlayerData player, GameManager gameManager)
    {
        if (effects == null || effects.Count == 0)
        {
            return true; // Sin efectos = siempre puede ejecutarse
        }

        CardData cardData = ToCardData();
        
        foreach (var effect in effects)
        {
            if (effect != null && !effect.CanExecute(player, cardData, gameManager))
            {
                return false;
            }
        }
        
        return true;
    }

    private void OnValidate()
    {
        // Enforcers solicitados
        switch (cardType)
        {
            case CardType.Loot:
                // Todas las cartas de Loot son de 1 solo uso
                isSingleUse = true;
                // Algunas cartas de Loot pueden ser únicas, no forzamos false para permitir casos especiales
                break;
            case CardType.Treasure:
            case CardType.Monster:
            case CardType.Room:
            case CardType.Character:
                // Únicos por regla
                isUnique = true;
                break;
        }

        // Eterno siempre único
        if (isEternal)
        {
            isUnique = true;
            treasureDestroyable = false; // Los eternos no se destruyen
        }

        // Regla: Todos los objetos activos (Tesoro no pasivo) pueden usarse en turnos de cualquier jugador.
        // Enforzamos el flag por defecto para reflejar esta regla en datos.
        if (cardType == CardType.Treasure && !isPassive)
        {
            canPlayOnOtherTurn = true;
        }

        // Asegurar que el nombre del asset coincida con el nombre de la carta
        if (!string.IsNullOrEmpty(cardName) && name != cardName)
        {
            // Nota: esto solo funciona en el editor
            #if UNITY_EDITOR
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (!string.IsNullOrEmpty(assetPath))
            {
                UnityEditor.AssetDatabase.RenameAsset(assetPath, cardName);
            }
            #endif
        }
    }
}

/// <summary>
/// Uso de cartas de loot
/// </summary>
public enum LootUsageType
{
    SingleUse,          // Se descarta al usar
    MultiUse,           // Puede usarse más de una vez
    ConvertsToTreasure  // Al usar, se convierte en un tesoro
}

/// <summary>
/// Plantillas típicas de efectos de loot (metadatos para guía de diseño)
/// </summary>
public enum LootPresetEffect
{
    None,
    GainCoins,          // lootAmount = monedas
    BombDamage,         // lootAmount = daño
    PeekTopOfDeck,      // lootTargetDeck, lootTopN
    RearrangeTopOfDeck, // lootTargetDeck, lootTopN
    PeekAndReturn       // lootTargetDeck, lootTopN
}

/// <summary>
/// Rango de monstruo
/// </summary>
public enum MonsterRank
{
    Normal,
    Boss
}
