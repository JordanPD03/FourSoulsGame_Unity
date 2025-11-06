using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject que define los datos de un personaje jugable
/// </summary>
[CreateAssetMenu(fileName = "New Character", menuName = "Four Souls/Character Data")]
public class CharacterDataSO : ScriptableObject
{
    [Header("Información Básica")]
    public CharacterType characterType;
    public string characterName;
    
    [Header("Sprites")]
    [Tooltip("Sprite de la carta de personaje (frente)")]
    public Sprite characterCardFront;
    
    [Tooltip("Sprite del dorso de la carta de personaje")]
    public Sprite characterCardBack;
    
    [Tooltip("Icono compacto del personaje para paneles de jugador (distinto al sprite de la carta)")]
    public Sprite CharacterIcon;
    
    [Header("Stats Iniciales")]
    public int startingHealth = 2;
    public int startingCoins = 3;
    public int startingAttack = 1;
    
    [Header("Objetos Eternos")]
    [Tooltip("Lista de objetos eternos que este personaje comienza con")]
    public List<CardDataSO> eternalItems = new List<CardDataSO>();
    
    [Header("Habilidad Especial")]
    [TextArea(3, 5)]
    public string abilityDescription;
}
