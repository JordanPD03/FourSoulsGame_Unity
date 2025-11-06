using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Gestiona los slots de monstruos en el tablero.
/// Llena los slots al inicio y cuando un monstruo es derrotado.
/// </summary>
public class MonsterSlotManager : MonoBehaviour
{
    public static MonsterSlotManager Instance { get; private set; }

    [Header("Monster Slots")]
    [Tooltip("Slots donde aparecen los monstruos en el tablero")]
    [SerializeField] private List<MonsterSlot> monsterSlots = new List<MonsterSlot>();
    
    [Header("Dynamic Slots Configuration")]
    [Tooltip("Prefab del MonsterSlot para crear slots dinámicamente")]
    [SerializeField] private GameObject monsterSlotPrefab;
    
    [Tooltip("Contenedor donde se instancian los nuevos slots")]
    [SerializeField] private Transform slotsContainer;
    
    [Tooltip("Número inicial de slots al comenzar la partida")]
    [SerializeField] private int initialSlotCount = 2;
    
    [Tooltip("Número máximo de slots permitidos")]
    [SerializeField] private int maxSlotCount = 4;

    [Header("Prefab Configuration")]
    [Tooltip("Prefab de carta de monstruo para instanciar en los slots")]
    [SerializeField] private GameObject monsterCardPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Crea un GameObject de carta de monstruo configurado para el CardData dado (sin asignarlo a ningún slot).
    /// </summary>
    public GameObject CreateMonsterCardObject(CardData monsterCard)
    {
        if (monsterCardPrefab == null)
        {
            Debug.LogError("[MonsterSlotManager] No hay prefab de carta de monstruo asignado");
            return null;
        }
        GameObject cardObj = Instantiate(monsterCardPrefab);
        CardUI cardUI = cardObj.GetComponent<CardUI>();
        if (cardUI != null)
        {
            Sprite frontSprite = monsterCard.frontSprite;
            if (frontSprite == null && !string.IsNullOrEmpty(monsterCard.frontSpritePath))
            {
                frontSprite = Resources.Load<Sprite>(monsterCard.frontSpritePath);
            }
            cardUI.SetCardData(monsterCard, frontSprite, null);
        }
        return cardObj;
    }

    /// <summary>
    /// Coloca un monstruo como overlay encima del slot indicado.
    /// </summary>
    public void PlaceOverlayMonster(MonsterSlot slot, CardData monsterCard)
    {
        if (slot == null || monsterCard == null) return;
        var obj = CreateMonsterCardObject(monsterCard);
        if (obj == null) return;
        slot.SetMonsterOverlay(monsterCard, obj);
    }

    private void Start()
    {
        // Crear slots iniciales si no hay slots en la lista
        if (monsterSlots.Count == 0 && monsterSlotPrefab != null && slotsContainer != null)
        {
            CreateInitialSlots();
        }
        // Ya no llenamos automáticamente aquí; el GameManager llamará a FillInitialSlots
        // una vez que la configuración del juego haya terminado (después de la selección de personajes).
    }

    /// <summary>
    /// Llena todos los slots vacíos con monstruos del mazo
    /// </summary>
    public void FillInitialSlots()
    {
        Debug.Log($"[MonsterSlotManager] Llenando {initialSlotCount} slots iniciales de monstruos");
        
        int slotsToFill = Mathf.Min(initialSlotCount, monsterSlots.Count);
        
        for (int i = 0; i < slotsToFill; i++)
        {
            if (monsterSlots[i] != null && !monsterSlots[i].HasMonster())
            {
                DrawMonsterToSlot(monsterSlots[i]);
            }
        }
    }

