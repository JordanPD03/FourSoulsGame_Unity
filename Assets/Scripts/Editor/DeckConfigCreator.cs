using UnityEngine;
using UnityEditor;

/// <summary>
/// Utilidad para crear configuraciones de mazos de ejemplo
/// </summary>
public class DeckConfigCreator : MonoBehaviour
{
    [MenuItem("Tools/Four Souls/Create Loot Deck Config")]
    public static void CreateLootDeckConfig()
    {
        // Buscar todas las cartas de Loot en el proyecto
        string[] guids = AssetDatabase.FindAssets("t:CardDataSO", new[] { "Assets/Cards/Loot" });
        
        if (guids.Length == 0)
        {
            Debug.LogWarning("No se encontraron cartas de Loot. Asegúrate de crear primero las cartas en Assets/Cards/Loot/");
            return;
        }

        // Crear la configuración del mazo
        DeckConfiguration deckConfig = ScriptableObject.CreateInstance<DeckConfiguration>();
        deckConfig.deckName = "Loot Deck";
        deckConfig.deckType = DeckType.Loot;
        
        // Crear array de entradas
        System.Collections.Generic.List<DeckEntry> entries = new System.Collections.Generic.List<DeckEntry>();
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CardDataSO card = AssetDatabase.LoadAssetAtPath<CardDataSO>(path);
            
            if (card != null)
            {
                // Asignar cantidades basadas en el nombre de la carta
                int quantity = GetDefaultQuantityForLootCard(card.cardName);
                entries.Add(new DeckEntry(card, quantity));
                Debug.Log($"Agregada: {card.cardName} x{quantity}");
            }
        }
        
        deckConfig.cards = entries.ToArray();
        
        // Guardar el asset
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/DeckConfigs"))
            AssetDatabase.CreateFolder("Assets/Resources", "DeckConfigs");
        
        string savePath = "Assets/Resources/DeckConfigs/LootDeckConfig.asset";
        AssetDatabase.CreateAsset(deckConfig, savePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"✅ Configuración de mazo de Loot creada en: {savePath}");
        Debug.Log($"Total de cartas en el mazo: {deckConfig.totalCards}");
        
        // Seleccionar el asset creado
        Selection.activeObject = deckConfig;
        EditorGUIUtility.PingObject(deckConfig);
    }

    /// <summary>
    /// Determina la cantidad por defecto de una carta de Loot basándose en su nombre
    /// </summary>
    private static int GetDefaultQuantityForLootCard(string cardName)
    {
        // Reglas por defecto (puedes ajustarlas)
        if (cardName.Contains("1 Coin") || cardName.Contains("1 Moneda"))
            return 10; // Muy común
        else if (cardName.Contains("2 Coin") || cardName.Contains("2 Moneda"))
            return 6;
        else if (cardName.Contains("3 Coin") || cardName.Contains("3 Moneda"))
            return 4;
        else if (cardName.Contains("4 Coin") || cardName.Contains("4 Moneda"))
            return 3;
        else if (cardName.Contains("5 Coin") || cardName.Contains("5 Moneda"))
            return 2;
        else if (cardName.Contains("10 Coin") || cardName.Contains("10 Moneda"))
            return 1; // Muy rara
        else if (cardName.Contains("Penny"))
            return 10;
        else if (cardName.Contains("Bomb"))
            return 4;
        else if (cardName.Contains("Heart") || cardName.Contains("Heal"))
            return 5;
        else
            return 3; // Por defecto: cantidad moderada
    }
}
