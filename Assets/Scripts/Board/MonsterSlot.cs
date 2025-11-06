using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

/// <summary>
/// Representa un slot individual donde se coloca un monstruo en el tablero.
/// Maneja la visualización y el estado del monstruo.
/// </summary>
public class MonsterSlot : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Objeto visual de la carta de monstruo base (se instancia dinámicamente)")]
    private GameObject monsterCardObject;
    
    [Tooltip("Datos del monstruo base en este slot")]
    private CardData currentMonster;

    // Sistema de pila de overlays (monstruos colocados encima del existente)
    private class OverlayCard
    {
        public CardData data;
        public GameObject gameObject;
    }
    private List<OverlayCard> overlayStack = new List<OverlayCard>();

    /// <summary>
    /// Propiedad pública para acceder a los datos del monstruo actual (el de hasta arriba)
    /// </summary>
    public CardData CurrentMonster
    {
        get
        {
            // El monstruo activo es el último overlay de la pila, o el base si no hay overlays
            if (overlayStack.Count > 0)
                return overlayStack[overlayStack.Count - 1].data;
            return currentMonster;
        }
    }

    [Header("Text Displays")]
    [Tooltip("Texto que muestra la vida del monstruo (HP)")]
    [SerializeField] private TMP_Text healthText;
    
    [Tooltip("Texto del requisito de dado para dañar al monstruo")]
    [SerializeField] private TMP_Text diceText;
    
    [Tooltip("Texto que muestra el ataque del monstruo (ATK)")]
    [SerializeField] private TMP_Text attackText;

    [Header("Auto-bind Hints (optional)")]
    [Tooltip("Nombre o parte del nombre del objeto de texto de HP (ej: 'HPText')")] 
    [SerializeField] private string healthTextNameHint;
    [Tooltip("Nombre o parte del nombre del objeto de texto de Dado (ej: 'DiceText' o 'Req')")] 
    [SerializeField] private string diceTextNameHint;
    [Tooltip("Nombre o parte del nombre del objeto de texto de ATK (ej: 'ATKText' o 'Damage')")] 
    [SerializeField] private string attackTextNameHint;

    [Header("Visual Feedback")]
    [Tooltip("Transform donde se posiciona la carta")]
    [SerializeField] private Transform cardAnchor;
    
    [Tooltip("Escala local de la carta de monstruo (ej: 0.3 para reducir tamaño)")]
    [SerializeField] private float cardScale = 0.3f;

    [Header("Animation Settings")]
    [Tooltip("Duración de la animación de entrada")]
    [SerializeField] private float spawnAnimationDuration = 0.5f;
    
    [Tooltip("Altura desde donde cae la carta")]
    [SerializeField] private float spawnDropHeight = 2f;

    // Highlight de combate
    private bool combatHighlighted = false;
    private Color originalSpriteColor = Color.white;

    private void Awake()
    {
        if (cardAnchor == null)
        {
            cardAnchor = transform;
        }

        // Intentar auto-vincular textos si no están asignados en el inspector
        EnsureStatTextsBound();
    }

    /// <summary>
    /// Asigna un monstruo a este slot
    /// </summary>
    public void SetMonster(CardData monsterData, GameObject cardObject)
    {
        // Limpiar monstruo anterior si existe
        ClearMonster();

        currentMonster = monsterData;
        monsterCardObject = cardObject;

        // Asegurar que los textos estén vinculados
        EnsureStatTextsBound();

        // Posicionar la carta en el anchor
        if (cardObject != null && cardAnchor != null)
        {
            cardObject.transform.SetParent(cardAnchor);
            cardObject.transform.localPosition = Vector3.zero;
            cardObject.transform.localScale = Vector3.one * cardScale;
            
            // Asegurar que la carta se renderiza por encima del slot
            var cardSR = cardObject.GetComponent<SpriteRenderer>();
            if (cardSR == null) cardSR = cardObject.GetComponentInChildren<SpriteRenderer>();
            if (cardSR != null)
            {
                cardSR.sortingOrder = 10; // Mayor que el MonsterSlot (asume que el slot usa 0)
            }
            
            // Animar entrada
            PlaySpawnAnimation();
        }

        // Actualizar WorldTargetable si existe
        var worldTargetable = GetComponent<WorldTargetable>();
        if (worldTargetable != null)
        {
            worldTargetable.targetType = TargetType.Monster;
            worldTargetable.monsterCardUI = monsterData;
        }
        // Configurar WorldTargetable en el objeto de la carta
        if (monsterCardObject != null)
        {
            var cardWT = monsterCardObject.GetComponent<WorldTargetable>();
            bool wasJustAdded = false;
            
            if (cardWT == null)
            {
                // Debug.Log($"[MonsterSlot] Añadiendo WorldTargetable a {monsterCardObject.name}");
                cardWT = monsterCardObject.AddComponent<WorldTargetable>();
                wasJustAdded = true;
            }
            
            if (cardWT != null)
            {
                cardWT.targetType = TargetType.Monster;
                cardWT.monsterCardUI = monsterData;
                // Asegurar que esté habilitado (importante si se reutiliza el objeto)
                cardWT.enabled = true;
                
                // Si acabamos de añadir el componente, inicializarlo manualmente
                if (wasJustAdded)
                {
                    // Añadir BoxCollider2D si no existe
                    var col = monsterCardObject.GetComponent<BoxCollider2D>();
                    if (col == null)
                    {
                        col = monsterCardObject.AddComponent<BoxCollider2D>();
                        // Debug.Log($"[MonsterSlot] BoxCollider2D añadido a {monsterCardObject.name}");
                    }
                    
                    // Ajustar collider al sprite
                    var sr = monsterCardObject.GetComponent<SpriteRenderer>();
                    if (sr == null) sr = monsterCardObject.GetComponentInChildren<SpriteRenderer>();
                    
                    if (sr != null && col != null)
                    {
                        var bounds = sr.bounds;
                        var sizeWorld = bounds.size;
                        var centerWorld = bounds.center;
                        
                        Vector3 lossy = monsterCardObject.transform.lossyScale;
                        float sx = Mathf.Approximately(lossy.x, 0f) ? 1f : lossy.x;
                        float sy = Mathf.Approximately(lossy.y, 0f) ? 1f : lossy.y;
                        Vector2 sizeLocal = new Vector2(sizeWorld.x / sx, sizeWorld.y / sy);
                        Vector2 centerLocal = monsterCardObject.transform.InverseTransformPoint(centerWorld);
                        
                        col.size = sizeLocal;
                        col.offset = centerLocal;
                        
                        // Debug.Log($"[MonsterSlot] Collider ajustado: size={col.size}, offset={col.offset}");
                    }
                    else if (sr == null)
                    {
                        Debug.LogWarning($"[MonsterSlot] No se encontró SpriteRenderer en {monsterCardObject.name}!");
                    }
                }
                else
                {
                    // Si ya existía el componente, asegurar que el collider también esté habilitado
                    var col = monsterCardObject.GetComponent<BoxCollider2D>();
                    if (col != null)
                    {
                        col.enabled = true;
                    }
                }
                
                // Debug.Log($"[MonsterSlot] WorldTargetable configurado: targetType={cardWT.targetType}, hasCollider={monsterCardObject.GetComponent<BoxCollider2D>() != null}");
            }
            else
            {
                Debug.LogError($"[MonsterSlot] No se pudo añadir WorldTargetable a {monsterCardObject.name}");
            }
        }

        // Actualizar displays
        UpdateHealthDisplay();
        UpdateDiceDisplay();
        UpdateAttackDisplay();

    // Debug.Log($"[MonsterSlot] Monstruo '{monsterData.cardName}' asignado (HP: {monsterData.health}, ATK: {monsterData.attackDamage})");
    }

    /// <summary>
    /// Coloca un monstruo en forma de overlay (encima del existente). El overlay pasa a ser el monstruo activo.
    /// </summary>
    public void SetMonsterOverlay(CardData monsterData, GameObject cardObject)
    {
        // Crear nuevo overlay
        var newOverlay = new OverlayCard
        {
            data = monsterData,
            gameObject = cardObject
        };
        
        // Ocultar el monstruo anterior (sea base o último overlay)
        HideCardVisually(GetTopCardObject());
        
        // Añadir a la pila
        overlayStack.Add(newOverlay);

        if (cardObject != null && cardAnchor != null)
        {
            cardObject.transform.SetParent(cardAnchor);
            // Offset vertical según la posición en la pila para efecto visual
            float stackOffset = overlayStack.Count * 0.02f;
            cardObject.transform.localPosition = Vector3.zero + Vector3.up * stackOffset;
            cardObject.transform.localScale = Vector3.one * cardScale;

            var sr = cardObject.GetComponent<SpriteRenderer>();
            if (sr == null) sr = cardObject.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                // Sorting order más alto para cada nivel de la pila
                sr.sortingOrder = 20 + overlayStack.Count;
            }

            // Asegurar WorldTargetable en overlay para interacciones
            var cardWT = cardObject.GetComponent<WorldTargetable>();
            if (cardWT == null)
            {
                cardWT = cardObject.AddComponent<WorldTargetable>();
            }
            
            if (cardWT != null)
            {
                cardWT.targetType = TargetType.Monster;
                cardWT.monsterCardUI = monsterData;
                cardWT.enabled = true;
                
                // Configurar collider
                var col = cardObject.GetComponent<BoxCollider2D>();
                if (col == null)
                {
                    col = cardObject.AddComponent<BoxCollider2D>();
                }
                
                if (col != null)
                {
                    col.enabled = true;
                    
                    // Ajustar collider al sprite del overlay
                    if (sr != null)
                    {
                        var bounds = sr.bounds;
                        var sizeWorld = bounds.size;
                        var centerWorld = bounds.center;
                        
                        Vector3 lossy = cardObject.transform.lossyScale;
                        float sx = Mathf.Approximately(lossy.x, 0f) ? 1f : Mathf.Abs(lossy.x);
                        float sy = Mathf.Approximately(lossy.y, 0f) ? 1f : Mathf.Abs(lossy.y);
                        Vector2 sizeLocal = new Vector2(sizeWorld.x / sx, sizeWorld.y / sy);
                        Vector2 centerLocal = cardObject.transform.InverseTransformPoint(centerWorld);
                        
                        col.size = sizeLocal;
                        col.offset = centerLocal;
                    }
                }
            }

            // IMPORTANTE: Actualizar el CardUI del overlay con las estadísticas correctas
            var cardUI = cardObject.GetComponent<CardUI>();
            if (cardUI != null)
            {
                Sprite frontSprite = monsterData.frontSprite;
                if (frontSprite == null && !string.IsNullOrEmpty(monsterData.frontSpritePath))
                {
                    frontSprite = Resources.Load<Sprite>(monsterData.frontSpritePath);
                }
                cardUI.SetCardData(monsterData, frontSprite, null);
            }

            // ACTUALIZAR textos TMP dentro del overlay si los tiene
            var overlayTexts = cardObject.GetComponentsInChildren<TMP_Text>(true);
            int updatedCount = 0;
            foreach (var txt in overlayTexts)
            {
                string objName = txt.gameObject.name.ToLower();
                
                if (objName.Contains("hp") || objName.Contains("health") || objName.Contains("vida"))
                {
                    txt.text = monsterData.health.ToString();
                    updatedCount++;
                }
                else if (objName.Contains("dice") || objName.Contains("dado") || objName.Contains("req"))
                {
                    int req = Mathf.Max(1, monsterData.diceRequirement);
                    txt.text = $"{req}+";
                    updatedCount++;
                }
                else if (objName.Contains("attack") || objName.Contains("ataque") || objName.Contains("atk"))
                {
                    txt.text = monsterData.attackDamage.ToString();
                    updatedCount++;
                }
            }
            
            if (updatedCount > 0)
            {
                Debug.Log($"[MonsterSlot] Overlay '{monsterData.cardName}' añadido a pila (nivel {overlayStack.Count}): Actualizados {updatedCount} textos internos");
            }

            PlaySpawnAnimationFor(cardObject);
        }

        // Ocultar la carta base también si es el primer overlay
        if (overlayStack.Count == 1 && monsterCardObject != null)
        {
            HideCardVisually(monsterCardObject);
        }

        // ASEGURAR que solo el top esté habilitado para interacciones
        UpdateInteractionStates();

        // Actualizar textos contra el overlay actual (monstruo activo)
        EnsureStatTextsBound();
        UpdateHealthDisplay();
        UpdateDiceDisplay();
        UpdateAttackDisplay();
    }

    /// <summary>
    /// Oculta visualmente un GameObject de carta (sprites, textos e imágenes)
    /// </summary>
    private void HideCardVisually(GameObject cardObj)
    {
        if (cardObj == null) return;
        
        // Ocultar TODOS los SpriteRenderer (no solo el principal, sino también íconos)
        var allSprites = cardObj.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in allSprites)
        {
            sr.enabled = false;
        }
        
        // Ocultar todos los textos
        var texts = cardObj.GetComponentsInChildren<TMP_Text>(true);
        foreach (var txt in texts)
        {
            txt.enabled = false;
        }
        
        // Ocultar todas las imágenes UI (íconos, etc.)
        var images = cardObj.GetComponentsInChildren<UnityEngine.UI.Image>(true);
        foreach (var img in images)
        {
            img.enabled = false;
        }
    }

    /// <summary>
    /// Muestra visualmente un GameObject de carta (sprites, textos e imágenes)
    /// </summary>
    private void ShowCardVisually(GameObject cardObj)
    {
        if (cardObj == null) return;
        
        // Mostrar TODOS los SpriteRenderer (no solo el principal, sino también íconos)
        var allSprites = cardObj.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in allSprites)
        {
            sr.enabled = true;
        }
        
        // Mostrar todos los textos
        var texts = cardObj.GetComponentsInChildren<TMP_Text>(true);
        foreach (var txt in texts)
        {
            txt.enabled = true;
        }
        
        // Mostrar todas las imágenes UI (íconos, etc.)
        var images = cardObj.GetComponentsInChildren<UnityEngine.UI.Image>(true);
        foreach (var img in images)
        {
            img.enabled = true;
        }
    }

    /// <summary>
    /// Actualiza los estados de interacción de todas las cartas en la pila
    /// </summary>
    private void UpdateInteractionStates()
    {
        // Deshabilitar la carta base si hay overlays
        if (monsterCardObject != null)
        {
            bool hasOverlays = overlayStack.Count > 0;
            SetCardInteractionEnabled(monsterCardObject, !hasOverlays);
        }
        
        // Deshabilitar todos los overlays excepto el último
        for (int i = 0; i < overlayStack.Count; i++)
        {
            bool isTop = (i == overlayStack.Count - 1);
            SetCardInteractionEnabled(overlayStack[i].gameObject, isTop);
        }
    }

    /// <summary>
    /// Habilita/deshabilita la interacción (WorldTargetable y collider) de una carta
    /// </summary>
    private void SetCardInteractionEnabled(GameObject cardObj, bool enabled)
    {
        if (cardObj == null) return;
        
        var wt = cardObj.GetComponent<WorldTargetable>();
        if (wt != null) wt.enabled = enabled;
        
        var col = cardObj.GetComponent<BoxCollider2D>();
        if (col != null) col.enabled = enabled;
    }

    public bool HasOverlay()
    {
        return overlayStack.Count > 0;
    }

    /// <summary>
    /// Elimina solo el monstruo superior (overlay), manteniendo el monstruo base en el slot.
    /// </summary>
    public void RemoveTopMonsterOnly()
    {
        if (overlayStack.Count == 0) return;
        
        // Obtener el último overlay
        var topOverlay = overlayStack[overlayStack.Count - 1];
        
        if (topOverlay.gameObject != null)
        {
            // IMPORTANTE: Matar TODAS las animaciones DOTween del overlay antes de destruirlo
            var t = topOverlay.gameObject.transform;
            if (t != null) t.DOKill(true);
            
            // Matar animaciones del SpriteRenderer también
            var sr = topOverlay.gameObject.GetComponent<SpriteRenderer>();
            if (sr == null) sr = topOverlay.gameObject.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.DOKill(true);
            
            var wt = topOverlay.gameObject.GetComponent<WorldTargetable>();
            if (wt != null) wt.monsterCardUI = null;
            
            Destroy(topOverlay.gameObject);
        }
        
        // Remover de la pila
        overlayStack.RemoveAt(overlayStack.Count - 1);

        // Si aún hay overlays, mostrar el siguiente
        if (overlayStack.Count > 0)
        {
            var nextTop = overlayStack[overlayStack.Count - 1];
            ShowCardVisually(nextTop.gameObject);
            UpdateInteractionStates();
        }
        else
        {
            // No hay más overlays, mostrar y habilitar la carta base
            if (monsterCardObject != null)
            {
                ShowCardVisually(monsterCardObject);
                SetCardInteractionEnabled(monsterCardObject, true);
            }
        }

        // Re-vincular textos a la carta activa (que ahora es el siguiente overlay o la base)
        EnsureStatTextsBound();
        UpdateHealthDisplay();
        UpdateDiceDisplay();
        UpdateAttackDisplay();
    }

    /// <summary>
    /// Vincula automáticamente los TMP_Text si no están asignados
    /// </summary>
    private void EnsureStatTextsBound()
    {
        bool missingHealth = (healthText == null);
        bool missingDice = (diceText == null);
        bool missingAttack = (attackText == null);

        if (!missingHealth && !missingDice && !missingAttack)
        {
            return; // todo ya asignado
        }

        // Preferir textos dentro del objeto de carta ACTIVO (overlay si existe, sino base)
        var topObj = GetTopCardObject();
        if (topObj != null)
        {
            TryBindByHints(topObj.transform, ref missingHealth, ref missingDice, ref missingAttack);
            if (missingHealth || missingDice || missingAttack)
                TryBindTextsFromHierarchy(topObj.transform, ref missingHealth, ref missingDice, ref missingAttack);
        }

        // Luego intentar en el propio slot si aún faltan
        if (missingHealth || missingDice || missingAttack)
        {
            TryBindByHints(transform, ref missingHealth, ref missingDice, ref missingAttack);
            if (missingHealth || missingDice || missingAttack)
                TryBindTextsFromHierarchy(transform, ref missingHealth, ref missingDice, ref missingAttack);
        }

        // Fallback: si sigue faltando ATK y tenemos varios TMP_Text, intentar asignar el que no sea HP ni Dice
        if (attackText == null)
        {
            TMP_Text candidate = FindAttackCandidate();
            if (candidate != null)
            {
                attackText = candidate;
                missingAttack = false;
                // Debug.Log($"[MonsterSlot] Fallback: asignado attackText => {candidate.gameObject.name}");
            }
        }

        if (healthText == null || diceText == null || attackText == null)
        {
            // Si aún no hay carta asignada, es normal que no se puedan auto-asignar en Awake; no advertir todavía
            if (monsterCardObject == null)
                return;

            // Listar TMP_Text encontrados para facilitar diagnóstico
            var allTexts = new System.Text.StringBuilder();
            var list = GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in list)
            {
                allTexts.AppendLine(" - " + GetHierarchyPath(t.transform));
            }
            if (monsterCardObject != null)
            {
                var list2 = monsterCardObject.GetComponentsInChildren<TMP_Text>(true);
                foreach (var t in list2)
                {
                    allTexts.AppendLine(" * " + GetHierarchyPath(t.transform));
                }
            }
            if (overlayStack.Count > 0)
            {
                var topOverlay = overlayStack[overlayStack.Count - 1];
                if (topOverlay.gameObject != null)
                {
                    var list3 = topOverlay.gameObject.GetComponentsInChildren<TMP_Text>(true);
                    foreach (var t in list3)
                    {
                        allTexts.AppendLine(" ^ " + GetHierarchyPath(t.transform));
                    }
                }
            }
            Debug.LogWarning($"[MonsterSlot] No se pudieron auto-asignar todos los textos (HP:{healthText!=null} Dice:{diceText!=null} ATK:{attackText!=null}). Asigna en el Inspector si es necesario.\nTMP_Text encontrados:\n{allTexts}");
        }
    }

    private void TryBindTextsFromHierarchy(Transform root, ref bool missingHealth, ref bool missingDice, ref bool missingAttack)
    {
        var texts = root.GetComponentsInChildren<TMP_Text>(true);
        if (texts == null) return;

        foreach (var t in texts)
        {
            string n = t.gameObject.name.ToLowerInvariant();

            if (missingHealth && (n.Contains("hp") || n.Contains("health") || n.Contains("vida") || n.Contains("hearts") || n.Contains("heart")))
            {
                healthText = t;
                missingHealth = false;
                // Debug.Log($"[MonsterSlot] Auto-asignado healthText => {t.gameObject.name}");
                continue;
            }

            if (missingDice && (n.Contains("dice") || n.Contains("dado") || n.Contains("req") || n.Contains("require") || n.Contains("roll")))
            {
                diceText = t;
                missingDice = false;
                // Debug.Log($"[MonsterSlot] Auto-asignado diceText => {t.gameObject.name}");
                continue;
            }

            if (missingAttack && (n.Contains("atk") || n.Contains("attack") || n.Contains("daño") || n.Contains("damage") || n.Contains("dmg") || n.Contains("sword") || n.Contains("power") || n.Contains("hit")))
            {
                attackText = t;
                missingAttack = false;
                // Debug.Log($"[MonsterSlot] Auto-asignado attackText => {t.gameObject.name}");
                continue;
            }
        }
    }

    private void TryBindByHints(Transform root, ref bool missingHealth, ref bool missingDice, ref bool missingAttack)
    {
        if (root == null) return;
        var texts = root.GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in texts)
        {
            string name = t.gameObject.name;
            string low = name.ToLowerInvariant();

            if (missingHealth && !string.IsNullOrEmpty(healthTextNameHint) && low.Contains(healthTextNameHint.ToLowerInvariant()))
            {
                healthText = t; missingHealth = false; // Debug.Log($"[MonsterSlot] Hint asignó healthText => {name}");
            }
            if (missingDice && !string.IsNullOrEmpty(diceTextNameHint) && low.Contains(diceTextNameHint.ToLowerInvariant()))
            {
                diceText = t; missingDice = false; // Debug.Log($"[MonsterSlot] Hint asignó diceText => {name}");
            }
            if (missingAttack && !string.IsNullOrEmpty(attackTextNameHint) && low.Contains(attackTextNameHint.ToLowerInvariant()))
            {
                attackText = t; missingAttack = false; // Debug.Log($"[MonsterSlot] Hint asignó attackText => {name}");
            }
        }
    }

    private TMP_Text FindAttackCandidate()
    {
        // Reunir todos los TMP_Text en slot y carta
        var list = new System.Collections.Generic.List<TMP_Text>();
        list.AddRange(GetComponentsInChildren<TMP_Text>(true));
        if (monsterCardObject != null)
            list.AddRange(monsterCardObject.GetComponentsInChildren<TMP_Text>(true));

        // Excluir los ya asignados
        list.RemoveAll(t => t == null || t == healthText || t == diceText);

        // Preferir uno cuyo nombre contenga números o que esté cerca de un icono de espada, etc. (simplificado: devuelve el primero)
        return list.Count > 0 ? list[0] : null;
    }

    private static string GetHierarchyPath(Transform t)
    {
        if (t == null) return "<null>";
        System.Collections.Generic.Stack<string> parts = new System.Collections.Generic.Stack<string>();
        var cur = t;
        while (cur != null)
        {
            parts.Push(cur.name);
            cur = cur.parent;
        }
        return string.Join("/", parts.ToArray());
    }

    private GameObject GetTopCardObject()
    {
        if (overlayStack.Count > 0)
        {
            return overlayStack[overlayStack.Count - 1].gameObject;
        }
        return monsterCardObject;
    }

    /// <summary>
    /// Remueve el monstruo de este slot
    /// </summary>
    public void ClearMonster()
    {
        // Eliminar todos los overlays
        foreach (var overlay in overlayStack)
        {
            if (overlay.gameObject != null)
            {
                var tOv = overlay.gameObject.transform;
                if (tOv != null) tOv.DOKill(true);
                
                // Matar animaciones del SpriteRenderer del overlay
                var srOv = overlay.gameObject.GetComponent<SpriteRenderer>();
                if (srOv == null) srOv = overlay.gameObject.GetComponentInChildren<SpriteRenderer>();
                if (srOv != null) srOv.DOKill(true);
                
                var wt = overlay.gameObject.GetComponent<WorldTargetable>();
                if (wt != null) wt.monsterCardUI = null;
                Destroy(overlay.gameObject);
            }
        }
        overlayStack.Clear();
        
        // Si la preview está mostrando este monstruo, ocultarla
        if (CardPreviewUI.Instance != null && CardPreviewUI.Instance.IsShowingMonster(this))
        {
            CardPreviewUI.Instance.HidePreview();
        }

        if (monsterCardObject != null)
        {
            // Detener tweens activos para evitar errores de DOTween sobre objetos destruidos
            var t = monsterCardObject.transform;
            if (t != null)
            {
                t.DOKill(true);
            }
            
            // Matar animaciones del SpriteRenderer de la carta base
            var sr = monsterCardObject.GetComponent<SpriteRenderer>();
            if (sr == null) sr = monsterCardObject.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.DOKill(true);
            
            // Limpiar WorldTargetable en la carta si existe
            var cardWT = monsterCardObject.GetComponent<WorldTargetable>();
            if (cardWT != null)
            {
                cardWT.monsterCardUI = null;
            }
            Destroy(monsterCardObject);
            monsterCardObject = null;
        }

        currentMonster = null;

        // Limpiar WorldTargetable si existe
        var worldTargetable = GetComponent<WorldTargetable>();
        if (worldTargetable != null)
        {
            worldTargetable.monsterCardUI = null;
        }

        // Limpiar UI
        if (healthText != null) healthText.text = "";
        if (diceText != null) diceText.text = "";
        if (attackText != null) attackText.text = "";

        // Forzar re-autoasignación para el próximo monstruo
        healthText = null;
        diceText = null;
        attackText = null;
    }

    /// <summary>
    /// Actualiza el display de vida
    /// </summary>
    public void UpdateHealthDisplay()
    {
        if (healthText != null && CurrentMonster != null)
        {
            healthText.text = CurrentMonster.health.ToString();
            // Debug.Log($"[MonsterSlot] UpdateHealthDisplay: {currentMonster.cardName} - HP: {currentMonster.health}");
        }
        else
        {
            Debug.LogWarning($"[MonsterSlot] UpdateHealthDisplay fallado - healthText: {healthText != null}, currentMonster: {CurrentMonster != null}");
        }
        
        // IMPORTANTE: Si hay overlay, actualizar también los textos internos del overlay superior
        if (HasOverlay())
        {
            var topOverlay = overlayStack[overlayStack.Count - 1];
            if (topOverlay.gameObject != null)
            {
                var overlayTexts = topOverlay.gameObject.GetComponentsInChildren<TMP_Text>(true);
                foreach (var txt in overlayTexts)
                {
                    string objName = txt.gameObject.name.ToLower();
                    if (objName.Contains("hp") || objName.Contains("health") || objName.Contains("vida"))
                    {
                        txt.text = topOverlay.data.health.ToString();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Actualiza el display del requisito de dado
    /// </summary>
    public void UpdateDiceDisplay()
    {
        if (diceText != null && CurrentMonster != null)
        {
            int req = Mathf.Max(1, CurrentMonster.diceRequirement);
            diceText.text = $"{req}+";
            // Debug.Log($"[MonsterSlot] UpdateDiceDisplay: {currentMonster.cardName} - Dice: {req}+");
        }
        else
        {
            Debug.LogWarning($"[MonsterSlot] UpdateDiceDisplay fallado - diceText: {diceText != null}, currentMonster: {CurrentMonster != null}");
        }
        
        // IMPORTANTE: Si hay overlay, actualizar también los textos internos del overlay superior
        if (HasOverlay())
        {
            var topOverlay = overlayStack[overlayStack.Count - 1];
            if (topOverlay.gameObject != null)
            {
                var overlayTexts = topOverlay.gameObject.GetComponentsInChildren<TMP_Text>(true);
                foreach (var txt in overlayTexts)
                {
                    string objName = txt.gameObject.name.ToLower();
                    if (objName.Contains("dice") || objName.Contains("dado") || objName.Contains("req"))
                    {
                        int req = Mathf.Max(1, topOverlay.data.diceRequirement);
                        txt.text = $"{req}+";
                    }
                }
            }
        }
    }

    /// <summary>
    /// Actualiza el display de ataque
    /// </summary>
    public void UpdateAttackDisplay()
    {
        if (attackText != null && CurrentMonster != null)
        {
            attackText.text = CurrentMonster.attackDamage.ToString();
            // Debug.Log($"[MonsterSlot] UpdateAttackDisplay: {currentMonster.cardName} - ATK: {currentMonster.attackDamage}");
        }
        else
        {
            Debug.LogWarning($"[MonsterSlot] UpdateAttackDisplay fallado - attackText: {attackText != null}, currentMonster: {CurrentMonster != null}");
        }
        
        // IMPORTANTE: Si hay overlay, actualizar también los textos internos del overlay superior
        if (HasOverlay())
        {
            var topOverlay = overlayStack[overlayStack.Count - 1];
            if (topOverlay.gameObject != null)
            {
                var overlayTexts = topOverlay.gameObject.GetComponentsInChildren<TMP_Text>(true);
                foreach (var txt in overlayTexts)
                {
                    string objName = txt.gameObject.name.ToLower();
                    if (objName.Contains("attack") || objName.Contains("ataque") || objName.Contains("atk"))
                    {
                        txt.text = topOverlay.data.attackDamage.ToString();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Verifica si hay un monstruo en este slot
    /// </summary>
    public bool HasMonster()
    {
        return CurrentMonster != null;
    }

    /// <summary>
    /// Obtiene los datos del monstruo actual
    /// </summary>
    public CardData GetMonsterData()
    {
        return CurrentMonster;
    }

    /// <summary>
    /// Obtiene el GameObject visual del monstruo
    /// </summary>
    public GameObject GetMonsterObject()
    {
        return GetTopCardObject();
    }

    /// <summary>
    /// Animación cuando el monstruo recibe daño
    /// </summary>
    public void PlayTakeDamageAnimation()
    {
        var topObj = GetTopCardObject();
        if (topObj == null) return;

        // Cancelar animaciones previas
        topObj.transform.DOKill();

        // Secuencia: shake + flash rojo
        Sequence damageSequence = DOTween.Sequence();
        
        // Shake horizontal
        damageSequence.Append(topObj.transform.DOShakePosition(0.3f, strength: new Vector3(0.1f, 0.05f, 0f), vibrato: 20, randomness: 90));
        
        // Flash rojo en el SpriteRenderer si existe
        var spriteRenderer = topObj.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = topObj.GetComponentInChildren<SpriteRenderer>();
        
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            // Matar animaciones previas del SpriteRenderer
            spriteRenderer.DOKill();
            // Añadir el flash con SetTarget para poder matarlo después
            damageSequence.Join(spriteRenderer.DOColor(Color.red, 0.1f).SetLoops(2, LoopType.Yoyo).SetTarget(spriteRenderer));
        }

        damageSequence.Play();
    }

    /// <summary>
    /// Animación cuando el monstruo ataca
    /// </summary>
    public void PlayAttackAnimation()
    {
        var atkObj = GetTopCardObject();
        if (atkObj == null) return;

        // Cancelar animaciones previas
    atkObj.transform.DOKill();

    Vector3 originalPos = atkObj.transform.localPosition;

        // Secuencia: avanzar y retroceder con rotación
        Sequence attackSequence = DOTween.Sequence();
        
        // Avanzar hacia adelante (derecha)
    attackSequence.Append(atkObj.transform.DOLocalMoveX(originalPos.x + 0.3f, 0.15f).SetEase(Ease.OutQuad));
        
        
        // Rotación leve al atacar
    attackSequence.Join(atkObj.transform.DOLocalRotate(new Vector3(0, 0, -10f), 0.15f).SetEase(Ease.OutQuad));
        
        // Retroceder a posición original
    attackSequence.Append(atkObj.transform.DOLocalMove(originalPos, 0.2f).SetEase(Ease.InOutQuad));
        
        // Volver a rotación normal
    attackSequence.Join(atkObj.transform.DOLocalRotate(Vector3.zero, 0.2f).SetEase(Ease.InOutQuad));
        
        attackSequence.Play();
    }

    /// <summary>
    /// Animación visual cuando el monstruo activa un efecto/trigger de combate
    /// (pequeño pulso + flash amarillo)
    /// </summary>
    public void PlayTriggerAnimation()
    {
        var trigObj = GetTopCardObject();
        if (trigObj == null) return;

        trigObj.transform.DOKill();

        var sr = trigObj.GetComponent<SpriteRenderer>();
        if (sr == null) sr = trigObj.GetComponentInChildren<SpriteRenderer>();

        Sequence seq = DOTween.Sequence();
        // Pulso de escala
    Vector3 baseScale = trigObj.transform.localScale;
    seq.Append(trigObj.transform.DOScale(baseScale * 1.08f, 0.12f).SetEase(Ease.OutSine));
    seq.Append(trigObj.transform.DOScale(baseScale, 0.18f).SetEase(Ease.OutBack));

        // Flash amarillo si hay SR
        if (sr != null)
        {
            Color original = sr.color;
            seq.Join(sr.DOColor(new Color(1f, 0.92f, 0.3f, original.a), 0.12f).SetLoops(2, LoopType.Yoyo));
        }

        seq.Play();
    }

    /// <summary>
    /// Animación de curación (flash verde suave)
    /// </summary>
    public void PlayHealAnimation()
    {
        var healObj = GetTopCardObject();
        if (healObj == null) return;

        healObj.transform.DOKill();

        var sr = healObj.GetComponent<SpriteRenderer>();
        if (sr == null) sr = healObj.GetComponentInChildren<SpriteRenderer>();

        Sequence seq = DOTween.Sequence();
    Vector3 baseScale = healObj.transform.localScale;
        // Pequeño pulso suave
    seq.Append(healObj.transform.DOScale(baseScale * 1.05f, 0.12f).SetEase(Ease.OutSine));
    seq.Append(healObj.transform.DOScale(baseScale, 0.2f).SetEase(Ease.OutQuad));

        if (sr != null)
        {
            Color original = sr.color;
            Color healColor = new Color(0.55f, 1f, 0.55f, original.a);
            seq.Join(sr.DOColor(healColor, 0.15f).SetLoops(2, LoopType.Yoyo));
        }

        seq.Play();
    }

    /// <summary>
    /// Animación cuando se coloca un monstruo en el slot
    /// </summary>
    private void PlaySpawnAnimation()
    {
        if (monsterCardObject == null) return;

        // Cancelar animaciones previas
        monsterCardObject.transform.DOKill();

        // Guardar posición final
        Vector3 finalPosition = monsterCardObject.transform.localPosition;
        Vector3 finalScale = monsterCardObject.transform.localScale;

        // Configurar posición inicial (arriba y con escala 0)
        monsterCardObject.transform.localPosition = finalPosition + Vector3.up * spawnDropHeight;
        monsterCardObject.transform.localScale = Vector3.zero;

        // Secuencia de animación: caída + escala + rebote
        Sequence spawnSequence = DOTween.Sequence();

        // Aparecer con escala (pop)
        spawnSequence.Append(monsterCardObject.transform.DOScale(finalScale * 1.2f, spawnAnimationDuration * 0.3f).SetEase(Ease.OutBack));
        
        // Caer a la posición final
        spawnSequence.Join(monsterCardObject.transform.DOLocalMove(finalPosition, spawnAnimationDuration).SetEase(Ease.OutBounce));
        
        // Ajustar a escala final (pequeño rebote)
        spawnSequence.Append(monsterCardObject.transform.DOScale(finalScale, spawnAnimationDuration * 0.2f).SetEase(Ease.InOutQuad));

        // Al completar, reajustar collider al tamaño final
        spawnSequence.OnComplete(() =>
        {
            if (monsterCardObject == null) return;
            
            var col = monsterCardObject.GetComponent<BoxCollider2D>();
            var sr = monsterCardObject.GetComponent<SpriteRenderer>();
            if (sr == null) sr = monsterCardObject.GetComponentInChildren<SpriteRenderer>();
            
            if (col != null && sr != null)
            {
                // Recalcular bounds después de la animación
                var bounds = sr.bounds;
                var sizeWorld = bounds.size;
                var centerWorld = bounds.center;
                
                Vector3 lossy = monsterCardObject.transform.lossyScale;
                float sx = Mathf.Approximately(lossy.x, 0f) ? 1f : lossy.x;
                float sy = Mathf.Approximately(lossy.y, 0f) ? 1f : lossy.y;
                Vector2 sizeLocal = new Vector2(sizeWorld.x / sx, sizeWorld.y / sy);
                Vector2 centerLocal = monsterCardObject.transform.InverseTransformPoint(centerWorld);
                
                col.size = sizeLocal;
                col.offset = centerLocal;
                
                Debug.Log($"[MonsterSlot] Collider recalibrado tras spawn: size={col.size}, offset={col.offset}");
            }
        });

        spawnSequence.Play();
    }

    private void PlaySpawnAnimationFor(GameObject cardObj)
    {
        if (cardObj == null) return;
        cardObj.transform.DOKill();
        Vector3 finalPosition = cardObj.transform.localPosition;
        Vector3 finalScale = cardObj.transform.localScale;
        cardObj.transform.localPosition = finalPosition + Vector3.up * spawnDropHeight;
        cardObj.transform.localScale = Vector3.zero;
        Sequence seq = DOTween.Sequence();
        seq.Append(cardObj.transform.DOScale(finalScale * 1.2f, spawnAnimationDuration * 0.3f).SetEase(Ease.OutBack));
        seq.Join(cardObj.transform.DOLocalMove(finalPosition, spawnAnimationDuration).SetEase(Ease.OutBounce));
        seq.Append(cardObj.transform.DOScale(finalScale, spawnAnimationDuration * 0.2f).SetEase(Ease.InOutQuad));
        seq.Play();
    }

    /// <summary>
    /// Activa/Desactiva un resaltado visual persistente indicando que este monstruo está en combate.
    /// </summary>
    public void SetCombatHighlight(bool on)
    {
        combatHighlighted = on;
        var obj = GetTopCardObject();
        if (obj == null) return;

        var sr = obj.GetComponent<SpriteRenderer>();
        if (sr == null) sr = obj.GetComponentInChildren<SpriteRenderer>();
        if (sr == null) return;

        if (on)
        {
            // Guardar color original solo la primera vez que activamos highlight
            if (originalSpriteColor == default(Color))
            {
                originalSpriteColor = sr.color;
            }
            // Tinte suave azulado para indicar "enfrentamiento"
            Color tint = new Color(0.8f, 0.9f, 1f, sr.color.a);
            sr.DOKill();
            sr.DOColor(tint, 0.15f).SetEase(Ease.OutQuad);
        }
        else
        {
            // Restaurar color original
            Color restore = (originalSpriteColor == default(Color)) ? Color.white : originalSpriteColor;
            sr.DOKill();
            sr.DOColor(new Color(restore.r, restore.g, restore.b, sr.color.a), 0.15f).SetEase(Ease.OutQuad);
        }
    }
}