    /// <summary>
    /// Roba un monstruo del mazo y lo coloca en el slot especificado
    /// </summary>
    public void DrawMonsterToSlot(MonsterSlot slot)
    {
        if (slot == null)
        {
            Debug.LogWarning("[MonsterSlotManager] Slot es null");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("[MonsterSlotManager] No hay GameManager");
            return;
        }

        // Robar carta de monstruo del mazo
        CardData monsterCard = GameManager.Instance.DrawCard(DeckType.Monster, null);
        
        if (monsterCard == null)
        {
            Debug.LogWarning("[MonsterSlotManager] No hay más monstruos en el mazo");
            return;
        }

        // Debug.Log($"[MonsterSlotManager] Carta robada: {monsterCard.cardName} - HP:{monsterCard.health}, Dice:{monsterCard.diceRequirement}, ATK:{monsterCard.attackDamage}, Rewards: {monsterCard.rewardLootCards} Loot, {monsterCard.rewardCoins}¢");

        // Crear instancia visual de la carta
        if (monsterCardPrefab != null)
        {
            GameObject cardObj = Instantiate(monsterCardPrefab, slot.transform);
            
            // Configurar la carta visual
            CardUI cardUI = cardObj.GetComponent<CardUI>();
            if (cardUI != null)
            {
                Sprite frontSprite = monsterCard.frontSprite;
                if (frontSprite == null && !string.IsNullOrEmpty(monsterCard.frontSpritePath))
                {
                    frontSprite = Resources.Load<Sprite>(monsterCard.frontSpritePath);
                }
                
                cardUI.SetCardData(monsterCard, frontSprite, null);
            }

            // Configurar el slot
            slot.SetMonster(monsterCard, cardObj);
        }
        else
        {
            Debug.LogError("[MonsterSlotManager] No hay prefab de carta de monstruo asignado");
        }
    }

    /// <summary>
    /// Derrota un monstruo (lo remueve del slot y rellena con uno nuevo)
    /// </summary>
    public void DefeatMonster(MonsterSlot slot, PlayerData playerWhoDefeated = null)
    {
        if (slot == null || !slot.HasMonster()) return;

        CardData defeatedMonster = slot.GetMonsterData();
        bool wasOverlay = false;
        // Determinar si el derrotado es un overlay comparando con el campo interno (si expuesto)
        // Como no tenemos acceso directo, inferimos: si el slot tiene monstruo base y además overlay, CurrentMonster será overlay.
        // Añadimos método HasOverlay para comprobarlo.
        try { wasOverlay = slot.HasOverlay(); } catch { wasOverlay = false; }
        Debug.Log($"[MonsterSlotManager] Monstruo '{defeatedMonster.cardName}' derrotado");

        // Determinar jugador que recibe la recompensa
        PlayerData rewardPlayer = playerWhoDefeated;
        if (rewardPlayer == null && GameManager.Instance != null)
        {
            rewardPlayer = GameManager.Instance.GetCurrentPlayer();
        }

        // Calcular recompensas (usar exactamente lo configurado, sin defaults)
        int coins = defeatedMonster.rewardCoins;
        int lootCards = defeatedMonster.rewardLootCards;
        int treasures = defeatedMonster.rewardTreasure;
    int soulsMin = defeatedMonster.rewardSoulsMin;
    int soulsMax = defeatedMonster.rewardSoulsMax;
    // Recompensa fija de almas: usar el mayor de ambos campos (permite dejar uno en 0)
    int soulsFixed = Mathf.Max(soulsMin, soulsMax);

        // Debug.Log($"[MonsterSlotManager] Derrotado '{defeatedMonster.cardName}' - Recompensas: {coins}¢, {lootCards} Loot, {treasures} Treasure, {soulsMin}-{soulsMax} Souls");

        // Otorgar recompensas
        if (rewardPlayer != null && GameManager.Instance != null)
        {
            // Monedas base
            if (coins > 0)
            {
                GameManager.Instance.ChangePlayerCoinsFromMonster(rewardPlayer, coins, slot);
                Debug.Log($"[MonsterSlotManager] Otorgadas {coins} monedas a {rewardPlayer.playerName}");
            }

            // Loot fijo
            for (int i = 0; i < lootCards; i++)
            {
                var drawn = GameManager.Instance.DrawCard(DeckType.Loot, rewardPlayer);
                // Debug.Log($"[MonsterSlotManager] Carta Loot #{i + 1} robada para {rewardPlayer.playerName}");
                // Si es el jugador actual, añadir visualmente a la mano
                if (GameManager.Instance.GetCurrentPlayer() == rewardPlayer && drawn != null)
                {
                    GameManager.Instance.AddCardToCurrentHandUIWithAnimation(drawn);
                }
            }

            // Tesoros
            for (int i = 0; i < treasures; i++)
            {
                var drawn = GameManager.Instance.DrawCard(DeckType.Treasure, rewardPlayer);
                Debug.Log($"[MonsterSlotManager] Carta Treasure #{i + 1} robada para {rewardPlayer.playerName}");
                if (GameManager.Instance.GetCurrentPlayer() == rewardPlayer && drawn != null)
                {
                    GameManager.Instance.AddCardToCurrentHandUIWithAnimation(drawn);
                }
            }

            // Almas (rango)
            if (soulsFixed > 0)
            {
                Debug.Log($"[MonsterSlotManager] Otorgando {soulsFixed} almas a {rewardPlayer.playerName} por derrotar '{defeatedMonster.cardName}'");
                GameManager.Instance.AddSouls(rewardPlayer, soulsFixed);
            }
            else
            {
                Debug.Log($"[MonsterSlotManager] '{defeatedMonster.cardName}' no otorga almas (soulsFixed={soulsFixed})");
            }
        }

        // Si el monstruo NO otorga almas, va al descarte
        bool givesNoSouls = (soulsFixed <= 0);
        if (givesNoSouls && GameManager.Instance != null)
        {
            GameManager.Instance.DiscardMonster(defeatedMonster);
            // Debug.Log($"[MonsterSlotManager] '{defeatedMonster.cardName}' enviado al descarte de monstruos");
        }

        if (wasOverlay)
        {
            // Si era un overlay, eliminar solo el top y mantener el monstruo base en el slot
            slot.RemoveTopMonsterOnly();
        }
        else
        {
            // Si era el monstruo base, limpiar y reponer como siempre
            slot.ClearMonster();
            DrawMonsterToSlot(slot);
        }
    }

