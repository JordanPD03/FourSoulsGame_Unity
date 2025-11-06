using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Utilidad para crear assets de cartas y efectos desde el menú de Unity.
/// Uso: Tools > Four Souls > Create Sample Cards
/// </summary>
public class CardAssetCreator : MonoBehaviour
{
    [MenuItem("Tools/Four Souls/Create Sample Effects")]
    public static void CreateSampleEffects()
    {
        string folderPath = "Assets/CardEffects";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "CardEffects");
        }

        // Gain 1 Coin
        var gain1Coin = ScriptableObject.CreateInstance<GainCoinsEffect>();
        gain1Coin.effectName = "Gain 1 Coin";
        gain1Coin.coinAmount = 1;
        AssetDatabase.CreateAsset(gain1Coin, $"{folderPath}/Gain1Coin.asset");

        // Gain 3 Coins
        var gain3Coins = ScriptableObject.CreateInstance<GainCoinsEffect>();
        gain3Coins.effectName = "Gain 3 Coins";
        gain3Coins.coinAmount = 3;
        AssetDatabase.CreateAsset(gain3Coins, $"{folderPath}/Gain3Coins.asset");

        // Heal 1 Heart
        var heal1 = ScriptableObject.CreateInstance<HealEffect>();
        heal1.effectName = "Heal 1 Heart";
        heal1.healAmount = 1;
        heal1.respectMaxHealth = true;
        AssetDatabase.CreateAsset(heal1, $"{folderPath}/Heal1Heart.asset");

        // Draw 1 Card
        var draw1 = ScriptableObject.CreateInstance<DrawCardEffect>();
        draw1.effectName = "Draw 1 Card";
        draw1.cardCount = 1;
        AssetDatabase.CreateAsset(draw1, $"{folderPath}/Draw1Card.asset");

        // Draw 2 Cards
        var draw2 = ScriptableObject.CreateInstance<DrawCardEffect>();
        draw2.effectName = "Draw 2 Cards";
        draw2.cardCount = 2;
        AssetDatabase.CreateAsset(draw2, $"{folderPath}/Draw2Cards.asset");

        // Deal 1 Damage
        var damage1 = ScriptableObject.CreateInstance<DealDamageEffect>();
        damage1.effectName = "Deal 1 Damage";
        damage1.damageAmount = 1;
        AssetDatabase.CreateAsset(damage1, $"{folderPath}/Deal1Damage.asset");

        // Deal 2 Damage
        var damage2 = ScriptableObject.CreateInstance<DealDamageEffect>();
        damage2.effectName = "Deal 2 Damage";
        damage2.damageAmount = 2;
        AssetDatabase.CreateAsset(damage2, $"{folderPath}/Deal2Damage.asset");

        // Roll D6
        var rollD6 = ScriptableObject.CreateInstance<RollDiceEffect>();
        rollD6.effectName = "Roll D6";
        rollD6.diceSides = 6;
        AssetDatabase.CreateAsset(rollD6, $"{folderPath}/RollD6.asset");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("[CardAssetCreator] ✅ Efectos de ejemplo creados en Assets/CardEffects/");
    }

    [MenuItem("Tools/Four Souls/Create Sample Cards")]
    public static void CreateSampleCards()
    {
        // Asegurar que existen las carpetas
        if (!AssetDatabase.IsValidFolder("Assets/Cards"))
            AssetDatabase.CreateFolder("Assets", "Cards");
        if (!AssetDatabase.IsValidFolder("Assets/Cards/Loot"))
            AssetDatabase.CreateFolder("Assets/Cards", "Loot");
        if (!AssetDatabase.IsValidFolder("Assets/Cards/Treasure"))
            AssetDatabase.CreateFolder("Assets/Cards", "Treasure");

        // Cargar los efectos previamente creados
        var gain1Coin = AssetDatabase.LoadAssetAtPath<GainCoinsEffect>("Assets/CardEffects/Gain1Coin.asset");
        var gain3Coins = AssetDatabase.LoadAssetAtPath<GainCoinsEffect>("Assets/CardEffects/Gain3Coins.asset");
        var heal1 = AssetDatabase.LoadAssetAtPath<HealEffect>("Assets/CardEffects/Heal1Heart.asset");
        var draw1 = AssetDatabase.LoadAssetAtPath<DrawCardEffect>("Assets/CardEffects/Draw1Card.asset");
        var damage2 = AssetDatabase.LoadAssetAtPath<DealDamageEffect>("Assets/CardEffects/Deal2Damage.asset");

        if (gain1Coin == null)
        {
            Debug.LogError("❌ Primero debes crear los efectos usando: Tools > Four Souls > Create Sample Effects");
            return;
        }

        // === LOOT CARDS ===

        // A Penny
        var aPenny = ScriptableObject.CreateInstance<CardDataSO>();
        aPenny.cardId = 1;
        aPenny.cardName = "A Penny";
        aPenny.cardType = CardType.Loot;
        aPenny.description = "Ganas 1¢";
        aPenny.isSingleUse = true;
        aPenny.effects.Add(gain1Coin);
        AssetDatabase.CreateAsset(aPenny, "Assets/Cards/Loot/A Penny.asset");

        // 2 of Coins
        var twoCoins = ScriptableObject.CreateInstance<CardDataSO>();
        twoCoins.cardId = 2;
        twoCoins.cardName = "2 of Coins";
        twoCoins.cardType = CardType.Loot;
        twoCoins.description = "Ganas 3¢";
        twoCoins.isSingleUse = true;
        twoCoins.effects.Add(gain3Coins);
        AssetDatabase.CreateAsset(twoCoins, "Assets/Cards/Loot/2 of Coins.asset");

        // The Bomb
        var theBomb = ScriptableObject.CreateInstance<CardDataSO>();
        theBomb.cardId = 3;
        theBomb.cardName = "The Bomb";
        theBomb.cardType = CardType.Loot;
        theBomb.description = "Inflige 2 de daño al monstruo activo";
        theBomb.isSingleUse = true;
        theBomb.effects.Add(damage2);
        AssetDatabase.CreateAsset(theBomb, "Assets/Cards/Loot/The Bomb.asset");

        // Yum Heart
        var yumHeart = ScriptableObject.CreateInstance<CardDataSO>();
        yumHeart.cardId = 4;
        yumHeart.cardName = "Yum Heart";
        yumHeart.cardType = CardType.Loot;
        yumHeart.description = "Recupera 1 de vida";
        yumHeart.isSingleUse = true;
        yumHeart.effects.Add(heal1);
        AssetDatabase.CreateAsset(yumHeart, "Assets/Cards/Loot/Yum Heart.asset");

        // Treasure Map
        var treasureMap = ScriptableObject.CreateInstance<CardDataSO>();
        treasureMap.cardId = 5;
        treasureMap.cardName = "Treasure Map";
        treasureMap.cardType = CardType.Loot;
        treasureMap.description = "Roba 1 carta";
        treasureMap.isSingleUse = true;
        treasureMap.effects.Add(draw1);
        AssetDatabase.CreateAsset(treasureMap, "Assets/Cards/Loot/Treasure Map.asset");

        // === TREASURE CARDS ===

        // The Dollar (Tesoro pasivo)
        var theDollar = ScriptableObject.CreateInstance<CardDataSO>();
        theDollar.cardId = 101;
        theDollar.cardName = "The Dollar";
        theDollar.cardType = CardType.Treasure;
    theDollar.description = "Al inicio de tu turno, ganas 1¢";
        theDollar.isPassive = true;
        theDollar.isSingleUse = false;
        theDollar.effects.Add(gain1Coin);
        AssetDatabase.CreateAsset(theDollar, "Assets/Cards/Treasure/The Dollar.asset");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[CardAssetCreator] ✅ Cartas de ejemplo creadas:");
        Debug.Log("  • Assets/Cards/Loot/ (5 cartas)");
        Debug.Log("  • Assets/Cards/Treasure/ (1 carta)");
    }

    [MenuItem("Tools/Four Souls/Create All Sample Assets")]
    public static void CreateAllSampleAssets()
    {
        CreateSampleEffects();
        CreateSampleCards();
        Debug.Log("[CardAssetCreator] ✅ Todos los assets de ejemplo han sido creados!");
    }
}
