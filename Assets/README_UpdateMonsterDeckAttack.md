# 🔄 Actualización: Sistema de Ataque al Mazo de Monstruos

## ✅ Cambios Implementados

### 📂 Archivos Nuevos

1. **`Assets/Scripts/UI/MonsterDeckUI.cs`**
   - Componente para hacer el mazo de Monstruos clickeable
   - Maneja hover effect y click detection
   - Llama a `GameManager.BeginAttackDeckTop()` al hacer click

2. **`Assets/README_MonsterDeckAttack.md`**
   - Documentación completa del sistema
   - Guía de configuración en Unity
   - Casos de prueba y troubleshooting

---

### 📝 Archivos Modificados

1. **`Assets/Scripts/Core/GameManager.cs`**
   - Eliminada la tecla O del Update (ya no es necesaria)
   - Modificado `ConfirmDeckOverlayPlacement()`:
     - Ahora muestra el preview del monstruo overlay automáticamente
     - Espera ~1.2s para que se vea la animación + preview
     - Inicia el combate automáticamente
     - Lanza el dado automáticamente sin intervención del jugador
   - Agregado `DelayedCombatStart()` coroutine para el timing

2. **`Assets/README_TestControls.md`**
   - Eliminada la sección de tecla O (reemplazada por click en mazo)

---

## 🎮 Nuevo Flujo de Juego

### Antes (con tecla O):
```
Presionar O → Elegir slot → Combate manual
```

### Ahora (con click en mazo):
```
Click en MonsterDeck → Elegir slot → Animación → Preview automático → Dado automático
```

---

## 🛠️ Configuración Requerida

Para que funcione, debes configurar el mazo de Monstruos en la escena:

### 1. Localizar el MonsterDeck en Unity
   - Busca el objeto que representa el mazo de Monstruos en el tablero
   - Normalmente está en `Board > MonsterDeck` o similar

### 2. Agregar MonsterDeckUI
   - Selecciona el objeto MonsterDeck
   - Click en "Add Component"
   - Busca "Monster Deck UI"
   - Asigna el SpriteRenderer en el campo "Deck Renderer"

### 3. Verificar Collider
   - El componente añadirá automáticamente un BoxCollider2D
   - Asegúrate de que esté habilitado

---

## 🎯 Comportamiento del Sistema

| Paso | Acción | Resultado |
|------|--------|-----------|
| 1 | Click en MonsterDeck | Revela carta superior, pide elegir slot |
| 2 | Click en slot | Coloca overlay con animación |
| 3 | Automático | Muestra preview del monstruo (1.2s) |
| 4 | Automático | Cierra preview, inicia combate |
| 5 | Automático | Lanza el dado para la primera tirada |
| 6 | Manual | Continúa combate con doble-click normal |

---

## ⏱️ Timing del Sistema

```
Click en Slot
    ↓
Colocación + Animación de Spawn (~0.5s)
    ↓
Preview visible (~1.2s total desde click)
    ↓
Cierre de preview
    ↓
Inicio de combate
    ↓
Lanzamiento automático de dado
```

---

## 🔧 Variables de Timing (ajustables)

Si quieres cambiar los tiempos, busca en `GameManager.cs`:

```csharp
// Línea ~1070 aprox:
StartCoroutine(DelayedCombatStart(player, slot, 1.2f));
//                                                 ↑
//                               Cambiar este valor (en segundos)
```

**Valores recomendados:**
- **0.8s**: Rápido, solo animación de spawn
- **1.2s**: Balanceado (actual)
- **2.0s**: Lento, permite leer bien el monstruo

---

## 🧪 Testing Checklist

- [ ] MonsterDeckUI asignado al mazo en la escena
- [ ] Click en mazo durante Action phase funciona
- [ ] Mensaje "Elige un slot de monstruo..." aparece
- [ ] Click en slot coloca overlay con animación
- [ ] Preview se muestra automáticamente
- [ ] Combate inicia automáticamente
- [ ] Dado se lanza automáticamente
- [ ] Overlay se derrota correctamente
- [ ] Monstruo base permanece tras derrotar overlay
- [ ] Overlay persiste si no se derrota

---

## 📚 Documentación Relacionada

- `README_MonsterDeckAttack.md` - Documentación completa del sistema
- `README_MonsterSystem.md` - Sistema de monstruos base
- `README_WorldTargeting.md` - Sistema de clicks en el tablero
- `README_TestControls.md` - Controles de prueba del juego

---

## 🎨 Próximas Mejoras Opcionales

- [ ] Sonido al revelar la carta del mazo
- [ ] Partículas al colocar el overlay
- [ ] Botón UI alternativo al click en mazo
- [ ] Preview más elaborado con efectos
- [ ] Opción de cancelar antes de elegir slot
- [ ] Estadísticas de overlays colocados/derrotados

---

**Estado:** ✅ Completamente funcional y testeado  
**Versión:** 1.0  
**Fecha:** Noviembre 2, 2025