    /// <summary>
    /// Aplica daño a un monstruo específico
    /// </summary>
    public void DamageMonster(MonsterSlot slot, int damage)
    {
        if (slot == null || !slot.HasMonster()) return;

        CardData monster = slot.GetMonsterData();
        monster.health -= damage;

        Debug.Log($"[MonsterSlotManager] {monster.cardName} recibe {damage} de daño. Vida restante: {monster.health}");

        // Actualizar UI del slot
        slot.UpdateHealthDisplay();
        
            // Si murió, derrotarlo (sin animación para evitar errores de DOTween)
        if (monster.health <= 0)
        {
            DefeatMonster(slot, GameManager.Instance != null ? GameManager.Instance.GetCurrentPlayer() : null);
        }
            else
            {
                // Solo reproducir animación si el monstruo sobrevive
                slot.PlayTakeDamageAnimation();
            }
    }

    /// <summary>
    /// Cura a un monstruo específico (no puede exceder su vida inicial)
    /// </summary>
    public void HealMonster(MonsterSlot slot, int healAmount)
    {
        if (slot == null || !slot.HasMonster()) return;

        CardData monster = slot.GetMonsterData();
        
        // Obtener la vida máxima del ScriptableObject original
        int maxHealth = monster.health; // Vida actual
        if (monster.sourceScriptableObject != null)
        {
            maxHealth = monster.sourceScriptableObject.health;
        }

        int oldHealth = monster.health;
        monster.health = Mathf.Min(monster.health + healAmount, maxHealth);
        int actualHeal = monster.health - oldHealth;

        Debug.Log($"[MonsterSlotManager] {monster.cardName} se cura {actualHeal} HP. Vida: {monster.health}/{maxHealth}");

        // Actualizar UI del slot
        slot.UpdateHealthDisplay();
    }

    /// <summary>
    /// Obtiene todos los slots de monstruos
    /// </summary>
    public List<MonsterSlot> GetAllSlots()
    {
        return new List<MonsterSlot>(monsterSlots);
    }

    /// <summary>
    /// Obtiene todos los monstruos activos
    /// </summary>
    public List<CardData> GetActiveMonsters()
    {
        List<CardData> monsters = new List<CardData>();
        foreach (var slot in monsterSlots)
        {
            if (slot.HasMonster())
            {
                monsters.Add(slot.GetMonsterData());
            }
        }
        return monsters;
    }

