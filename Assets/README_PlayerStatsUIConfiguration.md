# Configuración de PlayerStatsUI

## Resumen
El sistema PlayerStatsUI ahora soporta **6 estadísticas**:
1. ❤️ **Vida** (Health)
2. 💰 **Monedas** (Coins)
3. 👻 **Almas** (Souls)
4. ⚔️ **Daño de Ataque** (Attack Damage) - **NUEVO**
5. 🃏 **Cantidad de Loot** (mano) - **NUEVO**
6. 🏆 **Cantidad de Tesoros** (controlados) - **NUEVO**

## Configuración Recomendada

### Panel del Jugador Propio (Superior Derecha)
Este panel muestra tus propias estadísticas.

**Configuración Inspector:**
- `Player Index`: 0 (o el índice del jugador local)
- `Show Attack Damage`: ✅ **true**
- `Show Loot Count`: ❌ **false** (ya visible en tu mano)
- `Show Treasure Count`: ❌ **false** (ya visible en tu área de juego)

### Paneles de Otros Jugadores (Superior Izquierda)
Estos paneles muestran las estadísticas de tus oponentes.

**Configuración Inspector (para cada panel):**
- `Player Index`: 1, 2, 3 (según el jugador)
- `Show Attack Damage`: ✅ **true**
- `Show Loot Count`: ✅ **true** (necesitas ver cuántas cartas tienen)
- `Show Treasure Count`: ✅ **true** (necesitas ver cuántos tesoros controlan)

## Pasos de Configuración en Unity

### 1. Asignar Referencias de Iconos
Para cada panel PlayerStatsUI, arrastra los sprites desde tu jerarquía:

```
PlayerStatsUI (Inspector)
├── Referencias de Iconos
│   ├── Health Icon → [Image con sprite de corazón]
│   ├── Coins Icon → [Image con sprite de moneda]
│   ├── Souls Icon → [Image con sprite de alma]
│   ├── Attack Icon → [Image con sprite de espada/ataque]
│   ├── Loot Count Icon → [Image con sprite de carta/mano]
│   └── Treasure Count Icon → [Image con sprite de tesoro/cofre]
```

### 2. Asignar Referencias de Textos
Para cada panel, arrastra los TextMeshProUGUI correspondientes:

```
PlayerStatsUI (Inspector)
├── Textos de Cantidad
│   ├── Health Text → [TextMeshProUGUI que muestra "2/2"]
│   ├── Coins Text → [TextMeshProUGUI que muestra "3"]
│   ├── Souls Text → [TextMeshProUGUI que muestra "0/4"]
│   ├── Attack Text → [TextMeshProUGUI que muestra "1"]
│   ├── Loot Count Text → [TextMeshProUGUI que muestra "5"]
│   └── Treasure Count Text → [TextMeshProUGUI que muestra "2"]
```

### 3. Configurar Visibilidad por Panel

#### Panel Propio (playerIndex = 0)
```
Configuración
├── Player Index: 0
├── Show Attack Damage: ✅
├── Show Loot Count: ❌
└── Show Treasure Count: ❌
```

#### Panel Oponente 1 (playerIndex = 1)
```
Configuración
├── Player Index: 1
├── Show Attack Damage: ✅
├── Show Loot Count: ✅
└── Show Treasure Count: ✅
```

#### Panel Oponente 2 (playerIndex = 2)
```
Configuración
├── Player Index: 2
├── Show Attack Damage: ✅
├── Show Loot Count: ✅
└── Show Treasure Count: ✅
```

#### Panel Oponente 3 (playerIndex = 3)
```
Configuración
├── Player Index: 3
├── Show Attack Damage: ✅
├── Show Loot Count: ✅
└── Show Treasure Count: ✅
```

### 4. Configuración de Animaciones (Opcional)
```
Animación
├── Animate On Change: ✅
├── Punch Scale: 1.2
└── Punch Duration: 0.3
```

### 5. Ocultar Iconos/Textos No Usados

