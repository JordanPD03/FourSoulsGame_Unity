# Sistema de Efectos de Cartas - Four Souls Game

## Visión General

El sistema de efectos permite crear cartas modulares y reutilizables mediante ScriptableObjects. Cada carta puede tener múltiples efectos que se ejecutan secuencialmente.

## Arquitectura

### CardEffect (Base Abstracta)
Clase base para todos los efectos de cartas.

**Métodos principales:**
- `Execute(PlayerData, CardData, GameManager)`: Ejecuta el efecto
- `CanExecute(...)`: Verifica si el efecto puede ejecutarse
- `GetDescription()`: Retorna una descripción legible del efecto

### Efectos Implementados

#### 1. GainCoinsEffect
Otorga monedas al jugador.

**Parámetros:**
- `coinAmount`: Cantidad de monedas (puede ser negativo)

**Ejemplo de uso:**
- A Penny: +1 moneda
- The Dollar: +1 moneda pasiva

#### 2. HealEffect
Restaura puntos de vida al jugador.

**Parámetros:**
- `healAmount`: Cantidad de vida a restaurar
- `respectMaxHealth`: Si respeta el límite de vida máxima

**Ejemplo de uso:**
- Yum Heart: Cura 1 corazón
- The Wafer: Cura completamente

#### 3. DrawCardEffect
Permite robar cartas adicionales del mazo de Loot.

**Parámetros:**
- `cardCount`: Número de cartas a robar
- `animated`: (Deprecado - siempre usa la lógica del GameManager)

**Ejemplo de uso:**
- Treasure Map: Roba 1 carta
- Mom's Bottle of Pills: Roba 2 cartas

#### 4. DealDamageEffect
Inflige daño a un objetivo (jugador o monstruo).

**Parámetros:**
- `damageAmount`: Cantidad de daño
- `requiresTarget`: Si requiere selección de objetivo

**Ejemplo de uso:**
- The Bomb: 2 de daño al monstruo
- Poison Mushroom: 1 de daño a jugador

**Nota:** Sistema de combate será implementado próximamente.

#### 5. RollDiceEffect
Tira un dado y procesa el resultado.

**Parámetros:**
- `diceSides`: Número de caras del dado (por defecto 6)
- `resultDescription`: Descripción de efectos por resultado

**Ejemplo de uso:**
- D6: Tira un dado estándar
- Guppy's Paw: Efectos basados en tirada

**Nota:** Extender `ProcessDiceResult()` para lógica personalizada.

## Crear una Nueva Carta

### 1. Crear el ScriptableObject de la Carta

1. Click derecho en Project → `Create > Four Souls > Card`
2. Nombra el asset (ej: "A Penny")

### 2. Configurar la Carta

**Información Básica:**
- `cardId`: ID único (recomendado: asignar secuencialmente)
- `cardName`: Nombre visible de la carta
- `cardType`: Loot, Treasure, Monster, Room, etc.
- `description`: Texto descriptivo del efecto

**Sprites:**
- `frontSprite`: Imagen frontal de la carta
- `backSprite`: Dorso (opcional, usa el del mazo por defecto si está vacío)

**Stats de Combate:**
- `damage`: Daño que causa/recibe
- `health`: Vida del monstruo
- `attack`: Bonificación de ataque

**Economía:**
- `coinCost`: Costo en monedas (tesoros) o recompensa (monstruos)
- `souls`: Almas que otorga (monstruos/boss)

**Efectos:**
- Arrastra los ScriptableObjects de efectos a la lista `effects`
- Los efectos se ejecutan en orden de la lista

**Propiedades Especiales:**
- `isUnique`: Solo una copia puede estar en juego
- `canPlayOnOtherTurn`: Puede jugarse como reacción
- `isSingleUse`: Se descarta después de usar (por defecto)
- `isPassive`: Permanece en juego (tesoros)

### 3. Crear los Efectos

1. Click derecho en Project → `Create > Four Souls > Effects > [Tipo de Efecto]`
2. Configura los parámetros del efecto
3. Nombra el asset descriptivamente (ej: "Gain1Coin")

### 4. Asignar a la Carta

Arrastra el efecto creado a la lista `effects` de la carta.

## Ejemplo Completo: "A Penny"

### Crear el Efecto
1. `Create > Four Souls > Effects > Gain Coins`
2. Nombre: "Gain1Coin"
3. `coinAmount = 1`

### Crear la Carta
1. `Create > Four Souls > Card`
2. Configuración:
   ```
   cardName: "A Penny"
   cardType: Loot
   description: "Ganas 1¢"
   coinCost: 0
   isSingleUse: true
   effects: [Gain1Coin]
   ```

## Crear un Efecto Personalizado

Si necesitas un efecto que no existe, crea una nueva clase:

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "MyCustomEffect", menuName = "Four Souls/Effects/My Custom Effect")]
public class MyCustomEffect : CardEffect
{
    [Header("Custom Settings")]
    public int customParameter = 1;

    public override void Execute(PlayerData player, CardData card, GameManager gameManager)
    {
        // Tu lógica aquí
        Debug.Log($"Ejecutando efecto personalizado para {player.playerName}");
    }

    public override bool CanExecute(PlayerData player, CardData card, GameManager gameManager)
    {
        // Validaciones
        return true;
    }

    public override string GetDescription()
    {
        return $"Efecto personalizado con valor {customParameter}";
    }
}
```

## Cargar Cartas en el GameManager

El GameManager cargará las cartas desde las bases de datos:

```csharp
[Header("Card Databases")]
[SerializeField] private List<CardDataSO> lootCardDatabase;
[SerializeField] private List<CardDataSO> treasureCardDatabase;
[SerializeField] private List<CardDataSO> monsterCardDatabase;
```

Arrastra todos los ScriptableObjects de cartas a las listas correspondientes en el Inspector.

## Próximos Pasos

- [ ] Implementar sistema de combate para DealDamageEffect
- [ ] Sistema de selección de objetivos
- [ ] Efectos condicionales (basados en estado del juego)
- [ ] Efectos de inicio/fin de turno (pasivos)
- [ ] Stack de acciones y reacciones
- [ ] Persistencia de efectos pasivos

## Notas Técnicas

- Los efectos deben ser **stateless** (sin estado interno)
- Toda la lógica de estado va en `PlayerData` y `GameManager`
- Los ScriptableObjects son **definiciones**, no instancias de juego
- Usa `ToCardData()` para convertir CardDataSO → CardData en runtime
- Los efectos se ejecutan sincrónicamente (uno tras otro)

## Debugging

Para ver los efectos ejecutándose:
1. Activa la consola de Unity (Ctrl+Shift+C)
2. Busca logs con prefijo `[NombreDelEfecto]`
3. Verifica que los parámetros del efecto sean correctos
4. Asegúrate de que la carta tenga efectos asignados

---

**Autor:** Jordan  
**Versión:** 1.0  
**Fecha:** 2025