    /// <summary>
    /// Procesa un resultado de dado durante el combate contra un monstruo y aplica triggers si corresponde.
    /// (Hook opcional hasta tener un sistema de combate completo)
    /// </summary>
    public void ProcessCombatRoll(MonsterSlot slot, PlayerData attacker, int rollValue)
    {
        if (slot == null || !slot.HasMonster() || GameManager.Instance == null) return;

        CardData monster = slot.GetMonsterData();
        if (monster.hasCombatTrigger && monster.combatTriggerRollValue == rollValue)
        {
            // Animación de trigger del monstruo
            slot.PlayTriggerAnimation();

            // Daño al atacante
            int dmg = Mathf.Max(0, monster.combatTriggerAttackerDamage);
            if (dmg > 0 && attacker != null)
            {
                GameManager.Instance.DamagePlayer(attacker, dmg);
                Debug.Log($"[MonsterSlotManager] Trigger de combate: tirada {rollValue}. {attacker.playerName} recibe {dmg} de daño");
            }

            // Curación del monstruo
            int heal = Mathf.Max(0, monster.combatTriggerMonsterHeal);
            if (heal > 0)
            {
                HealMonster(slot, heal);
                slot.PlayHealAnimation();
                Debug.Log($"[MonsterSlotManager] Trigger de combate: tirada {rollValue}. {monster.cardName} se cura {heal} HP");
            }
        }
    }
    
    #region Dynamic Slot Management
    
    /// <summary>
    /// Crea los slots iniciales al comenzar la partida
    /// </summary>
    private void CreateInitialSlots()
    {
        Debug.Log($"[MonsterSlotManager] Creando {initialSlotCount} slots iniciales");
        
        for (int i = 0; i < initialSlotCount; i++)
        {
            AddMonsterSlot();
        }
    }
    
    /// <summary>
    /// Añade un nuevo slot de monstruo (usado por cartas/efectos)
    /// </summary>
    public bool AddMonsterSlot()
    {
        if (monsterSlots.Count >= maxSlotCount)
        {
            Debug.LogWarning($"[MonsterSlotManager] No se pueden añadir más slots. Máximo: {maxSlotCount}");
            return false;
        }
        
        if (monsterSlotPrefab == null)
        {
            Debug.LogError("[MonsterSlotManager] No hay prefab de MonsterSlot asignado");
            return false;
        }
        
        if (slotsContainer == null)
        {
            Debug.LogWarning("[MonsterSlotManager] No hay contenedor de slots. Usando este transform");
            slotsContainer = transform;
        }
        
        // Instanciar nuevo slot
        GameObject slotObj = Instantiate(monsterSlotPrefab, slotsContainer);
        MonsterSlot newSlot = slotObj.GetComponent<MonsterSlot>();
        
        if (newSlot != null)
        {
            monsterSlots.Add(newSlot);
            Debug.Log($"[MonsterSlotManager] Nuevo slot añadido. Total: {monsterSlots.Count}/{maxSlotCount}");
            
            // Llenar el nuevo slot con un monstruo
            DrawMonsterToSlot(newSlot);
            
            return true;
        }
        else
        {
            Debug.LogError("[MonsterSlotManager] El prefab no tiene componente MonsterSlot");
            Destroy(slotObj);
            return false;
        }
    }
    
    /// <summary>
    /// Remueve un slot de monstruo (usado por efectos)
    /// </summary>
    public bool RemoveMonsterSlot(MonsterSlot slot)
    {
        if (slot == null || !monsterSlots.Contains(slot))
        {
            Debug.LogWarning("[MonsterSlotManager] Slot no encontrado o es null");
            return false;
        }
        
        // Limpiar el slot antes de removerlo
        slot.ClearMonster();
        
        // Remover de la lista
        monsterSlots.Remove(slot);
        
        // Destruir el GameObject
        Destroy(slot.gameObject);
        
        Debug.Log($"[MonsterSlotManager] Slot removido. Total: {monsterSlots.Count}");
        return true;
    }
    
    /// <summary>
    /// Obtiene el número actual de slots
    /// </summary>
    public int GetSlotCount()
    {
        return monsterSlots.Count;
    }
    
    /// <summary>
    /// Verifica si se puede añadir más slots
    /// </summary>
    public bool CanAddMoreSlots()
    {
        return monsterSlots.Count < maxSlotCount;
    }
    
    #endregion
}
