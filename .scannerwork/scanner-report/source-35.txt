using UnityEngine;
using UnityEditor;

/// <summary>
/// Utilidad para crear una configuración de mazo de Monstruos a partir de Assets/Cards/Monsters
/// </summary>
public class MonsterDeckConfigCreator : MonoBehaviour
{
    [MenuItem("Tools/Four Souls/Create Monster Deck Config")] 
    public static void CreateMonsterDeckConfig()
    {
        string monstersFolder = "Assets/Cards/Monsters";
        if (!AssetDatabase.IsValidFolder(monstersFolder))
        {
            Debug.LogWarning($"No existe la carpeta {monstersFolder}. Crea tus cartas de monstruo en esa ruta.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:CardDataSO", new[] { monstersFolder });
        if (guids.Length == 0)
        {
            Debug.LogWarning("No se encontraron cartas de Monstruo. Asegúrate de crear primero las cartas en Assets/Cards/Monsters/");
            return;
        }

        // Crear la configuración del mazo
        DeckConfiguration deckConfig = ScriptableObject.CreateInstance<DeckConfiguration>();
        deckConfig.deckName = "Monster Deck";
        deckConfig.deckType = DeckType.Monster;

        var entries = new System.Collections.Generic.List<DeckEntry>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CardDataSO card = AssetDatabase.LoadAssetAtPath<CardDataSO>(path);
            if (card != null && (card.cardType == CardType.Monster || card.cardType == CardType.Boss))
            {
                // Por defecto 1 copia por monstruo; puedes ajustar reglas aquí
                int quantity = 1;
                entries.Add(new DeckEntry(card, quantity));
                Debug.Log($"Agregada: {card.cardName} x{quantity}");
            }
        }

        deckConfig.cards = entries.ToArray();

        // Guardar el asset en Resources para fácil acceso si lo deseas
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/DeckConfigs"))
            AssetDatabase.CreateFolder("Assets/Resources", "DeckConfigs");

        string savePath = "Assets/Resources/DeckConfigs/MonsterDeckConfig.asset";
        AssetDatabase.CreateAsset(deckConfig, savePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✅ Configuración de mazo de Monstruos creada en: {savePath}");
        Debug.Log($"Total de cartas en el mazo: {deckConfig.totalCards}");

        Selection.activeObject = deckConfig;
        EditorGUIUtility.PingObject(deckConfig);
    }
}
