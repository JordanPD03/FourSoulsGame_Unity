# Sistema de Preview de Cartas - Instrucciones de Configuración

## Archivos Creados/Modificados

### Nuevos archivos:
- `CardPreviewUI.cs` - Componente para mostrar la carta ampliada
- `CardPositionTracker.cs` - Helper para trackear posiciones de cartas

### Archivos modificados:
- `CardUI.cs` - Agregado sistema de selección
- `CardHover.cs` - Mantiene hover en cartas seleccionadas
- `PlayerHandUI.cs` - Integración con preview

## Configuración en Unity

### 1. Crear el GameObject de Preview

1. En tu Canvas, crea un GameObject vacío llamado "CardPreview"
2. Agrega el componente `CardPreviewUI` a este GameObject
3. Dentro de "CardPreview", crea:
   - Un Panel (RectTransform) llamado "PreviewContainer"
   - Dentro del container, agrega un objeto Image llamado "PreviewImage"

### 2. Configurar el CardPreviewUI (Inspector)

**Referencias:**
- `Preview Image`: Arrastra el objeto Image que creaste
- `Preview Container`: Arrastra el Panel container

**Configuración (valores sugeridos):**
- `Preview Position`: (-600, 0) - Posición en el lado izquierdo
- `Preview Size`: (400, 600) - Tamaño de la carta ampliada
- `Animation Duration`: 0.3
- `Animation Ease`: OutBack

### 3. Configurar el PreviewContainer

En el Inspector del PreviewContainer:
- **Anchors**: Centro (0.5, 0.5)
- **Pivot**: (0.5, 0.5)
- **Pos X**: -600 (lado izquierdo)
- **Pos Y**: 0 (centrado verticalmente)
- **Width**: 400
- **Height**: 600
- **Scale**: (0, 0, 0) - Se animará automáticamente

### 4. Configurar la PreviewImage

- **Anchors**: Stretch (fill)
- **Left, Right, Top, Bottom**: 0
- **Preserve Aspect**: Activado (recomendado)

## Funcionalidad

### Comportamiento:
1. **Click en carta**: Muestra la carta ampliada en el lado izquierdo
2. **Click en otra carta**: Reemplaza la carta mostrada
3. **Carta seleccionada**: Se mantiene elevada con el efecto de hover
4. **Tecla ESC**: Oculta el preview y baja todas las cartas
5. **Salir del área de la mano**: También oculta el preview

### Personalización:

En `CardPreviewUI.cs` puedes ajustar:
- Posición del preview (izquierda, derecha, centro)
- Tamaño de la carta ampliada
- Velocidad de animación
- Tipo de easing

## Notas Técnicas

- El componente usa **Singleton pattern** (CardPreviewUI.Instance)
- Las cartas automáticamente agregan `CardPositionTracker` si no existe
- El sistema se integra con los hovers existentes sin conflictos
- La tecla ESC es manejada por `CardPreviewUI.Update()`

## Troubleshooting

**La preview no aparece:**
- Verifica que CardPreviewUI.Instance esté configurado
- Asegúrate de que las referencias en el Inspector estén asignadas

**La carta no se mantiene elevada:**
- Verifica que CardHover esté en el mismo GameObject que CardUI

**Conflictos de input:**
- El ESC se procesa solo si hay un preview activo
