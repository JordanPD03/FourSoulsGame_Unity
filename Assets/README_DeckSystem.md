# Sistema de Mazos y Juego de Cartas - Four Souls Game

## 🎯 Funcionalidades Implementadas

### ✅ Sistema de Configuración de Mazos
- **DeckConfiguration**: ScriptableObject que define mazos con cantidades por carta
- Soporte para múltiples copias de la misma carta
- Cálculo automático del total de cartas

### ✅ Inicio de Partida
- Cada jugador recibe **3 cartas de Loot** al inicio
- Cada jugador recibe **3 monedas** al inicio
- Valores configurables desde el Inspector del GameManager

### ✅ Sistema de Turnos
- Cada turno el jugador roba **1 carta** automáticamente
- Fase de robo automática (no requiere acción del jugador)

### ✅ Jugar Cartas de Loot
- Las cartas de Loot ejecutan sus efectos cuando se juegan
- Sistema modular de efectos (GainCoins, Heal, Draw, etc.)
- Descarte automático de cartas de un solo uso

---

## 📋 Configuración Paso a Paso

### 1. Crear Cartas de Loot

Ya creaste 6 cartas para ganar monedas. Ahora asegúrate de que tengan sus efectos:

**Estructura de carpetas:**
```
Assets/
  ├── CardEffects/
  │   ├── Gain1Coin.asset
  │   ├── Gain2Coins.asset
  │   ├── Gain3Coins.asset
  │   ├── Gain4Coins.asset
  │   ├── Gain5Coins.asset
  │   └── Gain10Coins.asset
  └── Cards/
      └── Loot/
          ├── Gana 1 Moneda.asset
          ├── Gana 2 Monedas.asset
          ├── Gana 3 Monedas.asset
          ├── Gana 4 Monedas.asset
          ├── Gana 5 Monedas.asset
          └── Gana 10 Monedas.asset
```

### 2. Crear Configuración del Mazo de Loot

#### Opción A - Automática (Recomendada):
1. En Unity: `Tools > Four Souls > Create Loot Deck Config`
2. Se creará automáticamente en `Assets/Resources/DeckConfigs/LootDeckConfig.asset`
3. Las cantidades se asignan automáticamente:
   - Gana 1 Moneda: **10 copias**
   - Gana 2 Monedas: **6 copias**
   - Gana 3 Monedas: **4 copias**
   - Gana 4 Monedas: **3 copias**
   - Gana 5 Monedas: **2 copias**
   - Gana 10 Monedas: **1 copia** (rara!)

#### Opción B - Manual:
1. Click derecho en Project → `Create > Four Souls > Deck Configuration`
2. Nombra el asset: "LootDeckConfig"
3. Configura:
   - `Deck Name`: "Loot Deck"
   - `Deck Type`: Loot
4. En el array `Cards`, agrega cada carta:
   - Arrastra la carta de Loot al campo `Card`
   - Establece la `Quantity` (1-20)
5. El campo `Total Cards` se actualiza automáticamente

### 3. Asignar en el GameManager

1. Abre la escena principal
2. Selecciona el GameObject **GameManager**
3. En el Inspector, encuentra la sección **Deck Configurations**
4. Arrastra `LootDeckConfig` al campo **Loot Deck Config**

### 4. Configurar Recursos Iniciales (Opcional)

En el Inspector del GameManager, sección **Game Start Settings**:
- **Starting Hand Size**: 3 (cartas iniciales)
- **Starting Coins**: 3 (monedas iniciales)

Puedes cambiar estos valores si quieres un inicio diferente.

---

## 🎮 Cómo Funciona en el Juego

### Al Iniciar la Partida:
1. Se carga el mazo de Loot desde la configuración
2. Se mezcla aleatoriamente
3. Cada jugador recibe:
   - 3 cartas de Loot
   - 3 monedas

### Durante un Turno:
1. **Fase Start**: Procesamiento de efectos pasivos
2. **Fase Draw**: Roba 1 carta automáticamente
3. **Fase Action**: Puedes:
   - Jugar cartas de Loot de tu mano
   - Atacar monstruos (próximamente)
   - Comprar tesoros (próximamente)
4. **Fase End**: Terminar turno (presiona **T**)

### Jugar una Carta de Loot:
1. La carta debe estar en tu mano
2. Debe ser la **Fase de Acción**
3. Los efectos se ejecutan automáticamente:
   - `GainCoinsEffect` → Te da monedas
   - `HealEffect` → Recupera vida
   - `DrawCardEffect` → Robas más cartas
   - etc.
4. Si es de un solo uso, va al descarte

---

## 🧪 Pruebas en el Editor

### Verificar que Todo Funciona:

1. **Revisar el mazo cargado:**
   - Play en Unity
   - Abre la consola (Ctrl+Shift+C)
   - Busca: `[GameManager] Mazo de Loot cargado desde configuración: X cartas`

2. **Verificar recursos iniciales:**
   - Busca en consola: `[GameManager] Player 1 comienza con 3 cartas y 3¢`
   - Verifica que el UI muestre 3 cartas y 3 monedas

3. **Probar robo de carta:**
   - Espera a la Fase Draw
   - Debe robar automáticamente 1 carta
   - La animación debe mostrarse

4. **Probar jugar carta:**
   - (Próximamente: click en carta de la mano)
   - Por ahora, puedes llamar `GameManager.Instance.PlayCard(player, card)` desde código

---

## 📊 Estadísticas del Mazo de Ejemplo

Con las 6 cartas de monedas y las cantidades sugeridas:

| Carta           | Cantidad | Probabilidad |
|-----------------|----------|--------------|
| Gana 1 Moneda   | 10       | 38.5%        |
| Gana 2 Monedas  | 6        | 23.1%        |
| Gana 3 Monedas  | 4        | 15.4%        |
| Gana 4 Monedas  | 3        | 11.5%        |
| Gana 5 Monedas  | 2        | 7.7%         |
| Gana 10 Monedas | 1        | 3.8%         |
| **TOTAL**       | **26**   | **100%**     |

---

## 🔧 Próximos Pasos

### Jugar Cartas desde la UI:
Necesitarás modificar `PlayerHandUI.cs` para:
1. Detectar clicks en las cartas
2. Llamar a `GameManager.Instance.PlayCard(currentPlayer, clickedCard)`
3. Actualizar la UI (remover carta de la mano visual)

### Agregar Más Tipos de Cartas:
- Cartas de curación
- Cartas de daño (bombas)
- Cartas de robo adicional
- Efectos especiales

### Sistema de Compra:
- Mazo de Treasures
- Tienda con precios
- Validación de monedas

---

## 🐛 Troubleshooting

### "No hay cartas en el mazo"
- Verifica que `LootDeckConfig` esté asignado en GameManager
- Verifica que las cartas tengan `quantity > 0`

### "La carta no ejecuta efectos"
- Asegúrate de que la carta tenga efectos en el array `effects`
- Verifica que los efectos no sean `null`

### "El jugador no tiene cartas al inicio"
- Verifica `Starting Hand Size > 0` en GameManager
- Verifica que el mazo tenga suficientes cartas

### "Las monedas no se otorgan"
- Verifica que `GainCoinsEffect` tenga `coinAmount` configurado
- Revisa la consola para ver si el efecto se ejecuta

---

**¡Listo para jugar!** 🎉

Ahora tienes un sistema completo de mazos con cantidades configurables, inicio de partida funcional, y ejecución de efectos de cartas.
