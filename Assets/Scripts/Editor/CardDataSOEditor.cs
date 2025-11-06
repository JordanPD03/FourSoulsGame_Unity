#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CardDataSO))]
public class CardDataSOEditor : Editor
{
    SerializedProperty cardIdProp;
    SerializedProperty cardNameProp;
    SerializedProperty cardTypeProp;
    SerializedProperty descriptionProp;
    SerializedProperty frontSpriteProp;
    SerializedProperty backSpriteProp;

    SerializedProperty healthProp;
    SerializedProperty diceReqProp;
    SerializedProperty attackProp;

    SerializedProperty rewardCoinsProp;
    SerializedProperty rewardLootProp;
    SerializedProperty rewardTreasureProp;
    SerializedProperty rewardSoulsMinProp;
    SerializedProperty rewardSoulsMaxProp;

    SerializedProperty effectsProp;

    SerializedProperty hasTriggerProp;
    SerializedProperty triggerRollProp;
    SerializedProperty triggerAtkDmgProp;
    SerializedProperty triggerHealProp;

    SerializedProperty isUniqueProp;
    SerializedProperty canPlayOtherTurnProp;
    SerializedProperty isSingleUseProp;
    SerializedProperty isPassiveProp;
    SerializedProperty isEternalProp;

    // New
    SerializedProperty autoAssignBackProp;

    SerializedProperty lootConvertsToTreasureProp;
    SerializedProperty lootTreasureResultProp;

    SerializedProperty treasureDestroyableProp;

    SerializedProperty monsterRankProp;
    SerializedProperty bossSoulsProp;

