using UnityEngine;
using UnityEditor;

/// <summary>
/// Utilidad para crear una configuración de mazo de Tesoros a partir de Assets/Cards/Treasure
/// </summary>
public class TreasureDeckConfigCreator : MonoBehaviour
{
    [MenuItem("Tools/Four Souls/Create Treasure Deck Config")] 
    public static void CreateTreasureDeckConfig()
    {
        string treasureFolder = "Assets/Cards/Treasure";
        if (!AssetDatabase.IsValidFolder(treasureFolder))
        {
            Debug.LogWarning($"No existe la carpeta {treasureFolder}. Crea tus cartas de tesoro en esa ruta.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:CardDataSO", new[] { treasureFolder });
        if (guids.Length == 0)
        {
            Debug.LogWarning("No se encontraron cartas de Tesoro. Asegúrate de crear primero las cartas en Assets/Cards/Treasure/");
            return;
        }

        // Crear la configuración del mazo
        DeckConfiguration deckConfig = ScriptableObject.CreateInstance<DeckConfiguration>();
        deckConfig.deckName = "Treasure Deck";
        deckConfig.deckType = DeckType.Treasure;

        var entries = new System.Collections.Generic.List<DeckEntry>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CardDataSO card = AssetDatabase.LoadAssetAtPath<CardDataSO>(path);
            if (card != null && card.cardType == CardType.Treasure)
            {
                // Por defecto 1 copia por tesoro
                int quantity = GetDefaultQuantityForTreasure(card);
                entries.Add(new DeckEntry(card, quantity));
                Debug.Log($"Agregada: {card.cardName} x{quantity}");
            }
        }

        deckConfig.cards = entries.ToArray();

        // Guardar el asset en Resources para fácil acceso
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/DeckConfigs"))
            AssetDatabase.CreateFolder("Assets/Resources", "DeckConfigs");

        string savePath = "Assets/Resources/DeckConfigs/TreasureDeckConfig.asset";
        AssetDatabase.CreateAsset(deckConfig, savePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✅ Configuración de mazo de Tesoros creada en: {savePath}");
        Debug.Log($"Total de cartas en el mazo: {deckConfig.totalCards}");

        Selection.activeObject = deckConfig;
        EditorGUIUtility.PingObject(deckConfig);
    }

    private static int GetDefaultQuantityForTreasure(CardDataSO card)
    {
        // Reglas simples por defecto: 1 copia por tesoro
        // Puedes personalizar por nombre o flags si lo necesitas
        // Nota: los eternos se excluyen más adelante al cargar el mazo en GameManager.
        return 1;
    }
}