Si un panel **no muestra** Loot Count o Treasure Count:
1. Puedes dejar los campos Attack Icon/Text, Loot Count Icon/Text, Treasure Count Icon/Text **vacíos** en el Inspector
2. O puedes desactivar los GameObjects correspondientes en la jerarquía

El script verifica si las referencias existen antes de actualizar, así que es seguro dejarlas vacías.

## Comportamiento de Animaciones

### Animaciones Aisladas por Estadística
Cada estadística anima **solo su propio icono** cuando cambia:

- **Vida** → solo `healthIcon` hace punch
- **Monedas** → solo `coinsIcon` hace punch
- **Almas** → solo `soulsIcon` hace punch
- **Daño de Ataque** → se actualiza en Update (sin animación dedicada actualmente)
- **Loot Count** → solo `lootCountIcon` hace punch (al robar/jugar/descartar Loot)
- **Treasure Count** → solo `treasureCountIcon` hace punch (al jugar/descartar Tesoro)

### Garantía de Reset de Escala
Todas las animaciones DOTween incluyen `OnComplete` para resetear la escala a su valor original, evitando que los iconos se queden grandes.

## Formato de Textos

- **Vida**: `"2/2"` (actual/máxima)
- **Monedas**: `"3"` (solo número actual)
- **Almas**: `"0/4"` (actuales/para ganar)
- **Daño de Ataque**: `"1"` (solo número actual)
- **Loot Count**: `"5"` (cantidad en mano)
- **Treasure Count**: `"2"` (tesoros activos + pasivos, sin contar almas)

## Testing

### Checklist de Verificación
1. ✅ Panel propio muestra Vida, Monedas, Almas, Daño de Ataque
2. ✅ Panel propio NO muestra Loot Count ni Treasure Count
3. ✅ Paneles de oponentes muestran todas las 6 estadísticas
4. ✅ Al recibir daño, solo el icono de vida se anima (no todo el panel)
5. ✅ Al robar Loot, solo el icono de Loot Count se anima (si visible)
6. ✅ Al jugar Tesoro, solo el icono de Treasure Count se anima (si visible)
7. ✅ Los iconos vuelven a su tamaño original después de la animación
8. ✅ Los paneles de otros jugadores no se quedan escalados/grandes

### Escenarios de Prueba
1. **Daño**: Ataca un monstruo y pierde vida → solo healthIcon anima
2. **Monedas**: Compra una carta → solo coinsIcon anima
3. **Almas**: Mata un monstruo → solo soulsIcon anima
4. **Loot**: Roba una carta → lootCountIcon anima (solo en paneles de oponentes)
5. **Tesoro**: Juega un tesoro → treasureCountIcon anima (solo en paneles de oponentes)

## Notas Técnicas

### Eventos del GameManager
El sistema escucha estos eventos:
- `OnPlayerHealthChanged` → actualiza vida, anima healthIcon
- `OnPlayerCoinsChanged` → actualiza monedas, anima coinsIcon
- `OnSoulCollected` → actualiza almas, anima soulsIcon
- `OnPlayerDamaged` → NO anima panel (reverted para evitar scaling issues)
- `OnCardDrawn` → actualiza Loot Count, anima lootCountIcon
- `OnCardPlayed` → actualiza Loot/Treasure Count, anima icono correspondiente
- `OnCardDiscarded` → actualiza Loot/Treasure Count, anima icono correspondiente

### Update Loop
- **Daño de Ataque** se actualiza cada frame en `Update()` (no tiene evento dedicado todavía)
- **Loot Count** y **Treasure Count** se recalculan cada frame para garantizar precisión
- Optimización futura: añadir eventos dedicados para cambios de ataque

### Próximas Mejoras Opcionales
1. **OnPlayerAttackDamageChanged** evento para animación de ataque
2. **Temporizador visual** de 30s en selección de descarte
3. **Botón "Roll"** en UI de combate (alternativa a doble clic)

---

**Última Actualización**: Noviembre 1, 2025
**Estado**: ✅ Sistema completo y testeado