    void OnEnable()
    {
        cardIdProp = serializedObject.FindProperty("cardId");
        cardNameProp = serializedObject.FindProperty("cardName");
        cardTypeProp = serializedObject.FindProperty("cardType");
        descriptionProp = serializedObject.FindProperty("description");
        frontSpriteProp = serializedObject.FindProperty("frontSprite");
        backSpriteProp = serializedObject.FindProperty("backSprite");

        healthProp = serializedObject.FindProperty("health");
        diceReqProp = serializedObject.FindProperty("diceRequirement");
        attackProp = serializedObject.FindProperty("attackDamage");

        rewardCoinsProp = serializedObject.FindProperty("rewardCoins");
        rewardLootProp = serializedObject.FindProperty("rewardLootCards");
        rewardTreasureProp = serializedObject.FindProperty("rewardTreasure");
        rewardSoulsMinProp = serializedObject.FindProperty("rewardSoulsMin");
        rewardSoulsMaxProp = serializedObject.FindProperty("rewardSoulsMax");

        effectsProp = serializedObject.FindProperty("effects");

        hasTriggerProp = serializedObject.FindProperty("hasCombatTrigger");
        triggerRollProp = serializedObject.FindProperty("combatTriggerRollValue");
        triggerAtkDmgProp = serializedObject.FindProperty("combatTriggerAttackerDamage");
        triggerHealProp = serializedObject.FindProperty("combatTriggerMonsterHeal");

        isUniqueProp = serializedObject.FindProperty("isUnique");
        canPlayOtherTurnProp = serializedObject.FindProperty("canPlayOnOtherTurn");
        isSingleUseProp = serializedObject.FindProperty("isSingleUse");
        isPassiveProp = serializedObject.FindProperty("isPassive");
        isEternalProp = serializedObject.FindProperty("isEternal");

        autoAssignBackProp = serializedObject.FindProperty("autoAssignBackSprite");

    lootConvertsToTreasureProp = serializedObject.FindProperty("lootConvertsToTreasure");
    lootTreasureResultProp = serializedObject.FindProperty("lootTreasureResult");

        treasureDestroyableProp = serializedObject.FindProperty("treasureDestroyable");

        monsterRankProp = serializedObject.FindProperty("monsterRank");
        bossSoulsProp = serializedObject.FindProperty("bossSouls");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Información Básica", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(cardIdProp);
        EditorGUILayout.PropertyField(cardNameProp);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(cardTypeProp);
        bool typeChanged = EditorGUI.EndChangeCheck();

    // Ocultar descripción: el sprite contiene el texto
    // EditorGUILayout.PropertyField(descriptionProp);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Sprites", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(frontSpriteProp);
        EditorGUILayout.PropertyField(backSpriteProp);
        EditorGUILayout.PropertyField(autoAssignBackProp);

        if (typeChanged && autoAssignBackProp.boolValue)
        {
            TryAutoAssignBackSprite();
        }

        // Conditional sections
        var type = (CardType)cardTypeProp.enumValueIndex;

        if (type == CardType.Loot)
        {
            DrawLootSection();
        }
        else if (type == CardType.Treasure)
        {
            DrawTreasureSection();
        }
        else if (type == CardType.Monster || type == CardType.Boss)
        {
            DrawMonsterSection();
        }

        // Common properties
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Propiedades Especiales", EditorStyles.boldLabel);
        if (type == CardType.Loot)
        {
            // Para Loot permitimos marcar si es único (por defecto común)
            EditorGUILayout.PropertyField(isUniqueProp, new GUIContent("Único"));
        }
        else
        {
            // Para Tesoros/Monstruos/Habitaciones/Personajes queda forzado por validación
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(isUniqueProp, new GUIContent("Único (enforced)"));
            }
        }
        if (type == CardType.Treasure)
        {
            if (isPassiveProp.boolValue)
            {
                // Para tesoros pasivos, mostrar el flag editable (no aplica la regla de "activos en cualquier turno")
                EditorGUILayout.PropertyField(canPlayOtherTurnProp);
            }
            else
            {
                // Para objetos activos, el uso en turnos ajenos está habilitado por regla
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(canPlayOtherTurnProp, new GUIContent("Puede usarse en otros turnos (regla)"));
                }
                EditorGUILayout.HelpBox("Los objetos ACTIVOS pueden usarse en turnos de cualquier jugador cuando están cargados.", MessageType.Info);
            }
        }
        else
        {
            EditorGUILayout.PropertyField(canPlayOtherTurnProp);
        }
        if (type == CardType.Loot)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(isSingleUseProp, new GUIContent("Uso único (enforced)"));
            }
        }
        if (type == CardType.Treasure)
        {
            EditorGUILayout.PropertyField(isPassiveProp, new GUIContent("Es Pasivo"));
            EditorGUILayout.PropertyField(isEternalProp, new GUIContent("Es Eterno"));
        }
        if (type != CardType.Treasure && type != CardType.Loot)
        {
            EditorGUILayout.PropertyField(isPassiveProp, new GUIContent("Es Pasivo"));
            EditorGUILayout.PropertyField(isEternalProp, new GUIContent("Es Eterno"));
        }

        // Effects list
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(effectsProp, true);

        serializedObject.ApplyModifiedProperties();
    }

    void DrawLootSection()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Loot", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(isSingleUseProp, new GUIContent("Uso único (enforced)"));
        }
        EditorGUILayout.PropertyField(lootConvertsToTreasureProp, new GUIContent("Trinket: Convierte a Tesoro"));
        if (lootConvertsToTreasureProp.boolValue)
        {
            EditorGUILayout.PropertyField(lootTreasureResultProp, new GUIContent("Tesoro Destino"));
        }
        EditorGUILayout.HelpBox("Define el efecto con 'effects' para modularidad (CardEffects).", MessageType.Info);
    }

    void DrawTreasureSection()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Treasure", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(isPassiveProp, new GUIContent("Es Pasivo"));
        EditorGUILayout.PropertyField(isEternalProp, new GUIContent("Es Eterno"));
        using (new EditorGUI.DisabledScope(isEternalProp.boolValue))
        {
            EditorGUILayout.PropertyField(treasureDestroyableProp, new GUIContent("Destruible"));
        }
        EditorGUILayout.HelpBox("Los objetos eternos no pueden destruirse ni aparecer en el mazo de Tesoros.", MessageType.Info);
    }

    void DrawMonsterSection()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Monster", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(healthProp);
        EditorGUILayout.PropertyField(diceReqProp);
        EditorGUILayout.PropertyField(attackProp);

        EditorGUILayout.PropertyField(rewardCoinsProp);
        EditorGUILayout.PropertyField(rewardLootProp);
        EditorGUILayout.PropertyField(rewardTreasureProp);
        EditorGUILayout.PropertyField(rewardSoulsMinProp);
        EditorGUILayout.PropertyField(rewardSoulsMaxProp);

        EditorGUILayout.PropertyField(monsterRankProp);
        if ((MonsterRank)monsterRankProp.enumValueIndex == MonsterRank.Boss)
        {
            EditorGUILayout.PropertyField(bossSoulsProp, new GUIContent("Almas (Jefe)"));
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Trigger de Combate", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(hasTriggerProp);
        if (hasTriggerProp.boolValue)
        {
            EditorGUILayout.PropertyField(triggerRollProp);
            EditorGUILayout.PropertyField(triggerAtkDmgProp);
            EditorGUILayout.PropertyField(triggerHealProp);
        }
    }

    void TryAutoAssignBackSprite()
    {
        // Mapear por tipo a rutas por defecto en Resources (ajusta a tus carpetas)
        string path = null;
        var type = (CardType)cardTypeProp.enumValueIndex;
        switch (type)
        {
            case CardType.Loot: path = "Cards/Back/Loot/loot_back"; break;
            case CardType.Treasure: path = "Cards/Back/Treasure/treasure_back"; break;
            case CardType.Monster:
            case CardType.Boss: path = "Cards/Back/Monster/monster_back"; break;
            case CardType.Character: path = "Cards/Back/Character/character_back"; break;
            default: path = null; break;
        }

        if (!string.IsNullOrEmpty(path))
        {
            var spr = Resources.Load<Sprite>(path);
            if (spr != null)
            {
                backSpriteProp.objectReferenceValue = spr;
                serializedObject.ApplyModifiedProperties();
                Debug.Log($"[CardDataSOEditor] Dorso asignado automáticamente desde Resources: {path}");
            }
            else
            {
                Debug.LogWarning($"[CardDataSOEditor] No se encontró sprite de dorso en: Resources/{path}. Asigna manualmente o ajusta la ruta en el editor script.");
            }
        }
    }
}
#endif
