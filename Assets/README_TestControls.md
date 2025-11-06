# 🎮 Controles de Prueba - GameManager

## ⚠️ CONTROLES TEMPORALES PARA DEBUGGING

Estos controles están implementados en `GameManager.cs` solo para probar el sistema de stats.

---

## 🎯 Controles Disponibles

### Robo de Carta (Automático)
- En la Fase Draw, ahora el robo sucede automáticamente.
- Ya no es necesario presionar una tecla.

---

### **T** - Terminar Turno
- Finaliza el turno actual (solo en fase Action)
- Si no estás en fase Action, mostrará un aviso en consola

---

### **Vida (Health)**

| Tecla | Acción | Efecto |
|-------|--------|--------|
| **H** | Daño | Resta 1 vida |
| **J** | Curar | Suma 1 vida |
| **U** | +Vida Máxima | Aumenta vida máxima en 1 |
| **I** | -Vida Máxima | Reduce vida máxima en 1 (min: 1) |

**Ejemplos:**
```
Inicial:        ❤️ 2/2
Presionar H:    ❤️ 1/2
Presionar J:    ❤️ 2/2
Presionar U:    ❤️ 2/3
Presionar J:    ❤️ 3/3
```

---

### **Monedas (Coins)**

| Tecla | Acción | Efecto |
|-------|--------|--------|
| **K** | Gastar | Resta 1 moneda |
| **L** | Ganar | Suma 1 moneda |

**Ejemplos:**
```
Inicial:        💰 3
Presionar L:    💰 4
Presionar L:    💰 5
Presionar K:    💰 4
```

---

### **Almas (Souls)**

| Tecla | Acción | Efecto |
|-------|--------|--------|
| **N** | Recolectar | Suma 1 alma |
| **M** | Perder | Resta 1 alma |

**Ejemplos:**
```
Inicial:        👻 0/4
Presionar N:    👻 1/4
Presionar N:    👻 2/4
Presionar N:    👻 3/4
Presionar N:    👻 4/4  ← ¡VICTORIA!
Presionar M:    👻 3/4
```

**⚠️ Condición de Victoria:**
Al alcanzar 4 almas (o el valor configurado en `soulsToWin`), se dispara el evento `OnPlayerWon`.

---

## 📊 Eventos Disparados

Cada acción dispara eventos que puedes monitorear en la consola:

```
[GameManager] Player 1 vida: 2 → 1        // Al presionar H
[GameManager] Player 1 ha muerto!         // Al llegar a 0 vida
[GameManager] Player 1 monedas: 3 → 4     // Al presionar L
[GameManager] Player 1 recolectó alma     // Al presionar N
[GameManager] ¡Player 1 ha ganado!        // Al llegar a 4 almas
```

---

## 🎬 Animaciones

Si `PlayerStatsUI` tiene `Animate On Change` activado:
- Los iconos harán un efecto "punch" (escala 1.2x) cuando cambien valores
- Duración: 0.3 segundos

---

## 🧪 Escenarios de Prueba

### **Prueba 1: Muerte del Jugador**
1. Presiona **H** tres veces
2. Verás: `❤️ 2/2` → `1/2` → `0/2`
3. Consola: `[GameManager] Player 1 ha muerto!`

### **Prueba 2: Victoria**
1. Presiona **N** cuatro veces
2. Verás: `👻 0/4` → `1/4` → `2/4` → `3/4` → `4/4`
3. Consola: `[GameManager] ¡Player 1 ha ganado con 4 almas!`

### **Prueba 3: Vida Dinámica**
1. Presiona **U** (vida máxima +1)
2. Verás: `❤️ 2/2` → `❤️ 2/3`
3. Presiona **J** (curar)
4. Verás: `❤️ 3/3`

### **Prueba 4: Sin Monedas**
1. Presiona **K** cuatro veces
2. Verás: `💰 3` → `2` → `1` → `0` → `0` (no baja de 0)

---

## 🗑️ Eliminar Controles de Prueba

**Cuando termines de probar**, puedes eliminar todo el bloque de código en `GameManager.cs`:

```csharp
// Eliminar desde esta línea:
// ========== CONTROLES DE PRUEBA (TEMPORAL) ==========

// Hasta esta línea:
// ====================================================
```

O simplemente comenta todo el bloque con `/* ... */`

---

## 🔧 Personalización

Puedes cambiar las teclas editando `GameManager.cs` línea ~95:

```csharp
if (Input.GetKeyDown(KeyCode.TU_TECLA))
```

Teclas disponibles: `Alpha1`, `Alpha2`, `Q`, `W`, `E`, `R`, `T`, `Y`, etc.

---

## 📝 Notas Importantes

- ✅ Las monedas no bajan de 0
- ✅ La vida no baja de 0 (pero dispara evento de muerte)
- ✅ La vida máxima no baja de 1
- ✅ Al curar, no puedes superar la vida máxima
- ✅ Las almas se cuentan desde `activeItems` donde `cardType == CardType.Soul`

---

¡Listo para probar! 🚀
